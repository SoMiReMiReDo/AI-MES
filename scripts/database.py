#!/usr/bin/env python3
"""Local PostgreSQL lifecycle. Credentials are never printed or passed in command arguments."""
import os
from pathlib import Path
import secrets
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
ENV_FILE = ROOT / '.env'


def run(*args, env=None):
    subprocess.run(args, cwd=ROOT, env=env, check=True)


def settings():
    if not ENV_FILE.exists():
        raise SystemExit('先运行 python3 scripts/database.py init')
    values = {}
    for line in ENV_FILE.read_text().splitlines():
        if line.strip() and not line.lstrip().startswith('#'):
            key, value = line.split('=', 1)
            values[key.strip()] = value.strip()
    password = values.get('MES_DB_PASSWORD', '')
    if not password or password == 'replace-with-a-random-local-password':
        raise SystemExit('请在 .env 设置非空随机密码')
    # Npgsql connection string values require doubled quotes, not shell escaping.
    escaped = password.replace('"', '""')
    port = int(values.get('MES_DB_PORT', '55432'))
    connection = f'Host=127.0.0.1;Port={port};Database=mesagent;Username=mesagent_owner;Password="{escaped}"'
    env = os.environ.copy()
    env['ConnectionStrings__Mes'] = connection
    env['MES_TEST_ADMIN_CONNECTION'] = connection
    return env


def main():
    command = sys.argv[1] if len(sys.argv) == 2 else ''
    if command == 'init':
        if ENV_FILE.exists():
            print('.env 已存在，保留现有配置。')
            return
        fd = os.open(ENV_FILE, os.O_WRONLY | os.O_CREAT | os.O_EXCL, 0o600)
        with os.fdopen(fd, 'w') as file:
            file.write(f'MES_DB_PASSWORD={secrets.token_hex(32)}\nMES_DB_PORT=55432\n')
        print('已生成本地 .env（随机密码，权限 600，不纳入 Git）。')
    elif command == 'up':
        settings()
        run('docker', 'compose', 'up', '-d', '--wait', 'postgres')
    elif command in ('migrate', 'status'):
        run('dotnet', 'run', '--project', 'src/MESagent.Database', '--', command, env=settings())
    elif command == 'test':
        run('dotnet', 'test', 'tests/MESagent.Database.Tests', '--no-restore', env=settings())
    elif command == 'catalog':
        run('dotnet', 'build', 'src/MESagent.Database', '--no-restore')
        result = subprocess.run(['dotnet', 'run', '--no-build', '--project', 'src/MESagent.Database', '--', 'catalog'], cwd=ROOT, check=True, capture_output=True, text=True)
        path = ROOT / 'docs/development-process/05-database-catalog.md'
        path.write_text(result.stdout)
        print('已从 EF Core 模型更新数据库字段字典。')
    elif command == 'stop':
        run('docker', 'compose', 'stop', 'postgres')
    else:
        raise SystemExit('用法: python3 scripts/database.py init|up|migrate|status|test|catalog|stop')


if __name__ == '__main__':
    main()
