import { expect, type Page } from "@playwright/test";

export async function traceabilityFlow(page: Page, suffix: string) {
    const landing = page.getByRole("link", { name: "生产追溯", exact: true });
    await landing.click();
    const viewed = page.getByRole("region", { name: "最近查看", exact: true });
    const searched = page.getByRole("region", {
        name: "最近查询",
        exact: true,
    });
    await expect(viewed).toBeVisible();
    await expect(searched).toBeVisible();
    const choose = async (label: string, value: string) => {
        const input = page.getByRole("combobox", { name: label, exact: true });
        const dropdowns = page.locator(".ant-select-dropdown:visible");
        await input.click();
        await expect(dropdowns).toHaveCount(1);
        await dropdowns.getByText(value, { exact: true }).click();
        await expect(input).toHaveAttribute("aria-expanded", "false");
        await expect(dropdowns).toHaveCount(0);
    };
    const find = async (kind: string, code: string) => {
        await choose("追溯对象类型", kind);
        const number = page.getByLabel("追溯编号", { exact: true });
        await number.fill(code);
        await expect(number).toHaveValue(code);
        await page.getByRole("button", { name: /^查\s*询$/ }).click();
        await page.getByRole("link", { name: code, exact: true }).click();
        await expect(
            page.getByRole("heading", { name: "事件履历" }),
        ).toBeVisible();
    };
    await find("工单", `WO${suffix}`);
    await choose("履历类别", "工单");
    await expect(
        page.getByRole("link", { name: "工单下达", exact: true }),
    ).toBeVisible();

    await find("Wafer（晶圆）", `W-${suffix}-1`);
    await expect(
        page.getByRole("link", { name: `来料批 IN-${suffix}`, exact: true }),
    ).toBeVisible();
    await choose("履历类别", "拆批与合批");
    const edge = page.getByRole("row").filter({
        has: page.getByRole("link", { name: `来源批 LOT-${suffix}` }),
    });
    await edge.getByRole("link", { name: "拆合批血缘", exact: true }).click();
    const evidence = page.getByRole("dialog", { name: "原始记录" });
    await expect(
        evidence.getByText("拆合批事件 ID", { exact: true }),
    ).toBeVisible();
    await page.reload();
    await expect(
        evidence.getByText("拆合批事件 ID", { exact: true }),
    ).toBeVisible();
    await evidence
        .getByRole("link", { name: `目标批 SPLIT-${suffix}`, exact: true })
        .click();
    await expect(
        page.getByText(`Lot（批次） SPLIT-${suffix}`, { exact: true }),
    ).toBeVisible();

    await find("载具", `C-${suffix}`);
    await expect(
        page.getByRole("link", { name: "载具装载", exact: true }),
    ).toBeVisible();
    await find("设备", `EXEC-${suffix}`);
    await choose("履历类别", "加工");
    await page
        .getByRole("link", { name: "Track-in", exact: true })
        .first()
        .click();
    await expect(
        evidence.getByText("实际配方版本 ID", { exact: true }),
    ).toBeVisible();
    await expect(evidence.getByText("实际设备", { exact: true })).toBeVisible();
    await page.screenshot({
        path: "test-results/traceability.png",
        animations: "disabled",
        fullPage: true,
    });
    await page.keyboard.press("Escape");
    await expect(evidence).toBeHidden();
    await landing.click();
    await page.reload();

    await expect(
        viewed.getByRole("link", { name: new RegExp(`^EXEC-${suffix}`) }),
    ).toBeVisible();
    await searched
        .getByRole("link", { name: new RegExp(`^WO${suffix}`) })
        .click();
    await expect(
        page.getByRole("link", { name: `WO${suffix}`, exact: true }),
    ).toBeVisible();
    await landing.click();
    const number = page.getByLabel("追溯编号", { exact: true });
    await number.fill(`WO${suffix}`);
    await number.press("Enter");
    await expect(
        page.getByRole("link", { name: `WO${suffix}`, exact: true }),
    ).toBeVisible();
    await landing.click();
    await viewed
        .getByRole("link", { name: new RegExp(`^EXEC-${suffix}`) })
        .click();
    await expect(page.getByRole("heading", { name: "事件履历" })).toBeVisible();
    await landing.click();
    await page.screenshot({ path: "test-results/traceability-history.png" });

    const desktop = page.viewportSize()!;
    await page.setViewportSize({ width: 390, height: 844 });
    const viewedBox = await viewed.boundingBox();
    const searchedBox = await searched.boundingBox();
    expect(searchedBox!.y).toBeGreaterThan(viewedBox!.y + viewedBox!.height);
    await page.screenshot({
        path: "test-results/traceability-history-mobile.png",
    });
    await page.setViewportSize(desktop);
    await page.getByRole("button", { name: "清空历史" }).click();
    await page.reload();
    await expect(viewed.getByRole("link")).toHaveCount(0);
    await expect(searched.getByRole("link")).toHaveCount(0);
}
