import {
    AppstoreOutlined,
    ArrowRightOutlined,
    CheckCircleOutlined,
    DesktopOutlined,
    HourglassOutlined,
    PauseCircleOutlined,
    PlayCircleOutlined,
    RadarChartOutlined,
    WarningOutlined,
} from "@ant-design/icons";
import { Card, Modal, Space, Table, Tag, Typography } from "antd";
import { useState } from "react";
import { Link } from "react-router-dom";
import { type Me } from "./api";
import {
    has,
    metric,
    RecordLabel,
    TimeCell,
    useWorkbench,
    WorkbenchError,
    WorkbenchSelect,
    workbenchStatus,
    type WorkbenchSchemas,
} from "./workbenchShared";

type Overview = WorkbenchSchemas["WorkbenchOverview"];
type OverviewMetric = keyof Omit<Overview, "asOf">;

const lotLink = { to: "/production?view=lots", label: "批次列表" };
const waferLink = {
    to: "/production?view=wafers",
    label: "晶圆履历",
    permission: "production.wafers.read",
};

const cards: {
    key: OverviewMetric;
    kind: string;
    title: string;
    unit: string;
    icon: typeof AppstoreOutlined;
    note: string;
    tone?: "positive" | "warning" | "critical";
    share?: { total: OverviewMetric; label: string };
    link: { to: string; label: string; permission?: string };
}[] = [
    {
        key: "wipLots",
        kind: "wip",
        title: "在制批次",
        unit: "批",
        icon: AppstoreOutlined,
        note: "已投放且仍在制",
        link: lotLink,
    },
    {
        key: "wipWafers",
        kind: "wip",
        title: "在制晶圆",
        unit: "片",
        icon: RadarChartOutlined,
        note: "在制批次的有效片",
        link: waferLink,
    },
    {
        key: "waiting",
        kind: "waiting",
        title: "待加工",
        unit: "批",
        icon: HourglassOutlined,
        note: "等待当前工序开工",
        tone: "warning",
        share: { total: "wipLots", label: "占在制批次" },
        link: { to: "/execution", label: "派工列表" },
    },
    {
        key: "running",
        kind: "running",
        title: "运行中",
        unit: "Run",
        icon: PlayCircleOutlined,
        note: "已开工、尚未结束",
        tone: "positive",
        link: { to: "/execution?view=runs", label: "加工记录" },
    },
    {
        key: "completedToday",
        kind: "completed",
        title: "今日终点完工",
        unit: "片",
        icon: CheckCircleOutlined,
        note: "厂区当日累计完工",
        tone: "positive",
        link: waferLink,
    },
    {
        key: "held",
        kind: "held",
        title: "Hold 批次",
        unit: "批",
        icon: PauseCircleOutlined,
        note: "仍有有效 Hold 记录",
        tone: "warning",
        link: { to: "/quality?view=holds", label: "Hold 列表" },
    },
    {
        key: "equipment",
        kind: "equipment",
        title: "启用设备",
        unit: "台",
        icon: DesktopOutlined,
        note: "当前筛选下已启用",
        link: { to: "/equipment?view=tools", label: "设备台账" },
    },
    {
        key: "equipmentDown",
        kind: "down",
        title: "停机设备",
        unit: "台",
        icon: WarningOutlined,
        note: "当前状态为停机",
        tone: "critical",
        share: { total: "equipment", label: "占启用设备" },
        link: { to: "/equipment?view=downtimes", label: "停机记录" },
    },
];

export function WorkbenchRecords({
    me,
    drill,
    close,
}: {
    me: Me;
    drill: { kind: string; title: string; filters: Record<string, string> };
    close: () => void;
}) {
    const [page, setPage] = useState(1);
    const records = useWorkbench<WorkbenchSchemas["PageViewOfWorkbenchRecord"]>(
        "records",
        { ...drill.filters, kind: drill.kind, page: String(page) },
    );
    return (
        <Modal
            open
            title={drill.title}
            width={840}
            onCancel={close}
            footer={null}
        >
            <WorkbenchError error={records.error} />
            <Table
                size="small"
                rowKey="id"
                loading={records.isLoading}
                dataSource={records.data?.items}
                scroll={{ x: 575 }}
                pagination={{
                    current: page,
                    pageSize: 20,
                    total: records.data?.total,
                    onChange: setPage,
                    showSizeChanger: false,
                }}
                columns={[
                    {
                        title: "来源",
                        render: (_, row) => <RecordLabel me={me} row={row} />,
                    },
                    { title: "说明", dataIndex: "detail" },
                    {
                        title: "状态",
                        dataIndex: "status",
                        render: (v: string) => (
                            <Tag>{workbenchStatus[v] ?? v}</Tag>
                        ),
                    },
                    { title: "数量", dataIndex: "quantity" },
                    ...(["wait", "cycle"].includes(drill.kind)
                        ? [
                              {
                                  title: "分钟",
                                  dataIndex: "minutes",
                                  render: (value: number) => metric(value, 2),
                              },
                          ]
                        : []),
                    {
                        title: "时间",
                        dataIndex: "occurredAt",
                        render: (v: string) => <TimeCell value={v} />,
                    },
                ]}
            />
        </Modal>
    );
}

export function WorkbenchOverview({ me }: { me: Me }) {
    const options =
        useWorkbench<WorkbenchSchemas["WorkbenchOptions"]>("options");
    const [filters, setFilters] = useState<Record<string, string>>({});
    const [drill, setDrill] = useState<(typeof cards)[number]>();
    const change = (key: string) => (value: string) =>
        setFilters((old) => ({ ...old, [key]: value }));
    const overview = useWorkbench<Overview>("overview", filters);

    return (
        <Space orientation="vertical" size="middle" className="workbench-stack">
            <div className="workbench-filters">
                {!!options.data?.areas.length && (
                    <WorkbenchSelect
                        label="区域"
                        options={options.data.areas}
                        value={filters.areaId}
                        change={change("areaId")}
                    />
                )}
                <WorkbenchSelect
                    label="工序"
                    options={options.data?.operations}
                    value={filters.operationId}
                    change={change("operationId")}
                />
                {!!options.data?.groups.length && (
                    <WorkbenchSelect
                        label="设备组"
                        options={options.data.groups}
                        value={filters.equipmentGroupId}
                        change={change("equipmentGroupId")}
                    />
                )}
                <Typography.Text
                    type="secondary"
                    className="workbench-snapshot"
                >
                    {overview.data?.asOf && (
                        <>
                            快照 <TimeCell value={overview.data.asOf} />
                        </>
                    )}
                </Typography.Text>
            </div>
            <WorkbenchError error={options.error || overview.error} />
            <Card loading={overview.isLoading} className="workbench-kpis">
                <div className="workbench-kpi-grid">
                    {cards.map((card) => {
                        const value = overview.data?.[card.key];
                        if (value == null) return null;
                        const { icon: Icon, share, link } = card;
                        const total = share
                            ? overview.data?.[share.total]
                            : undefined;
                        const destination =
                            link.permission && !has(me, link.permission)
                                ? lotLink
                                : link;

                        return (
                            <article
                                className="workbench-kpi"
                                key={card.key}
                                data-tone={card.tone}
                            >
                                <div className="workbench-kpi-heading">
                                    <span className="workbench-kpi-icon">
                                        <Icon aria-hidden />
                                    </span>
                                    <h3>
                                        <Link
                                            className="workbench-kpi-link"
                                            to={destination.to}
                                            aria-label={`${card.title} · ${destination.label}`}
                                            title={destination.label}
                                        >
                                            {card.title}
                                        </Link>
                                    </h3>
                                </div>
                                <div className="workbench-kpi-value">
                                    <strong>{metric(value)}</strong>
                                    <small>{card.unit}</small>
                                </div>
                                <div className="workbench-kpi-note">
                                    {share && total != null && total > 0 ? (
                                        <>
                                            <span>{share.label}</span>
                                            <span>
                                                {metric((value / total) * 100)}%
                                            </span>
                                            <progress
                                                aria-label={share.label}
                                                value={value}
                                                max={total}
                                            />
                                        </>
                                    ) : (
                                        card.note
                                    )}
                                </div>
                                <div className="workbench-kpi-footer">
                                    <button
                                        type="button"
                                        aria-label={`查看${card.title}明细`}
                                        onClick={() => setDrill(card)}
                                    >
                                        查看明细
                                    </button>
                                    <ArrowRightOutlined aria-hidden />
                                </div>
                            </article>
                        );
                    })}
                </div>
            </Card>
            {drill && (
                <WorkbenchRecords
                    me={me}
                    drill={{ ...drill, filters }}
                    close={() => setDrill(undefined)}
                />
            )}
        </Space>
    );
}
