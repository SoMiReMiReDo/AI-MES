import { expect, type Page } from "@playwright/test";

export async function equipmentFlow(
    page: Page,
    suffix: string,
    password: string,
) {
    const code = `F7-${suffix}`;
    const executionCode = `EXEC-${suffix}`;
    const modal = () => page.locator(".ant-modal:visible");
    const drawer = () => page.locator(".ant-drawer-open");
    const choose = async (label: string, text: string) => {
        const input = page
            .getByLabel(label, { exact: true })
            .and(page.getByRole("combobox"));
        await input.click();
        await page
            .locator(".ant-select-dropdown:visible")
            .getByText(text, { exact: true })
            .click();
        if (label !== "必需耗材（可选）")
            await expect(
                page.locator(".ant-select-dropdown:visible"),
            ).toHaveCount(0);
    };
    const save = async () => {
        await modal()
            .getByRole("button", { name: /保\s*存/, exact: true })
            .click();
        await expect(modal()).toHaveCount(0);
    };
    const get = (path: string) =>
        page.request.get(`/api/v1/${path}`).then((r) => r.json());
    let csrf = (await get("auth/csrf")).requestToken;
    const post = async (path: string, data: unknown, etag?: string) => {
        const response = await page.request.post(`/api/v1/${path}`, {
            headers: {
                "X-CSRF-TOKEN": csrf,
                "Idempotency-Key": crypto.randomUUID(),
                ...(etag ? { "If-Match": etag } : {}),
            },
            data,
        });
        expect(response.ok(), await response.text()).toBeTruthy();
        return response.json();
    };
    const localDate = (hours: number) => {
        const date = new Date(Date.now() + hours * 3600000);
        return new Date(date.getTime() - date.getTimezoneOffset() * 60000)
            .toISOString()
            .slice(0, 23);
    };
    const date = (label: string, hours: number) =>
        page
            .getByLabel(`${label}（本地时间）`, { exact: true })
            .fill(localDate(hours));
    const reason = async (text: string) => {
        await page.getByLabel("操作说明", { exact: true }).fill(text);
        await save();
    };
    const open = async (title: string) =>
        page.getByRole("button", { name: `新增${title}`, exact: true }).click();
    const currentUserId = (await get("auth/me")).userId;
    const worker = await post("organization/workers", {
        code,
        name: "F7认证人员",
        fabId: (await get("auth/me")).fab.id,
    });
    const currentUser = await get(`security/users/${currentUserId}`);
    const bound = await page.request.patch(
        `/api/v1/security/users/${currentUserId}`,
        {
            headers: { "X-CSRF-TOKEN": csrf, "If-Match": currentUser.etag },
            data: { workerId: worker.id },
        },
    );
    expect(bound.ok(), await bound.text()).toBeTruthy();
    await page.goto("/");
    await page.getByLabel("用户名", { exact: true }).fill("e2e_admin");
    await page.getByLabel("密码", { exact: true }).fill(password);
    await page.getByRole("button", { name: /登\s*录/, exact: true }).click();
    await expect(
        page.getByRole("heading", { name: "你好，系统管理员" }),
    ).toBeVisible();
    csrf = (await get("auth/csrf")).requestToken;
    await page.getByRole("link", { name: "设备与资源", exact: true }).click();
    const options = await get("equipment/options");
    const equipment = options.equipment.find(
        (x: any) => x.code === executionCode,
    );
    const equipmentLabel = `${equipment.code} · ${equipment.name}`;
    const operation = options.operations.find(
        (x: any) => x.code === executionCode,
    );
    const operationLabel = `${operation.code} · ${operation.name}`;
    await choose("筛选设备", equipmentLabel);
    await page
        .getByRole("row")
        .filter({ hasText: executionCode })
        .getByRole("button", { name: /状\s*态/, exact: true })
        .click();
    await choose("设备运行条件", "故障停机");
    await reason("F7 浏览器故障");
    await page.getByRole("tab", { name: "停机与统计", exact: true }).click();
    await expect(
        page.getByRole("cell", { name: "是", exact: true }),
    ).toBeVisible();
    await choose("资源类别", "停机原因");
    await open("停机原因");
    await page.getByLabel("编码", { exact: true }).fill(code);
    await page.getByLabel("名称", { exact: true }).fill("F7故障原因");
    await save();
    await choose("资源类别", "停机");
    await page.getByRole("button", { name: /归\s*因/, exact: true }).click();
    await choose("停机原因", `${code} · F7故障原因`);
    await page.getByLabel("归因说明", { exact: true }).fill("浏览器补分类");
    await save();
    await choose("资源类别", "时长统计");
    await expect(page.getByText("故障停机", { exact: true })).toBeVisible();
    await page.screenshot({
        path: "test-results/equipment-downtime.png",
        fullPage: true,
        animations: "disabled",
        style: ".ant-message { visibility: hidden !important; }",
    });
    await page.getByRole("tab", { name: "台账", exact: true }).click();
    await page
        .getByRole("row")
        .filter({ hasText: executionCode })
        .getByRole("button", { name: /状\s*态/, exact: true })
        .click();
    await choose("设备运行条件", "空闲");
    await reason("故障排除");
    await page.getByRole("tab", { name: "状态与事件", exact: true }).click();
    await open("事件");
    await choose("设备", equipmentLabel);
    await page
        .getByLabel("模拟来源标识", { exact: true })
        .fill("browser-simulator");
    await page.getByLabel("来源事件编号", { exact: true }).fill(code);
    await date("发生时间", 0);
    await choose("事件类别", "通信状态");
    await choose("目标状态", "在线");
    await reason("联机模拟");
    await expect(
        page
            .getByRole("row")
            .filter({ hasText: "在线" })
            .getByText("已应用", { exact: true }),
    ).toBeVisible();

    await page.getByRole("tab", { name: "工具与维护", exact: true }).click();
    await choose("资源类别", "维护计划");
    await open("维护计划");
    await page.getByLabel("计划名称", { exact: true }).fill(code);
    await page.getByLabel("维护类型", { exact: true }).fill("PM");
    await choose("设备（与工具二选一）", equipmentLabel);
    await choose("周期单位", "小时");
    await page.getByLabel("周期数", { exact: true }).fill("24");
    await date("下次到期", -1);
    await save();
    await choose("资源类别", "维护工单");
    await open("维护工单");
    await page.getByLabel("标题", { exact: true }).fill(code);
    await page.getByLabel("维护类型", { exact: true }).fill("PM");
    await choose("关联计划（可选，目标需一致）", code);
    await date("计划时间", 0);
    await save();
    const work = () => page.getByRole("row").filter({ hasText: code });
    for (const [button, note] of [
        ["开始", "维护开始"],
        ["完成", "维护完成"],
        ["复机", "独立确认复机"],
    ]) {
        await work()
            .getByRole("button", {
                name: new RegExp(button.split("").join("\\s*")),
                exact: true,
            })
            .click();
        await reason(note);
    }
    await expect(work().getByText("已复机", { exact: true })).toBeVisible();

    await page.getByRole("tab", { name: "耗材", exact: true }).click();
    await choose("资源类别", "耗材定义");
    await open("耗材定义");
    await page.getByLabel("编码", { exact: true }).fill(code);
    await page.getByLabel("名称", { exact: true }).fill("F7化学品");
    await page.getByLabel("单位", { exact: true }).fill("L");
    await save();
    await choose("资源类别", "耗材批次");
    await open("耗材批次");
    await choose("耗材", `${code} · F7化学品`);
    await page.getByLabel("批次编号", { exact: true }).fill(code);
    await date("批次有效截止（可选）", 48);
    await save();
    await choose("资源类别", "耗材上机");
    await open("耗材上机");
    await choose("设备", equipmentLabel);
    await choose("耗材批次", code);
    await date("开封时间", -1);
    await date("上机有效截止", 24);
    const mountPath = `/api/v1/equipment/material-mounts/${equipment.id}`;
    const mountKeys: string[] = [];
    page.on("request", (request) => {
        if (
            request.method() === "POST" &&
            new URL(request.url()).pathname === mountPath
        )
            mountKeys.push(request.headers()["idempotency-key"]);
    });
    let interrupted!: () => void;
    const lostResponse = new Promise<void>((resolve) => {
        interrupted = resolve;
    });
    await page.route(
        `**${mountPath}`,
        async (route) => {
            await route.fetch();
            await route.abort("failed");
            interrupted();
        },
        { times: 1 },
    );
    await modal()
        .getByRole("button", { name: /保\s*存/, exact: true })
        .click();
    await lostResponse;
    await expect(
        modal().getByRole("button", { name: /保\s*存/, exact: true }),
    ).toBeEnabled();
    await save();
    expect(mountKeys).toHaveLength(2);
    expect(mountKeys[1]).toBe(mountKeys[0]);
    expect(
        (await get(`equipment/material-mounts?equipmentId=${equipment.id}`))
            .total,
    ).toBe(1);

    await page.getByRole("tab", { name: "光罩", exact: true }).click();
    await open("光罩");
    await page.getByLabel("编码", { exact: true }).fill(code);
    await page.getByLabel("名称", { exact: true }).fill("F7光罩");
    await page.getByLabel("版本", { exact: true }).fill("A");
    await page.getByLabel("层次", { exact: true }).fill("L1");
    await page.getByLabel("寿命（启动次数）", { exact: true }).fill("5");
    await save();
    await choose("资源类别", "光罩装卸");
    await open("光罩装卸");
    await choose("设备", equipmentLabel);
    await choose("光罩", `${code} · F7光罩 A`);
    await save();

    await page.getByRole("tab", { name: "资格与门禁", exact: true }).click();
    await open("设备资格");
    await choose("设备", equipmentLabel);
    await choose("工序", operationLabel);
    await date("生效时间", -1);
    await date("到期时间（可选）", 24);
    await save();
    await choose("资源类别", "人员资格");
    await open("人员资格");
    await choose("人员", `${code} · F7认证人员`);
    await choose("工序", operationLabel);
    await date("生效时间", -1);
    await date("到期时间（可选）", 24);
    await save();
    await choose("资源类别", "工序门禁");
    await open("工序门禁");
    await choose("工序", operationLabel);
    await page.getByLabel("启用设备与人员资格", { exact: true }).check();
    await page.getByLabel("必需光罩层次（可选）", { exact: true }).fill("L1");
    await choose("必需耗材（可选）", `${code} · F7化学品`);
    await page.keyboard.press("Escape");
    await save();

    const sourceOrder = (
        await get(`planning/work-orders?q=${executionCode}`)
    ).items.find((x: any) => x.orderNo === executionCode);
    let order = await post("planning/work-orders", {
        orderNo: code,
        productId: sourceOrder.productId,
        productRevisionId: sourceOrder.productRevisionId,
        plannedQty: 1,
    });
    order = await post(
        `planning/work-orders/${order.id}/release`,
        { reason: "F7验收下达" },
        order.etag,
    );
    let lot = await post("production/lots", {
        workOrderId: order.id,
        lotNumber: code,
        wafers: [{ waferCode: code + "-W1", waferSizeMm: 300 }],
    });
    lot = await post(`production/lots/${lot.id}/release`, {}, lot.etag);
    await page.getByRole("link", { name: "生产执行" }).click();
    await page.getByLabel("批次号", { exact: true }).fill(code);
    await page.getByRole("button", { name: /筛\s*选/, exact: true }).click();
    await page.getByRole("button", { name: code, exact: true }).click();
    await choose("设备与配方", `${executionCode} / ${executionCode} 1`);
    await drawer().getByRole("button", { name: "开始加工" }).click();
    await expect(drawer().getByText("加工中", { exact: true })).toBeVisible();
    await drawer()
        .getByRole("button", { name: "受控中止", exact: true })
        .click();
    await page.getByLabel("中止原因", { exact: true }).fill("F7受控收尾");
    await modal()
        .getByRole("button", { name: "提交记录", exact: true })
        .click();
    await expect(modal()).toHaveCount(0);
    await page.keyboard.press("Escape");
    await page.getByRole("link", { name: "设备与资源" }).click();
    await page.getByRole("tab", { name: "光罩", exact: true }).click();
    const reticle = page.getByRole("row").filter({ hasText: code });
    await reticle
        .getByRole("button", { name: "影响查询", exact: true })
        .click();
    await expect(
        drawer().getByText(code + "-W1", { exact: true }),
    ).toBeVisible();
    await expect(
        drawer().getByText(code + "-W1", { exact: true }),
    ).toBeInViewport({ ratio: 1 });
    await expect(drawer().getByText("已中止", { exact: true })).toBeVisible();
    await page.screenshot({
        path: "test-results/equipment-resource-impact.png",
        fullPage: true,
        animations: "disabled",
        style: ".ant-message { visibility: hidden !important; }",
    });
    await page.keyboard.press("Escape");

    await page.getByRole("tab", { name: "状态与事件", exact: true }).click();
    await choose("资源类别", "状态区间");
    await choose("筛选设备", "LEGACY-EQUIPMENT · 历史设备");
    await page.getByRole("button", { name: "补登记", exact: true }).click();
    await choose("停机原因（可后补）", `${code} · F7故障原因`);
    await page
        .getByLabel("登记说明", { exact: true })
        .fill("历史区间补登记，保留原起止时间");
    await save();
    await expect(
        page.getByRole("button", { name: "补登记", exact: true }),
    ).toHaveCount(0);
    const legacy = (await get("equipment/options")).equipment.find(
        (x: any) => x.code === "LEGACY-EQUIPMENT",
    );
    expect(
        (await get(`equipment/downtimes?equipmentId=${legacy.id}`)).total,
    ).toBe(1);
}
