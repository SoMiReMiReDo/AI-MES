import { expect, type Page } from "@playwright/test";

export async function qualityFlow(page: Page, suffix: string) {
    const code = `QUALITY-${suffix}`;
    const modal = () => page.locator(".ant-modal:visible");
    const drawer = () => page.locator(".ant-drawer-open");
    const choose = async (label: string, text: string) => {
        await page
            .getByLabel(label, { exact: true })
            .and(page.getByRole("combobox"))
            .click();
        await page
            .locator(".ant-select-dropdown:visible")
            .getByText(text, { exact: true })
            .click();
    };
    const save = async () => {
        await modal()
            .getByRole("button", { name: "确 定", exact: true })
            .click();
        await expect(modal()).toHaveCount(0);
    };
    const review = async () => {
        await page
            .getByLabel("审核说明", { exact: true })
            .fill("浏览器质量审核");
        await save();
    };
    const get = (path: string) =>
        page.request.get(`/api/v1/${path}`).then((r) => r.json());
    const csrf = (await get("auth/csrf")).requestToken;
    const post = async (path: string, data: unknown, etag?: string) => {
        const response = await page.request.post(`/api/v1/${path}`, {
            data,
            headers: {
                "X-CSRF-TOKEN": csrf,
                "Idempotency-Key": crypto.randomUUID(),
                ...(etag ? { "If-Match": etag } : {}),
            },
        });
        expect(response.ok(), await response.text()).toBeTruthy();
        return response.json();
    };
    const previous = (await get(`quality/lots?q=EXEC-${suffix}`)).items[0];
    const previousOrder = await get(
        `planning/work-orders/${previous.workOrderId}`,
    );
    let order = await post("planning/work-orders", {
        orderNo: code,
        productId: previousOrder.productId,
        productRevisionId: previousOrder.productRevisionId,
        plannedQty: 3,
    });
    order = await post(
        `planning/work-orders/${order.id}/release`,
        { reason: "质量浏览器验收" },
        order.etag,
    );
    let lot = await post("production/lots", {
        workOrderId: order.id,
        lotNumber: code,
        wafers: [1, 2, 3].map((i) => ({
            waferCode: `${code}-W${i}`,
            waferSizeMm: 300,
        })),
    });
    lot = await post(`production/lots/${lot.id}/release`, {}, lot.etag);
    await page.getByRole("link", { name: "质量管理", exact: true }).click();
    await page.getByRole("tab", { name: "质量配置", exact: true }).click();
    for (const [category, name] of [
        ["冻结", "Hold"],
        ["报废", "Scrap"],
    ]) {
        await page
            .getByRole("button", { name: "新增原因码", exact: true })
            .click();
        await page.getByLabel("代码", { exact: true }).fill(`${code}-${name}`);
        await page
            .getByLabel("名称", { exact: true })
            .fill(`浏览器${category}原因`);
        await choose("类别", category);
        await save();
    }
    await page.getByRole("tab", { name: "规格版本", exact: true }).click();
    await page
        .getByRole("button", { name: "新增规格版本", exact: true })
        .click();
    await page.getByLabel("参数代码", { exact: true }).fill(code);
    await page.getByLabel("单位", { exact: true }).fill("nm");
    await page.getByLabel("规格下限", { exact: true }).fill("10");
    await page.getByLabel("规格上限", { exact: true }).fill("20");
    await save();
    await page
        .getByRole("row")
        .filter({ hasText: code })
        .getByRole("button", { name: /发\s*布/, exact: true })
        .click();
    await save();
    await expect(
        page
            .getByRole("row")
            .filter({ hasText: code })
            .getByText("已发布", { exact: true }),
    ).toBeVisible();
    await page.getByRole("tab", { name: "检验模板", exact: true }).click();
    await page
        .getByRole("button", { name: "新增检验模板", exact: true })
        .click();
    await page.getByLabel("名称", { exact: true }).fill(code);
    await choose("路线版本", "三站演示路线 · v1");
    await choose("绑定已发布规格", `${code} · v1`);
    await modal().locator(".ant-modal-title").click();
    await save();
    await page
        .getByRole("row")
        .filter({ hasText: code })
        .getByRole("button", { name: /发\s*布/, exact: true })
        .click();
    await save();
    const openLot = async () => {
        await page
            .getByRole("tab", { name: "冻结与放行", exact: true })
            .click();
        const search = page.getByLabel("质量搜索", { exact: true });
        await search.fill(code);
        await search.press("Enter");
        await page
            .getByRole("row")
            .filter({ hasText: code })
            .getByRole("button", { name: "查看批次", exact: true })
            .click();
        await expect(
            drawer().getByRole("cell", { name: code, exact: true }).first(),
        ).toBeVisible();
    };
    const closeDrawer = async () => {
        await page.keyboard.press("Escape");
        await expect(drawer()).toHaveCount(0);
    };
    await openLot();
    await drawer()
        .getByRole("button", { name: "添加 Hold", exact: true })
        .click();
    await choose(
        "冻结原因",
        `${code}-Hold`.toUpperCase() + " · 浏览器冻结原因",
    );
    await save();
    await drawer()
        .getByRole("button", { name: "解除 Hold", exact: true })
        .click();
    await review();
    await drawer()
        .getByRole("button", { name: "质量放行", exact: true })
        .click();
    await review();
    await drawer()
        .getByRole("button", { name: "录入检验", exact: true })
        .click();
    await choose("已发布模板", `${code} · v1`);
    await modal().getByRole("spinbutton").fill("21");
    await save();
    await expect(drawer().getByText("有效冻结", { exact: true })).toBeVisible();
    await closeDrawer();
    await page.getByRole("tab", { name: "检验记录", exact: true }).click();
    await page
        .getByRole("row")
        .filter({ hasText: code })
        .getByRole("button", { name: "查看检验记录", exact: true })
        .click();
    await expect(drawer().getByText("不合格", { exact: true })).toBeVisible();
    await drawer()
        .getByRole("button", { name: "打开关联异常", exact: true })
        .click();
    await drawer()
        .getByRole("button", { name: "确认异常", exact: true })
        .click();
    await review();
    await drawer()
        .getByRole("button", { name: "新增整改", exact: true })
        .click();
    await choose("责任人", "e2e_admin · 系统管理员");
    await page.getByLabel("整改标题", { exact: true }).fill("核对量测设备");
    await save();
    await drawer()
        .getByRole("button", { name: /完\s*成/, exact: true })
        .click();
    await review();
    await drawer()
        .getByRole("button", { name: /复\s*核/, exact: true })
        .click();
    await review();
    await drawer()
        .getByRole("button", { name: "处置异常", exact: true })
        .click();
    await page
        .getByLabel("原因分析", { exact: true })
        .fill("量测偏差，复核后受控继续");
    await page
        .getByLabel("遏制措施", { exact: true })
        .fill("隔离样本并复核设备");
    await page.getByLabel("处置说明", { exact: true }).fill("软件演示质量审核");
    await save();
    await drawer()
        .getByRole("button", { name: "解除 Hold", exact: true })
        .click();
    await review();
    await drawer()
        .getByRole("button", { name: "关闭异常", exact: true })
        .click();
    await review();
    await expect(drawer().getByText("已关闭", { exact: true })).toBeVisible();
    await page.screenshot({
        path: "test-results/quality-issue.png",
        fullPage: true,
        animations: "disabled",
    });
    await closeDrawer();
    await openLot();
    await drawer()
        .getByRole("button", { name: "逐片报废", exact: true })
        .click();
    await choose("报废晶圆", `${code}-W1`);
    await page.getByText(`逐片报废 ${code}`, { exact: true }).click();
    await choose("报废原因", `${code}-SCRAP · 浏览器报废原因`);
    await page
        .getByLabel("报废说明", { exact: true })
        .fill("浏览器逐片报废验证");
    await save();
    await expect(
        drawer().getByRole("cell", { name: `${code}-W1`, exact: true }),
    ).toHaveCount(0);
    await drawer()
        .getByRole("button", { name: "质量放行", exact: true })
        .click();
    await review();
    await page.screenshot({
        path: "test-results/quality-lot.png",
        fullPage: true,
        animations: "disabled",
    });
    await closeDrawer();
    for (let i = 0; i < 3; i++) {
        const detail = await get(`execution/lots/${lot.id}`);
        const candidate = detail.candidates[0];
        let run = await post(
            `execution/lots/${lot.id}/track-in`,
            {
                equipmentId: candidate.equipmentId,
                equipmentEtag: candidate.equipmentEtag,
                recipeVersionId: candidate.recipeVersionId,
            },
            detail.lot.etag,
        );
        const work = (await get(`execution/runs/${run.id}`)).work;
        if (i === 0)
            run = await post(
                `execution/runs/${run.id}/records`,
                {
                    checklistItemIds: work.checklist.map((x: any) => x.id),
                    documentIds: work.documents.map((x: any) => x.id),
                    confirm: true,
                    outputs: work.outputs.map((x: any) => ({
                        outputId: x.id,
                        unit: x.unit,
                        valueNumber: 23,
                    })),
                },
                run.etag,
            );
        const wafers = (await get(`execution/runs/${run.id}`)).wafers;
        await post(
            `execution/runs/${run.id}/track-out`,
            {
                wafers: wafers.map((x: any) => ({
                    waferId: x.id,
                    result: "Passed",
                })),
            },
            run.etag,
        );
    }
    const progress = await get(`planning/work-orders/${order.id}/progress`);
    expect(progress.releasedQty).toBe(3);
    expect(progress.completedQty).toBe(2);
    expect(progress.scrappedQty).toBe(1);
    expect(progress.inProcessQty).toBe(0);
    await page.getByRole("tab", { name: "逐片报废", exact: true }).click();
    await expect(
        page.getByRole("cell", { name: "浏览器逐片报废验证", exact: true }),
    ).toBeVisible();
    await page.screenshot({
        path: "test-results/quality-scrap.png",
        fullPage: true,
        animations: "disabled",
    });
}
