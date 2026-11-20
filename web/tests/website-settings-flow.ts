import { expect, type Browser, type Page } from "@playwright/test";

export async function websiteSettingsFlow(page: Page, browser: Browser) {
    const nav = page.getByRole("navigation", { name: "主导航", exact: true });
    const search = page.getByRole("textbox", { name: "搜索导航", exact: true });
    await search.fill("设备与资源");
    await expect(nav.getByRole("link")).toHaveCount(1);
    await search.fill("设资");
    await search.press("Enter");
    await expect(page).toHaveURL(/\/equipment$/);
    await expect(nav.getByRole("link", { name: "设备与资源" })).toHaveAttribute(
        "aria-current",
        "page",
    );
    await search.fill("没有这样的菜单");
    await expect(nav.getByRole("status")).toHaveText("没有匹配的菜单");
    await page.getByRole("button", { name: "清空导航搜索" }).click();
    await expect(nav.getByRole("group", { name: "生产运营" })).toBeVisible();

    const sidebar = page.locator(".left-sidebar");
    const workspace = page.locator(".workspace");
    const expandedWidth = (await workspace.boundingBox())!.width;
    await page.getByRole("button", { name: "折叠导航" }).click();
    await expect(sidebar).toHaveCSS("width", "64px");
    expect((await workspace.boundingBox())!.width).toBeGreaterThan(
        expandedWidth,
    );
    await expect(sidebar.locator(".brand-mark")).toBeVisible();
    await page.reload();
    await expect(sidebar).toHaveCSS("width", "64px");
    await page.getByRole("button", { name: "搜索导航", exact: true }).click();
    await search.fill("系统设置");
    await search.press("Enter");
    await expect(page).toHaveURL(/\/settings\/system$/);
    await expect(sidebar).toHaveCSS("width", "64px");
    await page.getByRole("button", { name: "展开导航" }).click();

    const root = page.locator("html");
    if ((await root.getAttribute("data-theme")) === "light") {
        await page.getByRole("button", { name: "切换到黑夜模式" }).click();
    }
    await page.getByRole("button", { name: "切换到浅色模式" }).click();
    await page.getByRole("tab", { name: "系统信息" }).click();
    await expect(
        page.getByText("Asia/Shanghai", { exact: true }),
    ).toBeVisible();
    await expect(page.getByText("就绪", { exact: true })).toBeVisible();
    await expect(page.getByText("检查时间", { exact: true })).toBeVisible();
    await page.getByRole("tab", { name: "站点外观" }).click();
    const name = page.getByRole("textbox", { name: "站点名称" });
    await expect(name).toHaveValue("MESagent");
    const current = await page.request
        .get("/api/v1/settings/appearance")
        .then((r) => r.json());
    const csrf = await page.request
        .get("/api/v1/auth/csrf")
        .then((r) => r.json());
    const concurrent = await page.request.put("/api/v1/settings/appearance", {
        headers: {
            "If-Match": current.etag,
            "X-CSRF-TOKEN": csrf.requestToken,
        },
        data: { ...current.appearance, name: "并发验收" },
    });
    expect(concurrent.ok(), await concurrent.text()).toBeTruthy();
    await name.fill("过期版本");
    const save = page.getByRole("button", { name: "保存站点外观" });
    await save.click();
    await expect(
        page.getByText("设置已被其他操作更新，请重新加载后再保存"),
    ).toBeVisible();
    await page.getByRole("button", { name: "重新加载" }).click();
    await expect(name).toHaveValue("并发验收");

    await name.fill("晶圆制造工作台");
    await page
        .getByRole("textbox", { name: "副标题" })
        .fill("晶圆产线 · 制造执行");
    await page.getByRole("radio", { name: "深色" }).check();
    const preview = page.locator(".settings-preview");
    await expect(
        preview.getByText("晶圆制造工作台", { exact: true }),
    ).toBeVisible();
    await expect(preview).toHaveAttribute("data-preview-theme", "dark");
    await save.click();
    await expect(page).toHaveTitle("晶圆制造工作台");
    await expect(root).toHaveAttribute("data-theme", "light");
    await expect(
        sidebar.getByText("晶圆产线 · 制造执行", { exact: true }),
    ).toBeVisible();
    await page.screenshot({
        path: "test-results/website-settings.png",
        animations: "disabled",
    });

    await page.getByRole("tab", { name: "排程", exact: true }).click();
    await expect(page.getByRole("radio", { name: "周视图" })).toBeChecked();
    await page.getByRole("radio", { name: "日视图" }).check();
    await page.getByRole("button", { name: "保存排程设置" }).click();
    await expect(page.getByText("排程设置已保存")).toBeVisible();
    await nav.getByRole("link", { name: "工作台", exact: true }).click();
    await page.getByRole("tab", { name: "排程日历" }).click();
    await expect(page.getByText(/^设备日历 · /)).toBeVisible();
    await expect(page.getByRole("radio", { name: "日视图" })).toBeChecked();

    await page.setViewportSize({ width: 390, height: 844 });
    await expect(sidebar).toHaveCSS("width", "64px");
    await page.getByRole("button", { name: "展开导航" }).click();
    await expect(
        sidebar.getByText("晶圆制造工作台", { exact: true }),
    ).toBeVisible();
    await search.fill("组结");
    await search.press("Enter");
    await expect(page).toHaveURL(/\/settings\/organization$/);
    await expect(sidebar).toHaveCSS("width", "64px");
    await page.getByRole("button", { name: "切换到黑夜模式" }).click();
    await page.getByRole("button", { name: "搜索导航", exact: true }).click();
    await expect(search).toBeFocused();
    await search.fill("设资");
    await expect(
        page
            .getByRole("navigation", { name: "导航搜索结果" })
            .getByRole("link", { name: "设备与资源" }),
    ).toBeVisible();
    await page.screenshot({
        path: "test-results/sidebar-search-mobile-dark.png",
        animations: "disabled",
    });
    await search.press("Escape");
    await expect(search).toBeHidden();
    await page.getByRole("button", { name: "切换到浅色模式" }).click();
    await page.setViewportSize({ width: 1280, height: 900 });

    const anonymous = await browser.newContext();
    try {
        const login = await anonymous.newPage();
        await login.goto("/");
        await expect(login).toHaveTitle("晶圆制造工作台");
        await expect(login.locator("html")).toHaveAttribute(
            "data-theme",
            "dark",
        );
        await expect(login.locator(".login-card .brand-mark")).toBeVisible();
        await login.route("**/api/v1/site/appearance", (route) =>
            route.fulfill({ status: 503 }),
        );
        await login.reload();
        await expect(login).toHaveTitle("MESagent");
        await expect(login.locator(".login-card .brand")).toContainText(
            "MESagent",
        );
        await expect(
            login.getByRole("heading", { name: "登录工作台" }),
        ).toBeVisible();
    } finally {
        await anonymous.close();
    }
}
