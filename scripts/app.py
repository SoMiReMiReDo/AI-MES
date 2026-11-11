#!/usr/bin/env python3
"""OrganizationIdentityAccessControl local development; credentials stay in process environment, never command arguments."""
import getpass
import os
from pathlib import Path
import secrets
import subprocess
import sys
import uuid

from database import ROOT, ENV_FILE, settings


def config():
    values = dict(
        line.split('=', 1) for line in ENV_FILE.read_text().splitlines()
        if line.strip() and not line.startswith('#')
    )

    if 'MES_FAB_ID' not in values:
        raise SystemExit('先运行 python3 scripts/app.py configure')

    env = settings()
    env['Mes__DeploymentFabId'] = values['MES_FAB_ID']
    env['ASPNETCORE_ENVIRONMENT'] = 'Development'
    env['ASPNETCORE_URLS'] = 'http://127.0.0.1:5080'
    env['DataProtection__Path'] = str(ROOT / '.local/keys')

    return env, values


def main():
    cmd = sys.argv[1] if len(sys.argv) > 1 else ''

    if cmd == 'configure':
        settings()
        values = ENV_FILE.read_text()

        with ENV_FILE.open('a') as out:
            if 'MES_FAB_ID=' not in values:
                out.write(f'MES_FAB_ID={uuid.uuid4()}\n')
            if 'MES_APP_DB_PASSWORD=' not in values:
                out.write(f'MES_APP_DB_PASSWORD={secrets.token_hex(32)}\n')

        (ROOT / '.local/keys').mkdir(parents=True, exist_ok=True)
        print(
            '已配置单厂 ID 与运行账号密码（仅保存在忽略的 .env），保留既有配置。'
        )

        return

    env, values = config()

    if cmd == 'serve':
        subprocess.run(
            ['dotnet', 'run', '--no-launch-profile', '--project', 'src/MESagent.Api'],
            cwd=ROOT,
            env=env,
            check=True
        )
    elif cmd in ('bootstrap', 'bootstrap-generated', 'recover-admin'):
        env['MES_ADMIN_USER'] = 'admin' if cmd == 'bootstrap-generated' else input(
            '初始管理员用户名: '
        ).strip()
        env['MES_ADMIN_PASSWORD'] = secrets.token_urlsafe(24) if cmd == 'bootstrap-generated' else getpass.getpass(
            '临时密码（至少 12 字符）: '
        )
        credential_path = ROOT / '.local/initial-admin.txt'

        if cmd == 'bootstrap-generated' and credential_path.exists():
            raise SystemExit('初始化凭据文件已存在，拒绝覆盖')
        if cmd == 'recover-admin':
            env['MES_RECOVERY_REASON'] = input('恢复原因: ').strip()

        subprocess.run(
            [
                'dotnet', 'run', '--no-launch-profile', '--project',
                'src/MESagent.Api', '--',
                'recover-admin' if cmd == 'recover-admin' else 'bootstrap'
            ],
            cwd=ROOT,
            env=env,
            check=True
        )

        if cmd == 'bootstrap-generated':
            fd = os.open(credential_path, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)

            with os.fdopen(fd, 'w') as out:
                out.write(
                    '用户名: ' + env['MES_ADMIN_USER']
                    + '\n临时密码: ' + env['MES_ADMIN_PASSWORD']
                    + '\n首次登录须改密；改密后删除此文件。\n'
                )

            print(
                '初始登录凭据已写入 .local/initial-admin.txt（权限 600，Git 忽略）。'
            )
    elif cmd == 'test':
        subprocess.run(
            ['dotnet', 'test', 'MESagent.sln', '--no-restore'],
            cwd=ROOT,
            env=env,
            check=True
        )
    elif cmd == 'provision':
        # PostgreSQL psql receives SQL through stdin; the generated password is hex-only.
        pwd = values['MES_APP_DB_PASSWORD']

        if not all(c in '0123456789abcdef' for c in pwd) or len(pwd) != 64:
            raise SystemExit('运行账号密码须为 configure 生成的 64 位 hex')

        sql = f"""DO $$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='mesagent_app') THEN CREATE ROLE mesagent_app LOGIN; END IF; END $$;
ALTER ROLE mesagent_app PASSWORD '{pwd}';
GRANT CONNECT ON DATABASE mesagent TO mesagent_app;
GRANT USAGE ON SCHEMA organization,security,audit TO mesagent_app;
GRANT SELECT ON ALL TABLES IN SCHEMA organization,security,audit TO mesagent_app;
GRANT INSERT,UPDATE,DELETE ON ALL TABLES IN SCHEMA organization,security TO mesagent_app;
REVOKE UPDATE,DELETE ON security.security_events FROM mesagent_app;
GRANT INSERT ON audit.audit_logs TO mesagent_app;
GRANT USAGE ON SCHEMA engineering,production,equipment,quality,materials,integration TO mesagent_app;
GRANT SELECT ON ALL TABLES IN SCHEMA engineering,production,equipment,quality,materials,integration TO mesagent_app;
"""
        subprocess.run(
            [
                'docker', 'compose', 'exec', '-T', 'postgres', 'psql', '-v',
                'ON_ERROR_STOP=1', '-U', 'mesagent_owner', '-d', 'mesagent'
            ],
            input=sql,
            text=True,
            cwd=ROOT,
            check=True,
            stdout=subprocess.DEVNULL
        )

        print('已配置 OrganizationIdentityAccessControl 运行角色；无 DDL 权限、无生产表写权限。')
    else:
        raise SystemExit('用法: python3 scripts/app.py configure|serve|bootstrap|bootstrap-generated|recover-admin|test|provision')


if __name__ == '__main__':
    main()
