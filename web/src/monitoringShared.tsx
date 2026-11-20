import { ReloadOutlined } from "@ant-design/icons";
import { Alert, Button } from "antd";
import { ApiError } from "./api";
import { fmt } from "./shared";
import { workbenchStatus } from "./workbenchShared";
import "./productionMonitoring.css";

export const equipmentStates = [
    "Running",
    "Idle",
    "Down",
    "Maintenance",
    "Offline",
    "Unknown",
] as const;

export function stateName(state: string) {
    return state === "Offline" ? "离线" : (workbenchStatus[state] ?? state);
}

export const percent = (value?: number | null) =>
    value == null
        ? "—"
        : new Intl.NumberFormat("zh-CN", {
              style: "percent",
              maximumFractionDigits: 1,
          }).format(value);

export const number = (value: number) =>
    value.toLocaleString("zh-CN", { maximumFractionDigits: 2 });

export function duration(minutes: number) {
    const total = Math.max(0, Math.floor(minutes));
    if (total >= 60) return `${Math.floor(total / 60)} 小时 ${total % 60} 分`;
    return total ? `${total} 分钟` : minutes > 0 ? "< 1 分钟" : "0 分钟";
}

export function localTime(value: string, timezone?: string) {
    return new Date(value).toLocaleTimeString("zh-CN", {
        timeZone: timezone ?? "Asia/Shanghai",
        hour: "2-digit",
        minute: "2-digit",
        hourCycle: "h23",
    });
}

export const monitoringAccessLost = (error: Error | null) =>
    error instanceof ApiError && [401, 403, 404].includes(error.status);

export function MonitorState({ state }: { state: string }) {
    return (
        <span className="monitor-status-chip" data-state={state}>
            <i aria-hidden="true" />
            {stateName(state)}
        </span>
    );
}

export function MonitorRefresh({
    live,
    asOf,
    syncing,
    error,
    refresh,
}: {
    live: string;
    asOf?: string;
    syncing: boolean;
    error: Error | null;
    refresh: () => void;
}) {
    const push = live === "实时更新";
    return (
        <div className="monitor-refresh">
            <div className="monitor-refresh-meta">
                <span
                    className="monitor-refresh-state"
                    data-live={push && !error}
                >
                    <i aria-hidden="true" />
                    {error ? "刷新失败" : push ? "实时更新" : "轮询同步"}
                </span>
                {asOf && <time dateTime={asOf}>{fmt(asOf)}</time>}
            </div>
            <Button
                icon={<ReloadOutlined />}
                onClick={refresh}
                loading={syncing}
            >
                刷新
            </Button>
        </div>
    );
}

export function MonitorError({
    error,
    hasSnapshot,
}: {
    error: Error | null;
    hasSnapshot: boolean;
}) {
    if (!error) return null;
    return (
        <Alert
            showIcon
            type={hasSnapshot ? "warning" : "error"}
            title={hasSnapshot ? "刷新失败，正在显示上次快照" : error.message}
            description={hasSnapshot ? error.message : undefined}
        />
    );
}
