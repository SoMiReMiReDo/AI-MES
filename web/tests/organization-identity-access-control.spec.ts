import { test, expect } from "@playwright/test";
import { productionFlow } from "./production-flow";
import { executionFlow } from "./execution-flow";

test("真实浏览器：首登改密、组织维护、账号分权、退出与撤权", async ({
    page,
    browser,
}) => {
    test.setTimeout(180000);
    const suffix = Date.now().toString();
    const initial = process.env.MES_E2E_PASSWORD!;
    const final = initial + "-changed";
    const login = async (p: typeof page, name: string, pwd: string) => {
        await p.goto("/");
        await p.getByLabel("用户名", { exact: true }).fill(name);
        await p.getByLabel("密码", { exact: true }).fill(pwd);
        await p.getByRole("button", { name: /登\s*录/, exact: true }).click();
    };
    await login(page, "e2e_admin", initial);
    await expect(
        page.getByRole("heading", { name: "设置你的正式密码" }),
    ).toBeVisible();
    await page.getByLabel("当前密码", { exact: true }).fill(initial);
    await page
        .getByLabel("新密码（12～128 字符）", { exact: true })
        .fill(final);
    await page.getByLabel("确认新密码", { exact: true }).fill(final);
    await page.getByRole("button", { name: "更新密码并重新登录" }).click();
    await expect(
        page.getByRole("heading", { name: "登录工作台" }),
    ).toBeVisible();
    await login(page, "e2e_admin", final);
    await expect(
        page.getByRole("heading", { name: "你好，系统管理员" }),
    ).toBeVisible();
    await page.screenshot({
        path: "test-results/workspace.png",
        fullPage: true,
    });
    await page.getByRole("link", { name: "组织结构" }).click();
    await page.getByRole("tab", { name: "区域", exact: true }).click();
    await page.getByRole("button", { name: /新增区域/ }).click();
    await page.getByLabel("编码", { exact: true }).fill("E2E" + suffix);
    await page.getByLabel("名称", { exact: true }).fill("浏览器验收区域");
    await page.getByRole("button", { name: "确 定", exact: true }).click();
    await expect(
        page.getByRole("cell", { name: "浏览器验收区域", exact: true }),
    ).toBeVisible();
    await page.screenshot({
        path: "test-results/organization.png",
        fullPage: true,
    });
    // Create a second account through the same authenticated HTTP surface to isolate role assertions.
    const csrf = await page.request
        .get("/api/v1/auth/csrf")
        .then((r) => r.json());
    const roles = await page.request
        .get("/api/v1/security/roles?pageSize=100")
        .then((r) => r.json());
    const me = await page.request.get("/api/v1/auth/me").then((r) => r.json());
    const user = await page.request
        .post("/api/v1/security/users", {
            headers: { "X-CSRF-TOKEN": csrf.requestToken },
            data: {
                username: "viewer" + suffix,
                displayName: "只读验收员",
                temporaryPassword: initial,
                roleIds: [roles.items.find((r: any) => r.name === "Viewer").id],
                fabIds: [me.fab.id],
            },
        })
        .then(async (r) => {
            expect(r.status()).toBe(201);
            return r.json();
        });
    await page.getByRole("link", { name: "账号与访问" }).click();
    await expect(
        page.getByRole("cell", { name: "只读验收员", exact: true }),
    ).toBeVisible();
    const context = await browser.newContext();
    const viewer = await context.newPage();
    await login(viewer, "viewer" + suffix, initial);
    await viewer.getByLabel("当前密码", { exact: true }).fill(initial);
    await viewer
        .getByLabel("新密码（12～128 字符）", { exact: true })
        .fill(final);
    await viewer.getByLabel("确认新密码", { exact: true }).fill(final);
    await viewer.getByRole("button", { name: "更新密码并重新登录" }).click();
    await expect(
        viewer.getByRole("heading", { name: "登录工作台" }),
    ).toBeVisible();
    await login(viewer, "viewer" + suffix, final);
    await expect(
        viewer.getByRole("heading", { name: "你好，只读验收员" }),
    ).toBeVisible();
    await expect(viewer.getByRole("link", { name: "账号与访问" })).toHaveCount(
        0,
    );
    expect((await viewer.request.get("/api/v1/security/users")).status()).toBe(
        403,
    );
    const latest = await page.request
        .get("/api/v1/security/users/" + user.id)
        .then((r) => r.json());
    expect(
        (
            await page.request.post(
                `/api/v1/security/users/${user.id}/deactivate`,
                {
                    headers: {
                        "X-CSRF-TOKEN": csrf.requestToken,
                        "If-Match": latest.etag,
                    },
                    data: { reason: "浏览器撤权验收" },
                },
            )
        ).status(),
    ).toBe(200);
    await viewer.reload();
    await expect(
        viewer.getByRole("heading", { name: "登录工作台" }),
    ).toBeVisible();
    await context.close();
    // Manufacturing shares the same authenticated session and permission pipeline.
    await page.getByRole("link", { name: "工艺管理" }).click();
    await page.getByRole("tab", { name: "产品", exact: true }).click();
    await page.getByRole("button", { name: "新建产品", exact: true }).click();
    await page.getByLabel("编码", { exact: true }).fill("FAB" + suffix);
    await page.getByLabel("名称", { exact: true }).fill("浏览器晶圆产品");
    await page.getByLabel("单位", { exact: true }).fill("wafer");
    await page.getByRole("button", { name: "保 存", exact: true }).click();
    await expect(
        page.getByRole("cell", { name: "浏览器晶圆产品", exact: true }),
    ).toBeVisible();
    const create = async (path: string, data: any, etag?: string) => {
        const response = await page.request.post("/api/v1/" + path, {
            headers: {
                "X-CSRF-TOKEN": csrf.requestToken,
                "Idempotency-Key": crypto.randomUUID(),
                ...(etag ? { "If-Match": etag } : {}),
            },
            data,
        });
        expect(response.ok(), await response.text()).toBeTruthy();
        return response.json();
    };
    const products = await page.request
        .get("/api/v1/engineering/products")
        .then((r) => r.json());
    const product = products.items.find((x: any) => x.code === "FAB" + suffix);
    const route = await create("engineering/routes", {
        code: "R" + suffix,
        name: "测试路线",
        productId: product.id,
    });
    const version = await create("engineering/route-versions", {
        routeId: route.id,
        version: "1",
    });
    const operation = await create("engineering/operations", {
        code: "OP" + suffix,
        name: "光刻",
        segmentType: "Process",
    });
    await create("engineering/route-steps", {
        routeVersionId: version.id,
        operationId: operation.id,
        sequenceNo: 1,
        name: "首站",
    });
    await create(
        `engineering/route-versions/${version.id}/release`,
        { reason: "浏览器验收" },
        version.etag,
    );
    const revision = await create("engineering/product-revisions", {
        productId: product.id,
        routeVersionId: version.id,
        revisionCode: "A",
    });
    await create(
        `engineering/product-revisions/${revision.id}/release`,
        { reason: "浏览器验收" },
        revision.etag,
    );
    const workOrder = await create("planning/work-orders", {
        orderNo: "WO" + suffix,
        productId: product.id,
        productRevisionId: revision.id,
        plannedQty: 25,
    });
    await page.getByRole("link", { name: "工单管理" }).click();
    await expect(
        page.getByRole("cell", { name: "WO" + suffix, exact: true }),
    ).toBeVisible();
    await page.getByRole("button", { name: /下\s*达/, exact: true }).click();
    await page.getByPlaceholder("填写操作原因").fill("浏览器下达验收");
    await page.getByRole("button", { name: "确 定", exact: true }).click();
    await expect(page.getByText("Released", { exact: true })).toBeVisible();
    await page.getByRole("button", { name: "执行概况", exact: true }).click();
    await expect(
        page.getByText("生产进度按唯一晶圆事实统计", { exact: true }),
    ).toBeVisible();
    await expect(page.getByText(/计划：25；投放：0/)).toBeVisible();
    await page.screenshot({
        path: "test-results/manufacturing.png",
        animations: "disabled",
        fullPage: true,
    });
    await page.getByRole("button", { name: "关闭", exact: true }).click();
    await productionFlow(page, "WO" + suffix, suffix);
    await executionFlow(page, suffix);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto("/");
    await page.screenshot({ path: "test-results/mobile.png", fullPage: true });
    await page.getByRole("button", { name: /退\s*出/, exact: true }).click();
    await expect(
        page.getByRole("heading", { name: "登录工作台" }),
    ).toBeVisible();
});
