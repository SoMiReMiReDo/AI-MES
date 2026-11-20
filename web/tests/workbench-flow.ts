import { expect, type Page, type WebSocketRoute } from "@playwright/test";

export async function workbenchFlow(
    page: Page,
    suffix: string,
    sample: { productId: string; revisionId: string; equipmentId: string },
) {
    const csrf = await page.request
        .get("/api/v1/auth/csrf")
        .then((r) => r.json());
    const write = async (
        path: string,
        data: object,
        etag?: string,
        method = "POST",
    ) => {
        const response = await page.request.fetch(`/api/v1/${path}`, {
            method,
            headers: {
                "X-CSRF-TOKEN": csrf.requestToken,
                "Idempotency-Key": crypto.randomUUID(),
                ...(etag ? { "If-Match": etag } : {}),
            },
            data,
        });
        expect(response.ok(), await response.text()).toBeTruthy();
        return response.status() === 204 ? undefined : response.json();
    };

    const today = new Intl.DateTimeFormat("sv-SE", {
        timeZone: "Asia/Shanghai",
    }).format(new Date());
    const tomorrow = new Date(Date.parse(`${today}T00:00:00Z`) + 86400000)
        .toISOString()
        .slice(0, 10);
    const code = `WB-${suffix}`;
    const draft = await write("planning/work-orders", {
        orderNo: code,
        productId: sample.productId,
        productRevisionId: sample.revisionId,
        plannedQty: 2,
    });
    const order = await write(
        `planning/work-orders/${draft.id}/release`,
        { reason: "排程验收" },
        draft.etag,
    );
    const lot = await write("production/lots", {
        workOrderId: order.id,
        lotNumber: code,
        priority: 100,
        wafers: ["1", "2"].map((n) => ({
            waferCode: `${code}-${n}`,
            waferSizeMm: 300,
        })),
    });
    await write(`production/lots/${lot.id}/release`, {}, lot.etag);
    await write("equipment/shifts", {
        code,
        name: "工作台日历验收",
        startTime: "00:00:00",
        endTime: "23:59:59",
    });
    await write("workbench/rates", {
        equipmentId: sample.equipmentId,
        effectiveOn: tomorrow,
        idealSeconds: 1200,
    });
    await write(
        "workbench/subscriptions/downtime",
        { enabled: true },
        undefined,
        "PUT",
    );

    let socket: WebSocketRoute | undefined;
    let connections = 0;
    await page.routeWebSocket(/\/api\/v1\/workbench\/live\?/, (route) => {
        connections++;
        socket = route;
        route.connectToServer();
    });
    const home = page.getByRole("link", { name: "工作台", exact: true });
    await home.click();
    await expect(page.getByText("实时更新", { exact: true })).toBeVisible({
        timeout: 10000,
    });
    const menu = page.getByRole("textbox", { name: "搜索菜单" });
    await menu.fill("质量");
    await expect(
        page.getByRole("link", { name: "快捷入口 质量管理" }),
    ).toBeVisible();
    await expect(
        page.getByRole("link", { name: "快捷入口 设备与资源" }),
    ).toHaveCount(0);
    await menu.clear();

    await expect(
        page.getByRole("progressbar", { name: "占在制批次" }),
    ).toBeVisible();
    await expect(
        page.getByRole("progressbar", { name: "占启用设备" }),
    ).toBeVisible();
    await page.getByRole("button", { name: /在制批次/ }).click();
    const wip = page.getByRole("dialog", { name: "在制批次" });
    await expect(
        wip.getByRole("link", { name: code, exact: true }),
    ).toBeVisible();
    await page.keyboard.press("Escape");
    const runningValue = page
        .getByRole("article")
        .filter({
            has: page.getByRole("link", { name: "运行中 · 加工记录" }),
        })
        .locator(".workbench-kpi-value strong");
    await runningValue.scrollIntoViewIfNeeded();
    const bounds = (await runningValue.boundingBox())!;
    await page.mouse.click(
        bounds.x + bounds.width / 2,
        bounds.y + bounds.height / 2,
    );
    await expect(page.getByRole("tab", { name: "加工与历史" })).toHaveAttribute(
        "aria-selected",
        "true",
    );
    await home.click();
    await page.getByRole("tab", { name: "历史趋势" }).click();
    await expect(page.getByText("按日趋势", { exact: true })).toBeVisible();
    await page.getByRole("tab", { name: "班次与 OEE" }).click();
    await expect(
        page.getByText("设备班次与损失", { exact: true }),
    ).toBeVisible();
    await page.getByRole("tab", { name: "排程日历" }).click();
    const week = page.getByRole("table", { name: "设备周历" });
    await expect(week).toBeVisible();
    await expect(week.getByRole("columnheader")).toHaveCount(8);
    await page.getByLabel("日历日期", { exact: true }).fill("2030-01-01");
    await expect(
        week.getByRole("button", { name: "查看 2029-12-31 日历" }),
    ).toBeVisible();
    await expect(
        week.getByRole("button", { name: "查看 2030-01-06 日历" }),
    ).toBeVisible();
    await page.getByRole("button", { name: /^本\s*周$/ }).click();
    await page.getByLabel("日历日期", { exact: true }).fill(tomorrow);
    await page.getByRole("button", { name: "自动排程" }).click();
    const auto = page.getByRole("dialog", { name: "自动排程", exact: true });
    await expect(auto.getByLabel("起始日期（厂区）")).toHaveValue(tomorrow);
    await expect(auto.getByLabel("结束日期（厂区）")).toHaveValue(tomorrow);
    await auto.getByRole("button", { name: /确\s*定/ }).click();
    const outcome = page.getByRole("dialog", { name: "自动排程结果" });
    await expect(outcome.getByText(/已生成 [1-9]/)).toBeVisible();
    await page.keyboard.press("Escape");
    const plan = page.getByRole("button", {
        name: `计划 ${code}`,
        exact: true,
    });
    await week
        .getByRole("button", { name: `计划 ${code}`, exact: true })
        .click();
    await expect(
        page.getByRole("dialog", { name: `变更记录 · ${code}` }),
    ).toBeVisible();
    await page.keyboard.press("Escape");
    await week.getByRole("button", { name: `查看 ${tomorrow} 日历` }).click();
    await expect(plan).toBeVisible();
    const lane = page.getByTestId(`schedule-lane-${sample.equipmentId}`);
    const box = await lane.boundingBox();
    const move = page.waitForResponse(
        (response) =>
            response.request().method() === "PUT" &&
            /\/workbench\/schedule\/[^/?]+$/.test(response.url()),
    );
    await plan.dragTo(lane, { targetPosition: { x: box!.width * 0.4, y: 20 } });
    expect((await move).ok()).toBeTruthy();
    await plan.click();
    await expect(
        page
            .getByRole("dialog", { name: /变更记录/ })
            .getByText("调整", { exact: true }),
    ).toBeVisible();
    await page.keyboard.press("Escape");
    await page.screenshot({
        path: "test-results/workbench-schedule.png",
        fullPage: true,
        animations: "disabled",
    });

    await page.getByRole("tab", { name: /待办与告警/ }).click();
    await page.getByText("站内通知", { exact: true }).click();
    const equipment = await page.request
        .get(`/api/v1/equipment/tools/${sample.equipmentId}`)
        .then((r) => r.json());
    const active = connections;
    await socket!.close({ code: 1012, reason: "test reconnect" });
    const changed = await write(
        `equipment/tools/${sample.equipmentId}/state`,
        {
            state: "Down",
            reason: "站内告警浏览器验收",
        },
        equipment.equipment.etag,
    );
    await expect.poll(() => connections).toBeGreaterThan(active);
    await expect(page.getByText("实时更新", { exact: true })).toBeVisible();
    const notice = page.getByRole("row").filter({ hasText: `EXEC-${suffix}` });
    await expect(notice.getByText("设备停机", { exact: true })).toBeVisible();
    await notice.getByRole("button", { name: /^确\s*认$/ }).click();
    await expect(notice.getByText("已确认", { exact: true })).toBeVisible();
    await notice
        .getByRole("link", { name: `EXEC-${suffix}`, exact: true })
        .click();
    await expect(
        page.getByRole("dialog", { name: "停机", exact: true }),
    ).toBeVisible();
    await page.keyboard.press("Escape");
    await monitoringFlow(page, suffix, sample.equipmentId);
    await write(
        `equipment/tools/${sample.equipmentId}/state`,
        { state: "Idle", reason: "验收复机" },
        changed.etag,
    );
    await page.keyboard.press("Escape");
    await home.click();
    const desktop = page.viewportSize()!;
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(page.getByRole("button", { name: /在制批次/ })).toBeVisible();
    expect(
        await page.evaluate(() => document.documentElement.scrollWidth),
    ).toBeLessThanOrEqual(390);
    await page.screenshot({
        path: "test-results/workbench-mobile.png",
        fullPage: true,
        animations: "disabled",
    });
    await page.getByRole("button", { name: "切换到黑夜模式" }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    await page.screenshot({
        path: "test-results/workbench-mobile-dark.png",
        fullPage: true,
        animations: "disabled",
    });
    await page.getByRole("button", { name: "切换到浅色模式" }).click();
    await page.setViewportSize(desktop);
}

async function monitoringFlow(page: Page, suffix: string, equipmentId: string) {
    await page.getByRole("link", { name: "厂区看板", exact: true }).click();
    await expect(page.getByRole("heading", { name: "厂区看板" })).toBeVisible();
    await expect(page.getByText("实时更新", { exact: true })).toBeVisible();
    const keyword = page.getByRole("textbox", { name: "设备编号或名称" });
    await keyword.fill(`EXEC-${suffix}`);
    await keyword.press("Enter");
    await page.getByRole("button", { name: /^停机/ }).click();
    const card = page.getByRole("link", {
        name: new RegExp(`EXEC-${suffix}.*查看班次监控`),
    });
    await expect(card).toBeVisible();
    await expect(card.getByText("停机原因待补录")).toBeVisible();
    await page.getByRole("button", { name: "大屏模式" }).click();
    await expect(page.locator(".monitoring-kiosk")).toBeVisible();
    await expect(page.locator(".left-sidebar")).toBeHidden();
    await page.screenshot({
        path: "test-results/monitoring-kiosk.png",
        animations: "disabled",
    });
    await page.getByRole("button", { name: "退出大屏" }).click();
    await card.focus();
    await page.keyboard.press("Enter");
    await expect(page).toHaveURL(new RegExp(`/monitoring/${equipmentId}$`));
    await expect(
        page.getByRole("heading", { name: "班次时间轴" }),
    ).toBeVisible();
    await expect(
        page.getByRole("heading", { name: "晶圆过站速率" }),
    ).toBeVisible();
    await page.getByRole("button", { name: "上一班" }).click();
    await expect(page.getByText(/历史班次/)).toBeVisible();
    await page.getByRole("button", { name: "回到当前班" }).click();
    await expect(page.getByText(/当前时段/)).toBeVisible();
    await page.getByText("班次分析", { exact: true }).click();
    await expect(
        page.getByRole("heading", { name: "停机原因分布" }),
    ).toBeVisible();
    await page.screenshot({
        path: "test-results/monitoring-shift.png",
        fullPage: true,
        animations: "disabled",
    });
    const desktop = page.viewportSize()!;
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(
        page.getByRole("heading", { name: "停机原因分布" }),
    ).toBeVisible();
    expect(
        await page.evaluate(() => document.documentElement.scrollWidth),
    ).toBeLessThanOrEqual(390);
    await page.getByRole("button", { name: "切换到黑夜模式" }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    await page.screenshot({
        path: "test-results/monitoring-mobile-dark.png",
        fullPage: true,
        animations: "disabled",
    });
    await page.getByRole("button", { name: "切换到浅色模式" }).click();
    await page.setViewportSize(desktop);
    const record = await page
        .getByRole("link", { name: "查看当前停机" })
        .getAttribute("href");
    expect(record).toMatch(/\/equipment\?view=downtimes&record=/);
    await page
        .getByRole("region", { name: "班次停机" })
        .locator(`a[href="${record}"]`)
        .click();
    await expect(
        page.getByRole("dialog", { name: "停机", exact: true }),
    ).toBeVisible();

    const fallback = await page.context().newPage();
    try {
        await fallback.route("**/api/v1/workbench/live/negotiate?**", (route) =>
            route.abort(),
        );
        await fallback.clock.install();
        await fallback.goto("/monitoring");
        await expect(
            fallback.getByText("轮询同步", { exact: true }),
        ).toBeVisible();
        await expect(
            fallback.getByRole("link", {
                name: new RegExp(`EXEC-${suffix}.*查看班次监控`),
            }),
        ).toBeVisible();
        const refresh = fallback.waitForResponse((response) =>
            response.url().includes("/api/v1/workbench/plant-board?"),
        );
        await fallback.clock.fastForward(31_000);
        expect((await refresh).ok()).toBeTruthy();
    } finally {
        await fallback.close();
    }
}
