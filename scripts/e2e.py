#!/usr/bin/env python3
"""Run browser tests in a disposable PostgreSQL database, never against user data."""
import os, sys, subprocess, secrets, time, urllib.request, signal

from database import ROOT, settings

name = 'mesagent_test_browser_' + secrets.token_hex(6)
processes = []


def sql(command):
    subprocess.run(
        [
            'docker', 'compose', 'exec', '-T', 'postgres', 'psql', '-v',
            'ON_ERROR_STOP=1', '-U', 'mesagent_owner', '-d', 'postgres'
        ],
        input=command,
        text=True,
        cwd=ROOT,
        check=True,
        stdout=subprocess.DEVNULL
    )


def wait(url):
    for _ in range(100):
        try:
            with urllib.request.urlopen(url, timeout=1) as r:
                if r.status == 200:
                    return
        except Exception:
            time.sleep(.3)

    raise RuntimeError('Service did not start: ' + url)


try:
    sql('CREATE DATABASE ' + name + ';')

    env = settings();
    env['ConnectionStrings__Mes'] = env['ConnectionStrings__Mes'].replace(
        'Database=mesagent;', 'Database=' + name + ';'
    )
    env.update({
        'Mes__DeploymentFabId': 'aad7c77a-9d50-4e79-b1df-391681b7e226',
        'ASPNETCORE_ENVIRONMENT': 'Development',
        'ASPNETCORE_URLS': 'http://127.0.0.1:5081',
        'MES_ADMIN_USER': 'e2e_admin',
        'MES_ADMIN_PASSWORD': secrets.token_hex(20),
        'Security__LoginPermitLimit': '1000'
    })

    subprocess.run(
        [
            'dotnet', 'run', '--no-build', '--project', 'src/MESagent.Database',
            '--', 'migrate'
        ],
        env=env,
        cwd=ROOT,
        check=True
    )
    subprocess.run(
        [
            'dotnet', 'run', '--no-build', '--project', 'src/MESagent.Api',
            '--', 'bootstrap'
        ],
        env=env,
        cwd=ROOT,
        check=True,
        stdout=subprocess.DEVNULL
    )

    logdir = ROOT / '.local';
    logdir.mkdir(exist_ok=True)

    with (logdir / 'e2e-api.log').open('w') as logfile:
        processes.append(
            subprocess.Popen(
                ['dotnet', 'run', '--no-build', '--project', 'src/MESagent.Api'],
                cwd=ROOT,
                env=env,
                stdout=logfile,
                stderr=logfile,
                start_new_session=True
            )
        )

    wait('http://127.0.0.1:5081/health/ready')

    webenv = env.copy();
    webenv['MES_API_TARGET'] = 'http://127.0.0.1:5081';
    webenv['MES_E2E_PASSWORD'] = env['MES_ADMIN_PASSWORD'];
    webenv['MES_E2E_URL'] = 'http://127.0.0.1:5174'

    with (logdir / 'e2e-web.log').open('w') as logfile:
        processes.append(
            subprocess.Popen(
                ['npm', 'run', 'dev', '--', '--port', '5174', '--strictPort'],
                cwd=ROOT / 'web',
                env=webenv,
                stdout=logfile,
                stderr=logfile,
                start_new_session=True
            )
        )

    wait('http://127.0.0.1:5174')

    subprocess.run(['npm', 'test'], cwd=ROOT / 'web', env=webenv, check=True)
finally:
    for p in processes:
        os.killpg(p.pid, signal.SIGTERM)

        try:
            p.wait(timeout=10)
        except subprocess.TimeoutExpired:
            os.killpg(p.pid, signal.SIGKILL)

    sql('DROP DATABASE IF EXISTS ' + name + ' WITH (FORCE);')
