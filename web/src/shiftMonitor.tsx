import {
    ArrowLeftOutlined,
    ClockCircleOutlined,
    LeftOutlined,
    RightOutlined,
} from "@ant-design/icons";
import { Button, Empty, Segmented, Select, Skeleton } from "antd";
import { useState, type CSSProperties } from "react";
import {
    Link,
    useNavigate,
    useParams,
    useSearchParams,
} from "react-router-dom";
import type { Me } from "./api";
import {
    duration,
    localTime,
    MonitorError,
    MonitorRefresh,
    MonitorState,
    monitoringAccessLost,
    number,
    percent,
    stateName,
} from "./monitoringShared";
import { fmt } from "./shared";
import { useWorkbenchLive } from "./workbenchLive";
import { useWorkbench, type WorkbenchSchemas } from "./workbenchShared";

type Snapshot = WorkbenchSchemas["WorkbenchShiftMonitor"];
type Window = WorkbenchSchemas["WorkbenchMonitorWindow"];
type Hour = WorkbenchSchemas["WorkbenchMonitorHour"];
type Stop = WorkbenchSchemas["WorkbenchMonitorStop"];
type Run = WorkbenchSchemas["WorkbenchMonitorRun"];

const minuteMs = 60_000;

function clippedMinutes(stop: Stop, shift: Window, asOf: string) {
    const from = Math.max(
        Date.parse(stop.startsAt!),
        Date.parse(shift.startsAt!),
    );
    const to = Math.min(
        Date.parse(stop.endsAt ?? asOf),
        Date.parse(shift.endsAt!),
        Date.parse(asOf),
    );
    return Math.max(0, (to - from) / minuteMs);
}

function Timeline({
    data,
    shift,
    timezone,
}: {
    data: Snapshot;
    shift: Window;
    timezone?: string;
}) {
    const from = Date.parse(shift.startsAt!);
    const to = Date.parse(shift.endsAt!);
    const now = Date.parse(data.asOf!);
    const end = Math.min(now, to);
    const timeAt = (at: number) =>
        localTime(new Date(at).toISOString(), timezone);
    const offset = (at: number) => ((at - from) / (to - from)) * 100;
    const position = (start: string, stop?: string | null): CSSProperties => {
        const left = Math.max(from, Date.parse(start));
        const right = Math.min(end, stop ? Date.parse(stop) : end);
        return {
            left: `${offset(left)}%`,
            width: `${offset(right) - offset(left)}%`,
        };
    };
    const ticks = [0, 0.25, 0.5, 0.75, 1];
    const elapsed = Math.max(0, offset(end));

    return (
        <section className="monitor-panel" aria-label="班次时间轴">
            <div className="monitor-section-heading">
                <h2>班次时间轴</h2>
            </div>
            <div className="monitor-timeline">
                <div className="monitor-ruler" aria-hidden="true">
                    {ticks.map((tick) => (
                        <span key={tick}>
                            {timeAt(
                                tick === 0 || tick === 1
                                    ? from + tick * (to - from)
                                    : Math.round(
                                          (from + tick * (to - from)) /
                                              minuteMs,
                                      ) * minuteMs,
                            )}
                        </span>
                    ))}
                </div>
                <div className="monitor-lane-label">设备状态</div>
                <div className="monitor-lane" aria-label="设备状态区间">
                    <span
                        className="monitor-elapsed"
                        style={{ width: `${elapsed}%` }}
                    />
                    {data.states?.map((row) => (
                        <span
                            key={`${row.state}/${row.startsAt}`}
                            className="monitor-segment"
                            data-state={row.state}
                            style={position(row.startsAt!, row.endsAt)}
                            title={`${stateName(row.state!)} · ${localTime(row.startsAt!, timezone)}–${localTime(row.endsAt!, timezone)}`}
                        />
                    ))}
                    {now >= from && now < to && (
                        <span
                            className="monitor-now"
                            style={{ left: `${elapsed}%` }}
                            title="当前快照"
                        />
                    )}
                </div>
                {data.runs && (
                    <>
                        <div className="monitor-lane-label">实际 Run</div>
                        <div
                            className="monitor-lane"
                            aria-label="实际 Run 区间"
                        >
                            <span
                                className="monitor-elapsed"
                                style={{ width: `${elapsed}%` }}
                            />
                            {data.runs.map((run) => (
                                <Link
                                    key={run.id}
                                    className="monitor-segment monitor-run-segment"
                                    to={`/execution?run=${run.id}`}
                                    aria-label={`查看 Run ${run.lotNumber}，${fmt(run.startsAt)}`}
                                    title={`${run.lotNumber} · ${run.step}`}
                                    style={position(run.startsAt!, run.endsAt)}
                                />
                            ))}
                            {now >= from && now < to && (
                                <span
                                    className="monitor-now"
                                    style={{ left: `${elapsed}%` }}
                                    title="当前快照"
                                />
                            )}
                        </div>
                    </>
                )}
            </div>
            <div className="monitor-legend">
                {(
                    [
                        "Running",
                        "Idle",
                        "Down",
                        "Maintenance",
                        "Offline",
                        "Unknown",
                    ] as const
                )
                    .filter((state) =>
                        data.states?.some((row) => row.state === state),
                    )
                    .map((state) => (
                        <MonitorState key={state} state={state} />
                    ))}
                <span>
                    <i className="monitor-legend-elapsed" /> 无状态记录／无 Run
                </span>
                {now < to && (
                    <span>
                        <i className="monitor-legend-future" /> 未发生
                    </span>
                )}
            </div>
            {data.states?.length ? (
                <details className="monitor-intervals">
                    <summary>查看设备状态区间</summary>
                    <ul>
                        {data.states.map((row) => (
                            <li key={`${row.state}/${row.startsAt}`}>
                                {stateName(row.state!)} · {fmt(row.startsAt)} –{" "}
                                {fmt(row.endsAt)}
                            </li>
                        ))}
                    </ul>
                </details>
            ) : null}
        </section>
    );
}

const count = (hour: Hour) => (hour.passed ?? 0) + (hour.failed ?? 0);
const rate = (hour: Hour) =>
    (count(hour) * minuteMs) /
    (Date.parse(hour.endsAt!) - Date.parse(hour.startsAt!));

function SpeedChart({
    hours,
    idealSeconds,
    timezone,
}: {
    hours: Hour[];
    idealSeconds?: number | null;
    timezone?: string;
}) {
    const [unit, setUnit] = useState("rate");
    const [selected, setSelected] = useState<string>();
    const ideal = idealSeconds ? 60 / idealSeconds : null;
    const benchmark = unit === "ratio" ? 1 : ideal;
    const values = hours.map((hour) =>
        unit === "ratio" && ideal ? rate(hour) / ideal : rate(hour),
    );
    const max = Math.max(...values, benchmark ?? 0) || 1;
    const showValue = (value: number) =>
        unit === "ratio" ? percent(value) : number(value);
    const active =
        hours.find((hour) => hour.startsAt === selected) ?? hours.at(-1);
    const range = (hour: Hour) =>
        `${localTime(hour.startsAt!, timezone)}–${localTime(hour.endsAt!, timezone)}`;

    return (
        <section className="monitor-panel" aria-label="晶圆过站速率">
            <div className="monitor-section-heading">
                <div>
                    <h2>晶圆过站速率</h2>
                    <p>按班起每小时汇总实际结果</p>
                </div>
                <Segmented
                    aria-label="速率单位"
                    value={unit}
                    onChange={setUnit}
                    options={[
                        { label: "片／分钟", value: "rate" },
                        {
                            label: "理想产速 %",
                            value: "ratio",
                            disabled: !ideal,
                        },
                    ]}
                />
            </div>
            {hours.length ? (
                <>
                    <div className="monitor-chart-scroll">
                        <div
                            className="monitor-chart"
                            style={{
                                minWidth: Math.max(280, hours.length * 38),
                            }}
                        >
                            <div
                                className="monitor-chart-scale"
                                aria-hidden="true"
                            >
                                <span>{showValue(max)}</span>
                                <span>{showValue(max / 2)}</span>
                                <span>0</span>
                            </div>
                            <div className="monitor-chart-main">
                                <div
                                    className="monitor-chart-axis"
                                    aria-hidden="true"
                                />
                                {benchmark != null && (
                                    <div
                                        className="monitor-chart-benchmark"
                                        aria-hidden="true"
                                        style={{
                                            bottom: `${(benchmark / max) * 100}%`,
                                        }}
                                    />
                                )}
                                <div className="monitor-bars">
                                    {hours.map((hour, index) => (
                                        <button
                                            type="button"
                                            key={hour.startsAt}
                                            aria-label={`${range(hour)}，${count(hour)} 片，${showValue(values[index])}${unit === "rate" ? " 片每分钟" : ""}`}
                                            aria-pressed={active === hour}
                                            onClick={() =>
                                                setSelected(hour.startsAt)
                                            }
                                            title={`${range(hour)} · ${count(hour)} 片`}
                                        >
                                            {values[index] > 0 && (
                                                <span
                                                    className="monitor-chart-bar"
                                                    style={{
                                                        height: `${(values[index] / max) * 100}%`,
                                                    }}
                                                />
                                            )}
                                            <span className="monitor-hour-label">
                                                {localTime(
                                                    hour.startsAt!,
                                                    timezone,
                                                )}
                                            </span>
                                        </button>
                                    ))}
                                </div>
                            </div>
                        </div>
                    </div>
                    <div className="monitor-chart-detail" aria-live="polite">
                        {active && (
                            <span>
                                <strong>{range(active)}</strong> · 通过{" "}
                                {number(active.passed ?? 0)} 片 · 失败{" "}
                                {number(active.failed ?? 0)} 片
                            </span>
                        )}
                        <span>
                            {ideal
                                ? `理想产速 ${number(ideal)} 片／分钟`
                                : "设备未配置理想产速"}
                        </span>
                    </div>
                </>
            ) : (
                <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="该班次尚无过站数据"
                />
            )}
        </section>
    );
}

function StopDetails({
    stops,
    shift,
    asOf,
    showAnalysis,
}: {
    stops: Stop[];
    shift: Window;
    asOf: string;
    showAnalysis: boolean;
}) {
    const byReason = new Map<string, number>();
    const reason = (stop: Stop) =>
        stop.reason ?? (stop.needsReason ? "待补录原因" : "原因未记录");
    for (const stop of stops) {
        const label = reason(stop);
        byReason.set(
            label,
            (byReason.get(label) ?? 0) + clippedMinutes(stop, shift, asOf),
        );
    }
    const ranked = [...byReason].sort((a, b) => b[1] - a[1]);
    const max = ranked[0]?.[1] || 1;
    return (
        <section className="monitor-panel" aria-label="班次停机">
            <div className="monitor-section-heading">
                <div>
                    <h2>{showAnalysis ? "停机原因分布" : "停机记录"}</h2>
                    <p>
                        {stops.length} 条 ·{" "}
                        {stops.filter((stop) => stop.needsReason).length}{" "}
                        条待补录
                    </p>
                </div>
            </div>
            {ranked.length > 0 && showAnalysis && (
                <ul className="monitor-reasons">
                    {ranked.map(([label, minutes]) => (
                        <li key={label}>
                            <div>
                                <span>{label}</span>
                                <strong>{duration(minutes)}</strong>
                            </div>
                            <span className="monitor-reason-track">
                                <i
                                    style={{
                                        width: `${(minutes / max) * 100}%`,
                                    }}
                                />
                            </span>
                        </li>
                    ))}
                </ul>
            )}
            {stops.length ? (
                <ul className="monitor-records">
                    {stops.map((stop) => (
                        <li key={stop.id}>
                            <div>
                                <strong>{reason(stop)}</strong>
                                <p>
                                    {fmt(stop.startsAt)} · 班内{" "}
                                    {duration(
                                        clippedMinutes(stop, shift, asOf),
                                    )}
                                </p>
                                {stop.notes && <p>{stop.notes}</p>}
                            </div>
                            <Link
                                to={`/equipment?view=downtimes&record=${stop.id}`}
                                aria-label={`查看停机记录 ${fmt(stop.startsAt)}`}
                            >
                                查看停机
                            </Link>
                        </li>
                    ))}
                </ul>
            ) : (
                <Empty
                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                    description="没有与该班次重叠的停机记录"
                />
            )}
        </section>
    );
}

function RunSummary({ run }: { run: Run }) {
    return (
        <li>
            <div>
                <strong>{run.lotNumber}</strong>
                <p>
                    {[run.orderNo, run.step, fmt(run.startsAt)]
                        .filter(Boolean)
                        .join(" · ")}
                </p>
            </div>
            <Link
                to={`/execution?run=${run.id}`}
                aria-label={`查看 Run ${run.lotNumber}，${fmt(run.startsAt)}`}
            >
                查看 Run
            </Link>
        </li>
    );
}

function ShiftContent({
    data,
    shift,
    timezone,
}: {
    data: Snapshot;
    shift: Window;
    timezone?: string;
}) {
    const [tab, setTab] = useState("timeline");
    const hours = data.hours ?? [];
    const passed = hours.reduce((sum, row) => sum + (row.passed ?? 0), 0);
    const failed = hours.reduce((sum, row) => sum + (row.failed ?? 0), 0);
    const m = data.metrics;
    const metrics = [
        ["通过片数", `${number(passed)} 片`],
        ["失败片数", `${number(failed)} 片`],
        [
            "记录停机",
            m?.downtimeMinutes == null ? "—" : duration(m.downtimeMinutes),
        ],
        ["班次 OEE", percent(m?.oee)],
    ];
    return (
        <>
            {data.hours && hours.length > 0 && (
                <dl className="monitor-shift-metrics">
                    {metrics.map(([label, value]) => (
                        <div key={label}>
                            <dt>{label}</dt>
                            <dd>{value}</dd>
                        </div>
                    ))}
                </dl>
            )}
            <div className="monitor-tabs">
                <Segmented
                    aria-label="班次视图"
                    value={tab}
                    onChange={setTab}
                    options={[
                        { label: "运行时间轴", value: "timeline" },
                        { label: "班次分析", value: "analysis" },
                    ]}
                />
            </div>
            {tab === "timeline" ? (
                <>
                    <Timeline data={data} shift={shift} timezone={timezone} />
                    {data.hours && (
                        <SpeedChart
                            key={shift.startsAt}
                            hours={hours}
                            idealSeconds={m?.idealSeconds}
                            timezone={timezone}
                        />
                    )}
                </>
            ) : (
                m && (
                    <div className="monitor-analysis-grid">
                        <section className="monitor-panel">
                            <div className="monitor-section-heading">
                                <div>
                                    <h2>班次 OEE</h2>
                                    <p>
                                        {m.missing ??
                                            "可用率 × 性能率 × 良品率"}
                                    </p>
                                </div>
                                <strong className="monitor-oee">
                                    {percent(m.oee)}
                                </strong>
                            </div>
                            <dl className="monitor-analysis-values">
                                {[
                                    ["可用率", percent(m.availability)],
                                    ["性能率", percent(m.performance)],
                                    ["良品率", percent(m.quality)],
                                ].map(([label, value]) => (
                                    <div key={label}>
                                        <dt>{label}</dt>
                                        <dd>{value}</dd>
                                    </div>
                                ))}
                            </dl>
                        </section>
                        <section className="monitor-panel">
                            <div className="monitor-section-heading">
                                <h2>班内时间</h2>
                            </div>
                            <dl className="monitor-analysis-values">
                                {(
                                    [
                                        ["已过班次", m.plannedMinutes],
                                        ["Run 覆盖", m.runMinutes],
                                        ["未运行", m.nonRunningMinutes],
                                        ["记录停机", m.downtimeMinutes],
                                        ["性能损失", m.performanceLossMinutes],
                                    ] as const
                                ).map(([label, value]) => (
                                    <div key={label}>
                                        <dt>{label}</dt>
                                        <dd>
                                            {value == null
                                                ? "—"
                                                : duration(value)}
                                        </dd>
                                    </div>
                                ))}
                            </dl>
                        </section>
                    </div>
                )
            )}
            <div className="monitor-analysis-grid">
                <StopDetails
                    stops={data.stops ?? []}
                    shift={shift}
                    asOf={data.asOf!}
                    showAnalysis={tab === "analysis"}
                />
                {data.runs && (
                    <section className="monitor-panel" aria-label="最近 Run">
                        <div className="monitor-section-heading">
                            <h2>班内最近 Run</h2>
                        </div>
                        {data.runs.length > 0 ? (
                            <ul className="monitor-records">
                                {data.runs.slice(0, 3).map((run) => (
                                    <RunSummary run={run} key={run.id} />
                                ))}
                            </ul>
                        ) : (
                            <Empty
                                image={Empty.PRESENTED_IMAGE_SIMPLE}
                                description="没有与该班次重叠的 Run"
                            />
                        )}
                    </section>
                )}
            </div>
        </>
    );
}

export function ShiftMonitor({ me }: { me: Me }) {
    const { equipmentId } = useParams();
    const [params, setParams] = useSearchParams();
    const navigate = useNavigate();
    const startsAt = params.get("startsAt");
    const shiftId = params.get("shiftId");
    const timezone = me.fab?.timezone ?? undefined;
    const live = useWorkbenchLive(true);
    const query = useWorkbench<Snapshot>(
        `shift-monitor/${equipmentId}`,
        startsAt ? { startsAt, ...(shiftId ? { shiftId } : {}) } : {},
        !!equipmentId,
        live === "实时更新" ? false : 30_000,
    );
    const data = monitoringAccessLost(query.error) ? undefined : query.data;
    const device = data?.equipment;
    const shift = data?.shift;
    const now = Date.parse(data?.asOf ?? "");
    const upcoming = !!shift && Date.parse(shift.startsAt!) > now;
    const current = !!shift && !upcoming && Date.parse(shift.endsAt!) > now;
    const selectShift = (value?: string | null) =>
        setParams(value ? { startsAt: value } : {});

    return (
        <div className="production-monitor">
            <div className="monitor-detail-nav">
                <Link to="/monitoring">
                    <ArrowLeftOutlined /> 厂区看板
                </Link>
                <Select
                    aria-label="切换设备"
                    placeholder="切换设备"
                    showSearch={{ optionFilterProp: "label" }}
                    value={device?.id}
                    options={data?.devices?.map((item) => ({
                        value: item.id,
                        label: `${item.code} · ${item.name}`,
                    }))}
                    onChange={(id) => navigate(`/monitoring/${id}`)}
                />
            </div>
            <div className="monitor-hero monitor-hero-detail">
                <div>
                    <p className="monitor-fab">班次监控</p>
                    <h1>{device?.name ?? "设备班次监控"}</h1>
                    {device && (
                        <div className="monitor-detail-identity">
                            <span>
                                {[device.code, device.area, device.line]
                                    .filter(Boolean)
                                    .join(" · ")}
                            </span>
                            <MonitorState state={device.state!} />
                        </div>
                    )}
                </div>
                <MonitorRefresh
                    live={live}
                    asOf={data?.asOf}
                    syncing={query.isFetching}
                    error={query.error}
                    refresh={() => void query.refetch()}
                />
            </div>
            <MonitorError error={query.error} hasSnapshot={!!data} />
            {query.isPending && <Skeleton active />}
            {data && (
                <>
                    <div className="monitor-shift-window">
                        <div className="monitor-window-label">
                            <ClockCircleOutlined />
                            <div>
                                <h2>{shift?.label ?? "当前无适用班次"}</h2>
                                {shift && (
                                    <p
                                        title={`${fmt(shift.startsAt)} – ${fmt(shift.endsAt)}`}
                                    >
                                        {shift.day} ·{" "}
                                        {localTime(shift.startsAt!, timezone)}–
                                        {localTime(shift.endsAt!, timezone)}
                                        {upcoming
                                            ? " · 即将开始"
                                            : current
                                              ? " · 当前时段"
                                              : " · 历史班次"}
                                    </p>
                                )}
                            </div>
                        </div>
                        <div className="monitor-window-actions">
                            <Button
                                icon={<LeftOutlined />}
                                disabled={!data.previousShift}
                                onClick={() => selectShift(data.previousShift)}
                            >
                                上一班
                            </Button>
                            <Button
                                onClick={() => selectShift()}
                                disabled={!startsAt}
                            >
                                回到当前班
                            </Button>
                            <Button
                                icon={<RightOutlined />}
                                iconPlacement="end"
                                disabled={!data.nextShift}
                                onClick={() => selectShift(data.nextShift)}
                            >
                                下一班
                            </Button>
                        </div>
                    </div>
                    {device?.activeRun && (
                        <div className="monitor-current">
                            <span>设备当前批次</span>
                            <Link to={`/execution?run=${device.activeRun.id}`}>
                                {device.activeRun.lotNumber}
                            </Link>
                            {device.activeRun.orderNo && (
                                <span>{device.activeRun.orderNo}</span>
                            )}
                            <span>{device.activeRun.step}</span>
                        </div>
                    )}
                    {device?.currentStop && (
                        <div className="monitor-current monitor-current-stop">
                            <span>设备当前停机</span>
                            <strong>
                                {device.currentStop.reason ?? "停机原因待补录"}
                            </strong>
                            <Link
                                to={`/equipment?view=downtimes&record=${device.currentStop.id}`}
                            >
                                查看当前停机
                            </Link>
                        </div>
                    )}
                    {shift && !upcoming ? (
                        <ShiftContent
                            key={`${device?.id}/${shift.startsAt}`}
                            data={data}
                            shift={shift}
                            timezone={timezone}
                        />
                    ) : (
                        <Empty
                            className="monitor-empty"
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                            description={
                                upcoming
                                    ? "该班次尚未开始"
                                    : "当前无适用班次，可切换查看其它班次"
                            }
                        />
                    )}
                </>
            )}
        </div>
    );
}
