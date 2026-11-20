import { Card, Empty, Table } from "antd";
import { Link } from "react-router-dom";
import type { Me } from "./api";
import { OeeTrend, series } from "./analyticsOeeChart";
import {
    MissingTags,
    ratioText,
    ReportDates,
    ReportError,
    ReportHeader,
    ReportKpis,
    ReportPager,
    ReportSelect,
    SourceLinks,
    useAnalytics,
    useReportFilters,
    type AnalyticsSchemas,
} from "./analyticsShared";
import { fmt } from "./shared";
import { metric } from "./workbenchShared";

const minutePair = (run?: number | null, plan?: number | null) =>
    `${metric(run, 1)} / ${metric(plan, 1)}`;

export function AnalyticsOee({ me }: { me: Me }) {
    const filters = useReportFilters(me, 7, [
        "areaId",
        "lineId",
        "equipmentId",
        "shiftId",
    ]);
    const overview = useAnalytics<AnalyticsSchemas["AnalyticsOee"]>(
        "oee",
        filters.page(),
    );
    const shifts = useAnalytics<AnalyticsSchemas["AnalyticsOeeShifts"]>(
        "oee/shifts",
        filters.page("shiftsPage"),
    );
    const data = overview.error ? undefined : overview.data;
    const rows = shifts.error ? undefined : shifts.data?.records;
    const devices = data?.devices?.items ?? [];
    const chosen = filters.query.get("trendDevice");
    const focused =
        devices.find((item) => item.id === chosen) ??
        devices.find((item) => item.metrics?.oee != null) ??
        devices[0];
    const maximum = Math.max(
        1,
        ...devices.map((item) => item.metrics?.oee ?? 0),
    );

    return (
        <div className="analytics-page analytics-oee">
            <ReportHeader
                title="设备 OEE"
                context={data?.context}
                aside={<Link to="/monitoring">厂区看板</Link>}
            >
                按当前设备归属和班次日历重算历史指标，跨午夜班次归入开始日。
            </ReportHeader>
            <div className="analytics-filters">
                <ReportDates {...filters} />
                <ReportSelect
                    label="区域"
                    field="areaId"
                    options={data?.areas}
                    filters={filters}
                />
                <ReportSelect
                    label="产线"
                    field="lineId"
                    options={data?.lines}
                    filters={filters}
                />
                <ReportSelect
                    label="设备"
                    field="equipmentId"
                    options={data?.equipment}
                    filters={filters}
                />
                <ReportSelect
                    label="班次"
                    field="shiftId"
                    options={data?.shifts}
                    filters={filters}
                />
            </div>
            <ReportError error={overview.error} />
            {focused && (
                <ReportKpis
                    items={series.map((item) => ({
                        label: `${item.label} · ${item.name}`,
                        value: (
                            <span
                                className={`analytics-series analytics-series-${item.color}`}
                            >
                                {ratioText(focused.metrics?.[item.key])}
                            </span>
                        ),
                        detail:
                            item.key === "oee"
                                ? `${focused.code} · ${focused.completeShifts} / ${focused.startedShifts} 班有完整 OEE`
                                : item.key === "availability"
                                  ? `${minutePair(focused.metrics?.runMinutes, focused.metrics?.plannedMinutes)} 分钟`
                                  : item.key === "performance"
                                    ? `${minutePair(focused.metrics?.idealMinutes, focused.metrics?.runMinutes)} 分钟`
                                    : `${minutePair(focused.metrics?.passed, focused.metrics?.processed)} 片次`,
                    }))}
                />
            )}
            <div className="analytics-oee-grid">
                <Card
                    className="analytics-panel"
                    title="设备对比"
                    loading={overview.isPending}
                >
                    {devices.length ? (
                        <div
                            className="analytics-compare"
                            aria-label="切换趋势设备"
                        >
                            {devices.map((item) => (
                                <button
                                    key={item.id}
                                    type="button"
                                    aria-pressed={item.id === focused?.id}
                                    onClick={() =>
                                        filters.update({
                                            trendDevice: item.id,
                                            trendDay: undefined,
                                        })
                                    }
                                >
                                    <span>
                                        <b>{item.code}</b>
                                        <small>{item.name}</small>
                                    </span>
                                    <span
                                        className="analytics-bar-track"
                                        aria-hidden="true"
                                    >
                                        <i
                                            style={{
                                                width: `${((item.metrics?.oee ?? 0) * 100) / maximum}%`,
                                            }}
                                        />
                                    </span>
                                    <strong>
                                        {ratioText(item.metrics?.oee)}
                                    </strong>
                                </button>
                            ))}
                        </div>
                    ) : (
                        <Empty
                            image={Empty.PRESENTED_IMAGE_SIMPLE}
                            description="无符合筛选的设备"
                        />
                    )}
                    {(data?.devices?.total ?? 0) >
                        (data?.devices?.pageSize ?? 20) && (
                        <ReportPager
                            {...data?.devices}
                            size={data?.devices?.pageSize}
                            onChange={(page) =>
                                filters.update({
                                    page,
                                    trendDevice: undefined,
                                    trendDay: undefined,
                                })
                            }
                        />
                    )}
                </Card>
                <Card
                    className="analytics-panel"
                    title={focused ? `按日趋势 · ${focused.code}` : "按日趋势"}
                    loading={overview.isPending}
                >
                    <OeeTrend
                        days={focused?.days ?? []}
                        selected={filters.query.get("trendDay") ?? undefined}
                        choose={(trendDay) =>
                            filters.update({ trendDay }, true)
                        }
                    />
                    {focused && (
                        <MissingTags codes={focused.metrics?.missing} />
                    )}
                </Card>
            </div>
            <ReportError error={shifts.error} />
            <Card className="analytics-panel" title="设备 × 班次明细">
                <Table<AnalyticsSchemas["AnalyticsOeeShift"]>
                    rowKey={(row) =>
                        `${row.equipmentId}/${row.shiftId}/${row.startsAt}`
                    }
                    size="small"
                    scroll={{ x: 1500 }}
                    pagination={false}
                    loading={shifts.isPending}
                    dataSource={rows?.items ?? []}
                    columns={[
                        { title: "设备", dataIndex: "equipmentCode" },
                        {
                            title: "班次",
                            render: (_, row) => (
                                <>
                                    {row.shift ?? "—"}
                                    <br />
                                    {row.startsAt ? fmt(row.startsAt) : ""}
                                </>
                            ),
                        },
                        {
                            title: "计划 / Run / 停机（分钟）",
                            render: (_, row) =>
                                `${metric(row.metrics?.plannedMinutes, 1)} / ${metric(row.metrics?.runMinutes, 1)} / ${metric(row.metrics?.downtimeMinutes, 1)}`,
                        },
                        {
                            title: "理想秒／片",
                            render: (_, row) => metric(row.idealSeconds, 3),
                        },
                        {
                            title: "加工 / 通过 / 失败（片次）",
                            render: (_, row) =>
                                `${metric(row.metrics?.processed, 2)} / ${metric(row.metrics?.passed, 2)} / ${metric(row.metrics?.failed, 2)}`,
                        },
                        ...series.map((item) => ({
                            title: item.label,
                            key: item.key,
                            render: (
                                _: unknown,
                                row: AnalyticsSchemas["AnalyticsOeeShift"],
                            ) => ratioText(row.metrics?.[item.key]),
                        })),
                        {
                            title: "缺数原因",
                            render: (_, row) => (
                                <MissingTags codes={row.metrics?.missing} />
                            ),
                        },
                        {
                            title: "来源",
                            render: (_, row) => (
                                <SourceLinks
                                    items={row.evidence?.filter(
                                        (entry, index, all) =>
                                            all.findIndex(
                                                (item) =>
                                                    item.kind === entry.kind,
                                            ) === index,
                                    )}
                                />
                            ),
                        },
                    ]}
                />
                <ReportPager
                    {...rows}
                    size={rows?.pageSize}
                    onChange={(page) => filters.update({ shiftsPage: page })}
                />
            </Card>
        </div>
    );
}
