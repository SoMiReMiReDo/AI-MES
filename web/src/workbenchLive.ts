import {
    HubConnectionBuilder,
    HubConnectionState,
    LogLevel,
} from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useEffect, useState } from "react";
import { api, ApiError, refreshCsrf } from "./api";

export function useWorkbenchLive(enabled: boolean) {
    const client = useQueryClient();
    const [status, setStatus] = useState("连接中");
    useEffect(() => {
        if (!enabled) return;
        let stopped = false;
        let timer: ReturnType<typeof setTimeout> | undefined;
        let connection: ReturnType<HubConnectionBuilder["build"]> | undefined;
        let subscription: { dispose: () => void } | undefined;
        const refresh = () =>
            client.invalidateQueries({ queryKey: ["workbench"] });
        const watch = () => {
            if (stopped || !connection) return;
            subscription?.dispose();
            subscription = connection.stream<boolean>("Watch").subscribe({
                next: () => {
                    setStatus("实时更新");
                    void refresh();
                },
                error: () => {
                    if (
                        !stopped &&
                        connection?.state === HubConnectionState.Connected
                    )
                        void reset();
                },
                complete: () => {
                    if (
                        !stopped &&
                        connection?.state === HubConnectionState.Connected
                    )
                        void reset();
                },
            });
        };
        async function reset() {
            if (stopped || timer) return;
            setStatus("正在重连");
            timer = setTimeout(() => {
                timer = undefined;
                void start();
            }, 4000);
            try {
                await api("workbench/overview");
            } catch (error) {
                if (error instanceof ApiError && error.status === 403) {
                    setStatus("实时权限不可用");
                    clearTimeout(timer);
                }
            }
        }
        async function start() {
            if (stopped) return;
            try {
                if (
                    connection &&
                    connection.state !== HubConnectionState.Disconnected
                )
                    await connection.stop();
                const token = await refreshCsrf();
                if (stopped) return;
                connection = new HubConnectionBuilder()
                    .withUrl("/api/v1/workbench/live", {
                        headers: { "X-CSRF-TOKEN": token },
                    })
                    .withAutomaticReconnect([0, 1000, 3000, 8000])
                    .configureLogging(LogLevel.Error)
                    .build();
                connection.onreconnecting(() => {
                    setStatus("正在重连");
                });
                connection.onreconnected(() => {
                    clearTimeout(timer);
                    timer = undefined;
                    void refresh();
                    watch();
                });
                connection.onclose(() => {
                    if (!stopped) void reset();
                });
                await connection.start();
                if (stopped) {
                    await connection.stop();
                    return;
                }
                watch();
            } catch {
                if (!stopped) void reset();
            }
        }
        void start();
        return () => {
            stopped = true;
            clearTimeout(timer);
            subscription?.dispose();
            void connection?.stop().catch(() => {});
        };
    }, [client, enabled]);
    return status;
}
