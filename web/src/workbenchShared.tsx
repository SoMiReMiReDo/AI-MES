import { Alert, Select } from "antd";
import { useQuery } from "@tanstack/react-query";
import { Link } from "react-router-dom";
import { api, type Me } from "./api";
import { fmt } from "./shared";
import type { components } from "./generated/api";

// OpenAPI accepts numeric strings on input. Workbench responses serialize JSON numbers.
type WorkbenchJson<T> = [Extract<T, number>] extends [never]
    ? T extends object
        ? { [K in keyof T]: WorkbenchJson<T[K]> }
        : T
    : Exclude<T, string>;

export type WorkbenchSchemas = WorkbenchJson<components["schemas"]>;
export type WorkbenchOption = WorkbenchSchemas["WorkbenchOption"];
export type WorkbenchRecord = WorkbenchSchemas["WorkbenchRecord"];

export const workbenchPermissions = {
    lots: "production.lots.read",
    execution: "execution.read",
    equipment: "equipment.read",
    quality: "quality.read",
    schedule: "workbench.schedule.manage",
    rate: "workbench.oee.configure",
};

export const sourceNames: Record<string, string> = {
    issue: "质量异常",
    hold: "批次 Hold",
    disposition: "等待处置",
    downtime: "设备停机",
};

export const workbenchStatus: Record<string, string> = {
    Active: "有效",
    Open: "待处理",
    Acknowledged: "已受理",
    AwaitingDisposition: "等待处置",
    Running: "运行中",
    Waiting: "待加工",
    Idle: "空闲",
    Down: "停机",
    Maintenance: "维护",
    Unknown: "未知",
    Completed: "已完工",
    Passed: "通过",
    Failed: "不合格",
    Scrap: "报废",
    Planned: "已排程",
    Inactive: "到访已变更",
    Cancelled: "已取消",
    Conflict: "容量有变",
    Expired: "已过计划时段",
};

export const has = (me: Me, permission: string) =>
    me.permissions?.includes(permission) ?? false;

export function useWorkbench<T>(
    resource: string,
    params: Record<string, string> = {},
    enabled = true,
) {
    const path = `workbench/${resource}?${new URLSearchParams(params)}`;
    return useQuery({
        queryKey: ["workbench", path],
        queryFn: () => api<T>(path),
        enabled,
    });
}

export function WorkbenchError({ error }: { error: Error | null }) {
    return error && <Alert showIcon type="error" title={error.message} />;
}

export function WorkbenchSelect({
    label,
    value,
    options,
    change,
}: {
    label: string;
    value?: string;
    options?: WorkbenchOption[];
    change: (v: string) => void;
}) {
    return (
        <label className="workbench-filter">
            <span>{label}</span>
            <Select
                aria-label={label}
                allowClear
                showSearch={{ optionFilterProp: "label" }}
                placeholder="全部"
                value={value || undefined}
                options={options?.map((x) => ({
                    value: x.id,
                    label:
                        x.code && x.code !== x.name
                            ? `${x.code} · ${x.name}`
                            : x.name,
                }))}
                onChange={(next) => change(next ?? "")}
            />
        </label>
    );
}

export function fabDate(timezone?: string | null, offset = 0) {
    const parts = new Intl.DateTimeFormat("en-CA", {
        timeZone: timezone || "Asia/Shanghai",
        year: "numeric",
        month: "2-digit",
        day: "2-digit",
    }).formatToParts(new Date());
    const get = (key: string) => parts.find((part) => part.type === key)?.value;
    const today = new Date(
        `${get("year")}-${get("month")}-${get("day")}T00:00:00Z`,
    );
    today.setUTCDate(today.getUTCDate() + offset);
    return today.toISOString().slice(0, 10);
}

export function asLocalInput(value: Date | string) {
    const date = new Date(value);
    return new Date(date.getTime() - date.getTimezoneOffset() * 60000)
        .toISOString()
        .slice(0, 16);
}

export function metric(value?: number | null, places = 0) {
    return value == null
        ? "—"
        : new Intl.NumberFormat("zh-CN", {
              maximumFractionDigits: places,
          }).format(value);
}

export function sourceLink(me: Me, row: WorkbenchRecord) {
    if (
        row.kind === "issue" ||
        row.kind === "hold" ||
        row.kind === "failed-checks"
    ) {
        const view = {
            issue: "issues",
            hold: "holds",
            "failed-checks": "checks",
        }[row.kind];
        return `/quality?view=${view}&record=${row.id}`;
    }
    if (row.kind === "downtime")
        return `/equipment?view=downtimes&record=${row.id}`;
    if (row.kind === "equipment" || row.kind === "down")
        return `/equipment?view=tools&record=${row.id}`;
    if (
        row.kind === "running" ||
        row.kind === "disposition" ||
        row.kind === "wait"
    )
        return `/execution?run=${row.id}`;
    if (
        row.kind === "completed" &&
        row.lotId &&
        has(me, "production.wafers.read")
    )
        return `/traceability?kind=lot&id=${row.lotId}&event=wafer-completed&source=${row.id}`;
    if (row.runId && has(me, workbenchPermissions.execution))
        return `/execution?run=${row.runId}`;
    if (row.kind === "waiting" && row.lotId)
        return `/execution?lot=${row.lotId}`;
    if (row.lotId && has(me, workbenchPermissions.lots)) {
        if (row.kind === "scrap")
            return `/traceability?kind=lot&id=${row.lotId}&event=disposition&source=${row.id}`;
        return `/production?view=lots&detail=${row.lotId}`;
    }
}

export function RecordLabel({ me, row }: { me: Me; row: WorkbenchRecord }) {
    const link = sourceLink(me, row);
    return link ? <Link to={link}>{row.label}</Link> : <span>{row.label}</span>;
}

export function TimeCell({ value }: { value?: string | null }) {
    return <time dateTime={value ?? undefined}>{fmt(value ?? undefined)}</time>;
}
