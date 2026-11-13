import { expect, type Page } from "@playwright/test";

export async function executionFlow(page: Page, suffix: string) {
    const code = `EXEC-${suffix}`;
    const groupName = `执行设备组 ${suffix}`;
    const typeName = `执行类型 ${suffix}`;
    const drawer = () => page.locator(".ant-drawer-open");
    const modal = () => page.locator(".ant-modal:visible");
    const choose = async (label: string, text: string) => {
        const input = page
            .getByLabel(label, { exact: true })
            .and(page.getByRole("combobox"));
        await input.click();
        await page
            .locator(".ant-select-dropdown:visible")
            .getByText(text, { exact: true })
            .click();
        await expect(input).toHaveAttribute("aria-expanded", "false");
        await expect(page.locator(".ant-select-dropdown:visible")).toHaveCount(
            0,
        );
    };
    const save = async () => {
        await modal()
            .getByRole("button", { name: /保\s*存/, exact: true })
            .click();
        await expect(modal()).toHaveCount(0);
    };
    await page.getByRole("link", { name: "设备台账" }).click();
    for (const [tab, name] of [
        ["设备类型", typeName],
        ["设备组", groupName],
    ]) {
        await page.getByRole("tab", { name: tab, exact: true }).click();
        await page
            .getByRole("button", { name: `新增${tab}`, exact: true })
            .click();
        await page.getByLabel("编码", { exact: true }).fill(code);
        await page.getByLabel("名称", { exact: true }).fill(name);
        await save();
        await expect(
            page.getByRole("cell", { name, exact: true }),
        ).toBeVisible();
    }
    await page.getByRole("tab", { name: "设备", exact: true }).click();
    await page.getByRole("button", { name: "新增设备", exact: true }).click();
    await page.getByLabel("编码", { exact: true }).fill(code);
    await page.getByLabel("名称", { exact: true }).fill("演示加工机");
    await choose(
        "所属区域",
        (
            await page.request
                .get("/api/v1/equipment/options")
                .then((r) => r.json())
        ).areas
            .map((x: any) => `${x.code} ${x.name}`)
            .find((x: string) => x.includes("浏览器验收区域")),
    );
    const groupLabel = `${code} ${groupName}`;
    await choose("设备组", groupLabel);
    await choose("设备类型", `${code} ${typeName}`);
    await save();
    const toolRow = page.getByRole("row").filter({ hasText: code });
    await toolRow.getByRole("button", { name: "变更状态" }).click();
    await choose("人工状态", "维护");
    await page.getByLabel("变更原因").fill("浏览器维护状态验收");
    await save();
    await expect(toolRow.getByText("维护", { exact: true })).toBeVisible();
    await toolRow.getByRole("button", { name: "变更状态" }).click();
    await choose("人工状态", "空闲可用");
    await page.getByLabel("变更原因").fill("维护完成，恢复人工可用");
    await save();
    const get = (path: string) =>
        page.request.get(`/api/v1/${path}`).then((r) => r.json());
    const csrf = (await get("auth/csrf")).requestToken;
    const create = async (path: string, data: unknown, etag?: string) => {
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
    const tool = (await get("equipment/tools?pageSize=100")).items.find(
        (x: any) => x.code === code,
    );
    const product = await create("engineering/products", {
        code,
        name: "执行验收产品",
        unitOfMeasure: "wafer",
    });
    const route = await create("engineering/routes", {
        code,
        name: "三站演示路线",
        productId: product.id,
    });
    const version = await create("engineering/route-versions", {
        routeId: route.id,
        version: "1",
    });
    const operation = await create("engineering/operations", {
        code,
        name: "演示工序",
        segmentType: "Process",
    });
    const recipe = await create("engineering/recipes", {
        code,
        name: "演示配方",
    });
    let recipeVersion = await create("engineering/recipe-versions", {
        recipeId: recipe.id,
        version: "1",
    });
    recipeVersion = await create(
        `engineering/recipe-versions/${recipeVersion.id}/release`,
        { reason: "发布" },
        recipeVersion.etag,
    );
    for (let i = 1; i <= 3; i++) {
        const step = await create("engineering/route-steps", {
            routeVersionId: version.id,
            operationId: operation.id,
            sequenceNo: i * 10,
            name: `浏览器执行站 ${i}`,
            equipmentTypeId: tool.equipmentTypeId,
            instruction: "核对片号与设备，记录实际加工结果",
            requiresConfirmation: i === 1,
        });
        await create("engineering/recipe-applicabilities", {
            routeStepId: step.id,
            equipmentId: tool.id,
            recipeVersionId: recipeVersion.id,
            effectiveFrom: new Date(Date.now() - 86400000).toISOString(),
        });
        if (i === 1) {
            await create("engineering/checklist-items", {
                routeStepId: step.id,
                label: "核对载具",
                isRequired: true,
            });
            await create("engineering/output-definitions", {
                routeStepId: step.id,
                key: "temperature",
                label: "温度",
                valueType: "decimal",
                unit: "C",
                isRequired: true,
            });
            const doc = await create("engineering/documents", {
                entityType: "RouteStep",
                entityId: step.id,
                revision: "A",
                filename: "instruction.pdf",
                content: Buffer.from("%PDF-1.7\n%%EOF").toString("base64"),
            });
            await create(
                `engineering/documents/${doc.id}/release`,
                {},
                doc.etag,
            );
        }
    }
    await create(
        `engineering/route-versions/${version.id}/release`,
        { reason: "发布" },
        version.etag,
    );
    const revision = await create("engineering/product-revisions", {
        productId: product.id,
        routeVersionId: version.id,
        revisionCode: "A",
    });
    await create(
        `engineering/product-revisions/${revision.id}/release`,
        { reason: "发布" },
        revision.etag,
    );
    let order = await create("planning/work-orders", {
        orderNo: code,
        productId: product.id,
        productRevisionId: revision.id,
        plannedQty: 5,
    });
    order = await create(
        `planning/work-orders/${order.id}/release`,
        { reason: "下达" },
        order.etag,
    );
    const waferCodes = [1, 2, 3, 4, 5].map((i) => `${code}-W${i}`);
    let lot = await create("production/lots", {
        workOrderId: order.id,
        lotNumber: code,
        wafers: waferCodes.map((waferCode) => ({
            waferCode,
            waferSizeMm: 300,
        })),
    });
    lot = await create(`production/lots/${lot.id}/release`, {}, lot.etag);
    await page.getByRole("link", { name: "生产执行" }).click();
    await page.getByLabel("批次号", { exact: true }).fill(code);
    await page.getByRole("button", { name: /筛\s*选/, exact: true }).click();
    await page.getByRole("button", { name: code, exact: true }).click();
    for (let i = 1; i <= 3; i++) {
        await expect(
            drawer().getByText(`当前步骤：浏览器执行站 ${i}`),
        ).toBeVisible();
        await choose("设备与配方", `${code} / ${code} 1`);
        if (i === 1) {
            await drawer().getByRole("button", { name: "人工指派" }).click();
            await expect(drawer().getByText(/已人工指派/)).toBeVisible();
        }
        await drawer().getByRole("button", { name: "开始加工" }).click();
        await expect(
            drawer().getByText("加工中", { exact: true }),
        ).toBeVisible();
        if (i === 1) {
            await drawer()
                .getByRole("button", { name: "录入作业记录" })
                .click();
            await modal().getByRole("checkbox", { name: "核对载具" }).check();
            await modal()
                .getByRole("checkbox", { name: "instruction.pdf" })
                .check();
            await page.getByLabel("温度（C）", { exact: true }).fill("23.5");
            await modal()
                .getByRole("checkbox", { name: "确认已完成关键操作" })
                .check();
            await modal().getByRole("button", { name: "提交记录" }).click();
            await expect(modal()).toHaveCount(0);
            await expect(drawer().getByText(/温度 C.*23.5/)).toBeVisible();
            await page.screenshot({
                path: "test-results/execution-work.png",
                fullPage: true,
                animations: "disabled",
            });
        }
        await drawer()
            .getByRole("button", { name: "结束加工", exact: true })
            .click();
        for (const waferCode of waferCodes) await choose(waferCode, "通过");
        await modal().getByRole("button", { name: "提交记录" }).click();
        await expect(modal()).toHaveCount(0);
        await drawer().getByRole("button", { name: "查看批次" }).click();
    }
    await expect(drawer().locator(".ant-tag").first()).toHaveText("已完成");
    await page.screenshot({
        path: "test-results/execution-completed.png",
        fullPage: true,
        animations: "disabled",
    });
    await drawer().getByRole("button", { name: "关闭", exact: true }).click();
    await page.getByRole("link", { name: "工单管理" }).click();
    await page
        .getByRole("row")
        .filter({ hasText: code })
        .getByRole("button", { name: "执行概况" })
        .click();
    await expect(drawer().getByText(/完工：5/)).toBeVisible();
    await page.screenshot({
        path: "test-results/execution-progress.png",
        fullPage: true,
        animations: "disabled",
    });
    await drawer().getByRole("button", { name: "关闭", exact: true }).click();
    const progress = await get(`planning/work-orders/${order.id}/progress`);
    expect(progress.completedQty).toBe(5);
    expect(progress.inProcessQty).toBe(0);
}
