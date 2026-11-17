import { useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import { App, Select, Tag } from "antd";
import { api, ApiError } from "./api";

export type QualityItem = { id: string; etag: string; [key: string]: any };
export const required = [{ required: true, message: "请填写此项" }];
export const qualityNames: Record<string, string> = {
    Active: "在制",
    Registered: "已登记",
    Scrapped: "已报废",
    Open: "待确认",
    Acknowledged: "已确认",
    Resolved: "已处置",
    Closed: "已关闭",
    Ready: "等待加工",
    Running: "加工中",
    AwaitingDisposition: "等待处置",
    Waiting: "等待加工",
    Completed: "已完成",
    Aborted: "已中止",
    Verified: "已复核",
    Draft: "草稿",
    Released: "已发布",
    Passed: "通过",
    Failed: "不合格",
    Continue: "审核继续",
    Retry: "当前站重试",
    Scrap: "报废",
    None: "登记质量放行",
    Hold: "冻结",
    Disposition: "处置",
    Minor: "一般",
    Major: "严重",
    Critical: "紧急",
    Containment: "遏制措施",
    Corrective: "纠正措施",
    Preventive: "预防措施",
    Cancelled: "已取消",
    Merged: "合批耗尽",
    RetryAuthorized: "已授权重试",
};
export const qualityStatus = (value: string) => (
    <Tag
        color={
            ["Failed", "AwaitingDisposition"].includes(value)
                ? "orange"
                : ["Passed", "Verified", "Released"].includes(value)
                  ? "green"
                  : undefined
        }
    >
        {qualityNames[value] || value}
    </Tag>
);
export const choice = (values: string[]) =>
    values.map((value) => ({ value, label: qualityNames[value] || value }));
export const qualityOptions = (items?: QualityItem[]) =>
    (items || []).map((x) => ({
        value: x.id,
        label: [
            x.code ||
                x.parameterCode ||
                x.lotNumber ||
                x.orderNo ||
                x.userName ||
                "",
            x.name || x.title || "",
            x.version ? `v${x.version}` : "",
        ]
            .filter(Boolean)
            .join(" · "),
    }));

export function templateSpecificationIds(parameters?: string): string[] {
    try {
        const value: unknown = JSON.parse(parameters || "[]");
        return Array.isArray(value) && value.every((x) => typeof x === "string")
            ? value
            : [];
    } catch {
        return [];
    }
}

export function useQualityOptions(resource: string) {
    return useQuery<{ items: QualityItem[] }>({
        queryKey: ["quality", "options", resource],
        queryFn: async () => {
            let items: QualityItem[] = [];
            for (let page = 1; page <= 100; page++) {
                const result = await api<{
                    items: QualityItem[];
                    total?: number;
                }>(
                    `quality/${resource}${resource.includes("?") ? "&" : "?"}pageSize=100&page=${page}`,
                );
                items = items.concat(result.items);
                if (result.total === undefined || items.length >= result.total)
                    break;
            }
            return { items };
        },
    });
}

export function QualitySelect({
    resource,
    ...props
}: {
    resource: string;
    [key: string]: any;
}) {
    const { data, isLoading, error } = useQualityOptions(resource);
    return (
        <Select
            showSearch
            allowClear
            optionFilterProp="label"
            loading={isLoading}
            status={error ? "error" : undefined}
            options={qualityOptions(data?.items)}
            {...props}
        />
    );
}

export function useQualityCommand() {
    const { message } = App.useApp();
    const client = useQueryClient();
    const [busy, setBusy] = useState(false);
    const pending = useRef<{ signature: string; key: string } | null>(null);
    const send = async (
        path: string,
        body: unknown,
        etag?: string,
        method = "POST",
    ) => {
        const signature = JSON.stringify([path, method, body, etag]);
        if (pending.current?.signature !== signature)
            pending.current = { signature, key: crypto.randomUUID() };
        setBusy(true);
        try {
            const result = await api(
                path,
                method,
                body,
                etag,
                pending.current.key,
            );
            pending.current = null;
            await client.invalidateQueries();
            message.success({ content: "操作成功", key: "quality-command" });
            return result;
        } catch (error) {
            const text = (error as Error).message;
            message.error(
                error instanceof ApiError &&
                    [409, 412, 428].includes(error.status)
                    ? `${text}；请刷新并重新核对`
                    : text,
            );
            throw error;
        } finally {
            setBusy(false);
        }
    };
    return { send, busy };
}
