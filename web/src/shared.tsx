import { useQuery } from "@tanstack/react-query";
import { Alert, Select } from "antd";
import React from "react";
import { api, type Page, type Row } from "./api";

export const labels: Record<string, string> = {
    fabs: "厂区",
    areas: "区域",
    lines: "产线",
    locations: "位置",
    workers: "员工",
    workstations: "工位",
    terminals: "终端",
};

export const roleLabels: Record<string, string> = {
    SystemAdmin: "系统管理员",
    Operator: "操作员",
    ProcessEngineer: "工艺工程师",
    QualityEngineer: "质量工程师",
    Planner: "计划员",
    EquipmentEngineer: "设备工程师",
    Viewer: "只读用户",
};

let displayTimezone = "Asia/Shanghai";

export const fmt = (v?: string) =>
    v
        ? new Date(v).toLocaleString("zh-CN", { timeZone: displayTimezone })
        : "—";

export function setDisplayTimezone(value: string) {
    displayTimezone = value;
}

export function Brand() {
    return (
        <div className="brand">
            <div className="brand-mark">M</div>
            <div>
                MESagent<small>MANUFACTURING SYSTEM</small>
            </div>
        </div>
    );
}

export function Heading({
    eyebrow,
    title,
    description,
    extra,
}: {
    eyebrow: string;
    title: string;
    description: string;
    extra?: React.ReactNode;
}) {
    return (
        <div className="page-heading">
            <div>
                <div className="eyebrow">{eyebrow}</div>
                <h1>{title}</h1>
                <p>{description}</p>
            </div>
            {extra}
        </div>
    );
}

export function Denied() {
    return (
        <Alert
            type="warning"
            showIcon
            message="没有访问权限"
            description="请联系管理员配置角色与厂区范围。"
        />
    );
}

export function RemoteSelect({
    resource,
    ...props
}: {
    resource: string;
    [key: string]: any;
}) {
    const { data } = useQuery({
        queryKey: ["options", resource],
        queryFn: async () => {
            let items: Row[] = [];
            for (let page = 1; page <= 100; page++) {
                const r = await api<Page>(
                    `${resource}?pageSize=100&page=${page}`,
                );
                items = items.concat(r.items);
                if (items.length >= r.total) break;
            }
            return items;
        },
    });

    return (
        <Select
            showSearch
            optionFilterProp="label"
            allowClear
            options={(data || []).map((x) => ({
                value: x.id,
                label: `${x.code || x.username || ""} ${roleLabels[x.name || ""] || x.name || x.displayName || ""}${x.isActive === false ? "（停用）" : ""}`,
            }))}
            {...props}
        />
    );
}

export const permissionLabels: Record<string, string> = {
    "org.structure.read": "组织结构 · 查询",
    "org.structure.manage": "组织结构 · 维护",
    "org.workers.read": "员工 · 查询",
    "org.workers.manage": "员工 · 维护",
    "org.workstations.read": "工位 · 查询",
    "org.workstations.manage": "工位 · 维护",
    "org.terminals.read": "终端 · 查询",
    "org.terminals.manage": "终端 · 维护",
    "security.users.read": "账号 · 查询",
    "security.users.manage": "账号 · 管理",
    "security.users.reset-password": "账号 · 重置密码",
    "security.sessions.revoke": "会话 · 撤销",
    "security.roles.read": "角色 · 查询",
    "security.roles.manage": "角色 · 管理",
    "security.assignments.manage": "用户角色与范围 · 授权",
    "security.audit.read": "审计与安全 · 查询",
};
