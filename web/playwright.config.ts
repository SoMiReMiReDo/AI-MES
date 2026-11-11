import { defineConfig } from "@playwright/test";

export default defineConfig({
    testDir: "tests",
    workers: 1,
    use: {
        baseURL: process.env.MES_E2E_URL || "http://127.0.0.1:5174",
        channel: "chrome",
        trace: "retain-on-failure",
        screenshot: "only-on-failure",
    },
    reporter: "list",
});
