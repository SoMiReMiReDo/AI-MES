import {
    createContext,
    useContext,
    useEffect,
    useState,
    type ReactNode,
} from "react";

export type ThemeMode = "light" | "dark" | "system";

const themeModes: readonly string[] = ["light", "dark", "system"];

export function isThemeMode(value: unknown): value is ThemeMode {
    return typeof value === "string" && themeModes.includes(value);
}

export type SiteAppearance = {
    name: string;
    subtitle: string;
    defaultTheme: ThemeMode;
};

const defaults: SiteAppearance = {
    name: "MESagent",
    subtitle: "MANUFACTURING SYSTEM",
    defaultTheme: "light",
};

const SiteContext = createContext({
    appearance: defaults,
    updateAppearance: (_appearance: SiteAppearance) => {},
});

export function SiteAppearanceProvider({ children }: { children: ReactNode }) {
    const [appearance, updateAppearance] = useState(defaults);

    useEffect(() => {
        const request = new AbortController();
        void fetch("/api/v1/site/appearance", {
            credentials: "omit",
            signal: request.signal,
        })
            .then(async (response) => {
                if (!response.ok) return;
                const value: SiteAppearance = await response.json();
                if (
                    !request.signal.aborted &&
                    typeof value?.name === "string" &&
                    typeof value.subtitle === "string" &&
                    isThemeMode(value.defaultTheme)
                ) {
                    updateAppearance(value);
                }
            })
            .catch(() => {});
        return () => request.abort();
    }, []);

    useEffect(() => {
        document.title = appearance.name;
    }, [appearance.name]);

    return (
        <SiteContext.Provider value={{ appearance, updateAppearance }}>
            {children}
        </SiteContext.Provider>
    );
}

export function useSiteAppearance() {
    return useContext(SiteContext);
}
