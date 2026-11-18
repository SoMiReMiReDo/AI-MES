import { expect, type Page } from "@playwright/test";

export async function productionFlow(
    page: Page,
    orderNo: string,
    suffix: string,
) {
    page.setDefaultTimeout(15000);
    const lotNo = "LOT-" + suffix;
    const materialCode = "SI-" + suffix;
    const sourceCode = "SRC-" + suffix;
    const incomingLot = "IN-" + suffix;
    const supplierLot = "SUP-" + suffix;
    const splitNo = "SPLIT-" + suffix;
    const mergedNo = "MERGE-" + suffix;
    const waferCodes = [1, 2, 3, 4, 5].map((n) => `W-${suffix}-${n}`);
    const modal = () =>
        page.getByRole("dialog").filter({
            has: page.getByRole("button", {
                name: /保存|保 存|确认执行/,
                exact: true,
            }),
        });
    const drawer = () => page.locator(".ant-drawer");
    const choose = async (label: string, option: string) => {
        await page.getByLabel(label, { exact: true }).click();
        await page
            .locator(".ant-select-dropdown:visible")
            .getByText(option, { exact: true })
            .click();
    };
    const save = async () => {
        await modal()
            .getByRole("button", { name: /保\s*存/, exact: true })
            .click();
        await expect(modal()).toHaveCount(0);
    };
    const registration = page.locator(".ant-modal:visible");
    const register = async () => {
        await registration.getByRole("button", { name: /^确\s*定$/ }).click();
        await expect(registration).toHaveCount(0);
    };
    await page.getByRole("link", { name: "硅片来料", exact: true }).click();
    await page.getByRole("button", { name: "登记物料", exact: true }).click();
    await registration.getByLabel("代码", { exact: true }).fill(materialCode);
    await registration.getByLabel("名称", { exact: true }).fill("硅片 300 mm");
    await register();
    await page.getByRole("button", { name: "登记来源", exact: true }).click();
    await choose("硅片物料", `${materialCode} · 硅片 300 mm`);
    await registration.getByLabel("代码", { exact: true }).fill(sourceCode);
    await registration
        .getByLabel("名称", { exact: true })
        .fill("浏览器演示来源");
    await register();
    await page.getByRole("button", { name: "登记来料批", exact: true }).click();
    await choose("来源", `${sourceCode} · 浏览器演示来源 · ${materialCode}`);
    await registration
        .getByLabel("来料批号", { exact: true })
        .fill(incomingLot);
    await registration
        .getByLabel("供应方批号", { exact: true })
        .fill(supplierLot);
    await register();
    await expect(
        drawer().getByText(supplierLot, { exact: true }),
    ).toBeVisible();
    await page.locator(".ant-drawer-close").click();

    await page.getByRole("link", { name: "生产管理" }).click();
    await page.getByRole("button", { name: "工单建批", exact: true }).click();
    await choose("已下达工单", orderNo);
    await page
        .getByLabel("批号（留空按编号规则分配）", { exact: true })
        .fill(lotNo);
    await page
        .getByLabel("晶圆编号（每行一片）", { exact: true })
        .fill(waferCodes.join("\n"));
    await choose(
        "硅片来料批",
        `${incomingLot} · ${materialCode} · ${supplierLot}`,
    );
    await save();
    await page.getByRole("button", { name: lotNo, exact: true }).click();
    await drawer()
        .getByRole("button", { name: "投放批次", exact: true })
        .click();
    await save();
    await expect(drawer().getByText("等待加工", { exact: true })).toBeVisible();
    for (const code of waferCodes.slice(0, 2))
        await drawer()
            .getByRole("row")
            .filter({ hasText: code })
            .getByRole("checkbox")
            .check();
    await drawer()
        .getByRole("button", { name: "按片拆批", exact: true })
        .click();
    await page.getByLabel("新批号（留空按规则分配）").fill(splitNo);
    await page.getByLabel("操作原因").fill("浏览器验证 2+3 拆批");
    await modal()
        .getByRole("button", { name: "预览并检查", exact: true })
        .click();
    await expect(modal().getByText(/源批保留 3 片/)).toBeVisible();
    await modal()
        .getByRole("button", { name: "确认执行", exact: true })
        .click();
    await expect(modal()).toHaveCount(0);
    await expect(
        drawer().locator(".ant-drawer-title").filter({ hasText: splitNo }),
    ).toBeVisible();
    await page.locator(".ant-drawer-close").click();
    for (const code of [lotNo, splitNo])
        await page
            .getByRole("row")
            .filter({ hasText: code })
            .getByRole("checkbox")
            .check();
    await page
        .getByRole("button", { name: "合并选中批次", exact: true })
        .click();
    await page.getByLabel("新批号（留空按规则分配）").fill(mergedNo);
    await page.getByLabel("操作原因").fill("浏览器验证合回 5 片");
    await modal()
        .getByRole("button", { name: "预览并检查", exact: true })
        .click();
    await expect(modal().getByText(/新批 5 片/)).toBeVisible();
    await modal()
        .getByRole("button", { name: "确认执行", exact: true })
        .click();
    await expect(modal()).toHaveCount(0);
    await expect(
        drawer().locator(".ant-drawer-title").filter({ hasText: mergedNo }),
    ).toBeVisible();
    await expect(drawer().getByText("合批", { exact: true })).toBeVisible();
    await page.screenshot({
        path: "test-results/production-genealogy.png",
        fullPage: true,
    });
    await page.locator(".ant-drawer-close").click();
    await page.getByRole("tab", { name: "载具与槽位", exact: true }).click();
    await page.getByRole("button", { name: "新建载具", exact: true }).click();
    await page.getByLabel("载具编号", { exact: true }).fill("C-" + suffix);
    await page.getByLabel("槽位容量", { exact: true }).fill("5");
    await save();
    await page
        .getByRole("button", { name: "C-" + suffix, exact: true })
        .click();
    await drawer()
        .getByRole("button", { name: "装卸与换槽", exact: true })
        .click();
    await choose("晶圆", waferCodes[0]);
    await choose("目标槽（空为卸载）", "槽 1");
    await page.getByLabel("操作原因", { exact: true }).fill("浏览器装载");
    await save();
    await expect(
        drawer().getByRole("cell", { name: waferCodes[0], exact: true }),
    ).toBeVisible();
    await page.screenshot({
        path: "test-results/production-carrier.png",
        fullPage: true,
    });
    await page.locator(".ant-drawer-close").click();
    await page.getByRole("tab", { name: "晶圆履历", exact: true }).click();
    await page
        .getByRole("button", { name: waferCodes[0], exact: true })
        .click();
    await expect(
        drawer().getByRole("cell", { name: "装载", exact: true }),
    ).toBeVisible();
    await drawer()
        .getByRole("link", { name: incomingLot, exact: true })
        .click();
    await expect(
        drawer().getByRole("heading", { name: "来源晶圆" }),
    ).toBeVisible();
    await expect(
        drawer().getByRole("link", { name: mergedNo, exact: true }),
    ).toHaveCount(5);
    await page.screenshot({
        path: "test-results/wafer-origin-trace.png",
        fullPage: true,
    });
    await drawer()
        .getByRole("link", { name: waferCodes[0], exact: true })
        .click();
    await expect(
        drawer().getByRole("link", { name: incomingLot, exact: true }),
    ).toBeVisible();
    await page.locator(".ant-drawer-close").click();
}
