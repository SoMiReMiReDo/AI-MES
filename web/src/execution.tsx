import { useEffect, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    App,
    Button,
    Checkbox,
    Descriptions,
    Drawer,
    Empty,
    Form,
    Input,
    InputNumber,
    Modal,
    Select,
    Space,
    Table,
    Tabs,
    Tag,
    Typography,
} from "antd";
import { api, ApiError, type Me } from "./api";
import { fmt } from "./shared";
import type { components } from "./generated/api";

type Item = { id: string; etag: string; [key: string]: any };
type Page = { items: Item[]; total: number };
const required = [{ required: true, message: "请填写此项" }];
const names: Record<string, string> = {
    Ready: "等待加工",
    Waiting: "等待加工",
    Running: "加工中",
    Completed: "已完成",
    Aborted: "已中止",
    AwaitingDisposition: "等待处置",
    Idle: "空闲可用",
    Down: "故障",
    Maintenance: "维护",
    Offline: "离线",
    Passed: "通过",
    Failed: "失败",
};
const status = (value: string) => (
    <Tag color={value === "Running" ? "blue" : undefined}>
        {names[value] || value}
    </Tag>
);
const options = (items?: Item[]) =>
    (items || []).map((x) => ({
        value: x.id,
        label: `${x.code || ""} ${x.name}`.trim(),
    }));

function useCommand() {
    const { message } = App.useApp();
    const client = useQueryClient();
    const [busy, setBusy] = useState(false);
    const pending = useRef<{ signature: string; key: string } | null>(null);
    const send = async (
        path: string,
        body: unknown,
        etag?: string,
        method = "POST",
    ) => {
        const signature = JSON.stringify([path, method, body, etag]);
        if (pending.current?.signature !== signature)
            pending.current = { signature, key: crypto.randomUUID() };
        setBusy(true);
        try {
            const result = await api(
                path,
                method,
                body,
                etag,
                pending.current.key,
            );
            pending.current = null;
            await client.invalidateQueries();
            message.success({ content: "操作成功", key: "execution-command" });
            return result;
        } catch (error) {
            const text = (error as Error).message;
            message.error(
                error instanceof ApiError &&
                    [409, 412, 428].includes(error.status)
                    ? `${text}；请刷新并重新核对`
                    : text,
            );
            throw error;
        } finally {
            setBusy(false);
        }
    };
    return { send, busy };
}

export function Execution({ me }: { me: Me }) {
    const [filters, setFilters] = useState<Record<string, string>>({});
    const [page, setPage] = useState(1);
    const [tab, setTab] = useState("dispatch");
    const [runStatus, setRunStatus] = useState("Running");
    const [lotId, setLotId] = useState<string | null>(null);
    const [runId, setRunId] = useState<string | null>(null);
    const params = new URLSearchParams({ ...filters, page: String(page) });
    const candidates = useQuery({
        queryKey: ["execution", "options"],
        queryFn: () => api("execution/options"),
    });
    const query = useQuery({
        queryKey: ["execution", tab, params.toString(), runStatus],
        queryFn: () =>
            api<Page>(
                tab === "dispatch"
                    ? `execution/dispatch?${params}`
                    : `execution/runs?status=${runStatus}&page=${page}`,
            ),
    });
    return (
        <>
            <Typography.Title level={2}>生产执行</Typography.Title>
            <Space wrap>
                <Button onClick={() => query.refetch()}>刷新列表</Button>
                <Input.Search
                    placeholder="输入批次 ID 打开执行详情"
                    onSearch={(value) => value && setLotId(value.trim())}
                    enterButton="打开批次"
                />
            </Space>
            <Tabs
                activeKey={tab}
                onChange={(value) => {
                    setTab(value);
                    setPage(1);
                }}
                items={[
                    { key: "dispatch", label: "待加工派工" },
                    { key: "runs", label: "加工与历史" },
                ]}
            />
            {tab === "dispatch" ? (
                <Form
                    layout="inline"
                    onFinish={(values) => {
                        setFilters(
                            Object.fromEntries(
                                Object.entries(values)
                                    .filter(([, value]) => value != null)
                                    .map(([key, value]) => [
                                        key,
                                        String(value),
                                    ]),
                            ),
                        );
                        setPage(1);
                    }}
                >
                    <Form.Item name="lotNumber" label="批次号">
                        <Input allowClear />
                    </Form.Item>
                    <Form.Item name="equipmentGroupId" label="设备组">
                        <Select
                            allowClear
                            style={{ width: 160 }}
                            options={options(candidates.data?.groups)}
                        />
                    </Form.Item>
                    <Form.Item name="routeStepId" label="当前步骤">
                        <Select
                            allowClear
                            style={{ width: 160 }}
                            options={options(candidates.data?.steps)}
                        />
                    </Form.Item>
                    <Form.Item name="minPriority" label="最低优先级">
                        <InputNumber min={0} max={100} />
                    </Form.Item>
                    <Form.Item name="minWaitingMinutes" label="等待分钟≥">
                        <InputNumber min={0} max={5256000} />
                    </Form.Item>
                    <Button htmlType="submit">筛选</Button>
                </Form>
            ) : (
                <Select
                    aria-label="加工状态"
                    style={{ width: 180 }}
                    value={runStatus}
                    onChange={(value) => {
                        setRunStatus(value);
                        setPage(1);
                    }}
                    options={["Running", "Completed", "Aborted", ""].map(
                        (value) => ({
                            value,
                            label: names[value] || "全部历史",
                        }),
                    )}
                />
            )}
            {query.error && <Alert type="error" title={query.error.message} />}
            <Table
                style={{ marginTop: 16 }}
                rowKey={(row) => row.lot?.id || row.id}
                loading={query.isLoading}
                dataSource={query.data?.items}
                pagination={{
                    current: page,
                    total: query.data?.total,
                    pageSize: 20,
                    showSizeChanger: false,
                    onChange: setPage,
                }}
                columns={
                    tab === "dispatch"
                        ? [
                              {
                                  title: "批次号",
                                  render: (_, row) => (
                                      <Button
                                          type="link"
                                          onClick={() => setLotId(row.lot.id)}
                                      >
                                          {row.lot.lotNumber}
                                      </Button>
                                  ),
                              },
                              {
                                  title: "当前步骤",
                                  render: (_, row) => row.step.name,
                              },
                              {
                                  title: "优先级",
                                  render: (_, row) => row.lot.priority,
                              },
                              {
                                  title: "等待分钟",
                                  render: (_, row) =>
                                      Math.floor(row.waitingMinutes),
                              },
                              {
                                  title: "可执行性",
                                  render: (_, row) =>
                                      row.executable ? (
                                          <Tag color="green">可以加工</Tag>
                                      ) : (
                                          row.blocks.join("；")
                                      ),
                              },
                          ]
                        : [
                              {
                                  title: "加工记录",
                                  render: (_, row) => (
                                      <Button
                                          type="link"
                                          onClick={() => setRunId(row.id)}
                                      >
                                          {row.id}
                                      </Button>
                                  ),
                              },
                              {
                                  title: "状态",
                                  dataIndex: "status",
                                  render: status,
                              },
                              {
                                  title: "开始时间",
                                  dataIndex: "trackInAt",
                                  render: fmt,
                              },
                              {
                                  title: "结束时间",
                                  dataIndex: "trackOutAt",
                                  render: fmt,
                              },
                          ]
                }
            />
            <LotExecution
                id={lotId}
                me={me}
                close={() => setLotId(null)}
                openRun={(id: string) => {
                    setLotId(null);
                    setRunId(id);
                }}
            />
            <RunExecution
                id={runId}
                me={me}
                close={() => setRunId(null)}
                openLot={(id: string) => {
                    setRunId(null);
                    setLotId(id);
                }}
            />
        </>
    );
}

function LotExecution({ id, me, close, openRun }: any) {
    const query = useQuery({
        queryKey: ["execution", "lot", id],
        enabled: !!id,
        queryFn: () => api(`execution/lots/${id}`),
    });
    const { send, busy } = useCommand();
    const [selection, select] = useState<string | undefined>();
    const data = query.data;
    useEffect(() => select(undefined), [id, data?.step?.id]);
    const candidate = data?.candidates.find(
        (x: any) => x.applicabilityId === selection,
    );
    const can = (permission: string) =>
        me.permissions?.includes(`execution.${permission}`);
    const action = async (track: boolean) => {
        if (!candidate) return;
        try {
            const result = await send(
                `execution/lots/${id}/${track ? "track-in" : "assign"}`,
                {
                    equipmentId: candidate.equipmentId,
                    recipeVersionId: candidate.recipeVersionId,
                    ...(track
                        ? { equipmentEtag: candidate.equipmentEtag }
                        : {}),
                },
                data.lot.etag,
            );
            if (track) openRun(result.id);
        } catch {
            // Keep the selection available for an explicit retry.
        }
    };
    return (
        <Drawer
            title={data?.lot.lotNumber || "批次执行"}
            open={!!id}
            onClose={close}
            width={800}
            loading={query.isLoading}
        >
            {query.error && <Alert type="error" title={query.error.message} />}
            {data && (
                <>
                    <Space wrap>
                        {status(data.lot.status)}
                        <Button onClick={() => query.refetch()}>
                            刷新详情
                        </Button>
                    </Space>
                    <p>当前步骤：{data.step?.name || "已终结"}</p>
                    <p>{data.step?.instruction || "无额外作业指令"}</p>
                    {data.blocks.length > 0 && (
                        <Alert type="warning" title={data.blocks.join("；")} />
                    )}
                    {data.lot.status === "AwaitingDisposition" && (
                        <Alert
                            type="info"
                            title="等待质量处置"
                            description="请保留当前加工记录，处置与返工入口将在后续模块提供。"
                        />
                    )}
                    {data.step?.assignedAt && (
                        <p>
                            已人工指派：
                            {data.candidates.find(
                                (x: any) =>
                                    x.equipmentId ===
                                    data.step.assignedEquipmentId,
                            )?.equipmentCode || data.step.assignedEquipmentId}
                            （{fmt(data.step.assignedAt)}）
                        </p>
                    )}
                    {data.step?.status === "Waiting" && (
                        <Space wrap>
                            <Select
                                aria-label="设备与配方"
                                placeholder="选择设备与配方"
                                style={{ minWidth: 340 }}
                                value={selection}
                                onChange={select}
                                options={data.candidates.map((x: any) => ({
                                    value: x.applicabilityId,
                                    label: `${x.equipmentCode} / ${x.recipeCode} ${x.recipeVersion}`,
                                }))}
                            />
                            {can("dispatch") && (
                                <Button
                                    disabled={!candidate || busy}
                                    onClick={() => action(false)}
                                >
                                    人工指派
                                </Button>
                            )}
                            {can("track-in") && (
                                <Button
                                    type="primary"
                                    disabled={
                                        !candidate ||
                                        busy ||
                                        data.blocks.length > 0
                                    }
                                    onClick={() => action(true)}
                                >
                                    开始加工
                                </Button>
                            )}
                        </Space>
                    )}
                    <h3>参与晶圆</h3>
                    <Table
                        size="small"
                        rowKey="id"
                        pagination={false}
                        dataSource={data.wafers}
                        columns={[
                            { title: "晶圆编号", dataIndex: "waferCode" },
                            {
                                title: "生命周期",
                                dataIndex: "lifecycleStatus",
                            },
                        ]}
                    />
                    <h3>加工记录</h3>
                    {data.runs.map((run: Item) => (
                        <p key={run.id}>
                            <Button type="link" onClick={() => openRun(run.id)}>
                                {fmt(run.trackInAt)}
                            </Button>
                            {status(run.status)}
                        </p>
                    ))}
                </>
            )}
        </Drawer>
    );
}

function RunExecution({ id, me, close, openLot }: any) {
    const query = useQuery({
        queryKey: ["execution", "run", id],
        enabled: !!id,
        queryFn: () => api(`execution/runs/${id}`),
    });
    const { send, busy } = useCommand();
    const [mode, setMode] = useState<string | null>(null);
    const [form] = Form.useForm();
    const data = query.data;
    const can = (permission: string) =>
        me.permissions?.includes(`execution.${permission}`);
    const begin = (value: string) => {
        form.resetFields();
        setMode(value);
    };
    const submit = async (values: any) => {
        let body: unknown;
        if (mode === "records") {
            const outputs = data.work.outputs
                .filter(
                    (x: Item) =>
                        values.outputs?.[x.id] !== undefined &&
                        values.outputs?.[x.id] !== "",
                )
                .map((x: Item) => {
                    const value = values.outputs[x.id];
                    return {
                        outputId: x.id,
                        unit: x.unit,
                        ...(x.valueType === "decimal" ||
                        x.valueType === "integer"
                            ? { valueNumber: value }
                            : x.valueType === "boolean"
                              ? { valueBoolean: value }
                              : x.valueType === "datetime"
                                ? {
                                      valueDate: new Date(value).toISOString(),
                                  }
                                : { valueText: value }),
                    };
                });
            body = {
                checklistItemIds: values.checklist || [],
                outputs,
                documentIds: values.documents || [],
                confirm: values.confirm || false,
                notes: values.notes,
            } satisfies components["schemas"]["RecordProductionWork"];
        } else if (mode === "track-out") {
            body = {
                wafers: data.wafers.map((wafer: Item) => ({
                    waferId: wafer.id,
                    result: values.results[wafer.id],
                })),
            } satisfies components["schemas"]["TrackOutProductionRun"];
        } else body = { reason: values.reason };
        try {
            await send(`execution/runs/${id}/${mode}`, body, data.run.etag);
            setMode(null);
        } catch {
            // Form values and the idempotency key survive recoverable failures.
        }
    };
    const unrecorded = (definition: Item) =>
        !data.work.values.some((x: Item) => x.outputId === definition.id);
    return (
        <Drawer
            title={
                data ? `${data.lot.lotNumber} · ${data.step.name}` : "加工记录"
            }
            open={!!id}
            onClose={close}
            width={860}
            loading={query.isLoading}
        >
            {query.error && <Alert type="error" title={query.error.message} />}
            {data && (
                <>
                    <Space wrap>
                        {status(data.run.status)}
                        <Button onClick={() => query.refetch()}>
                            刷新详情
                        </Button>
                        <Button onClick={() => openLot(data.lot.id)}>
                            查看批次
                        </Button>
                        {data.run.status === "Running" && (
                            <>
                                {can("record") && (
                                    <Button onClick={() => begin("records")}>
                                        录入作业记录
                                    </Button>
                                )}
                                {can("track-out") && (
                                    <Button
                                        type="primary"
                                        onClick={() => begin("track-out")}
                                    >
                                        结束加工
                                    </Button>
                                )}
                                {can("abort") && (
                                    <Button
                                        danger
                                        onClick={() => begin("abort")}
                                    >
                                        受控中止
                                    </Button>
                                )}
                            </>
                        )}
                    </Space>
                    <Descriptions
                        column={2}
                        style={{ marginTop: 16 }}
                        items={[
                            {
                                key: "tool",
                                label: "设备",
                                children: data.equipment.code,
                            },
                            {
                                key: "recipe",
                                label: "实际配方",
                                children: `${data.recipe.recipeName} ${data.recipe.version}`,
                            },
                            {
                                key: "in",
                                label: "开始时间",
                                children: fmt(data.run.trackInAt),
                            },
                            {
                                key: "out",
                                label: "结束时间",
                                children: fmt(data.run.trackOutAt),
                            },
                        ]}
                    />
                    {data.blocks.length > 0 && (
                        <Alert
                            type="warning"
                            title={data.blocks.join("；")}
                            description="可受控结束当前加工，批次将等待处置。"
                        />
                    )}
                    <h3>作业指令</h3>
                    <Typography.Paragraph style={{ whiteSpace: "pre-wrap" }}>
                        {data.step.instruction || "无额外作业指令"}
                    </Typography.Paragraph>
                    <h3>指导书</h3>
                    {data.work.documents.length === 0 && (
                        <Empty description="无绑定指导书" />
                    )}
                    {data.work.documents.map((doc: Item) => (
                        <p key={doc.id}>
                            <a
                                href={`/api/v1/execution/lots/${data.lot.id}/documents/${doc.id}/content`}
                                target="_blank"
                                rel="noreferrer"
                            >
                                {doc.name}
                            </a>
                            {doc.validatedAt
                                ? ` 已确认 ${fmt(doc.validatedAt)}`
                                : " 待确认"}
                        </p>
                    ))}
                    <h3>点检与输出</h3>
                    {data.work.checklist.map((item: Item) => {
                        const done = data.work.completions.find(
                            (x: Item) => x.checklistItemId === item.id,
                        );
                        return (
                            <p key={item.id}>
                                {item.label} {item.isRequired && "（必填）"}：
                                {done
                                    ? `已完成 ${fmt(done.checkedAt)}`
                                    : "待点检"}
                            </p>
                        );
                    })}
                    {data.work.outputs.map((item: Item) => {
                        const value = data.work.values.find(
                            (x: Item) => x.outputId === item.id,
                        );
                        return (
                            <p key={item.id}>
                                {item.label} {item.unit}{" "}
                                {item.isRequired && "（必填）"}：
                                {value
                                    ? String(
                                          value.valueText ??
                                              value.valueNumber ??
                                              value.valueBoolean ??
                                              value.valueDate,
                                      )
                                    : "待录入"}
                            </p>
                        );
                    })}
                    <p>
                        关键操作：
                        {data.step.confirmedAt
                            ? `已确认 ${fmt(data.step.confirmedAt)}`
                            : data.step.requiresConfirmation
                              ? "待确认"
                              : "无需确认"}
                    </p>
                    <h3>逐片结果</h3>
                    <Table
                        rowKey="id"
                        size="small"
                        pagination={false}
                        dataSource={data.wafers}
                        columns={[
                            { title: "晶圆编号", dataIndex: "waferCode" },
                            {
                                title: "结果",
                                dataIndex: "result",
                                render: (value) =>
                                    value ? status(value) : "待记录",
                            },
                            {
                                title: "记录时间",
                                dataIndex: "processedAt",
                                render: fmt,
                            },
                        ]}
                    />
                    <h3>过站历史</h3>
                    {data.tracks.map((track: Item) => (
                        <p key={track.id}>
                            {track.eventType} · {fmt(track.occurredAt)}
                        </p>
                    ))}
                    <Modal
                        open={!!mode}
                        title={
                            mode === "records"
                                ? "录入作业记录"
                                : mode === "track-out"
                                  ? "逐片结束加工"
                                  : "受控中止加工"
                        }
                        onCancel={() => setMode(null)}
                        onOk={() => form.submit()}
                        confirmLoading={busy}
                        okText="提交记录"
                        destroyOnHidden
                    >
                        <Form form={form} layout="vertical" onFinish={submit}>
                            {mode === "records" && (
                                <>
                                    <Form.Item
                                        name="checklist"
                                        label="已完成点检"
                                    >
                                        <Checkbox.Group
                                            options={data.work.checklist
                                                .filter(
                                                    (x: Item) =>
                                                        !data.work.completions.some(
                                                            (c: Item) =>
                                                                c.checklistItemId ===
                                                                x.id,
                                                        ),
                                                )
                                                .map((x: Item) => ({
                                                    label: x.label,
                                                    value: x.id,
                                                }))}
                                        />
                                    </Form.Item>
                                    <Form.Item
                                        name="documents"
                                        label="已阅读指导书"
                                    >
                                        <Checkbox.Group
                                            options={data.work.documents
                                                .filter(
                                                    (x: Item) => !x.validatedAt,
                                                )
                                                .map((x: Item) => ({
                                                    label: x.name,
                                                    value: x.id,
                                                }))}
                                        />
                                    </Form.Item>
                                    {data.work.outputs
                                        .filter(unrecorded)
                                        .map((definition: Item) => (
                                            <Form.Item
                                                key={definition.id}
                                                name={[
                                                    "outputs",
                                                    definition.id,
                                                ]}
                                                label={`${definition.label}${definition.unit ? `（${definition.unit}）` : ""}`}
                                            >
                                                {definition.valueType ===
                                                    "decimal" ||
                                                definition.valueType ===
                                                    "integer" ? (
                                                    <InputNumber
                                                        precision={
                                                            definition.valueType ===
                                                            "integer"
                                                                ? 0
                                                                : 6
                                                        }
                                                    />
                                                ) : definition.valueType ===
                                                  "boolean" ? (
                                                    <Select
                                                        allowClear
                                                        options={[
                                                            {
                                                                value: true,
                                                                label: "是",
                                                            },
                                                            {
                                                                value: false,
                                                                label: "否",
                                                            },
                                                        ]}
                                                    />
                                                ) : definition.valueType ===
                                                  "enum" ? (
                                                    <Select
                                                        allowClear
                                                        options={JSON.parse(
                                                            definition.options ||
                                                                "[]",
                                                        ).map((x: string) => ({
                                                            value: x,
                                                            label: x,
                                                        }))}
                                                    />
                                                ) : (
                                                    <Input
                                                        type={
                                                            definition.valueType ===
                                                            "datetime"
                                                                ? "datetime-local"
                                                                : "text"
                                                        }
                                                    />
                                                )}
                                            </Form.Item>
                                        ))}
                                    {!data.step.confirmedAt &&
                                        data.step.requiresConfirmation && (
                                            <Form.Item
                                                name="confirm"
                                                valuePropName="checked"
                                            >
                                                <Checkbox>
                                                    确认已完成关键操作
                                                </Checkbox>
                                            </Form.Item>
                                        )}
                                    <Form.Item name="notes" label="操作备注">
                                        <Input.TextArea maxLength={2000} />
                                    </Form.Item>
                                </>
                            )}
                            {mode === "track-out" &&
                                data.wafers.map((wafer: Item) => (
                                    <Form.Item
                                        key={wafer.id}
                                        name={["results", wafer.id]}
                                        label={wafer.waferCode}
                                        rules={required}
                                    >
                                        <Select
                                            options={[
                                                {
                                                    value: "Passed",
                                                    label: "通过",
                                                },
                                                {
                                                    value: "Failed",
                                                    label: "失败，等待处置",
                                                },
                                            ]}
                                        />
                                    </Form.Item>
                                ))}
                            {mode === "abort" && (
                                <Form.Item
                                    name="reason"
                                    label="中止原因"
                                    rules={required}
                                >
                                    <Input.TextArea maxLength={500} />
                                </Form.Item>
                            )}
                        </Form>
                    </Modal>
                </>
            )}
        </Drawer>
    );
}

export function EquipmentRegister({ me }: { me: Me }) {
    const [resource, setResource] = useState("tools");
    const [page, setPage] = useState(1);
    const [modal, setModal] = useState<string | null>(null);
    const [selected, select] = useState<Item | null>(null);
    const [detailId, setDetailId] = useState<string | null>(null);
    const [form] = Form.useForm();
    const { send, busy } = useCommand();
    const query = useQuery({
        queryKey: ["equipment", resource, page],
        queryFn: () => api<Page>(`equipment/${resource}?page=${page}`),
    });
    const choices = useQuery({
        queryKey: ["equipment", "options"],
        queryFn: () => api("equipment/options"),
    });
    const detail = useQuery({
        queryKey: ["equipment", "detail", detailId],
        enabled: !!detailId,
        queryFn: () => api(`equipment/tools/${detailId}`),
    });
    const begin = (mode: string, row: Item | null = null) => {
        form.resetFields();
        select(row);
        setModal(mode);
        if (row) form.setFieldsValue(row);
    };
    const submit = async (values: any) => {
        const path =
            modal === "create"
                ? `equipment/${resource}`
                : `equipment/tools/${selected!.id}${modal === "state" ? "/state" : ""}`;
        try {
            await send(
                path,
                values,
                selected?.etag,
                modal === "edit" ? "PUT" : "POST",
            );
            setModal(null);
        } catch {
            /* Preserve values for correction or retry. */
        }
    };
    return (
        <>
            <Typography.Title level={2}>设备台账</Typography.Title>
            <Tabs
                activeKey={resource}
                onChange={(key) => {
                    setResource(key);
                    setPage(1);
                }}
                items={[
                    { key: "tools", label: "设备" },
                    { key: "groups", label: "设备组" },
                    { key: "types", label: "设备类型" },
                ]}
            />
            <Space>
                {me.permissions?.includes("equipment.manage") && (
                    <Button type="primary" onClick={() => begin("create")}>
                        新增
                        {resource === "tools"
                            ? "设备"
                            : resource === "groups"
                              ? "设备组"
                              : "设备类型"}
                    </Button>
                )}
                <Button onClick={() => query.refetch()}>刷新列表</Button>
            </Space>
            {query.error && <Alert type="error" title={query.error.message} />}
            <Table
                rowKey="id"
                dataSource={query.data?.items}
                loading={query.isLoading}
                pagination={{
                    current: page,
                    total: query.data?.total,
                    pageSize: 20,
                    showSizeChanger: false,
                    onChange: setPage,
                }}
                columns={[
                    {
                        title: "编码",
                        dataIndex: "code",
                        render: (code, row) =>
                            resource === "tools" ? (
                                <Button
                                    type="link"
                                    onClick={() => setDetailId(row.id)}
                                >
                                    {code}
                                </Button>
                            ) : (
                                code
                            ),
                    },
                    { title: "名称", dataIndex: "name" },
                    {
                        title: "启用",
                        dataIndex: "isActive",
                        render: (value) => (value ? "启用" : "停用"),
                    },
                    ...(resource === "tools"
                        ? [
                              {
                                  title: "人工状态",
                                  dataIndex: "operationalState",
                                  render: status,
                              },
                              {
                                  title: "操作",
                                  render: (_: unknown, row: Item) => (
                                      <Space>
                                          {me.permissions?.includes(
                                              "equipment.manage",
                                          ) && (
                                              <Button
                                                  onClick={() =>
                                                      begin("edit", row)
                                                  }
                                              >
                                                  编辑
                                              </Button>
                                          )}
                                          {me.permissions?.includes(
                                              "equipment.state",
                                          ) && (
                                              <Button
                                                  onClick={() =>
                                                      begin("state", row)
                                                  }
                                              >
                                                  变更状态
                                              </Button>
                                          )}
                                      </Space>
                                  ),
                              },
                          ]
                        : []),
                ]}
            />
            <Drawer
                title="设备占用与状态履历"
                open={!!detailId}
                onClose={() => setDetailId(null)}
                width={720}
            >
                {detail.error && (
                    <Alert type="error" title={detail.error.message} />
                )}
                {detail.data && (
                    <>
                        <p>
                            {detail.data.equipment.code} ·{" "}
                            {detail.data.equipment.name}
                        </p>
                        <p>
                            人工状态：
                            {status(detail.data.equipment.operationalState)}
                        </p>
                        <p>
                            加工占用：
                            {detail.data.currentRun
                                ? `加工中，自 ${fmt(detail.data.currentRun.trackInAt)}`
                                : "无运行加工"}
                        </p>
                        <Table
                            rowKey="id"
                            dataSource={detail.data.states}
                            columns={[
                                {
                                    title: "状态",
                                    dataIndex: "state",
                                    render: status,
                                },
                                {
                                    title: "开始",
                                    dataIndex: "startedAt",
                                    render: fmt,
                                },
                                {
                                    title: "结束",
                                    dataIndex: "endedAt",
                                    render: fmt,
                                },
                            ]}
                        />
                    </>
                )}
            </Drawer>
            <Modal
                title={
                    modal === "state"
                        ? "变更人工状态"
                        : modal === "edit"
                          ? "编辑设备"
                          : "新增台账"
                }
                open={!!modal}
                onCancel={() => setModal(null)}
                onOk={() => form.submit()}
                confirmLoading={busy}
                okText="保存"
                destroyOnHidden
            >
                <Form form={form} layout="vertical" onFinish={submit}>
                    {modal === "state" ? (
                        <>
                            <Form.Item
                                name="state"
                                label="人工状态"
                                rules={required}
                            >
                                <Select
                                    options={[
                                        "Idle",
                                        "Down",
                                        "Maintenance",
                                        "Offline",
                                    ].map((value) => ({
                                        value,
                                        label: names[value],
                                    }))}
                                />
                            </Form.Item>
                            <Form.Item
                                name="reason"
                                label="变更原因"
                                rules={required}
                            >
                                <Input.TextArea maxLength={500} />
                            </Form.Item>
                        </>
                    ) : (
                        <>
                            {modal === "create" && (
                                <Form.Item
                                    name="code"
                                    label="编码"
                                    rules={required}
                                >
                                    <Input maxLength={64} />
                                </Form.Item>
                            )}
                            <Form.Item
                                name="name"
                                label="名称"
                                rules={required}
                            >
                                <Input maxLength={200} />
                            </Form.Item>
                            {modal === "edit" && (
                                <Form.Item
                                    name="isActive"
                                    valuePropName="checked"
                                >
                                    <Checkbox>启用设备</Checkbox>
                                </Form.Item>
                            )}
                            {modal === "create" && resource === "tools" && (
                                <>
                                    {[
                                        ["areaId", "所属区域", "areas"],
                                        [
                                            "equipmentGroupId",
                                            "设备组",
                                            "groups",
                                        ],
                                        [
                                            "equipmentTypeId",
                                            "设备类型",
                                            "types",
                                        ],
                                    ].map(([name, label, key]) => (
                                        <Form.Item
                                            key={name}
                                            name={name}
                                            label={label}
                                            rules={required}
                                        >
                                            <Select
                                                options={options(
                                                    choices.data?.[key],
                                                )}
                                            />
                                        </Form.Item>
                                    ))}
                                </>
                            )}
                        </>
                    )}
                </Form>
            </Modal>
        </>
    );
}
