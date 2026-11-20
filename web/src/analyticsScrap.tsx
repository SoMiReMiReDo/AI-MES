import { Card, Empty, Table, Tag } from "antd";
import { Link } from "react-router-dom";
import type { Me } from "./api";
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
import { has, metric } from "./workbenchShared";

type Breakdown = AnalyticsSchemas["AnalyticsBreakdown"];

function Distribution({ rows }: { rows: Breakdown[] }) {
    if (!rows.length)
        return (
            <Empty
                image={Empty.PRESENTED_IMAGE_SIMPLE}
                description="暂无报废"
            />
        );
    return (
        <ul className="analytics-distribution">
            {rows.map((row) => (
                <li key={row.id ?? "unknown"}>
                    <span>
                        {row.name} {row.code && <small>{row.code}</small>}
                    </span>
                    <b>{metric(row.wafers)} 片</b>
                </li>
            ))}
        </ul>
    );
}

export function AnalyticsScrap({ me }: { me: Me }) {
    const filters = useReportFilters(me, 30, [
        "productId",
        "operationId",
        "reasonId",
        "equipmentId",
    ]);
    const report = useAnalytics<AnalyticsSchemas["AnalyticsScrap"]>(
        "scrap",
        filters.filter,
    );
    const details = useAnalytics<AnalyticsSchemas["AnalyticsScrapRecords"]>(
        "scrap/records",
        filters.page(),
    );
    const data = report.error ? undefined : report.data;
    const records = details.error ? undefined : details.data?.records;
    const reasons = data?.reasons ?? [];
    const days = data?.days ?? [];
    const peak = Math.max(1, ...days.map((day) => day.wafers ?? 0));
    const showDevices = has(me, "equipment.read");

    return (
        <div className="analytics-page">
            <ReportHeader
                title="报废分析"
                context={data?.context}
                aside={
                    <>
                        <Link to="/?tab=history">历史趋势</Link>
                        <Link to="/quality?view=scraps">质量报废记录</Link>
                    </>
                }
            >
                按实际报废日期和处置时的批次统计逐片报废。
            </ReportHeader>
            <div className="analytics-filters">
                <ReportDates {...filters} />
                <ReportSelect
                    label="产品"
                    field="productId"
                    options={data?.products}
                    filters={filters}
                />
                <ReportSelect
                    label="工序"
                    field="operationId"
                    options={data?.operationOptions}
                    filters={filters}
                />
                <ReportSelect
                    label="原因"
                    field="reasonId"
                    options={data?.reasonOptions}
                    filters={filters}
                />
                {showDevices && (
                    <ReportSelect
                        label="设备"
                        field="equipmentId"
                        options={data?.equipmentOptions}
                        filters={filters}
                    />
                )}
            </div>
            <ReportError error={report.error} />
            {data && (
                <>
                    <ReportKpis
                        items={[
                            {
                                label: "报废片数",
                                value: metric(data.wafers),
                                detail: `${metric(data.entries)} 张报废单`,
                            },
                            {
                                label: "报废原因",
                                value: metric(reasons.length),
                                detail: reasons[0]?.name
                                    ? `首要原因 · ${reasons[0].name}`
                                    : "暂无报废",
                            },
                            {
                                label: "同期已结案片报废占比",
                                value: ratioText(data.settledScrapRatio),
                                detail:
                                    data.settled == null
                                        ? undefined
                                        : `${metric(data.wafers)} / ${metric(data.settled)} 片 · 终点完工 ${metric(data.completed)} 片`,
                            },
                        ]}
                    />
                    <MissingTags codes={data.context?.missing} />
                    <div className="analytics-scrap-grid">
                        <Card title="原因 Pareto" className="analytics-panel">
                            {reasons.length > 0 ? (
                                <div className="analytics-pareto">
                                    <div className="analytics-pareto-label">
                                        <span>原因</span>
                                        <span>片数</span>
                                        <span>累计占比</span>
                                    </div>
                                    {reasons.map((row) => (
                                        <button
                                            key={row.id}
                                            type="button"
                                            aria-pressed={
                                                filters.query.get(
                                                    "reasonId",
                                                ) === row.id
                                            }
                                            onClick={() => {
                                                filters.change(
                                                    "reasonId",
                                                    filters.query.get(
                                                        "reasonId",
                                                    ) === row.id
                                                        ? ""
                                                        : row.id!,
                                                );
                                                document
                                                    .getElementById(
                                                        "analytics-scrap-records",
                                                    )
                                                    ?.scrollIntoView({
                                                        block: "nearest",
                                                    });
                                            }}
                                        >
                                            <span>
                                                <b>{row.name}</b>
                                                <small>{row.code}</small>
                                            </span>
                                            <span className="analytics-pareto-count">
                                                <span
                                                    className="analytics-bar-track"
                                                    aria-hidden="true"
                                                >
                                                    <i
                                                        style={{
                                                            width: `${((row.wafers ?? 0) * 100) / (reasons[0].wafers ?? 1)}%`,
                                                        }}
                                                    />
                                                </span>
                                                <b>{metric(row.wafers)}</b>
                                            </span>
                                            <strong>
                                                {ratioText(row.cumulativeRatio)}
                                            </strong>
                                        </button>
                                    ))}
                                </div>
                            ) : (
                                <Empty
                                    image={Empty.PRESENTED_IMAGE_SIMPLE}
                                    description="暂无报废原因"
                                />
                            )}
                        </Card>
                        <Card title="按日报废趋势" className="analytics-panel">
                            <div className="analytics-chart-scroll">
                                <div
                                    className="analytics-scrap-days"
                                    style={{
                                        minWidth: Math.max(
                                            270,
                                            days.length * 15,
                                        ),
                                    }}
                                >
                                    {days.map((day, index) => (
                                        <div
                                            key={day.date}
                                            title={`${day.date} · ${day.wafers} 片`}
                                        >
                                            <span className="analytics-scrap-track">
                                                <i
                                                    style={{
                                                        height: `${((day.wafers ?? 0) * 100) / peak}%`,
                                                    }}
                                                />
                                            </span>
                                            <span className="analytics-scrap-tick">
                                                {index %
                                                    Math.max(
                                                        1,
                                                        Math.ceil(
                                                            days.length / 6,
                                                        ),
                                                    ) ===
                                                0
                                                    ? day.date?.slice(5)
                                                    : ""}
                                            </span>
                                        </div>
                                    ))}
                                </div>
                            </div>
                            <p className="analytics-kpi-detail">
                                单日峰值{" "}
                                {metric(
                                    Math.max(
                                        0,
                                        ...days.map((day) => day.wafers ?? 0),
                                    ),
                                )}{" "}
                                片
                            </p>
                        </Card>
                        <Card title="关联工序" className="analytics-panel">
                            <Distribution rows={data.operations ?? []} />
                        </Card>
                        {showDevices && (
                            <Card title="关联设备" className="analytics-panel">
                                <Distribution rows={data.equipment ?? []} />
                            </Card>
                        )}
                    </div>
                </>
            )}
            <ReportError error={details.error} />
            <Card
                id="analytics-scrap-records"
                title="逐片处置"
                className="analytics-panel"
            >
                <Table<AnalyticsSchemas["AnalyticsScrapRecord"]>
                    rowKey={(row) => row.id!}
                    size="small"
                    pagination={false}
                    scroll={{ x: 1000 }}
                    loading={details.isPending}
                    dataSource={records?.items ?? []}
                    columns={[
                        {
                            title: "处置时间",
                            render: (_, row) => fmt(row.occurredAt),
                        },
                        { title: "晶圆", dataIndex: "waferCode" },
                        { title: "处置时批次", dataIndex: "lotNumber" },
                        {
                            title: "产品",
                            render: (_, row) => row.product?.code,
                        },
                        {
                            title: "原因",
                            render: (_, row) => (
                                <Tag title={row.reason?.code}>
                                    {row.reason?.name}
                                </Tag>
                            ),
                        },
                        {
                            title: "工序",
                            render: (_, row) =>
                                row.operation?.name ?? "归属未知",
                        },
                        ...(showDevices
                            ? [
                                  {
                                      title: "设备",
                                      render: (
                                          _: unknown,
                                          row: AnalyticsSchemas["AnalyticsScrapRecord"],
                                      ) => row.equipment?.code ?? "归属未知",
                                  },
                              ]
                            : []),
                        {
                            title: "来源",
                            render: (_, row) => (
                                <SourceLinks items={row.evidence} />
                            ),
                        },
                    ]}
                />
                <ReportPager
                    {...records}
                    size={records?.pageSize}
                    onChange={(page) => filters.update({ page })}
                />
            </Card>
        </div>
    );
}

const readiness: Record<string, [string, string]> = {
    available: ["来源已具备", "success"],
    "no-data": ["暂无事实", "default"],
    pending: ["待接入", "warning"],
    restricted: ["来源受限", "default"],
};

export function AnalyticsCost() {
    const report =
        useAnalytics<AnalyticsSchemas["AnalyticsCostReadiness"]>(
            "cost-readiness",
        );
    const data = report.error ? undefined : report.data;

    return (
        <div className="analytics-page">
            <ReportHeader title="生产成本" context={data?.context}>
                成本数据准备状态
            </ReportHeader>
            <ReportError error={report.error} />
            {data && (
                <ReportKpis
                    items={[
                        {
                            label: "总成本",
                            value: "—",
                            detail: "待接入核算口径",
                        },
                        {
                            label: "单片成本",
                            value: "—",
                            detail: "待接入核算口径",
                        },
                    ]}
                />
            )}
            <Card title="来源与准备状态" className="analytics-panel">
                <Table<AnalyticsSchemas["AnalyticsCostSource"]>
                    rowKey={(row) => row.kind!}
                    size="small"
                    pagination={false}
                    loading={report.isPending}
                    dataSource={data?.sources ?? []}
                    columns={[
                        { title: "来源", dataIndex: "description" },
                        {
                            title: "状态",
                            render: (_, row) => (
                                <Tag color={readiness[row.status!]?.[1]}>
                                    {readiness[row.status!]?.[0]}
                                </Tag>
                            ),
                        },
                        {
                            title: "查看",
                            render: (_, row) => (
                                <SourceLinks
                                    items={row.evidence ? [row.evidence] : []}
                                />
                            ),
                        },
                    ]}
                />
            </Card>
        </div>
    );
}
