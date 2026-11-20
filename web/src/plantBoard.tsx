import {
    ArrowRightOutlined,
    FullscreenExitOutlined,
    FullscreenOutlined,
    SearchOutlined,
} from "@ant-design/icons";
import { Button, Empty, Input, Pagination, Select, Skeleton } from "antd";
import { useState } from "react";
import { Link, useSearchParams } from "react-router-dom";
import type { Me } from "./api";
import {
    duration,
    equipmentStates,
    localTime,
    MonitorError,
    MonitorRefresh,
    MonitorState,
    monitoringAccessLost,
    number,
    percent,
    stateName,
} from "./monitoringShared";
import { useWorkbenchLive } from "./workbenchLive";
import {
    has,
    useWorkbench,
    workbenchPermissions,
    type WorkbenchSchemas,
} from "./workbenchShared";

type Equipment = WorkbenchSchemas["WorkbenchMonitorEquipment"];

const PAGE_SIZE = 24;

function EquipmentCard({
    equipment,
    asOf,
    timezone,
    showRuns,
}: {
    equipment: Equipment;
    asOf: string;
    timezone?: string;
    showRuns: boolean;
}) {
    const {
        id,
        code,
        name,
        area,
        line,
        state,
        stateSince,
        currentStop,
        activeRun,
        shift,
        metrics,
    } = equipment;
    const downSince = currentStop?.startsAt ?? stateSince;
    const downtime = downSince
        ? (Date.parse(asOf) - Date.parse(downSince)) / 60_000
        : null;
    return (
        <Link
            className="monitor-equipment"
            data-state={state}
            to={`/monitoring/${id}`}
            aria-label={`${code} ${name}，${stateName(state)}，查看班次监控`}
        >
            <div className="monitor-equipment-top">
                <div className="monitor-equipment-identity">
                    <span className="monitor-equipment-code">{code}</span>
                    <h2>{name}</h2>
                    <p>{[area, line].filter(Boolean).join(" · ")}</p>
                </div>
                <MonitorState state={state} />
            </div>
            {state === "Down" && (
                <div className="monitor-stop-note">
                    <span>
                        {currentStop?.reason ??
                            (currentStop?.needsReason
                                ? "停机原因待补录"
                                : "停机原因未记录")}
                    </span>
                    {downtime != null && <strong>{duration(downtime)}</strong>}
                </div>
            )}
            {showRuns && (
                <div className="monitor-card-run">
                    <span>当前批次</span>
                    {activeRun ? (
                        <div>
                            <strong>{activeRun.lotNumber}</strong>
                            <span>
                                {[activeRun.orderNo, activeRun.step]
                                    .filter(Boolean)
                                    .join(" · ")}
                            </span>
                        </div>
                    ) : (
                        <strong>暂无 Run</strong>
                    )}
                </div>
            )}
            <div className="monitor-equipment-shift">
                <span>
                    {shift
                        ? `${shift.label} · ${localTime(shift.startsAt!, timezone)}–${localTime(shift.endsAt!, timezone)}`
                        : "当前无适用班次"}
                </span>
                <ArrowRightOutlined />
            </div>
            {showRuns && (
                <dl
                    className="monitor-card-metrics"
                    title={metrics?.missing ?? undefined}
                >
                    {(
                        [
                            ["OEE", metrics?.oee],
                            ["可用率", metrics?.availability],
                            ["性能率", metrics?.performance],
                            ["良品率", metrics?.quality],
                        ] as const
                    ).map(([label, value]) => (
                        <div key={label}>
                            <dt>{label}</dt>
                            <dd>{percent(value)}</dd>
                        </div>
                    ))}
                </dl>
            )}
        </Link>
    );
}

export function PlantBoard({ me }: { me: Me }) {
    const [params, setParams] = useSearchParams();
    const area = params.get("areaId") ?? "";
    const state = params.get("state") ?? "";
    const keyword = params.get("q") ?? "";
    const page = Number(params.get("page") ?? 1) || 1;
    const kiosk = params.get("kiosk") === "1";
    const [draft, setDraft] = useState({ keyword, value: keyword });
    const search = draft.keyword === keyword ? draft.value : keyword;
    const live = useWorkbenchLive(true);
    const board = useWorkbench<WorkbenchSchemas["WorkbenchPlantBoard"]>(
        "plant-board",
        {
            areaId: area,
            state,
            q: keyword,
            page: String(page),
            pageSize: String(PAGE_SIZE),
        },
        true,
        live === "实时更新" ? false : 30_000,
    );
    const data = monitoringAccessLost(board.error) ? undefined : board.data;
    const counts = new Map(data?.counts?.map((row) => [row.state, row.count]));
    const update = (values: Record<string, string>, resetPage = false) => {
        const next = new URLSearchParams(params);
        if (resetPage) next.delete("page");
        Object.entries(values).forEach(([key, value]) =>
            value ? next.set(key, value) : next.delete(key),
        );
        setParams(next);
    };

    return (
        <div className="production-monitor">
            <div className="monitor-hero">
                <div>
                    {me.fab?.name && (
                        <p className="monitor-fab">{me.fab.name}</p>
                    )}
                    <h1>厂区看板</h1>
                    <p className="monitor-subtitle">
                        查看当前设备状态与班次表现
                    </p>
                </div>
                <div className="monitor-hero-actions">
                    <MonitorRefresh
                        live={live}
                        asOf={data?.asOf}
                        syncing={board.isFetching}
                        error={board.error}
                        refresh={() => void board.refetch()}
                    />
                    <Button
                        icon={
                            kiosk ? (
                                <FullscreenExitOutlined />
                            ) : (
                                <FullscreenOutlined />
                            )
                        }
                        onClick={() => update({ kiosk: kiosk ? "" : "1" })}
                    >
                        {kiosk ? "退出大屏" : "大屏模式"}
                    </Button>
                </div>
            </div>
            <div
                className="monitor-counters"
                role="group"
                aria-label="按设备状态筛选"
            >
                {["", ...equipmentStates].map((key) => (
                    <button
                        type="button"
                        key={key}
                        data-state={key || "All"}
                        aria-pressed={key === state}
                        onClick={() => update({ state: key }, true)}
                    >
                        <span>
                            <i aria-hidden="true" />
                            {key ? stateName(key) : "全部设备"}
                        </span>
                        <strong>
                            {data
                                ? number(
                                      key
                                          ? (counts.get(key) ?? 0)
                                          : [...counts.values()].reduce(
                                                (a, b) => a + (b ?? 0),
                                                0,
                                            ),
                                  )
                                : "—"}
                        </strong>
                    </button>
                ))}
            </div>
            <MonitorError error={board.error} hasSnapshot={!!data} />
            <div className="monitor-tools">
                <div className="monitor-results-heading">
                    <h2>{state ? `${stateName(state)}设备` : "设备列表"}</h2>
                    {data && (
                        <span>{number(data.equipment?.total ?? 0)} 台</span>
                    )}
                </div>
                <div className="monitor-filters">
                    <Select
                        aria-label="筛选区域"
                        placeholder="全部区域"
                        allowClear
                        value={area || undefined}
                        options={data?.areas.map((item) => ({
                            value: item.id,
                            label: item.name,
                        }))}
                        onChange={(value) =>
                            update({ areaId: value ?? "" }, true)
                        }
                    />
                    <form
                        className="monitor-search"
                        onSubmit={(event) => {
                            event.preventDefault();
                            update({ q: search.trim() }, true);
                        }}
                    >
                        <Input
                            aria-label="设备编号或名称"
                            placeholder="设备编号或名称"
                            allowClear
                            maxLength={100}
                            value={search}
                            onChange={(event) => {
                                const value = event.target.value;
                                setDraft({ keyword, value });
                                if (!value && keyword) update({ q: "" }, true);
                            }}
                        />
                        <Button
                            htmlType="submit"
                            icon={<SearchOutlined />}
                            aria-label="搜索设备"
                        />
                    </form>
                </div>
            </div>
            {board.isPending && <Skeleton active />}
            {data &&
                (data.equipment?.items?.length ? (
                    <div className="monitor-card-grid">
                        {data.equipment.items.map((item) => (
                            <EquipmentCard
                                key={item.id}
                                equipment={item}
                                asOf={data.asOf!}
                                timezone={me.fab?.timezone ?? undefined}
                                showRuns={has(
                                    me,
                                    workbenchPermissions.execution,
                                )}
                            />
                        ))}
                    </div>
                ) : (
                    <Empty
                        className="monitor-empty"
                        image={Empty.PRESENTED_IMAGE_SIMPLE}
                        description="暂无符合条件的设备"
                    />
                ))}
            {data?.equipment && (data.equipment.total ?? 0) > PAGE_SIZE && (
                <Pagination
                    className="monitor-pagination"
                    current={page}
                    pageSize={PAGE_SIZE}
                    total={data.equipment.total}
                    showSizeChanger={false}
                    onChange={(next) =>
                        update({ page: next === 1 ? "" : String(next) })
                    }
                />
            )}
        </div>
    );
}
