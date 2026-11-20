import {
    CloseOutlined,
    MenuFoldOutlined,
    MenuUnfoldOutlined,
    SearchOutlined,
} from "@ant-design/icons";
import { Popover } from "antd";
import {
    useEffect,
    useRef,
    useState,
    type KeyboardEvent,
    type ReactNode,
} from "react";
import { NavLink, useLocation, useNavigate } from "react-router-dom";
import { searchNavigation } from "./navigationSearch";
import { Brand } from "./shared";

const compactLayout = "(max-width: 720px)";
const storageKey = "mesagent.sidebarCollapsed";

export type NavigationEntry = {
    path: string;
    label: string;
    group?: string;
    icon: ReactNode;
};

function readCollapsed() {
    try {
        return localStorage.getItem(storageKey) === "true";
    } catch {
        return false;
    }
}

function useNarrowLayout() {
    const [narrow, setNarrow] = useState(
        () => window.matchMedia(compactLayout).matches,
    );
    useEffect(() => {
        const media = window.matchMedia(compactLayout);
        const update = () => setNarrow(media.matches);
        media.addEventListener("change", update);
        return () => media.removeEventListener("change", update);
    }, []);
    return narrow;
}

export function Sidebar({ entries }: { entries: NavigationEntry[] }) {
    const narrow = useNarrowLayout();
    const [collapsed, setCollapsed] = useState(readCollapsed);
    const [drawer, setDrawer] = useState(false);
    const [popover, setPopover] = useState(false);
    const [query, setQuery] = useState("");
    const searchButton = useRef<HTMLButtonElement>(null);
    const toggleButton = useRef<HTMLButtonElement>(null);
    const searchInput = useRef<HTMLInputElement>(null);
    const location = useLocation();
    const navigate = useNavigate();
    const compact = narrow ? !drawer : collapsed;
    const expandedOnMobile = narrow && drawer;
    const matches = searchNavigation(entries, query);
    const groups = new Map<string | undefined, NavigationEntry[]>();
    for (const entry of entries) {
        if (!groups.has(entry.group)) groups.set(entry.group, []);
        groups.get(entry.group)?.push(entry);
    }

    useEffect(() => {
        setDrawer(false);
        setPopover(false);
        setQuery("");
    }, [location.key, narrow]);

    function toggle() {
        setPopover(false);
        setQuery("");
        if (narrow) {
            setDrawer(!drawer);
            return;
        }

        setCollapsed(!collapsed);
        try {
            localStorage.setItem(storageKey, String(!collapsed));
        } catch {
            // The current layout still works without browser storage.
        }
    }

    function dismiss() {
        setQuery("");
        setPopover(false);
        setDrawer(false);
    }

    function keyboard(event: KeyboardEvent<HTMLElement>) {
        if (event.key !== "Escape") return;
        if (popover) {
            setQuery("");
            setPopover(false);
            searchButton.current?.focus();
        } else if (expandedOnMobile) {
            dismiss();
            toggleButton.current?.focus();
        } else if (query) {
            setQuery("");
            searchInput.current?.focus();
        } else {
            return;
        }
        event.stopPropagation();
    }

    function links(items: NavigationEntry[]) {
        return items.map(({ path, label, icon }) => (
            <NavLink
                key={path}
                to={path}
                end={path === "/"}
                aria-label={label}
                title={label}
                onClick={dismiss}
            >
                {icon}
                <span className="sidebar-link-label">{label}</span>
            </NavLink>
        ));
    }

    const search = (
        <form
            className="sidebar-search"
            role="search"
            aria-label="导航搜索"
            onSubmit={(event) => {
                event.preventDefault();
                if (!matches.length) return;
                navigate(matches[0].path);
                dismiss();
            }}
        >
            <SearchOutlined />
            <input
                ref={searchInput}
                type="text"
                aria-label="搜索导航"
                placeholder="搜索导航"
                value={query}
                onChange={(event) => setQuery(event.target.value)}
            />
            {query && (
                <button
                    type="button"
                    className="sidebar-control"
                    aria-label="清空导航搜索"
                    onClick={() => {
                        setQuery("");
                        searchInput.current?.focus();
                    }}
                >
                    <CloseOutlined />
                </button>
            )}
        </form>
    );
    const results = matches.length ? (
        <div className="sidebar-group">{links(matches)}</div>
    ) : (
        <p className="sidebar-empty" role="status">
            没有匹配的菜单
        </p>
    );
    const toggleLabel = compact ? "展开导航" : "折叠导航";

    return (
        <>
            {expandedOnMobile && (
                <div
                    className="sidebar-backdrop"
                    aria-hidden="true"
                    onClick={dismiss}
                />
            )}
            <aside
                className={`left-sidebar${compact ? " is-compact" : ""}${expandedOnMobile ? " is-overlay" : ""}`}
                onKeyDown={keyboard}
            >
                <div className="sidebar-brand">
                    <Brand compact={compact} />
                    <button
                        ref={toggleButton}
                        type="button"
                        className="sidebar-control sidebar-toggle"
                        aria-label={toggleLabel}
                        aria-expanded={!compact}
                        title={toggleLabel}
                        onClick={toggle}
                    >
                        {compact ? (
                            <MenuUnfoldOutlined />
                        ) : (
                            <MenuFoldOutlined />
                        )}
                    </button>
                </div>
                {compact ? (
                    <Popover
                        placement="rightTop"
                        trigger="click"
                        open={popover}
                        onOpenChange={(open) => {
                            setPopover(open);
                            if (!open) setQuery("");
                        }}
                        afterOpenChange={(open) => {
                            if (open) searchInput.current?.focus();
                        }}
                        content={
                            <div
                                className="sidebar-search-popover"
                                onKeyDown={keyboard}
                            >
                                {search}
                                <nav
                                    className="sidebar-nav"
                                    aria-label="导航搜索结果"
                                >
                                    {results}
                                </nav>
                            </div>
                        }
                    >
                        <button
                            ref={searchButton}
                            type="button"
                            className="sidebar-control sidebar-search-trigger"
                            aria-label="搜索导航"
                            aria-expanded={popover}
                            title="搜索导航"
                        >
                            <SearchOutlined />
                        </button>
                    </Popover>
                ) : (
                    search
                )}
                <nav className="sidebar-nav" aria-label="主导航">
                    {!compact && query.trim()
                        ? results
                        : Array.from(groups, ([group, items]) => (
                              <div
                                  key={group ?? "/"}
                                  className="sidebar-group"
                                  role={group ? "group" : undefined}
                                  aria-label={group}
                              >
                                  {group && (
                                      <div className="side-label">{group}</div>
                                  )}
                                  {links(items)}
                              </div>
                          ))}
                </nav>
            </aside>
        </>
    );
}
