import {
    ApartmentOutlined,
    AppstoreOutlined,
    AuditOutlined,
    DesktopOutlined,
    LogoutOutlined,
    SafetyCertificateOutlined,
    TeamOutlined,
    UserOutlined,
} from "@ant-design/icons";
import { QueryClientProvider } from "@tanstack/react-query";
import { App as AntApp, Button, ConfigProvider, Space, Spin, Tag } from "antd";
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
import "./style.css";
import { Users } from "./users";

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
    const nav = [
        ["/", "工作台", <AppstoreOutlined />, true],
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
            can("security.audit.read"),
        ],
        ["/account/profile", "个人设置", <UserOutlined />, true],
    ] as const;

    return (
        <div className="shell">
            <aside>
                <Brand />
                <div className="side-label">制造基础平台</div>
                <nav>
                    {nav
                        .filter((x) => x[3])
                        .map(([path, name, icon]) => (
                            <NavLink key={path} to={path} end={path === "/"}>
                                {icon}
                                <span>{name}</span>
                            </NavLink>
                        ))}
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
                        {nav.find((x) => x[0] === location.pathname)?.[1] ||
                            "基础管理"}
                    </div>
                    <Space size="middle">
                        <Tag color="cyan">{me.fab?.code || "未分配厂区"}</Tag>
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
                        <Route path="/" element={<Dashboard me={me} />} />
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
                            element={
                                can("security.audit.read") ? (
                                    <Logs />
                                ) : (
                                    <Denied />
                                )
                            }
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

createRoot(document.getElementById("root")!).render(
    <React.StrictMode>
        <ConfigProvider
            locale={zhCN}
            theme={{
                token: {
                    colorPrimary: "#17625c",
                    borderRadius: 7,
                    fontFamily:
                        'Inter, -apple-system, BlinkMacSystemFont, "Segoe UI", "PingFang SC", sans-serif',
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
    </React.StrictMode>,
);
