import {
    ArrowRightOutlined,
    ReloadOutlined,
    SearchOutlined,
} from "@ant-design/icons";
import { useQueryClient } from "@tanstack/react-query";
import {
    Badge,
    Button,
    Card,
    Empty,
    Input,
    Space,
    Tabs,
    Typography,
} from "antd";
import { useState, type ReactNode } from "react";
import { Link } from "react-router-dom";
import { type Me } from "./api";
import { roleLabels } from "./shared";
import { WorkbenchAlerts } from "./workbenchAlerts";
import { useWorkbenchLive } from "./workbenchLive";
import { WorkbenchOverview } from "./workbenchOverview";
import { WorkbenchReports, WorkbenchShifts } from "./workbenchReports";
import { WorkbenchSchedule } from "./workbenchSchedule";
import {
    has,
    useWorkbench,
    workbenchPermissions as permission,
    type WorkbenchSchemas,
} from "./workbenchShared";
import "./workbench.css";

export type WorkbenchMenuItem = {
    path: string;
    label: string;
    group?: string;
    icon: ReactNode;
};

const roleEntries: Record<string, string[]> = {
    SystemAdmin: [
        "/planning",
        "/production",
        "/execution",
        "/quality",
        "/equipment",
        "/settings/security-events",
    ],
    Planner: ["/planning", "/production", "/execution", "/traceability"],
    Operator: ["/execution", "/production", "/quality", "/traceability"],
    ProcessEngineer: [
        "/engineering",
        "/execution",
        "/traceability",
        "/planning",
    ],
    QualityEngineer: ["/quality", "/traceability", "/production", "/execution"],
    EquipmentEngineer: ["/equipment", "/execution", "/traceability"],
    Viewer: ["/traceability", "/planning", "/production"],
};

export function Dashboard({ me, menu }: { me: Me; menu: WorkbenchMenuItem[] }) {
    const [search, setSearch] = useState("");
    const [tab, setTab] = useState("overview");
    const client = useQueryClient();
    const sources = [
        permission.lots,
        permission.execution,
        permission.quality,
        permission.equipment,
    ];
    const canView = !!me.fab && sources.some((p) => has(me, p));
    const canMonitor =
        has(me, permission.execution) && has(me, permission.equipment);
    const canNotify = [
        permission.execution,
        permission.quality,
        permission.equipment,
    ].some((p) => has(me, p));
    const live = useWorkbenchLive(canView);
    const unread = useWorkbench<WorkbenchSchemas["WorkbenchAlerts"]>(
        "alerts",
        { mode: "notifications", pageSize: "1" },
        canView && canNotify,
    );
    const normalized = search.trim().toLocaleLowerCase();
    const preferred = new Set(
        (me.roles ?? []).flatMap((role) => roleEntries[role] ?? []),
    );
    const visible = normalized
        ? menu.filter((entry) =>
              `${entry.label} ${entry.group ?? ""} ${entry.path}`
                  .toLocaleLowerCase()
                  .includes(normalized),
          )
        : [
              ...Array.from(preferred).flatMap((path) =>
                  menu.filter((entry) => entry.path === path),
              ),
              ...menu.filter((entry) => !preferred.has(entry.path)),
          ].slice(0, 6);
    const tabs = [
        {
            key: "overview",
            label: "生产总览",
            children: <WorkbenchOverview me={me} />,
        },
        {
            key: "alerts",
            label: (
                <Badge
                    count={unread.data?.unread}
                    size="small"
                    offset={[10, 2]}
                >
                    待办与告警
                </Badge>
            ),
            children: <WorkbenchAlerts me={me} />,
        },
        {
            key: "history",
            label: "历史趋势",
            children: <WorkbenchReports me={me} />,
        },
        {
            key: "shifts",
            label: "班次与 OEE",
            children: <WorkbenchShifts me={me} />,
        },
        {
            key: "schedule",
            label: "排程日历",
            children: <WorkbenchSchedule me={me} />,
        },
    ].filter(
        (item) =>
            ((item.key !== "shifts" && item.key !== "schedule") ||
                canMonitor) &&
            (item.key !== "alerts" || canNotify) &&
            (item.key !== "history" ||
                sources.slice(0, 3).some((p) => has(me, p))),
    );

    return (
        <div className="workbench">
            <div className="workbench-header">
                <div>
                    <h1>你好，{me.displayName}</h1>
                    <p>
                        {me.fab
                            ? `${me.fab.name} · ${me.fab.timezone}${me.fab.isActive ? "" : " · 厂区已停用"}`
                            : "尚未分配厂区范围"}
                        {!!me.roles?.length &&
                            ` · ${me.roles.map((role) => roleLabels[role] || role).join(" / ")}`}
                    </p>
                </div>
                {canView && (
                    <Space wrap>
                        <Badge
                            status={
                                live === "实时更新" ? "success" : "processing"
                            }
                            text={live}
                        />
                        <Button
                            icon={<ReloadOutlined />}
                            onClick={() =>
                                void client.invalidateQueries({
                                    queryKey: ["workbench"],
                                })
                            }
                        >
                            刷新
                        </Button>
                    </Space>
                )}
            </div>
            <Card size="small" className="workbench-launch">
                <div className="workbench-launch-heading">
                    <Typography.Title level={2}>快捷入口</Typography.Title>
                    <Input
                        aria-label="搜索菜单"
                        placeholder="搜索菜单"
                        prefix={<SearchOutlined />}
                        value={search}
                        allowClear
                        onChange={(event) => setSearch(event.target.value)}
                    />
                </div>
                {visible.length === 0 ? (
                    <Empty
                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                        description="没有匹配的菜单"
                    />
                ) : (
                    <div className="workbench-links">
                        {visible.map(({ path, label, icon }) => (
                            <Link
                                to={path}
                                key={path}
                                aria-label={`快捷入口 ${label}`}
                            >
                                {icon}
                                <span>{label}</span>
                                <ArrowRightOutlined className="workbench-link-arrow" />
                            </Link>
                        ))}
                    </div>
                )}
            </Card>
            {canView && (
                <Tabs
                    activeKey={tab}
                    onChange={setTab}
                    items={tabs}
                    destroyOnHidden
                    className="workbench-tabs"
                />
            )}
        </div>
    );
}
