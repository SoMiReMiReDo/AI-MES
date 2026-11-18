import {
    ApartmentOutlined,
    AppstoreOutlined,
    AuditOutlined,
    DashboardOutlined,
    DesktopOutlined,
    ImportOutlined,
    InboxOutlined,
    PlayCircleOutlined,
    ScheduleOutlined,
    SearchOutlined,
    SettingOutlined,
    LogoutOutlined,
    SafetyCertificateOutlined,
    TeamOutlined,
    UserOutlined,
} from "@ant-design/icons";
import { QueryClientProvider } from "@tanstack/react-query";
import {
    App as AntApp,
    Button,
    ConfigProvider,
    Space,
    Spin,
    Tag,
    theme,
} from "antd";
import zhCN from "antd/locale/zh_CN";
import React, { useEffect, useState } from "react";
import { createRoot } from "react-dom/client";
import {
    BrowserRouter,
    Navigate,
    NavLink,
    Route,
    Routes,
    useLocation,
} from "react-router-dom";
import { api, refreshCsrf, type Me } from "./api";
import { Logs } from "./audit";
import { Login, PasswordForm } from "./auth";
import { Dashboard } from "./dashboard";
import { Organization, OrgTable } from "./organization";
import { Profile } from "./profile";
import { qc } from "./queryClient";
import { Roles } from "./roles";
import { Brand, Denied, setDisplayTimezone } from "./shared";
import { ThemeProvider, ThemeToggle, useTheme } from "./theme";
import "./style.css";
import { Manufacturing } from "./manufacturing";
import { Users } from "./users";
import { Production } from "./production";
import { Execution } from "./execution";
import { Equipment } from "./equipment";
import { Quality } from "./quality";
import { Materials } from "./materials";
import { Traceability, canTrace } from "./traceability";
import { clearTraceHistory } from "./traceHistory";
import { Integration } from "./integration";
import { integrationAccess } from "./integrationShared";

type SidebarLink = [
    path: string,
    name: string,
    icon: React.ReactNode,
    visible: boolean,
];

function Application() {
    const location = useLocation();
    const { message } = AntApp.useApp();
    const [me, setMe] = useState<Me | null>(null);
    const [loading, setLoading] = useState(true);
    const [expired, setExpired] = useState(false);
    const load = async () => {
        try {
            setMe(await api<Me>("auth/me"));
            await refreshCsrf();
        } catch {
            setMe(null);
            clearTraceHistory();
        } finally {
            setLoading(false);
        }
    };
    useEffect(() => {
        load();
        const clear = () => {
            setMe(null);
            setExpired(true);
            qc.clear();
            clearTraceHistory();
            refreshCsrf().catch(() => {});
        };
        window.addEventListener("session-invalid", clear);
        return () => window.removeEventListener("session-invalid", clear);
    }, []);
    const logout = async () => {
        try {
            await refreshCsrf();
            await api("auth/logout", "POST", {});
        } finally {
            setMe(null);
            qc.clear();
            clearTraceHistory();
            await refreshCsrf();
        }
    };
    if (loading)
        return (
            <div className="center">
                <Spin size="large" />
            </div>
        );
    if (!me)
        return (
            <Login
                expired={expired}
                onDone={async () => {
                    setExpired(false);
                    await refreshCsrf();
                    await load();
                }}
            />
        );
    if (me.passwordChangeRequired)
        return (
            <div className="login-scene">
                <div className="login-card">
                    <Brand />
                    <h2>设置你的正式密码</h2>
                    <p>初始密码仅用于验证身份。修改完成后请重新登录。</p>
                    <PasswordForm done={logout} />
                    <Button type="text" onClick={logout}>
                        退出登录
                    </Button>
                </div>
            </div>
        );
    setDisplayTimezone(me.fab?.timezone || "Asia/Shanghai");
    const can = (p: string) => me.permissions?.includes(p) ?? false;
    const integration = integrationAccess(me);
    const canIntegrate = integration.simulator || integration.imports;
    const canAudit =
        can("security.audit.read") ||
        can("audit.operations.read") ||
        can("security.outbox.read");
    const nav: { label?: string; links: SidebarLink[] }[] = [
        {
            links: [["/", "工作台", <DashboardOutlined />, true]],
        },
        {
            label: "生产运营",
            links: [
                [
                    "/planning",
                    "工单管理",
                    <ScheduleOutlined />,
                    can("planning.work-orders.read"),
                ],
                [
                    "/production",
                    "生产管理",
                    <AppstoreOutlined />,
                    [
                        "production.lots.read",
                        "production.wafers.read",
                        "production.carriers.read",
                    ].some(can),
                ],
                [
                    "/execution",
                    "生产执行",
                    <PlayCircleOutlined />,
                    can("execution.read"),
                ],
                [
                    "/quality",
                    "质量管理",
                    <SafetyCertificateOutlined />,
                    can("quality.read"),
                ],
                ["/traceability", "生产追溯", <SearchOutlined />, canTrace(me)],
            ],
        },
        {
            label: "生产准备",
            links: [
                [
                    "/engineering",
                    "工艺管理",
                    <SettingOutlined />,
                    can("engineering.read"),
                ],
                [
                    "/equipment",
                    "设备与资源",
                    <DesktopOutlined />,
                    can("equipment.read"),
                ],
                [
                    "/materials",
                    "硅片来料",
                    <InboxOutlined />,
                    can("materials.wafer-sources.read"),
                ],
                [
                    "/integration",
                    "数据导入与集成",
                    <ImportOutlined />,
                    canIntegrate,
                ],
            ],
        },
        {
            label: "组织与系统",
            links: [
                [
                    "/settings/organization",
                    "组织结构",
                    <ApartmentOutlined />,
                    can("org.structure.read"),
                ],
                [
                    "/settings/workers",
                    "员工",
                    <TeamOutlined />,
                    can("org.workers.read"),
                ],
                [
                    "/settings/workstations",
                    "工位",
                    <DesktopOutlined />,
                    can("org.workstations.read"),
                ],
                [
                    "/settings/terminals",
                    "终端",
                    <DesktopOutlined />,
                    can("org.terminals.read"),
                ],
                [
                    "/settings/users",
                    "账号与访问",
                    <UserOutlined />,
                    can("security.users.read"),
                ],
                [
                    "/settings/roles",
                    "角色与权限",
                    <SafetyCertificateOutlined />,
                    can("security.roles.read"),
                ],
                [
                    "/settings/security-events",
                    "审计与安全",
                    <AuditOutlined />,
                    canAudit,
                ],
                ["/account/profile", "个人设置", <UserOutlined />, true],
            ],
        },
    ];
    const activeLabel = nav
        .flatMap((group) => group.links)
        .find(([path]) => path === location.pathname)?.[1];

    return (
        <div className="shell">
            <aside>
                <Brand />
                <nav className="sidebar-nav" aria-label="主导航">
                    {nav.map(({ label, links }) => {
                        const visibleLinks = links.filter(
                            ([, , , visible]) => visible,
                        );
                        if (!visibleLinks.length) return null;
                        return (
                            <div
                                key={label ?? "/"}
                                className="sidebar-group"
                                role={label ? "group" : undefined}
                                aria-label={label}
                            >
                                {label && (
                                    <div className="side-label">{label}</div>
                                )}
                                {visibleLinks.map(([path, name, icon]) => (
                                    <NavLink
                                        key={path}
                                        to={path}
                                        end={path === "/"}
                                        aria-label={name}
                                        title={name}
                                    >
                                        {icon}
                                        <span>{name}</span>
                                    </NavLink>
                                ))}
                            </div>
                        );
                    })}
                </nav>
                <div className="side-bottom">
                    <span className="status-dot" /> 组织、身份与访问控制
                    <br />
                    <small>单厂部署 · 服务端权限校验</small>
                </div>
            </aside>
            <div className="workspace">
                <header>
                    <div>
                        <span className="breadcrumb">MESagent / </span>
                        {activeLabel ?? "基础管理"}
                    </div>
                    <Space size="middle">
                        <ThemeToggle />
                        <Tag color="red">{me.fab?.code || "未分配厂区"}</Tag>
                        <span>{me.displayName}</span>
                        <Button
                            type="text"
                            icon={<LogoutOutlined />}
                            onClick={logout}
                        >
                            退出
                        </Button>
                    </Space>
                </header>
                <main>
                    <Routes>
                        <Route
                            path="/traceability"
                            element={
                                canTrace(me) ? (
                                    <Traceability
                                        key={`${me.userId}/${me.fab?.id ?? ""}`}
                                        me={me}
                                    />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/quality"
                            element={
                                can("quality.read") ? (
                                    <Quality me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/execution"
                            element={
                                can("execution.read") ? (
                                    <Execution me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/equipment"
                            element={
                                can("equipment.read") ? (
                                    <Equipment me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/materials"
                            element={
                                can("materials.wafer-sources.read") ? (
                                    <Materials me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/integration"
                            element={
                                canIntegrate ? (
                                    <Integration me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/production"
                            element={
                                [
                                    "production.lots.read",
                                    "production.wafers.read",
                                    "production.carriers.read",
                                ].some(can) ? (
                                    <Production me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/engineering"
                            element={
                                can("engineering.read") ? (
                                    <Manufacturing key="engineering" me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/planning"
                            element={
                                can("planning.work-orders.read") ? (
                                    <Manufacturing
                                        key="planning"
                                        me={me}
                                        planning
                                    />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/"
                            element={
                                <Dashboard
                                    key={`${me.userId}/${me.fab?.id ?? ""}`}
                                    me={me}
                                    menu={nav.flatMap(
                                        ({ label: group, links }) =>
                                            links
                                                .filter(
                                                    ([path, , , visible]) =>
                                                        path !== "/" && visible,
                                                )
                                                .map(([path, label, icon]) => ({
                                                    path,
                                                    label,
                                                    icon,
                                                    group,
                                                })),
                                    )}
                                />
                            }
                        />
                        <Route
                            path="/settings/organization"
                            element={
                                can("org.structure.read") ? (
                                    <Organization me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        {["workers", "workstations", "terminals"].map((r) => (
                            <Route
                                key={r}
                                path={"/settings/" + r}
                                element={
                                    can(`org.${r}.read`) ? (
                                        <OrgTable
                                            key={r}
                                            resource={r}
                                            me={me}
                                        />
                                    ) : (
                                        <Denied />
                                    )
                                }
                            />
                        ))}
                        <Route
                            path="/settings/users"
                            element={
                                can("security.users.read") ? (
                                    <Users me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/settings/roles"
                            element={
                                can("security.roles.read") ? (
                                    <Roles me={me} />
                                ) : (
                                    <Denied />
                                )
                            }
                        />
                        <Route
                            path="/settings/security-events"
                            element={canAudit ? <Logs me={me} /> : <Denied />}
                        />
                        <Route
                            path="/account/profile"
                            element={
                                <Profile
                                    me={me}
                                    reload={load}
                                    logout={logout}
                                />
                            }
                        />
                        <Route path="*" element={<Navigate to="/" />} />
                    </Routes>
                </main>
                <footer>
                    MESagent / Semiconductor Manufacturing{" "}
                    <span>组织、身份与访问控制</span>
                </footer>
            </div>
        </div>
    );
}

function Root() {
    const { mode } = useTheme();
    const dark = mode === "dark";
    return (
        <ConfigProvider
            locale={zhCN}
            theme={{
                algorithm: dark ? theme.darkAlgorithm : theme.defaultAlgorithm,
                token: {
                    colorPrimary: dark ? "#e31837" : "#c42233",
                    colorInfo: dark ? "#a6a6ae" : "#6e6f76",
                    colorSuccess: dark ? "#46c48b" : "#2e8b5e",
                    colorWarning: dark ? "#f2b33d" : "#d98a12",
                    colorError: dark ? "#ff7a45" : "#b3261e",
                    borderRadius: 7,
                    fontFamily:
                        'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", "PingFang SC", sans-serif',
                    ...(dark
                        ? {
                              colorTextBase: "#ededef",
                              colorBgLayout: "#17181c",
                              colorBgContainer: "#1f2026",
                              colorBgElevated: "#23242b",
                              colorBorder: "#33343d",
                              colorBorderSecondary: "#2a2b33",
                          }
                        : {}),
                },
            }}
        >
            <AntApp>
                <QueryClientProvider client={qc}>
                    <BrowserRouter>
                        <Application />
                    </BrowserRouter>
                </QueryClientProvider>
            </AntApp>
        </ConfigProvider>
    );
}

createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <ThemeProvider>
            <Root />
        </ThemeProvider>
    </React.StrictMode>,
);
