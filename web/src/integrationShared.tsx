import { useRef, useState } from "react";
import { Tag } from "antd";
import { api, type Me } from "./api";
import type { components } from "./generated/api";

export type IntegrationSchema = components["schemas"];
export type IntegrationPage<T> = { items: T[]; total: number | string };
export const integrationPageSize = 20;

export function integrationAccess(me: Me) {
    const can = (permission: string) =>
        me.permissions?.includes(permission) ?? false;
    return {
        simulator: can("integration.simulator.read") && can("equipment.read"),
        publish: can("equipment.simulate"),
        imports:
            can("integration.work-orders.import") &&
            can("planning.work-orders.manage"),
        planning: can("planning.work-orders.read"),
    };
}

const labels: Record<string, [string, string]> = {
    Applied: ["已应用", "green"],
    Rejected: ["已拒绝", "red"],
    Online: ["在线", "green"],
    Offline: ["离线", "orange"],
    Unknown: ["未知", "default"],
    Ready: ["预检通过", "blue"],
    Imported: ["已导入", "green"],
};

export function IntegrationStatus({ status }: { status: string }) {
    const [label, color] = labels[status] ?? [status, "default"];
    return <Tag color={color}>{label}</Tag>;
}

export function integrationError(error: unknown) {
    return error instanceof Error ? error.message : "操作失败";
}

export function useIntegrationCommand() {
    const pending = useRef<{ signature: string; key: string }>(undefined);
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");

    async function send<T>(path: string, body: unknown, etag?: string) {
        const signature = JSON.stringify([path, body, etag]);
        if (pending.current?.signature !== signature)
            pending.current = { signature, key: crypto.randomUUID() };
        setBusy(true);
        setError("");
        try {
            const result = await api<T>(
                path,
                "POST",
                body,
                etag,
                pending.current.key,
            );
            pending.current = undefined;
            return result;
        } catch (e) {
            setError(integrationError(e));
            return undefined;
        } finally {
            setBusy(false);
        }
    }

    return { send, busy, error, setError };
}
