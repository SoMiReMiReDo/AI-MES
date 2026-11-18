import {
    App,
    Button,
    Card,
    Form,
    Input,
    InputNumber,
    Modal,
    Segmented,
    Space,
    Table,
    Typography,
} from "antd";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api, type Me } from "./api";
import { WorkbenchRecords } from "./workbenchOverview";
import {
    fabDate,
    has,
    metric,
    TimeCell,
    useWorkbench,
    WorkbenchError,
    WorkbenchSelect,
    workbenchPermissions as permission,
    type WorkbenchSchemas,
} from "./workbenchShared";

type Day = WorkbenchSchemas["WorkbenchHistoryDay"];

const historyMetrics = [
    { key: "passed", label: "通过片次", kind: "passed" },
    { key: "completed", label: "终点完工片数", kind: "completed" },
    { key: "failed", label: "不合格片次", kind: "failed" },
    { key: "scrap", label: "报废片数", kind: "scrap" },
    { key: "failedChecks", label: "未通过检验", kind: "failed-checks" },
    { key: "waitMinutes", label: "平均等待（分）", kind: "wait" },
    { key: "cycleMinutes", label: "平均周期（分）", kind: "cycle" },
] as const;

function DateFilters({
    from,
    to,
    change,
}: {
    from: string;
    to: string;
    change: (name: string, value: string) => void;
}) {
    return (
        <>
            <label className="workbench-filter">
                <span>起始日期</span>
                <Input
                    type="date"
                    aria-label="起始日期"
                    value={from}
                    onChange={(event) => change("from", event.target.value)}
                />
            </label>
            <label className="workbench-filter">
                <span>结束日期</span>
                <Input
                    type="date"
                    aria-label="结束日期"
                    value={to}
                    onChange={(event) => change("to", event.target.value)}
                />
            </label>
        </>
    );
}

export function WorkbenchReports({ me }: { me: Me }) {
    const [filters, setFilters] = useState({
        from: fabDate(me.fab?.timezone, -6),
        to: fabDate(me.fab?.timezone),
        productId: "",
        operationId: "",
        equipmentId: "",
        shiftId: "",
    });
    const [selected, setSelected] = useState<string>();
    const [drill, setDrill] = useState<{
        kind: string;
        title: string;
        filters: Record<string, string>;
    }>();
    const options =
        useWorkbench<WorkbenchSchemas["WorkbenchOptions"]>("options");
    const result = useWorkbench<Day[]>(
        "history",
        filters,
        !!filters.from && !!filters.to,
    );
    const data = result.data ?? [];
    const metrics = historyMetrics.filter(({ key }) =>
        data.some((day) => day[key] != null),
    );
    const active = metrics.find((item) => item.key === selected) ?? metrics[0];
    const max = active
        ? Math.max(1, ...data.map((day) => day[active.key] ?? 0))
        : 1;
    const change = (key: string, value: string) =>
        setFilters((old) => ({ ...old, [key]: value }));
    return (
        <Space orientation="vertical" size="middle" className="workbench-stack">
            <div className="workbench-filters">
                <DateFilters
                    from={filters.from}
                    to={filters.to}
                    change={change}
                />
                <WorkbenchSelect
                    label="产品"
                    options={options.data?.products}
                    value={filters.productId}
                    change={(v) => change("productId", v)}
                />
                <WorkbenchSelect
                    label="工序"
                    options={options.data?.operations}
                    value={filters.operationId}
                    change={(v) => change("operationId", v)}
                />
                {has(me, permission.equipment) && (
                    <WorkbenchSelect
                        label="设备"
                        options={options.data?.equipment}
                        value={filters.equipmentId}
                        change={(v) => change("equipmentId", v)}
                    />
                )}
                {!!options.data?.shifts.length && (
                    <WorkbenchSelect
                        label="班次"
                        options={options.data.shifts}
                        value={filters.shiftId}
                        change={(v) => change("shiftId", v)}
                    />
                )}
            </div>
            <WorkbenchError error={result.error || options.error} />
            {active && (
                <Card size="small" title="按日趋势" className="workbench-trend">
                    <Segmented
                        value={active.key}
                        options={metrics.map((item) => ({
                            value: item.key,
                            label: item.label,
                        }))}
                        onChange={(value) => setSelected(value)}
                    />
                    <figure
                        className="workbench-chart"
                        aria-label={active.label}
                    >
                        {data.map((day) => (
                            <div key={day.date} className="workbench-bar-item">
                                <span>{metric(day[active.key], 1)}</span>
                                <button
                                    type="button"
                                    title={`${day.date} · ${metric(day[active.key], 1)}`}
                                    aria-label={`${day.date} ${active.label}`}
                                    style={{
                                        height: `${Math.max(4, ((day[active.key] ?? 0) * 106) / max)}px`,
                                    }}
                                    onClick={() =>
                                        setDrill({
                                            kind: active.kind,
                                            title: `${day.date} · ${active.label}`,
                                            filters: {
                                                ...filters,
                                                date: day.date,
                                            },
                                        })
                                    }
                                />
                                <small>{day.date.slice(5)}</small>
                            </div>
                        ))}
                    </figure>
                </Card>
            )}
            <Table
                size="small"
                rowKey="date"
                dataSource={data}
                loading={result.isLoading}
                scroll={{ x: 820 }}
                pagination={false}
                columns={[
                    { title: "厂区日期", dataIndex: "date" },
                    ...metrics.map(({ key, label, kind }) => ({
                        title: label,
                        dataIndex: key,
                        render: (value: number | null, day: Day) =>
                            value == null ? (
                                "—"
                            ) : (
                                <Button
                                    type="link"
                                    size="small"
                                    onClick={() =>
                                        setDrill({
                                            kind,
                                            title: `${day.date} · ${label}`,
                                            filters: {
                                                ...filters,
                                                date: day.date,
                                            },
                                        })
                                    }
                                >
                                    {metric(value, 1)}
                                </Button>
                            ),
                    })),
                ]}
            />
            {drill && (
                <WorkbenchRecords
                    me={me}
                    drill={drill}
                    close={() => setDrill(undefined)}
                />
            )}
        </Space>
    );
}

const percent = (value?: number | null) =>
    value == null ? "—" : `${metric(value * 100, 1)}%`;

export function WorkbenchShifts({ me }: { me: Me }) {
    const today = fabDate(me.fab?.timezone);
    const [filters, setFilters] = useState({
        from: today,
        to: today,
        equipmentId: "",
    });
    const [page, setPage] = useState(1);
    const [rate, setRate] = useState(false);
    const [busy, setBusy] = useState(false);
    const [form] = Form.useForm<WorkbenchSchemas["AddWorkbenchRate"]>();
    const { message } = App.useApp();
    const client = useQueryClient();
    const options =
        useWorkbench<WorkbenchSchemas["WorkbenchOptions"]>("options");
    const results = useWorkbench<WorkbenchSchemas["WorkbenchShifts"]>(
        "shifts",
        { ...filters, page: String(page) },
        !!filters.from && !!filters.to,
    );
    const change = (key: string, value: string) => {
        setPage(1);
        setFilters((old) => ({ ...old, [key]: value }));
    };
    const save = async (body: WorkbenchSchemas["AddWorkbenchRate"]) => {
        setBusy(true);
        try {
            await api("workbench/rates", "POST", body);
            setRate(false);
            void client.invalidateQueries({ queryKey: ["workbench"] });
            message.success("理想产速已登记");
        } catch (error) {
            message.error((error as Error).message);
        } finally {
            setBusy(false);
        }
    };
    return (
        <Space orientation="vertical" size="middle" className="workbench-stack">
            <div className="workbench-filters">
                <DateFilters
                    from={filters.from}
                    to={filters.to}
                    change={change}
                />
                <WorkbenchSelect
                    label="设备"
                    options={options.data?.equipment}
                    value={filters.equipmentId}
                    change={(v) => change("equipmentId", v)}
                />
                {has(me, permission.rate) && (
                    <Button
                        onClick={() => {
                            form.resetFields();
                            form.setFieldsValue({
                                equipmentId: filters.equipmentId || undefined,
                                effectiveOn: today,
                            });
                            setRate(true);
                        }}
                    >
                        登记理想产速
                    </Button>
                )}
            </div>
            <WorkbenchError error={results.error || options.error} />
            <Card size="small" title="设备班次与损失">
                <Table
                    size="small"
                    rowKey={(row) =>
                        `${row.equipmentId}/${row.shift}/${row.startsAt}`
                    }
                    loading={results.isLoading}
                    dataSource={results.data?.metrics.items}
                    scroll={{ x: 1370 }}
                    pagination={{
                        current: page,
                        pageSize: 20,
                        total: results.data?.metrics.total,
                        onChange: setPage,
                        showSizeChanger: false,
                    }}
                    columns={[
                        { title: "设备", dataIndex: "equipmentCode" },
                        {
                            title: "班次",
                            dataIndex: "shift",
                            render: (v) => v ?? "—",
                        },
                        {
                            title: "开始时间",
                            dataIndex: "startsAt",
                            render: (v) => <TimeCell value={v} />,
                        },
                        {
                            title: "计划分钟",
                            dataIndex: "plannedMinutes",
                            render: (v) => metric(v, 1),
                        },
                        {
                            title: "运行分钟",
                            dataIndex: "runMinutes",
                            render: (v) => metric(v, 1),
                        },
                        {
                            title: "登记停机",
                            dataIndex: "downtimeMinutes",
                            render: (v) => metric(v, 1),
                        },
                        {
                            title: "非运行",
                            dataIndex: "nonRunningMinutes",
                            render: (v) => metric(v, 1),
                        },
                        {
                            title: "性能损失",
                            dataIndex: "performanceLossMinutes",
                            render: (v) => metric(v, 1),
                        },
                        {
                            title: "不合格片次",
                            dataIndex: "failedWafers",
                            render: (v) => metric(v, 1),
                        },
                        {
                            title: "理想秒/片",
                            dataIndex: "idealSeconds",
                            render: (v) => metric(v, 3),
                        },
                        {
                            title: "可用率",
                            dataIndex: "availability",
                            render: percent,
                        },
                        {
                            title: "性能",
                            dataIndex: "performance",
                            render: percent,
                        },
                        {
                            title: "质量率",
                            dataIndex: "quality",
                            render: percent,
                        },
                        {
                            title: "OEE",
                            dataIndex: "oee",
                            render: (v) => <strong>{percent(v)}</strong>,
                        },
                        { title: "覆盖说明", dataIndex: "missing" },
                    ]}
                />
            </Card>
            <Card size="small" title="当前工序排队">
                <Table
                    size="small"
                    rowKey="operationId"
                    dataSource={results.data?.bottlenecks}
                    pagination={false}
                    scroll={{ x: 440 }}
                    columns={[
                        { title: "工序", dataIndex: "name" },
                        { title: "等待批次", dataIndex: "waiting" },
                        {
                            title: "最早进入等待",
                            dataIndex: "oldest",
                            render: (v) => <TimeCell value={v} />,
                        },
                    ]}
                />
            </Card>
            <Modal
                open={rate}
                title="登记设备理想产速"
                onCancel={() => setRate(false)}
                confirmLoading={busy}
                onOk={() => form.submit()}
            >
                <Form layout="vertical" form={form} onFinish={save}>
                    <Form.Item
                        name="equipmentId"
                        label="设备"
                        rules={[{ required: true }]}
                    >
                        <RateEquipment options={options.data?.equipment} />
                    </Form.Item>
                    <Form.Item
                        name="effectiveOn"
                        label="生效日期（厂区）"
                        rules={[{ required: true }]}
                    >
                        <Input type="date" />
                    </Form.Item>
                    <Form.Item
                        name="idealSeconds"
                        label="理想秒/片"
                        rules={[{ required: true }]}
                    >
                        <InputNumber min={0.001} max={86400} precision={3} />
                    </Form.Item>
                    <Typography.Text type="secondary">
                        每台设备同一天可登记一条生效记录。
                    </Typography.Text>
                </Form>
            </Modal>
        </Space>
    );
}

function RateEquipment({
    value,
    onChange,
    options,
}: {
    value?: string;
    onChange?: (value: string) => void;
    options?: WorkbenchSchemas["WorkbenchOption"][];
}) {
    return (
        <WorkbenchSelect
            label="选择设备"
            value={value}
            options={options}
            change={(v) => onChange?.(v)}
        />
    );
}
