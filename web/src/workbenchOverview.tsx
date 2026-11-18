import { ArrowRightOutlined } from "@ant-design/icons";
import { Card, Modal, Space, Table, Tag, Typography } from "antd";
import { useState } from "react";
import { type Me } from "./api";
import {
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

const cards: {
    key: keyof Omit<Overview, "asOf">;
    kind: string;
    title: string;
    unit: string;
}[] = [
    { key: "wipLots", kind: "wip", title: "在制批次", unit: "批" },
    { key: "wipWafers", kind: "wip", title: "在制晶圆", unit: "片" },
    { key: "waiting", kind: "waiting", title: "待加工", unit: "批" },
    { key: "running", kind: "running", title: "运行中", unit: "Run" },
    { key: "held", kind: "held", title: "Hold 批次", unit: "批" },
    { key: "equipment", kind: "equipment", title: "启用设备", unit: "台" },
    { key: "equipmentDown", kind: "down", title: "停机设备", unit: "台" },
    {
        key: "completedToday",
        kind: "completed",
        title: "今日终点完工",
        unit: "片",
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
                        return (
                            <button
                                className="workbench-kpi"
                                type="button"
                                key={card.key}
                                onClick={() => setDrill(card)}
                            >
                                <span>
                                    {card.title}
                                    <ArrowRightOutlined />
                                </span>
                                <div>
                                    <strong>{metric(value)}</strong>
                                    <small>{card.unit}</small>
                                </div>
                            </button>
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
