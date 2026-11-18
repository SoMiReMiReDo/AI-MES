#!/usr/bin/env python3
"""Deploy isolated MES Compose instances and operate their persistent state."""
import argparse
from contextlib import contextmanager
import fcntl
import getpass
import hashlib
import json
import os
from pathlib import Path
import re
import secrets
import subprocess
import tarfile
import uuid

from database import ROOT


INSTANCES = ROOT / ".local/delivery"
ARCHIVES = ("database.dump", "keys.tar", "documents.tar")
VOLUMES = {"keys.tar": "keys", "documents.tar": "documents"}


def write_private(path, content):
    fd = os.open(path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
    with os.fdopen(fd, "w", encoding="utf-8") as file:
        file.write(content)


def digest(path):
    with path.open("rb") as file:
        checksum = hashlib.sha256()
        for block in iter(lambda: file.read(1024 * 1024), b""):
            checksum.update(block)
        return checksum.hexdigest()


class Delivery:
    def __init__(self, name, prebuilt=False):
        if not re.fullmatch(r"[a-z0-9][a-z0-9-]{0,31}", name):
            raise ValueError("实例名须为 1～32 位小写字母、数字或连字符，并以字母或数字开头。")
        self.name = name
        self.prebuilt = prebuilt
        self.path = INSTANCES / name
        self.project = "mesagent-delivery-" + name
        self.env_file = self.path / "compose.env"
        self.test_override = self.path / "test-database.yaml"
        self.restore_pending = self.path / "restore-incomplete"
        self.values = {}
        if self.env_file.exists():
            lines = self.env_file.read_text().splitlines()
            self.values = dict(line.split("=", 1) for line in lines)

    @property
    def url(self):
        return f"http://127.0.0.1:{self.values['MES_WEB_PORT']}"

    def run(self, *args, prebuilt=False, **kwargs):
        if not self.values:
            raise ValueError("先运行 init，或使用 restore、demo 创建新实例。")
        command = [
            "docker", "compose", "--project-name", self.project,
            "--env-file", str(self.env_file), "-f", "compose.app.yaml"
        ]
        if prebuilt:
            command += ["-f", "compose.prebuilt.yaml"]
        if self.test_override.exists():
            command += ["-f", str(self.test_override)]
        env = os.environ.copy()
        for key in (*self.values, "MES_LOGIN_LIMIT", "MES_FAB_CODE", "MES_FAB_TIMEZONE"):
            env.pop(key, None)
        env.pop("COMPOSE_PROFILES", None)
        return subprocess.run([*command, *args], cwd=ROOT, check=True, env=env, **kwargs)

    def init(self, port, mode, fab_id=None, test_database_port=None):
        if self.path.exists():
            raise ValueError(f"实例目录已存在：{self.path}")
        if not 1 <= port <= 65535:
            raise ValueError("Web 端口必须在 1～65535。")

        # A missing config directory must never cause adoption of an old project's volumes.
        existing = subprocess.check_output([
            "docker", "volume", "ls", "-q", "--filter",
            f"label=com.docker.compose.project={self.project}"
        ], text=True)
        if existing.strip():
            raise ValueError(f"Docker 项目 {self.project} 已有卷，请使用新的实例名。")

        https = mode == "https-proxy"
        self.values = {
            "MES_DB_PASSWORD": secrets.token_hex(32),
            "MES_APP_DB_PASSWORD": secrets.token_hex(32),
            "MES_FAB_ID": str(fab_id or uuid.uuid4()),
            "MES_WEB_PORT": str(port),
            "MES_ENVIRONMENT": "Production" if https else "Development",
            "MES_PROXY_SCHEME": "https" if https else "http",
            "MES_FORWARD_HTTPS": "true" if https else "false"
        }
        self.path.mkdir(mode=0o700, parents=True)
        settings = "".join(f"{key}={value}\n" for key, value in self.values.items())
        write_private(self.env_file, settings)
        if test_database_port is not None:
            write_private(
                self.test_override,
                f'''services:
  postgres:
    ports: ["127.0.0.1:{test_database_port}:5432"]
    networks: [database, test-access]
networks:
  test-access:
'''
            )

    def build(self):
        # Pass explicit targets so the optional tools do not join the build.
        self.run("build", "api", "web", "migrate", prebuilt=self.prebuilt)

    def start(self):
        self.require_complete_restore()
        writers = self.running_writers()
        if writers:
            self.run("stop", *writers)
        self.run("up", "-d", "--wait", "--wait-timeout", "180", "web")

    def sql(self, statement, capture=False):
        result = self.run(
            "exec", "-T", "postgres", "psql", "-XqAt", "-v", "ON_ERROR_STOP=1",
            "-U", "mesagent_owner", "-d", "mesagent",
            input=statement, text=True, stdout=subprocess.PIPE if capture else subprocess.DEVNULL
        )
        return result.stdout.strip() if capture else None

    def bootstrap(self, username, password):
        if not 12 <= len(password) <= 128 or any(char in password for char in "\n\r\0"):
            raise ValueError("管理员临时密码须为单行 12～128 字符。")
        self.run(
            "run", "--rm", "--no-deps", "-T", "-e", f"MES_ADMIN_USER={username}", "bootstrap",
            input=password + "\n", text=True
        )

    def running_writers(self):
        running = self.run(
            "ps", "--services", "--status", "running", capture_output=True, text=True
        )
        return [name for name in ("web", "api") if name in running.stdout.splitlines()]

    @contextmanager
    def paused_writers(self):
        writers = self.running_writers()
        try:
            if writers:
                self.run("stop", *writers)
            yield
        finally:
            if writers:
                self.run("up", "-d", "--no-deps", "--wait", "--wait-timeout", "120", *writers)

    def backup(self, target):
        if not self.values:
            raise ValueError("实例尚未初始化。")
        self.require_complete_restore()
        target.mkdir(mode=0o700)
        with self.paused_writers():
            with (target / ARCHIVES[0]).open("xb") as file:
                self.run(
                    "exec", "-T", "postgres", "pg_dump", "-Fc", "--no-owner", "--no-acl",
                    "-U", "mesagent_owner", "-d", "mesagent", stdout=file
                )
            for archive, volume in VOLUMES.items():
                with (target / archive).open("xb") as file:
                    self.run(
                        "run", "--rm", "--no-deps", "-T", "state",
                        f"tar -cf - -C /state/{volume} .", stdout=file
                    )
        write_private(target / "manifest.json", json.dumps({
            "version": 1,
            "fabId": self.values["MES_FAB_ID"],
            "sha256": {name: digest(target / name) for name in ARCHIVES}
        }, indent=2) + "\n")

    def restore(self, source, port, mode):
        manifest = json.loads((source / "manifest.json").read_text())
        if manifest.get("version") != 1 or set(manifest.get("sha256", {})) != set(ARCHIVES):
            raise ValueError("备份清单格式无效。")
        fab = uuid.UUID(manifest["fabId"])
        for name in ARCHIVES:
            if digest(source / name) != manifest["sha256"][name]:
                raise ValueError(f"备份摘要不一致：{name}")
        for archive in VOLUMES:
            with tarfile.open(source / archive) as tar:
                for member in tar:
                    if member.isdir() and member.name == ".":
                        continue
                    if not member.isfile() or not re.fullmatch(
                        r"(?:\./)?[A-Za-z0-9][A-Za-z0-9.-]*\.(?:pdf|xml)", member.name
                    ):
                        raise ValueError(f"归档含无效文件路径或类型：{archive}")

        self.init(port, mode, fab)
        write_private(self.restore_pending, "restore in progress\n")
        self.run("up", "-d", "--wait", "postgres")
        with (source / ARCHIVES[0]).open("rb") as file:
            self.run(
                "exec", "-T", "postgres", "pg_restore", "--single-transaction", "--exit-on-error",
                "--no-owner", "--no-acl", "-U", "mesagent_owner", "-d", "mesagent", stdin=file
            )
        for archive, volume in VOLUMES.items():
            with (source / archive).open("rb") as file:
                self.run(
                    "run", "--rm", "--no-deps", "-T", "state",
                    f"tar -xf - -C /state/{volume}", stdin=file
                )
        self.restore_pending.unlink()
        self.build()
        self.start()

    def require_complete_restore(self):
        if self.restore_pending.exists():
            raise ValueError("该实例的恢复尚未完成。请保留原备份并换用新的实例名重试。")


@contextmanager
def instance_lock(name):
    directory = INSTANCES / ".locks"
    directory.mkdir(mode=0o700, parents=True, exist_ok=True)
    with (directory / name).open("a") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError(f"实例 {name} 正在执行另一项部署操作。") from None
        yield


def execute(args, deployment):
    action = args.action
    mode = "https-proxy" if getattr(args, "https_proxy", False) else "loopback"

    if action == "init":
        deployment.init(args.port, mode)
    elif action == "build":
        deployment.build()
    elif action == "up":
        deployment.build()
        deployment.start()
        print(f"Web：{deployment.url}")
    elif action == "bootstrap":
        deployment.bootstrap(args.username, getpass.getpass("初始管理员临时密码："))
    elif action == "backup":
        deployment.backup(args.target.resolve())
        print(f"已生成备份：{args.target.resolve()}")
    elif action == "restore":
        deployment.restore(args.source.resolve(), args.port, mode)
        print(f"已完成恢复：{deployment.url}")
    elif action == "demo":
        from delivery_acceptance import create_demo
        create_demo(deployment, args.port)
    elif action == "acceptance":
        from delivery_acceptance import accept
        accept(args.port, args.keep, deployment.prebuilt)
    elif action == "status":
        deployment.run("ps", "-a")
    elif action == "stop":
        deployment.run("stop")


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--instance", default="local")
    commands = parser.add_subparsers(dest="action", required=True)
    for command in ("init", "demo", "restore", "acceptance"):
        sub = commands.add_parser(command)
        sub.add_argument("--port", type=int, default=5088)
        if command in ("init", "restore"):
            sub.add_argument("--https-proxy", action="store_true")
        if command in ("demo", "restore", "acceptance"):
            sub.add_argument("--prebuilt", action="store_true")
        if command == "restore":
            sub.add_argument("source", type=Path)
        if command == "acceptance":
            sub.add_argument("--keep", action="store_true", help="保留通过验收的演示实例")
    for command in ("up", "build"):
        commands.add_parser(command).add_argument("--prebuilt", action="store_true")
    commands.add_parser("bootstrap").add_argument("--username", default="admin")
    commands.add_parser("backup").add_argument("target", type=Path)
    for command in ("status", "stop"):
        commands.add_parser(command)
    args = parser.parse_args()
    try:
        prebuilt = getattr(args, "prebuilt", False)
        deployment = Delivery(args.instance, prebuilt)
        with instance_lock(deployment.name):
            execute(args, deployment)
    except (OSError, ValueError, KeyError, subprocess.CalledProcessError) as ex:
        parser.exit(1, f"{ex}\n")


if __name__ == "__main__":
    main()
