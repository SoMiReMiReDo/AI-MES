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
    env['MES_DOCUMENT_ROOT'] = str(ROOT / '.local/engineering-documents')

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
        # Development requests use the provisioned runtime role; migrations/bootstrap keep the owner connection.
        app_password = values.get('MES_APP_DB_PASSWORD', '')
        if len(app_password) != 64 or not all(c in '0123456789abcdef' for c in app_password):
            raise SystemExit('先运行 configure 和 provision 配置运行账号')
        port = int(values.get('MES_DB_PORT', '55432'))
        env['ConnectionStrings__Mes'] = (
            f'Host=127.0.0.1;Port={port};Database=mesagent;'
            f'Username=mesagent_app;Password={app_password}'
        )
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
    elif cmd in (
        'provision-workbench', 'provision-integration', 'provision-reliability', 'provision-settings'
    ):
        module = cmd.removeprefix('provision-')
        sql = (ROOT / f'database/{module}-runtime-grants.sql').read_text()
        subprocess.run(
            ['docker', 'compose', 'exec', '-T', 'postgres', 'psql', '-v',
             'ON_ERROR_STOP=1', '-U', 'mesagent_owner', '-d', 'mesagent'],
            input=sql, text=True, cwd=ROOT, check=True, stdout=subprocess.DEVNULL
        )
        print(f'已增量授予 {module} 运行权限。')
    elif cmd == 'provision-materials':
        sql = """DO $$ BEGIN
IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='mesagent_app' AND NOT rolsuper) THEN
    RAISE EXCEPTION 'Existing non-superuser mesagent_app role is required';
END IF;
END $$;
GRANT INSERT ON materials.material_types,materials.materials,materials.material_sources,
    materials.warehouses,materials.material_lots TO mesagent_app;
"""
        subprocess.run(
            ['docker', 'compose', 'exec', '-T', 'postgres', 'psql', '-v',
             'ON_ERROR_STOP=1', '-U', 'mesagent_owner', '-d', 'mesagent'],
            input=sql, text=True, cwd=ROOT, check=True, stdout=subprocess.DEVNULL
        )
        print('已增量授予硅片来料登记权限。')
    elif cmd == 'provision-equipment':
        sql = """DO $$ BEGIN
IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='mesagent_app' AND NOT rolsuper) THEN
    RAISE EXCEPTION 'Existing non-superuser mesagent_app role is required';
END IF;
END $$;
""" + (ROOT / 'database/equipment-runtime-grants.sql').read_text()
        subprocess.run(
            ['docker', 'compose', 'exec', '-T', 'postgres', 'psql', '-v',
             'ON_ERROR_STOP=1', '-U', 'mesagent_owner', '-d', 'mesagent'],
            input=sql, text=True, cwd=ROOT, check=True, stdout=subprocess.DEVNULL
        )
        print('已增量授予 F7 所需权限；保留密码，无新增 DDL 或生产历史 DELETE 权限。')
    elif cmd == 'provision-quality':
        # Add only F6 writes; preserve credentials, existing grants and deployment data.
        sql = """DO $$ BEGIN
IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='mesagent_app' AND NOT rolsuper) THEN
    RAISE EXCEPTION 'Existing non-superuser mesagent_app role is required';
END IF;
END $$;
GRANT INSERT,UPDATE ON quality.reason_codes,quality.issue_types,quality.issues,
    quality.issue_actions,quality.hold_records,quality.specification_versions,
    quality.quality_check_templates TO mesagent_app;
GRANT INSERT ON quality.quality_checks,quality.measurements,quality.lot_releases,
    quality.scrap_entries,quality.wafer_dispositions TO mesagent_app;
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
        print('已增量授予 F6 写权限；保留账号密码，无新增 DDL/DELETE 权限。')
    elif cmd == 'provision-execution':
        # Incremental F5 upgrade: keep the existing login, password and all unrelated grants unchanged.
        sql = """DO $$ BEGIN
IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='mesagent_app' AND NOT rolsuper) THEN
    RAISE EXCEPTION 'Existing non-superuser mesagent_app role is required';
END IF;
END $$;
GRANT INSERT,UPDATE ON production.process_runs,production.process_run_wafers,
    equipment.equipment,equipment.equipment_states TO mesagent_app;
GRANT UPDATE ON production.step_documents TO mesagent_app;
GRANT INSERT ON production.track_events,production.checklist_completions,production.output_values,
    production.process_confirmations,equipment.equipment_types,equipment.equipment_groups,
    equipment.equipment_events TO mesagent_app;
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
        print('已增量授予既有运行角色 F5 写权限；未修改账号密码，无新增 DDL/DELETE 权限。')
    elif cmd == 'provision':
        # PostgreSQL psql receives SQL through stdin; the generated password is hex-only.
        pwd = values['MES_APP_DB_PASSWORD']

        if not all(c in '0123456789abcdef' for c in pwd) or len(pwd) != 64:
            raise SystemExit('运行账号密码须为 configure 生成的 64 位 hex')

        sql = f"""DO $$ BEGIN IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname='mesagent_app') THEN CREATE ROLE mesagent_app LOGIN; END IF; END $$;
ALTER ROLE mesagent_app PASSWORD '{pwd}';
""" + (ROOT / "database/runtime-grants.sql").read_text()
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

        subprocess.run(
            ['docker', 'compose', 'exec', '-T', 'postgres', 'psql', '-v',
             'ON_ERROR_STOP=1', '-U', 'mesagent_owner', '-d', 'mesagent'],
            input=(ROOT / 'database/equipment-runtime-grants.sql').read_text()
            + (ROOT / 'database/workbench-runtime-grants.sql').read_text()
            + (ROOT / 'database/integration-runtime-grants.sql').read_text()
            + (ROOT / 'database/reliability-runtime-grants.sql').read_text()
            + (ROOT / 'database/settings-runtime-grants.sql').read_text(),
            text=True, cwd=ROOT, check=True, stdout=subprocess.DEVNULL
        )
        print('已配置应用运行角色；无 DDL 权限，已授予当前模块所需权限。')
    else:
        raise SystemExit(
            '用法: python3 scripts/app.py configure|serve|bootstrap|bootstrap-generated|recover-admin|'
            'test|provision|provision-execution|provision-quality|provision-equipment|provision-materials|'
            'provision-workbench|provision-integration|provision-reliability|provision-settings'
        )


if __name__ == '__main__':
    main()
