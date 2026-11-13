import { expect, type Page } from "@playwright/test";

export async function productionFlow(
    page: Page,
    orderNo: string,
    suffix: string,
) {
    page.setDefaultTimeout(15000);
    const lotNo = "LOT-" + suffix;
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
    await page.getByRole("link", { name: "生产管理" }).click();
    await page.getByRole("button", { name: "工单建批", exact: true }).click();
    await choose("已下达工单", orderNo);
    await page
        .getByLabel("批号（留空按编号规则分配）", { exact: true })
        .fill(lotNo);
    await page
        .getByLabel("晶圆编号（每行一片）", { exact: true })
        .fill(waferCodes.join("\n"));
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
    await expect(
        drawer().getByText("来源未登记", { exact: true }),
    ).toBeVisible();
    await page.locator(".ant-drawer-close").click();
}
