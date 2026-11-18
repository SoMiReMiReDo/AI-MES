import { CalendarOutlined, ThunderboltOutlined } from "@ant-design/icons";
import {
    App,
    Alert,
    Button,
    Card,
    Empty,
    Form,
    Input,
    InputNumber,
    Modal,
    Select,
    Space,
    Table,
    Tag,
    Typography,
} from "antd";
import { useQueryClient } from "@tanstack/react-query";
import { useRef, useState, type DragEvent } from "react";
import { Link } from "react-router-dom";
import { api, type Me } from "./api";
import {
    asLocalInput,
    fabDate,
    has,
    TimeCell,
    useWorkbench,
    WorkbenchError,
    WorkbenchSelect,
    workbenchPermissions as permission,
    workbenchStatus,
    type WorkbenchSchemas,
} from "./workbenchShared";

type Plan = WorkbenchSchemas["WorkbenchPlan"];
type QueueRow = WorkbenchSchemas["WorkbenchQueueItem"];
type Schedule = WorkbenchSchemas["WorkbenchSchedule"];
type PlanInput = WorkbenchSchemas["WorkbenchPlanInput"];
type AutoInput = WorkbenchSchemas["WorkbenchAutoPlanInput"];
type AutoResult = WorkbenchSchemas["WorkbenchAutoResult"];
type History = WorkbenchSchemas["WorkbenchPlanChange"];
type FuturePlan = { row: QueueRow | Plan; plan?: Plan };

const planActions: Record<string, string> = {
    ScheduleCreated: "创建",
    ScheduleMoved: "调整",
    ScheduleCancelled: "取消",
    ScheduleAutoCreated: "自动排程",
};

function timeLabel(value: number, timezone?: string | null) {
    return new Intl.DateTimeFormat("zh-CN", {
        timeZone: timezone || "Asia/Shanghai",
        hour: "2-digit",
        minute: "2-digit",
        hourCycle: "h23",
    }).format(new Date(value));
}

function Timeline({
    data,
    timezone,
    manage,
    move,
    show,
}: {
    data: Schedule;
    timezone?: string | null;
    manage: boolean;
    move: (row: Plan, tool: string, at: Date) => void;
    show: (row: Plan) => void;
}) {
    const start = Date.parse(data.startsAt);
    const end = Date.parse(data.endsAt);
    const span = end - start;
    const position = (a: string | number, b: string | number) => {
        const left = Math.max(start, new Date(a).getTime());
        const right = Math.min(end, new Date(b).getTime());
        return {
            left: `${((left - start) / span) * 100}%`,
            width: `${Math.max(0, ((right - left) / span) * 100)}%`,
        };
    };
    const drop = (event: DragEvent<HTMLDivElement>, id: string) => {
        event.preventDefault();
        const row = data.plans.find(
            (plan) => plan.id === event.dataTransfer.getData("text/plain"),
        );
        if (!row) return;
        const rect = event.currentTarget.getBoundingClientRect();
        const step = 15 * 60 * 1000;
        const time =
            start +
            Math.round(
                (((event.clientX - rect.left) / rect.width) * span) / step,
            ) *
                step;
        move(row, id, new Date(time));
    };
    return (
        <div className="workbench-calendar-scroll">
            <div className="workbench-calendar">
                <div className="workbench-calendar-ruler">
                    <strong>设备</strong>
                    <div>
                        {Array.from({ length: 9 }, (_, i) => (
                            <span key={i}>
                                {timeLabel(start + (span * i) / 8, timezone)}
                            </span>
                        ))}
                    </div>
                </div>
                {data.equipment.map((tool) => (
                    <div className="workbench-calendar-row" key={tool.id}>
                        <div
                            className="workbench-calendar-tool"
                            title={tool.name}
                        >
                            <strong>{tool.code}</strong>
                            <small>{tool.name}</small>
                        </div>
                        <div
                            className="workbench-calendar-lane"
                            data-testid={`schedule-lane-${tool.id}`}
                            onDragOver={
                                manage
                                    ? (event) => {
                                          if (
                                              event.dataTransfer.types.includes(
                                                  "text/plain",
                                              )
                                          ) {
                                              event.preventDefault();
                                              event.dataTransfer.dropEffect =
                                                  "move";
                                          }
                                      }
                                    : undefined
                            }
                            onDrop={
                                manage
                                    ? (event) => drop(event, tool.id)
                                    : undefined
                            }
                        >
                            {data.capacity
                                .filter((row) => row.equipmentId === tool.id)
                                .map((row, index) => {
                                    const ableUntil = row.blockedAt
                                        ? Math.min(
                                              Date.parse(row.blockedAt),
                                              Date.parse(row.endsAt),
                                          )
                                        : Date.parse(row.endsAt);
                                    return (
                                        <div
                                            key={index}
                                            className="workbench-shift-band"
                                            style={position(
                                                row.startsAt,
                                                ableUntil,
                                            )}
                                            title={`${row.label} · ${timeLabel(Date.parse(row.startsAt), timezone)}–${timeLabel(ableUntil, timezone)}`}
                                        />
                                    );
                                })}
                            {data.plans
                                .filter(
                                    (row) =>
                                        row.equipmentId === tool.id &&
                                        row.status !== "Cancelled",
                                )
                                .map((row) => (
                                    <button
                                        type="button"
                                        key={row.id}
                                        className={`workbench-plan-pill ${row.status.toLowerCase()}`}
                                        style={position(
                                            row.startsAt,
                                            row.endsAt,
                                        )}
                                        title={`${row.lotNumber} · ${row.step} · ${timeLabel(Date.parse(row.startsAt), timezone)}`}
                                        aria-label={`计划 ${row.lotNumber}`}
                                        draggable={
                                            manage &&
                                            [
                                                "Planned",
                                                "Conflict",
                                                "Expired",
                                            ].includes(row.status)
                                        }
                                        onDragStart={(event) => {
                                            event.dataTransfer.effectAllowed =
                                                "move";
                                            event.dataTransfer.setData(
                                                "text/plain",
                                                row.id,
                                            );
                                        }}
                                        onClick={() => show(row)}
                                    >
                                        {row.lotNumber}
                                    </button>
                                ))}
                        </div>
                    </div>
                ))}
            </div>
            {data.equipment.length === 0 && (
                <Empty description="没有启用的设备" />
            )}
        </div>
    );
}

export function WorkbenchSchedule({ me }: { me: Me }) {
    const [day, setDay] = useState(fabDate(me.fab?.timezone));
    const [equipmentId, setEquipmentId] = useState("");
    const [page, setPage] = useState(1);
    const [edit, setEdit] = useState<FuturePlan>();
    const [view, setView] = useState<Plan>();
    const [cancel, setCancel] = useState<Plan>();
    const [auto, setAuto] = useState(false);
    const [result, setResult] = useState<AutoResult>();
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [form] = Form.useForm<PlanInput>();
    const [autoForm] = Form.useForm<AutoInput>();
    const autoKey = useRef<{ body: string; key: string } | undefined>(
        undefined,
    );
    const { message } = App.useApp();
    const client = useQueryClient();
    const manage = has(me, permission.schedule);
    const options =
        useWorkbench<WorkbenchSchemas["WorkbenchOptions"]>("options");
    const schedule = useWorkbench<Schedule>(
        "schedule",
        { from: day, to: day, equipmentId, page: String(page) },
        !!day,
    );
    const history = useWorkbench<History[]>(
        `schedule/${view?.id}/history`,
        {},
        !!view,
    );

    function open(row: QueueRow | Plan, plan?: Plan) {
        const range = schedule.data;
        const capacity = range?.capacity.find(
            (item) =>
                item.equipmentId === equipmentId ||
                (!equipmentId && Date.parse(item.endsAt) > Date.now()),
        );
        const earliest = Math.max(
            Date.now() + 120000,
            capacity
                ? Date.parse(capacity.startsAt)
                : range
                  ? Date.parse(range.startsAt)
                  : 0,
        );
        const start = Math.ceil(earliest / 60000) * 60000;
        setError("");
        form.resetFields();
        form.setFieldsValue({
            equipmentId:
                plan?.equipmentId ||
                equipmentId ||
                capacity?.equipmentId ||
                undefined,
            startsAt: asLocalInput(plan?.startsAt ?? new Date(start)),
            endsAt: asLocalInput(plan?.endsAt ?? new Date(start + 30 * 60000)),
        });
        setEdit({ row, plan });
    }

    async function submit(body: PlanInput, plan?: Plan, inDialog = false) {
        setBusy(true);
        setError("");
        try {
            await api(
                plan ? `workbench/schedule/${plan.id}` : "workbench/schedule",
                plan ? "PUT" : "POST",
                body,
                plan?.etag,
            );
            setEdit(undefined);
            message.success(plan ? "计划已调整" : "计划已创建");
            void client.invalidateQueries({ queryKey: ["workbench"] });
        } catch (error) {
            const msg = (error as Error).message;
            if (inDialog) setError(msg);
            else message.error(msg);
            void client.invalidateQueries({ queryKey: ["workbench"] });
        } finally {
            setBusy(false);
        }
    }

    async function runAuto(body: AutoInput) {
        const serialized = JSON.stringify(body);
        if (autoKey.current?.body !== serialized)
            autoKey.current = { body: serialized, key: crypto.randomUUID() };
        setBusy(true);
        setError("");
        try {
            const response = await api<AutoResult>(
                "workbench/schedule/auto",
                "POST",
                body,
                undefined,
                autoKey.current.key,
            );
            setAuto(false);
            setResult(response);
            setDay(body.from);
            void client.invalidateQueries({ queryKey: ["workbench"] });
        } catch (error) {
            setError((error as Error).message);
        } finally {
            setBusy(false);
        }
    }

    async function cancelPlan(plan: Plan) {
        setBusy(true);
        try {
            await api(
                `workbench/schedule/${plan.id}/cancel`,
                "POST",
                {},
                plan.etag,
            );
            setCancel(undefined);
            message.success("计划已取消");
            void client.invalidateQueries({ queryKey: ["workbench"] });
        } catch (error) {
            message.error((error as Error).message);
            void client.invalidateQueries({ queryKey: ["workbench"] });
        } finally {
            setBusy(false);
        }
    }

    const tools = options.data?.equipment ?? [];
    const toolName = (id: string) =>
        tools.find((item) => item.id === id)?.code ?? id;
    return (
        <Space orientation="vertical" size="middle" className="workbench-stack">
            <div className="workbench-filters">
                <label className="workbench-filter">
                    <span>日历日期（厂区）</span>
                    <Input
                        type="date"
                        aria-label="日历日期"
                        value={day}
                        onChange={(event) => setDay(event.target.value)}
                    />
                </label>
                <WorkbenchSelect
                    label="设备"
                    options={tools}
                    value={equipmentId}
                    change={setEquipmentId}
                />
                {manage && (
                    <Button
                        icon={<ThunderboltOutlined />}
                        type="primary"
                        onClick={() => {
                            setError("");
                            autoKey.current = undefined;
                            autoForm.setFieldsValue({
                                from: day,
                                to: day,
                                maxLots: 20,
                            });
                            setAuto(true);
                        }}
                    >
                        自动排程
                    </Button>
                )}
            </div>
            <WorkbenchError error={schedule.error || options.error} />
            <Card
                size="small"
                title="设备日历"
                loading={schedule.isLoading}
                extra={
                    <span className="workbench-capacity-key">班次可排时段</span>
                }
            >
                {schedule.data && (
                    <Timeline
                        data={schedule.data}
                        timezone={me.fab?.timezone}
                        manage={manage && !busy}
                        show={setView}
                        move={(row, tool, start) => {
                            const duration =
                                Date.parse(row.endsAt) -
                                Date.parse(row.startsAt);
                            void submit(
                                {
                                    stepExecutionId: row.stepExecutionId,
                                    equipmentId: tool,
                                    startsAt: start.toISOString(),
                                    endsAt: new Date(
                                        start.getTime() + duration,
                                    ).toISOString(),
                                },
                                row,
                            );
                        }}
                    />
                )}
            </Card>
            <Card size="small" title="计划明细">
                <Table
                    size="small"
                    rowKey="id"
                    dataSource={schedule.data?.plans}
                    pagination={{ pageSize: 10, showSizeChanger: false }}
                    scroll={{ x: 760 }}
                    columns={[
                        {
                            title: "批次",
                            dataIndex: "lotNumber",
                            render: (label, row) => (
                                <Link to={`/execution?lot=${row.lotId}`}>
                                    {label}
                                </Link>
                            ),
                        },
                        { title: "工序", dataIndex: "step" },
                        {
                            title: "设备",
                            dataIndex: "equipmentId",
                            render: toolName,
                        },
                        {
                            title: "开始",
                            dataIndex: "startsAt",
                            render: (value) => <TimeCell value={value} />,
                        },
                        {
                            title: "结束",
                            dataIndex: "endsAt",
                            render: (value) => <TimeCell value={value} />,
                        },
                        {
                            title: "状态",
                            dataIndex: "status",
                            render: (value) => (
                                <Tag>{workbenchStatus[value] ?? value}</Tag>
                            ),
                        },
                        {
                            title: "操作",
                            render: (_, row) => (
                                <Space wrap>
                                    <Button
                                        size="small"
                                        onClick={() => setView(row)}
                                    >
                                        变更记录
                                    </Button>
                                    {manage && row.status !== "Cancelled" && (
                                        <>
                                            {row.status !== "Inactive" && (
                                                <Button
                                                    size="small"
                                                    onClick={() =>
                                                        open(row, row)
                                                    }
                                                >
                                                    调整
                                                </Button>
                                            )}
                                            <Button
                                                size="small"
                                                onClick={() => setCancel(row)}
                                            >
                                                取消计划
                                            </Button>
                                        </>
                                    )}
                                </Space>
                            ),
                        },
                    ]}
                />
            </Card>
            <Card size="small" title="未排程批次">
                <Table
                    size="small"
                    rowKey="stepExecutionId"
                    dataSource={schedule.data?.queue.items}
                    scroll={{ x: 585 }}
                    pagination={{
                        current: page,
                        pageSize: 20,
                        total: schedule.data?.queue.total,
                        onChange: setPage,
                        showSizeChanger: false,
                    }}
                    columns={[
                        {
                            title: "批次",
                            dataIndex: "lotNumber",
                            render: (label, row) => (
                                <Link to={`/execution?lot=${row.lotId}`}>
                                    {label}
                                </Link>
                            ),
                        },
                        { title: "工序", dataIndex: "step" },
                        { title: "优先级", dataIndex: "priority" },
                        {
                            title: "进入等待",
                            dataIndex: "enteredAt",
                            render: (value) => <TimeCell value={value} />,
                        },
                        ...(manage
                            ? [
                                  {
                                      title: "操作",
                                      render: (_: unknown, row: QueueRow) => (
                                          <Button
                                              icon={<CalendarOutlined />}
                                              size="small"
                                              onClick={() => open(row)}
                                          >
                                              排程
                                          </Button>
                                      ),
                                  },
                              ]
                            : []),
                    ]}
                />
            </Card>
            <Modal
                open={!!edit}
                title={
                    edit
                        ? `${edit.plan ? "调整" : "创建"}计划 · ${edit.row.lotNumber}`
                        : "排程"
                }
                onCancel={() => setEdit(undefined)}
                confirmLoading={busy}
                onOk={() => form.submit()}
            >
                {error && edit && <Alert type="error" showIcon title={error} />}
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={(value: PlanInput) => {
                        if (!edit) return;
                        void submit(
                            {
                                ...value,
                                stepExecutionId: edit.row.stepExecutionId,
                                startsAt: new Date(
                                    value.startsAt,
                                ).toISOString(),
                                endsAt: new Date(value.endsAt).toISOString(),
                            },
                            edit.plan,
                            true,
                        );
                    }}
                >
                    <Form.Item
                        name="equipmentId"
                        label="设备"
                        rules={[{ required: true }]}
                    >
                        <Select
                            showSearch={{ optionFilterProp: "label" }}
                            options={tools.map((x) => ({
                                value: x.id,
                                label: `${x.code} · ${x.name}`,
                            }))}
                        />
                    </Form.Item>
                    <Form.Item
                        name="startsAt"
                        label="开始（浏览器时区）"
                        rules={[{ required: true }]}
                    >
                        <Input type="datetime-local" />
                    </Form.Item>
                    <Form.Item
                        name="endsAt"
                        label="结束（浏览器时区）"
                        rules={[{ required: true }]}
                    >
                        <Input type="datetime-local" />
                    </Form.Item>
                </Form>
            </Modal>
            <Modal
                open={auto}
                title="自动排程"
                onCancel={() => setAuto(false)}
                confirmLoading={busy}
                onOk={() => autoForm.submit()}
            >
                {error && auto && <Alert type="error" showIcon title={error} />}
                <Form form={autoForm} layout="vertical" onFinish={runAuto}>
                    <Form.Item
                        name="from"
                        label="起始日期（厂区）"
                        rules={[{ required: true }]}
                    >
                        <Input type="date" />
                    </Form.Item>
                    <Form.Item
                        name="to"
                        label="结束日期（厂区）"
                        rules={[{ required: true }]}
                    >
                        <Input type="date" />
                    </Form.Item>
                    <Form.Item
                        name="maxLots"
                        label="最多尝试批次数"
                        rules={[{ required: true }]}
                    >
                        <InputNumber min={1} max={50} precision={0} />
                    </Form.Item>
                </Form>
            </Modal>
            <Modal
                open={!!result}
                title="自动排程结果"
                footer={null}
                onCancel={() => setResult(undefined)}
            >
                <Typography.Paragraph>
                    已生成 {result?.created.length ?? 0} 条计划，
                    {result?.unplanned.length ?? 0} 批未排。
                </Typography.Paragraph>
                <Table
                    size="small"
                    rowKey="lotId"
                    dataSource={result?.unplanned}
                    pagination={{ pageSize: 8 }}
                    columns={[
                        { title: "未排批次", dataIndex: "lotNumber" },
                        { title: "原因", dataIndex: "reason" },
                    ]}
                />
            </Modal>
            <Modal
                open={!!cancel}
                title="取消计划"
                onCancel={() => setCancel(undefined)}
                confirmLoading={busy}
                okText="取消该计划"
                cancelText="返回"
                onOk={() => cancel && void cancelPlan(cancel)}
            >
                <Typography.Paragraph>
                    {cancel?.lotNumber} · {cancel?.step}
                </Typography.Paragraph>
            </Modal>
            <Modal
                open={!!view}
                title={`变更记录 · ${view?.lotNumber ?? ""}`}
                onCancel={() => setView(undefined)}
                width={760}
                footer={null}
            >
                <WorkbenchError error={history.error} />
                <Table
                    size="small"
                    rowKey={(row) => `${row.action}/${row.occurredAt}`}
                    dataSource={history.data}
                    loading={history.isLoading}
                    scroll={{ x: 570 }}
                    columns={[
                        {
                            title: "时间",
                            dataIndex: "occurredAt",
                            render: (value) => <TimeCell value={value} />,
                        },
                        {
                            title: "操作",
                            dataIndex: "action",
                            render: (value) => planActions[value] ?? value,
                        },
                        { title: "操作人", dataIndex: "actor" },
                        {
                            title: "原计划开始",
                            render: (_, row) => (
                                <TimeCell value={row.before?.startsAt} />
                            ),
                        },
                        {
                            title: "变更后开始",
                            render: (_, row) => (
                                <TimeCell value={row.after?.startsAt} />
                            ),
                        },
                    ]}
                />
            </Modal>
        </Space>
    );
}
