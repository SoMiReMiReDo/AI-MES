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

    await page.getByRole("button", { name: /在制批次/ }).click();
    const wip = page.getByRole("dialog", { name: "在制批次" });
    await expect(
        wip.getByRole("link", { name: code, exact: true }),
    ).toBeVisible();
    await page.keyboard.press("Escape");
    await page.getByRole("tab", { name: "历史趋势" }).click();
    await expect(page.getByText("按日趋势", { exact: true })).toBeVisible();
    await page.getByRole("tab", { name: "班次与 OEE" }).click();
    await expect(
        page.getByText("设备班次与损失", { exact: true }),
    ).toBeVisible();
    await page.getByRole("tab", { name: "排程日历" }).click();
    await page.getByLabel("日历日期", { exact: true }).fill(tomorrow);
    await page.getByRole("button", { name: "自动排程" }).click();
    const auto = page.getByRole("dialog", { name: "自动排程", exact: true });
    await auto.getByRole("button", { name: /确\s*定/ }).click();
    const outcome = page.getByRole("dialog", { name: "自动排程结果" });
    await expect(outcome.getByText(/已生成 [1-9]/)).toBeVisible();
    await page.keyboard.press("Escape");
    const plan = page.getByRole("button", {
        name: `计划 ${code}`,
        exact: true,
    });
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
