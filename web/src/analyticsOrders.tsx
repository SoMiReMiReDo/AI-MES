import {
    Button,
    Card,
    Descriptions,
    Empty,
    Input,
    Select,
    Skeleton,
    Table,
    Tag,
} from "antd";
import { useState } from "react";
import {
    Link,
    useLocation,
    useParams,
    useSearchParams,
} from "react-router-dom";
import { ApiError, refreshCsrf, type Me } from "./api";
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

const events: Record<string, string> = {
    reached: "已达计划",
    closed: "已关闭",
    cancelled: "已取消",
    "missing-time": "历史时间缺失",
};

const statuses: Record<string, string> = {
    Released: "已下达",
    Closed: "已关闭",
    Cancelled: "已取消",
    Completed: "已完工",
    Merged: "已合批",
};

async function download(filters: URLSearchParams) {
    const response = await fetch(
        `/api/v1/analytics/work-orders/export?${filters}`,
        {
            method: "POST",
            headers: { "X-CSRF-TOKEN": await refreshCsrf() },
        },
    );
    if (!response.ok) {
        const problem = await response.json().catch(() => ({}));
        if (response.status === 401)
            window.dispatchEvent(new Event("session-invalid"));
        throw new ApiError(
            response.status,
            problem.code,
            problem.title ?? "导出失败",
        );
    }
    const disposition = response.headers.get("content-disposition");
    const filename = disposition?.match(/filename="?([^";]+)/)?.[1];
    const url = URL.createObjectURL(await response.blob());
    const link = document.createElement("a");
    link.href = url;
    link.download = filename ?? "work-orders.csv";
    link.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
}

export function AnalyticsOrders({ me }: { me: Me }) {
    const filters = useReportFilters(me, 30, [
        "q",
        "productId",
        "revisionId",
        "event",
    ]);
    const report = useAnalytics<AnalyticsSchemas["AnalyticsOrders"]>(
        "work-orders",
        filters.page(),
    );
    const data = report.error ? undefined : report.data;
    const summary = data?.summary;
    const [exporting, setExporting] = useState(false);
    const [exportError, setExportError] = useState<Error | null>(null);
    const location = useLocation();
    const exportCsv = async () => {
        setExporting(true);
        setExportError(null);
        try {
            await download(new URLSearchParams(data?.context?.filters));
        } catch (error) {
            setExportError(
                error instanceof Error ? error : new Error("导出失败"),
            );
        } finally {
            setExporting(false);
        }
    };

    return (
        <div className="analytics-page">
            <ReportHeader
                title="工单履历"
                context={data?.context}
                aside={
                    <Button
                        loading={exporting}
                        disabled={!data}
                        onClick={exportCsv}
                    >
                        导出 CSV
                    </Button>
                }
            >
                <Link to="/planning">工单管理</Link>
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
                    label="产品修订"
                    field="revisionId"
                    options={data?.revisions}
                    filters={filters}
                />
                <label className="workbench-filter">
                    <span>履历事件</span>
                    <Select
                        aria-label="履历事件"
                        allowClear
                        placeholder="全部"
                        value={filters.query.get("event") ?? undefined}
                        options={Object.entries(events).map(
                            ([value, label]) => ({ value, label }),
                        )}
                        onChange={(value) =>
                            filters.change("event", value ?? "")
                        }
                    />
                </label>
                <label className="workbench-filter analytics-order-search">
                    <span>工单号</span>
                    <Input.Search
                        key={filters.query.get("q") ?? ""}
                        aria-label="工单号"
                        defaultValue={filters.query.get("q") ?? ""}
                        allowClear
                        maxLength={100}
                        placeholder="输入工单号"
                        onSearch={(value) => filters.change("q", value.trim())}
                    />
                </label>
            </div>
            <ReportError error={report.error ?? exportError} />
            {summary && (
                <>
                    <ReportKpis
                        items={[
                            {
                                label: "期间工单",
                                value: metric(summary.orders),
                                detail: `已达计划 ${metric(summary.reached)} · 未达计划关闭 ${metric(summary.closedBelowPlan)}`,
                            },
                            {
                                label: "计划 / 终点完工",
                                value: `${metric(summary.planned)} / ${metric(summary.completed)}`,
                                detail: `已投放 ${metric(summary.released)} 片`,
                            },
                            {
                                label: "按期达成率",
                                value: ratioText(summary.onTimeRatio),
                                detail: `${metric(summary.onTime)} / ${metric(summary.withDueDate)} 张有交期且已达计划工单`,
                            },
                            {
                                label: "平均生产历时",
                                value:
                                    summary.averageProductionMinutes == null
                                        ? "—"
                                        : `${metric(summary.averageProductionMinutes, 1)} 分钟`,
                                detail: `${metric(summary.durationSamples)} 张工单有有效历时`,
                            },
                        ]}
                    />
                    <MissingTags codes={data.context?.missing} />
                </>
            )}
            <Card title="履历明细" className="analytics-panel">
                <Table<AnalyticsSchemas["AnalyticsWorkOrder"]>
                    rowKey={(row) => row.id!}
                    size="small"
                    loading={report.isPending}
                    dataSource={data?.records?.items ?? []}
                    pagination={false}
                    scroll={{ x: 950 }}
                    columns={[
                        {
                            title: "工单号",
                            render: (_, row) => (
                                <Link
                                    to={
                                        row.evidence?.find((item) =>
                                            item.url?.startsWith("/analytics/"),
                                        )?.url ??
                                        `/analytics/work-orders/${row.id}`
                                    }
                                    state={{ back: location.search }}
                                >
                                    {row.orderNo}
                                </Link>
                            ),
                        },
                        {
                            title: "产品 / 修订",
                            render: (_, row) =>
                                `${row.product?.code} / ${row.revision?.code}`,
                        },
                        {
                            title: "履历事件",
                            render: (_, row) => (
                                <>
                                    <Tag>{events[row.event!]}</Tag>
                                    {row.status === "Released" &&
                                        statuses.Released}
                                </>
                            ),
                        },
                        {
                            title: "事件时间",
                            render: (_, row) =>
                                row.eventAt ? fmt(row.eventAt) : "—",
                        },
                        {
                            title: "交期",
                            render: (_, row) => row.dueDate ?? "—",
                        },
                        {
                            title: "计划 / 投放 / 完工",
                            render: (_, row) =>
                                `${metric(row.planned)} / ${metric(row.released)} / ${metric(row.completed)}`,
                        },
                        ...(has(me, "quality.read")
                            ? [
                                  {
                                      title: "报废",
                                      dataIndex: "scrapped",
                                      render: (value?: number | null) =>
                                          metric(value),
                                  },
                              ]
                            : []),
                        {
                            title: "按期",
                            render: (_, row) =>
                                row.onTime == null
                                    ? "—"
                                    : row.onTime
                                      ? "按期"
                                      : "逾期",
                        },
                    ]}
                />
                <ReportPager
                    {...data?.records}
                    size={data?.records?.pageSize}
                    onChange={(page) => filters.update({ page })}
                />
            </Card>
        </div>
    );
}

export function AnalyticsOrderDetail() {
    const { id } = useParams();
    const location = useLocation();
    const [params, setParams] = useSearchParams();
    const detail = useAnalytics<AnalyticsSchemas["AnalyticsOrderDetail"]>(
        `work-orders/${id}`,
        params,
    );
    const data = detail.error ? undefined : detail.data;
    const row = data?.order;
    const baseline = data?.baseline;
    const back =
        typeof location.state?.back === "string" ? location.state.back : "";

    return (
        <div className="analytics-page">
            <ReportHeader
                title={row?.orderNo ?? "工单履历详情"}
                context={data?.context}
                aside={
                    <Link to={`/analytics/work-orders${back}`}>
                        返回工单履历
                    </Link>
                }
            >
                {row &&
                    `${row.product?.name} / ${row.revision?.code} · ${statuses[row.status!]} · ${events[row.event!]}`}
            </ReportHeader>
            <ReportError error={detail.error} />
            {detail.isPending && <Skeleton active />}
            {row && (
                <>
                    <MissingTags codes={data?.context?.missing} />
                    <Card className="analytics-panel" title="计划与实际">
                        <Descriptions
                            bordered
                            size="small"
                            column={{ xs: 1, sm: 2, lg: 3 }}
                            items={[
                                {
                                    key: "plan",
                                    label: "计划 / 投放 / 终点完工",
                                    children: `${metric(row.planned)} / ${metric(row.released)} / ${metric(row.completed)} 片`,
                                },
                                ...(row.scrapped != null
                                    ? [
                                          {
                                              key: "scrap",
                                              label: "报废",
                                              children: `${row.scrapped} 片`,
                                          },
                                      ]
                                    : []),
                                {
                                    key: "reach",
                                    label: "达到计划",
                                    children: row.completedAt
                                        ? fmt(row.completedAt)
                                        : "—",
                                },
                                {
                                    key: "terminal",
                                    label: "终态时间",
                                    children: row.terminalAt
                                        ? fmt(row.terminalAt)
                                        : "—",
                                },
                                {
                                    key: "due",
                                    label: "当前交期",
                                    children: row.dueDate ?? "—",
                                },
                                {
                                    key: "duration",
                                    label: "生产历时",
                                    children:
                                        row.productionMinutes == null
                                            ? "—"
                                            : `${metric(row.productionMinutes, 1)} 分钟`,
                                },
                            ]}
                        />
                    </Card>
                    <Card className="analytics-panel" title="下达计划基线">
                        {baseline ? (
                            <Descriptions
                                size="small"
                                column={{ xs: 1, sm: 2, lg: 3 }}
                                items={[
                                    {
                                        key: "version",
                                        label: "版本",
                                        children: baseline.version,
                                    },
                                    {
                                        key: "released",
                                        label: "下达时间",
                                        children: fmt(baseline.releasedAt),
                                    },
                                    {
                                        key: "qty",
                                        label: "计划片数",
                                        children: metric(baseline.planned),
                                    },
                                    {
                                        key: "priority",
                                        label: "优先级",
                                        children: metric(baseline.priority),
                                    },
                                    {
                                        key: "due",
                                        label: "下达交期",
                                        children: baseline.dueDate ?? "—",
                                    },
                                ]}
                            />
                        ) : (
                            <Empty
                                image={Empty.PRESENTED_IMAGE_SIMPLE}
                                description="无可用下达基线"
                            />
                        )}
                    </Card>
                    <Card className="analytics-panel" title="来源记录">
                        <SourceLinks
                            items={data?.context?.evidence?.filter(
                                (item) =>
                                    item.url?.startsWith(
                                        "/traceability?kind=work-order",
                                    ) ||
                                    ["quality-issue", "process-run"].includes(
                                        item.kind,
                                    ),
                            )}
                        />
                        {data?.lots && (
                            <>
                                <Table<AnalyticsSchemas["AnalyticsOrderLot"]>
                                    rowKey={(lot) => lot.id!}
                                    size="small"
                                    pagination={false}
                                    dataSource={data.lots.items ?? []}
                                    columns={[
                                        {
                                            title: "关联批次",
                                            render: (_, lot) => (
                                                <Link
                                                    to={lot.evidence?.url ?? ""}
                                                >
                                                    {lot.number}
                                                </Link>
                                            ),
                                        },
                                        {
                                            title: "状态",
                                            render: (_, lot) =>
                                                statuses[lot.status!] ??
                                                lot.status,
                                        },
                                    ]}
                                />
                                <ReportPager
                                    {...data.lots}
                                    size={data.lots.pageSize}
                                    onChange={(page) =>
                                        setParams({ page: String(page) })
                                    }
                                />
                            </>
                        )}
                    </Card>
                </>
            )}
        </div>
    );
}
