import { MoonOutlined, SunOutlined } from "@ant-design/icons";
import { Button } from "antd";
import React, { createContext, useContext, useEffect, useState } from "react";

export const THEME_STORAGE_KEY = "mesagent.theme";

export type ThemeMode = "light" | "dark";

const DARK_BACKGROUND = "#0c1719";
const LIGHT_BACKGROUND = "#163f43";

function readTheme(): ThemeMode {
    try {
        const stored = localStorage.getItem(THEME_STORAGE_KEY);
        if (stored === "light" || stored === "dark") return stored;
    } catch {
        // 隐私模式下 localStorage 不可用，使用默认浅色
    }
    return "light";
}

function applyTheme(mode: ThemeMode) {
    document.documentElement.dataset.theme = mode;
    document
        .querySelector('meta[name="theme-color"]')
        ?.setAttribute(
            "content",
            mode === "dark" ? DARK_BACKGROUND : LIGHT_BACKGROUND,
        );
}

// 模块加载即应用主题，避免首帧按浅色渲染
applyTheme(readTheme());

type ThemeValue = {
    mode: ThemeMode;
    setMode: (mode: ThemeMode) => void;
    toggle: () => void;
};

const ThemeContext = createContext<ThemeValue | null>(null);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
    const [mode, setMode] = useState<ThemeMode>(readTheme);

    useEffect(() => {
        applyTheme(mode);
        try {
            localStorage.setItem(THEME_STORAGE_KEY, mode);
        } catch {
            // 忽略无法持久化的场景
        }
    }, [mode]);

    return (
        <ThemeContext.Provider
            value={{
                mode,
                setMode,
                toggle: () => setMode((x) => (x === "dark" ? "light" : "dark")),
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
    const { mode, toggle } = useTheme();
    const dark = mode === "dark";
    return (
        <Button
            type="text"
            className={className}
            aria-label={dark ? "切换到浅色模式" : "切换到黑夜模式"}
            title={dark ? "切换到浅色模式" : "切换到黑夜模式"}
            icon={dark ? <SunOutlined /> : <MoonOutlined />}
            onClick={toggle}
        />
    );
}
