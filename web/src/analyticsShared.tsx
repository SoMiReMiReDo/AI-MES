import { Alert, Card, Pagination, Space, Tag, Typography } from "antd";
import { useQuery } from "@tanstack/react-query";
import type { PropsWithChildren, ReactNode } from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api, type Me } from "./api";
import { fmt } from "./shared";
import {
    fabDate,
    metric,
    WorkbenchSelect,
    type WorkbenchOption,
    type WorkbenchSchemas,
} from "./workbenchShared";
import "./productionAnalytics.css";

export type AnalyticsSchemas = WorkbenchSchemas;
export type Evidence = AnalyticsSchemas["AnalyticsEvidence"];
export type Context = AnalyticsSchemas["AnalyticsContext"];

const missingLabels: Record<string, string> = {
    "incomplete-production-history": "历史生产事实待核验",
    "terminal-time-missing": "历史终态时间缺失",
    "quality-restricted": "报废数据受权限限制",
    "release-time-missing": "缺首次实际投放时间",
    "release-baseline-missing": "下达基线缺失",
    "lots-restricted": "批次数据受权限限制",
    "equipment-restricted": "设备数据受权限限制",
    "settlement-scope": "占比仅适用厂区或产品范围",
    "incomplete-settlement-history": "同期结案事实待核验",
    "no-settled-wafers": "期间结案片数为零",
    "no-shift": "无适用班次",
    "shift-not-started": "班次尚未开始",
    "no-run": "无实际 Run",
    "run-results-missing": "缺完整逐片结果",
    "ideal-rate-missing": "缺设备理想产速",
    "overlapping-shifts": "班次时段重叠",
};

export const reasonLabel = (code: string) => missingLabels[code] ?? code;

export const ratioText = (value?: number | null) =>
    value == null ? "—" : `${metric(value * 100, 1)}%`;

export function useAnalytics<T>(path: string, params?: URLSearchParams) {
    const address = `analytics/${path}${params?.size ? "?" + params : ""}`;
    return useQuery({
        queryKey: ["analytics", address],
        queryFn: () => api<T>(address),
        retry: false,
    });
}

export function useReportFilters(me: Me, days: number, keys: string[]) {
    const [query, setQuery] = useSearchParams();
    const filter = new URLSearchParams();
    for (const key of ["from", "to", ...keys]) {
        const value = query.get(key);
        if (value) filter.set(key, value);
    }
    const to = filter.get("to") ?? fabDate(me.fab?.timezone);
    const anchor = new Date(`${to}T00:00:00Z`);
    anchor.setUTCDate(anchor.getUTCDate() - days + 1);
    const from =
        filter.get("from") ??
        (Number.isFinite(anchor.getTime())
            ? anchor.toISOString().slice(0, 10)
            : fabDate(me.fab?.timezone, 1 - days));
    const update = (
        changes: Record<string, string | number | undefined>,
        replace = false,
    ) => {
        const next = new URLSearchParams(query);
        for (const [key, value] of Object.entries(changes)) {
            if (value) next.set(key, String(value));
            else next.delete(key);
        }
        setQuery(next, { replace });
    };
    const change = (key: string, value: string) =>
        update({
            [key]: value,
            page: undefined,
            shiftsPage: undefined,
            ...(key === "productId" ? { revisionId: undefined } : {}),
            ...(key === "areaId"
                ? { lineId: undefined, equipmentId: undefined }
                : {}),
            ...(key === "lineId" ? { equipmentId: undefined } : {}),
        });
    const page = (key = "page") => {
        const params = new URLSearchParams(filter);
        const current = query.get(key);
        if (current) params.set("page", current);
        return params;
    };
    return { query, filter, from, to, update, change, page };
}

export function ReportHeader({
    title,
    children,
    context,
    aside,
}: PropsWithChildren<{
    title: string;
    context?: Context;
    aside?: ReactNode;
}>) {
    return (
        <div className="analytics-heading">
            <div>
                <Typography.Title level={2}>{title}</Typography.Title>
                <div className="analytics-subtitle">{children}</div>
            </div>
            <Space size="middle" wrap>
                {context?.asOf && (
                    <span className="analytics-snapshot">
                        快照 {fmt(context.asOf)} · {context.timezone}
                    </span>
                )}
                {aside}
            </Space>
        </div>
    );
}

export function ReportDates({
    from,
    to,
    change,
}: {
    from: string;
    to: string;
    change: (key: string, value: string) => void;
}) {
    return (
        <>
            <label className="analytics-date">
                <span>开始日期</span>
                <input
                    type="date"
                    aria-label="开始日期"
                    value={from}
                    onChange={(e) => change("from", e.target.value)}
                />
            </label>
            <label className="analytics-date">
                <span>结束日期</span>
                <input
                    type="date"
                    aria-label="结束日期"
                    value={to}
                    onChange={(e) => change("to", e.target.value)}
                />
            </label>
        </>
    );
}

export function ReportSelect({
    label,
    field,
    options,
    filters,
}: {
    label: string;
    field: string;
    options?: WorkbenchOption[] | null;
    filters: ReturnType<typeof useReportFilters>;
}) {
    return (
        <WorkbenchSelect
            label={label}
            value={filters.query.get(field) ?? undefined}
            options={options ?? []}
            change={(value) => filters.change(field, value)}
        />
    );
}

export function ReportKpis({
    items,
}: {
    items: { label: string; value: ReactNode; detail?: ReactNode }[];
}) {
    return (
        <div className="analytics-kpis">
            {items.map(({ label, value, detail }) => (
                <Card key={label} size="small">
                    <div className="analytics-kpi-label">{label}</div>
                    <strong>{value}</strong>
                    {detail && (
                        <div className="analytics-kpi-detail">{detail}</div>
                    )}
                </Card>
            ))}
        </div>
    );
}

export function ReportPager({
    page,
    total,
    size,
    onChange,
}: {
    page?: number;
    total?: number;
    size?: number;
    onChange: (page: number) => void;
}) {
    return (
        <Pagination
            className="analytics-pager"
            size="small"
            current={page ?? 1}
            total={total ?? 0}
            pageSize={size ?? 20}
            showSizeChanger={false}
            showTotal={(value) => `共 ${value} 条`}
            onChange={onChange}
        />
    );
}

export function ReportError({ error }: { error?: Error | null }) {
    return error && <Alert type="error" showIcon title={error.message} />;
}

const sourceLabels: Record<string, string> = {
    "work-order": "工单追溯",
    lot: "批次追溯",
    wafer: "晶圆追溯",
    "quality-issue": "质量异常",
    "quality-scrap": "报废单",
    "equipment-shift": "班次监控",
    "process-run": "实际 Run",
    downtime: "停机详情",
    "wafer-origin": "硅片来料",
    "work-order-history": "工单履历",
};

export function SourceLinks({
    items,
    only,
}: {
    items?: Evidence[];
    only?: string[];
}) {
    return (
        <Space wrap size={[10, 4]}>
            {items
                ?.filter((entry) => !only || only.includes(entry.kind!))
                .map((entry) =>
                    entry.url ? (
                        <Link
                            key={entry.url}
                            to={entry.url}
                            title={
                                entry.occurredAt
                                    ? fmt(entry.occurredAt)
                                    : undefined
                            }
                        >
                            {sourceLabels[entry.kind!] ?? entry.kind}
                        </Link>
                    ) : null,
                )}
        </Space>
    );
}

export function MissingTags({ codes }: { codes?: string[] }) {
    return (
        <Space wrap size={[2, 4]}>
            {codes?.map((code) => (
                <Tag key={code}>{reasonLabel(code)}</Tag>
            ))}
        </Space>
    );
}
