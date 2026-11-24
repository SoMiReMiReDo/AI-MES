import { MoonOutlined, SunOutlined } from "@ant-design/icons";
import { Button } from "antd";
import React, { createContext, useContext, useEffect, useState } from "react";
import {
    isThemeMode,
    useSiteAppearance,
    type ThemeMode,
} from "./siteAppearance";

export type { ThemeMode } from "./siteAppearance";

export type ResolvedTheme = "light" | "dark";

export const THEME_STORAGE_KEY = "mesagent.theme";

const DARK_SCHEME = "(prefers-color-scheme: dark)";
const DARK_BACKGROUND = "#17181c";
const LIGHT_BACKGROUND = "#ffffff";

/** 读取操作系统的浅色／深色偏好。 */
export function systemTheme(): ResolvedTheme {
    return window.matchMedia?.(DARK_SCHEME).matches ? "dark" : "light";
}

/** 把主题偏好（含跟随系统）解析为实际生效的浅色或深色。 */
export function resolveTheme(mode: ThemeMode): ResolvedTheme {
    return mode === "system" ? systemTheme() : mode;
}

function readTheme(): ThemeMode | undefined {
    try {
        const stored = localStorage.getItem(THEME_STORAGE_KEY);
        if (isThemeMode(stored)) return stored;
    } catch {
        // 隐私模式下 localStorage 不可用，使用默认浅色
    }
}

function applyTheme(mode: ResolvedTheme) {
    document.documentElement.dataset.theme = mode;
    document
        .querySelector('meta[name="theme-color"]')
        ?.setAttribute(
            "content",
            mode === "dark" ? DARK_BACKGROUND : LIGHT_BACKGROUND,
        );
}

// 模块加载即应用主题，避免首帧按浅色渲染
applyTheme(resolveTheme(readTheme() ?? "light"));

type ThemeValue = {
    /** 当前主题偏好，可能为「跟随系统」。 */
    mode: ThemeMode;
    /** 当前实际生效的浅色或深色。 */
    resolved: ResolvedTheme;
    setMode: (mode: ThemeMode) => void;
    toggle: () => void;
};

const ThemeContext = createContext<ThemeValue | null>(null);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
    const { appearance } = useSiteAppearance();
    const [storedMode, setStoredMode] = useState(readTheme);
    const [systemMode, setSystemMode] = useState<ResolvedTheme>(systemTheme);
    const mode = storedMode ?? appearance.defaultTheme;
    const resolved = mode === "system" ? systemMode : mode;

    useEffect(() => {
        const query = window.matchMedia(DARK_SCHEME);
        const update = () => setSystemMode(query.matches ? "dark" : "light");
        update();
        query.addEventListener("change", update);
        return () => query.removeEventListener("change", update);
    }, []);

    useEffect(() => {
        applyTheme(resolved);
    }, [resolved]);

    function setMode(next: ThemeMode) {
        setStoredMode(next);
        try {
            localStorage.setItem(THEME_STORAGE_KEY, next);
        } catch {
            // 忽略无法持久化的场景
        }
    }

    return (
        <ThemeContext.Provider
            value={{
                mode,
                resolved,
                setMode,
                toggle: () => setMode(resolved === "dark" ? "light" : "dark"),
            }}
        >
            {children}
        </ThemeContext.Provider>
    );
}

export function useTheme() {
    const value = useContext(ThemeContext);
    if (!value) throw new Error("useTheme 必须在 ThemeProvider 内使用");
    return value;
}

export function ThemeToggle({ className }: { className?: string }) {
    const { mode, resolved, toggle } = useTheme();
    const dark = resolved === "dark";
    const label = dark ? "切换到浅色模式" : "切换到黑夜模式";
    return (
        <Button
            type="text"
            className={className}
            aria-label={label}
            title={mode === "system" ? `${label}（当前跟随系统）` : label}
            icon={dark ? <SunOutlined /> : <MoonOutlined />}
            onClick={toggle}
        />
    );
}
