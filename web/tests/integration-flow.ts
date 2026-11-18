import { expect, type Page } from "@playwright/test";

export async function integrationFlow(page: Page, suffix: string) {
    const originalViewport = page.viewportSize()!;
    const equipmentCode = `EXEC-${suffix}`;
    const name = `模拟连接 ${suffix}`;
    const link = page.getByRole("link", {
        name: "数据导入与集成",
        exact: true,
    });
    await link.click();
    await page.getByRole("button", { name: "登记模拟连接" }).click();
    const equipment = page.getByRole("combobox", { name: "设备" });
    await equipment.fill(equipmentCode);
    await page
        .locator(".ant-select-dropdown:visible")
        .getByText(new RegExp(equipmentCode))
        .click();
    await page.getByLabel("连接名称").fill(name);
    await page.getByRole("button", { name: /^登\s*记$/ }).click();
    await expect(page.getByText(`报文履历 · ${name}`)).toBeVisible();
    await page.getByRole("button", { name: "投递模拟报文" }).click();
    await page.getByLabel("来源编号").fill(`F11-${suffix}`);
    await page.getByLabel("原始报文", { exact: true }).fill(
        JSON.stringify(
            {
                occurredAt: new Date().toISOString(),
                kind: "State",
                state: "Down",
                reason: "浏览器验收停机",
            },
            null,
            2,
        ),
    );
    await page.getByRole("button", { name: /^投\s*递$/ }).click();
    const drawer = page.locator(".ant-drawer-open");
    await expect(drawer.getByText("已应用", { exact: true })).toBeVisible();
    await expect(drawer.getByText("解析结果", { exact: true })).toBeVisible();
    await expect(drawer.getByText(/"state": "Down"/)).toHaveCount(2);
    await page.screenshot({
        path: "test-results/integration-message.png",
        animations: "disabled",
    });
    await drawer
        .getByRole("link", { name: "查看设备事件", exact: true })
        .click();
    await expect(
        page.getByRole("dialog", { name: "设备事件", exact: true }),
    ).toBeVisible();
    await expect(
        drawer.getByText(`F11-${suffix}`, { exact: true }),
    ).toBeVisible();
    await drawer.getByRole("button", { name: /^(Close|关闭)$/ }).click();
    await link.click();
    await page.getByRole("tab", { name: "工单导入" }).click();

    const orderNo = `F11-CSV-${suffix}`;
    await page.getByLabel("工单 CSV", { exact: true }).setInputFiles({
        name: "e2e-work-orders.csv",
        mimeType: "text/csv",
        buffer: Buffer.from(
            `单号,物料,版本,计划\r\n${orderNo},FAB${suffix},A,6\r\n`,
        ),
    });
    await page.getByText("字段映射", { exact: true }).click();
    await page.getByPlaceholder("orderNo", { exact: true }).fill("单号");
    await page.getByPlaceholder("productCode", { exact: true }).fill("物料");
    await page.getByPlaceholder("revisionCode", { exact: true }).fill("版本");
    await page.getByPlaceholder("plannedQty", { exact: true }).fill("计划");
    await page.getByRole("button", { name: "运行预检" }).click();
    await expect(drawer.getByText("预检通过", { exact: true })).toBeVisible();
    await expect(
        drawer.getByRole("cell", { name: orderNo, exact: true }),
    ).toBeVisible();
    await expect(
        drawer.getByRole("link", { name: orderNo, exact: true }),
    ).toHaveCount(0);
    await drawer.getByRole("button", { name: "确认导入" }).click();
    await page.getByRole("button", { name: "确认创建" }).click();
    await expect(drawer.getByText("已导入", { exact: true })).toBeVisible();
    await page.screenshot({
        path: "test-results/integration-import.png",
        animations: "disabled",
    });

    await drawer.getByRole("link", { name: orderNo, exact: true }).click();
    await expect(
        page.getByRole("cell", { name: orderNo, exact: true }),
    ).toBeVisible();
    await expect(
        page.getByRole("cell", { name: "Draft", exact: true }),
    ).toBeVisible();

    await link.click();
    await page.getByRole("tab", { name: "工单导入" }).click();
    await page.setViewportSize({ width: 390, height: 844 });
    await expect(
        page.getByRole("heading", { name: "数据导入与集成" }),
    ).toBeVisible();
    expect(
        await page.locator("body").evaluate((body) => body.scrollWidth),
    ).toBeLessThanOrEqual(390);
    await page.screenshot({
        path: "test-results/integration-mobile.png",
        animations: "disabled",
    });
    await page.getByRole("button", { name: "切换到黑夜模式" }).click();
    await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
    await page.getByRole("button", { name: "查看任务", exact: true }).click();
    await expect(drawer.getByText("已导入", { exact: true })).toBeVisible();
    await page.screenshot({
        path: "test-results/integration-mobile-dark.png",
        animations: "disabled",
    });
    await drawer.getByRole("button", { name: /^(Close|关闭)$/ }).click();
    await page.getByRole("button", { name: "切换到浅色模式" }).click();
    await page.setViewportSize(originalViewport);
}
