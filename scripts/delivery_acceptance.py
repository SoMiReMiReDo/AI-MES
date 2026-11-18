"""Populate and verify a disposable delivery using the same browser flow as local development."""
from hashlib import sha256
from http.cookiejar import CookieJar
import json
import os
from pathlib import Path
import secrets
import socket
import subprocess
import tempfile
import time
from urllib.error import HTTPError, URLError
from urllib.request import HTTPCookieProcessor, Request, build_opener, urlopen

from delivery import Delivery, INSTANCES, ROOT, write_private


ADMIN = "e2e_admin"


def create_demo(deployment, port, test_database_port=None):
    runner = ROOT / "web/node_modules/@playwright/test/cli.js"
    if not runner.is_file():
        raise ValueError("先运行 npm --prefix web ci 安装浏览器验收依赖。")
    deployment.init(port, "loopback", test_database_port=test_database_port)
    deployment.build()
    deployment.start()
    password = secrets.token_hex(20)
    deployment.bootstrap(ADMIN, password)
    for fixture in ("legacy-equipment.sql", "reliability.sql"):
        sql = (ROOT / "web/tests/fixtures" / fixture).read_text()
        deployment.sql(sql.replace("{{FAB_ID}}", deployment.values["MES_FAB_ID"]))

    subprocess.run(
        ["node", str(runner), "test", "--config", str(ROOT / "web/playwright.config.ts"),
         "--output", str(deployment.path / "test-results")],
        cwd=deployment.path, check=True,
        env=dict(os.environ, MES_E2E_PASSWORD=password, MES_E2E_URL=deployment.url)
    )
    credentials = deployment.path / "demo-login.txt"
    write_private(credentials, f"username={ADMIN}\npassword={password}-changed\n")
    print(f"演示入口：{deployment.url}；登录凭据：{credentials}", flush=True)
    return password + "-changed"


def api(session, deployment, path, body=None, headers=None):
    data = None if body is None else json.dumps(body).encode()
    request_headers = {"Content-Type": "application/json"} if data is not None else {}
    request_headers.update(headers or {})
    request = Request(
        f"{deployment.url}/api/v1/{path}", data=data,
        headers=request_headers
    )
    try:
        with session.open(request, timeout=10) as response:
            return response.read()
    except HTTPError as error:
        raise ValueError(f"{path} 返回 HTTP {error.code}") from None


def snapshot(session, deployment, ids):
    lot, order, pdf, checksum = ids
    account = json.loads(api(session, deployment, "auth/me"))
    progress = json.loads(api(session, deployment, f"planning/work-orders/{order}/progress"))
    events = json.loads(api(session, deployment, f"traceability/lot/{lot}/events?pageSize=100"))
    attachment = api(session, deployment, f"engineering/documents/{pdf}/content")
    if progress["completedQty"] != 5 or events["total"] == 0:
        raise ValueError("演示预期五片完工及可查询的追溯事件。")
    if sha256(attachment).hexdigest().upper() != checksum:
        raise ValueError("工艺 PDF 与数据库摘要不一致。")
    return {
        "account": account["userId"],
        "fab": account["fab"]["id"],
        "grants": account["permissions"],
        "quantities": {k: v for k, v in progress.items() if k.endswith("Qty")},
        "traceability": events,
        "pdf": checksum,
        "facts": deployment.sql("""
            SELECT (SELECT count(*) FROM production.process_runs),
                   (SELECT count(*) FROM production.wafer_completions),
                   (SELECT count(*) FROM production.genealogy_events),
                   (SELECT count(*) FROM audit.audit_logs),
                   (SELECT count(*) FROM public.__ef_migrations_history);
        """, capture=True)
    }


def check_runtime_role(deployment):
    result = deployment.run(
        "exec", "-T", "api", "id", "-u", capture_output=True, text=True
    )
    if result.stdout.strip() in ("", "0"):
        raise ValueError("API 应以非 root 用户运行。")
    role = deployment.sql("""
        SELECT NOT (rolsuper OR rolcreaterole OR rolcreatedb OR rolreplication OR rolbypassrls)
            AND NOT has_schema_privilege('mesagent_app', 'production', 'CREATE')
            AND NOT has_table_privilege('mesagent_app', 'production.process_runs', 'DELETE')
            AND NOT has_table_privilege('mesagent_app', 'public.__ef_migrations_history', 'INSERT')
        FROM pg_roles WHERE rolname = 'mesagent_app';
    """, capture=True)
    if role != "t":
        raise ValueError("运行账号的 DDL 或生产历史写权限异常。")


def run_regression(deployment, pg_port):
    password = deployment.values["MES_DB_PASSWORD"]
    connection = (
        f"Host=127.0.0.1;Port={pg_port};Database=mesagent;Username=mesagent_owner;"
        f"Password={password}"
    )
    try:
        subprocess.run(
            ["dotnet", "test", "MESagent.sln", "-m:1", "--disable-build-servers",
             "-p:RestoreLockedMode=true"],
            cwd=ROOT, env=dict(os.environ, MES_TEST_ADMIN_CONNECTION=connection), check=True
        )
    finally:
        deployment.test_override.unlink()
        deployment.run("up", "-d", "--wait", "--wait-timeout", "120", "web")


def expect_unready(deployment):
    for _ in range(12):
        try:
            with urlopen(deployment.url + "/health/ready", timeout=5):
                pass
        except HTTPError as error:
            if error.code == 503:
                with urlopen(deployment.url + "/health/live", timeout=5) as live:
                    if live.status == 200:
                        return
            raise
        except URLError:
            raise ValueError("停止数据库时 Web 和 API 应继续提供存活探测。") from None
        time.sleep(1)
    raise ValueError("停止数据库后未返回 503 就绪状态。")


def free_port():
    with socket.socket() as sock:
        sock.bind(("127.0.0.1", 0))
        return sock.getsockname()[1]


def verify_recovery(source, target, password):
    ids = source.sql("""
        SELECT l.id, l.work_order_id, d.id, d.checksum
        FROM production.lots l CROSS JOIN engineering.engineering_documents d
        WHERE l.lot_number LIKE 'EXEC-%' AND l.completed_at IS NOT NULL
            AND d.original_filename = 'instruction.pdf' AND d.lifecycle_status = 'Released';
    """, capture=True).split("|")
    session = build_opener(HTTPCookieProcessor(CookieJar()))
    token = json.loads(api(session, source, "auth/csrf"))["requestToken"]
    api(session, source, "auth/login", {"username": ADMIN, "password": password},
        {"X-CSRF-TOKEN": token})
    before = snapshot(session, source, ids)

    source.run("stop", "postgres")
    try:
        expect_unready(source)
    finally:
        source.run("up", "-d", "--wait", "postgres")
    source.run(
        "up", "-d", "--wait", "--wait-timeout", "120", "--no-deps", "--force-recreate", "api"
    )
    source.run("up", "-d", "--wait", "--wait-timeout", "120", "--no-deps", "web")
    if snapshot(session, source, ids) != before:
        raise ValueError("API 容器重建后，业务事实或会话发生变化。")
    print("断库、就绪恢复及 API 重建验证通过。", flush=True)

    with tempfile.TemporaryDirectory(prefix=source.name + "-", dir=INSTANCES) as tmp:
        backup = Path(tmp) / "backup"
        source.backup(backup)
        target.restore(backup, free_port(), "loopback")
        check_runtime_role(target)
        if snapshot(session, target, ids) != before:
            raise ValueError("恢复后会话、授权、工单、追溯或 PDF 与原实例不一致。")
        print("独立实例恢复验证通过，原登录 Cookie 仍可读取原有工单、追溯和 PDF。", flush=True)


def accept(port, keep, prebuilt):
    tag = "qa-" + secrets.token_hex(4)
    source = Delivery(tag, prebuilt)
    target = Delivery(tag + "-restore", prebuilt)
    pg_port = free_port()
    password = create_demo(source, port, test_database_port=pg_port)
    check_runtime_role(source)
    run_regression(source, pg_port)
    verify_recovery(source, target, password)

    target.run("down", "--volumes", "--rmi", "local")
    if not keep:
        source.run("down", "--volumes", "--rmi", "local")
    else:
        print(f"已保留通过验收的实例：{source.name}，{source.url}", flush=True)
