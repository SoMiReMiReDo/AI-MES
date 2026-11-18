import { ReloadOutlined } from "@ant-design/icons";
import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    Button,
    DatePicker,
    Descriptions,
    Form,
    Input,
    Modal,
    Select,
    Space,
    Table,
    Tag,
    Typography,
} from "antd";
import { useState } from "react";
import { ApiError, api } from "./api";
import type { components } from "./generated/api";
import { fmt } from "./shared";

type Audit = components["schemas"]["OperationAuditView"];
type OutboxItem = components["schemas"]["OutboxView"];
type OutboxStatus = components["schemas"]["OutboxStatus"];
type List<T> = { items: T[]; total: number };

const auditFilters = [
    { value: "correlationId", label: "追踪 ID" },
    { value: "entityType", label: "对象类型" },
    { value: "entityId", label: "对象 ID" },
    { value: "actorId", label: "操作者 ID" },
    { value: "action", label: "动作" },
    { value: "sourceChannel", label: "来源渠道" },
];

function RequestError({ error }: { error: Error | null }) {
    return error ? (
        <Alert
            showIcon
            type="error"
            message={error.message}
            description={
                error instanceof ApiError && error.traceId
                    ? `追踪 ID ${error.traceId}`
                    : undefined
            }
        />
    ) : null;
}

export function OperationAudit() {
    const [page, setPage] = useState(1);
    const [detail, setDetail] = useState<Audit>();
    const [range, setRange] = useState<{ from: string; to: string }>();
    const [field, setField] = useState("correlationId");
    const [value, setValue] = useState("");
    const params = new URLSearchParams({ page: String(page), ...range });
    if (value) params.set(field, value);
    const query = useQuery({
        queryKey: ["operation-audit", params.toString()],
        queryFn: () => api<List<Audit>>(`reliability/audit-logs?${params}`),
    });

    return (
        <section className="panel">
            <div className="toolbar">
                <Space wrap>
                    <DatePicker.RangePicker
                        showTime
                        placeholder={["最近 7 天", "结束时间"]}
                        onChange={(v) => {
                            setRange(
                                v?.[0] && v?.[1]
                                    ? {
                                          from: v[0].toISOString(),
                                          to: v[1].toISOString(),
                                      }
                                    : undefined,
                            );
                            setPage(1);
                        }}
                    />
                    <Space.Compact>
                        <Select
                            aria-label="审计筛选字段"
                            value={field}
                            options={auditFilters}
                            onChange={(next) => {
                                setField(next);
                                setValue("");
                                setPage(1);
                            }}
                        />
                        <Input.Search
                            key={field}
                            aria-label="审计筛选值"
                            placeholder="输入并搜索"
                            allowClear
                            onSearch={(next) => {
                                setValue(next.trim());
                                setPage(1);
                            }}
                        />
                    </Space.Compact>
                </Space>
                <Button
                    icon={<ReloadOutlined />}
                    onClick={() => query.refetch()}
                >
                    刷新
                </Button>
            </div>
            <RequestError error={query.error} />
            <Table<Audit>
                rowKey={(row) => row.id!}
                scroll={{ x: 820 }}
                loading={query.isLoading}
                dataSource={query.data?.items}
                pagination={{
                    current: page,
                    pageSize: 20,
                    total: query.data?.total,
                    onChange: setPage,
                    showSizeChanger: false,
                }}
                columns={[
                    { title: "时间", dataIndex: "occurredAt", render: fmt },
                    { title: "对象", dataIndex: "entityType" },
                    { title: "动作", dataIndex: "action" },
                    {
                        title: "操作者",
                        render: (_, row) => row.actorName || row.userId || "—",
                    },
                    { title: "来源", dataIndex: "sourceChannel" },
                    {
                        title: "详情",
                        render: (_, row) => (
                            <Button type="link" onClick={() => setDetail(row)}>
                                查看
                            </Button>
                        ),
                    },
                ]}
            />
            <Modal
                title="业务审计详情"
                open={!!detail}
                onCancel={() => setDetail(undefined)}
                footer={null}
                width={760}
            >
                {detail && (
                    <Space orientation="vertical" style={{ width: "100%" }}>
                        <Descriptions size="small" column={1}>
                            <Descriptions.Item label="对象">
                                {detail.entityType}{" "}
                                <Typography.Text copyable>
                                    {detail.entityId}
                                </Typography.Text>
                            </Descriptions.Item>
                            <Descriptions.Item label="操作者 ID">
                                {detail.userId || "—"}
                            </Descriptions.Item>
                            <Descriptions.Item label="追踪 ID">
                                <Typography.Text copyable>
                                    {detail.correlationId}
                                </Typography.Text>
                            </Descriptions.Item>
                        </Descriptions>
                        <Typography.Text strong>变更前</Typography.Text>
                        <pre className="json-detail">
                            {JSON.stringify(detail.beforeState, null, 2) || "—"}
                        </pre>
                        <Typography.Text strong>变更后</Typography.Text>
                        <pre className="json-detail">
                            {JSON.stringify(detail.afterState, null, 2) || "—"}
                        </pre>
                    </Space>
                )}
            </Modal>
        </section>
    );
}

const statuses: Record<string, { label: string; color: string }> = {
    Pending: { label: "待投递", color: "gold" },
    Published: { label: "已投递", color: "green" },
    DeadLettered: { label: "死信", color: "red" },
};

export function Outbox({ canReplay }: { canReplay: boolean }) {
    const cache = useQueryClient();
    const [form] = Form.useForm<{ reason: string }>();
    const [status, setStatus] = useState<string>();
    const [trace, setTrace] = useState("");
    const [page, setPage] = useState(1);
    const [selected, setSelected] = useState<OutboxItem>();
    const params = new URLSearchParams({ page: String(page) });
    if (status) params.set("status", status);
    if (trace) params.set("correlationId", trace);
    const query = useQuery({
        queryKey: ["outbox", params.toString()],
        queryFn: () => api<List<OutboxItem>>(`reliability/outbox?${params}`),
        refetchInterval: 5000,
    });
    const counts = useQuery({
        queryKey: ["outbox", "status"],
        queryFn: () => api<OutboxStatus>("reliability/status"),
        refetchInterval: 5000,
    });
    const replay = useMutation({
        mutationFn: ({ item, reason }: { item: OutboxItem; reason: string }) =>
            api(
                `reliability/outbox/${item.id}/replay`,
                "POST",
                { reason },
                item.etag,
            ),
        onSettled: () => cache.invalidateQueries({ queryKey: ["outbox"] }),
        onSuccess: () => setSelected(undefined),
    });

    return (
        <section className="panel">
            {counts.data && (
                <Space wrap className="toolbar">
                    <Tag color="gold">待投递 {counts.data.pending}</Tag>
                    <Tag color="green">已投递 {counts.data.published}</Tag>
                    <Tag color="red">死信 {counts.data.deadLettered}</Tag>
                    {counts.data.oldestPendingAt && (
                        <span>
                            最早待投递 {fmt(counts.data.oldestPendingAt)}
                        </span>
                    )}
                </Space>
            )}
            <div className="toolbar">
                <Space wrap>
                    <Select
                        aria-label="投递状态"
                        placeholder="全部投递状态"
                        allowClear
                        options={Object.entries(statuses).map(
                            ([value, item]) => ({
                                value,
                                label: item.label,
                            }),
                        )}
                        onChange={(next) => {
                            setStatus(next);
                            setPage(1);
                        }}
                        style={{ width: 150 }}
                    />
                    <Input.Search
                        aria-label="事件追踪 ID"
                        placeholder="按追踪 ID 筛选"
                        allowClear
                        onSearch={(next) => {
                            setTrace(next.trim());
                            setPage(1);
                        }}
                    />
                </Space>
                <Button
                    icon={<ReloadOutlined />}
                    onClick={() =>
                        cache.invalidateQueries({ queryKey: ["outbox"] })
                    }
                >
                    刷新
                </Button>
            </div>
            <RequestError error={query.error || counts.error} />
            <Table<OutboxItem>
                rowKey={(row) => row.id!}
                scroll={{ x: 900 }}
                dataSource={query.data?.items}
                loading={query.isLoading}
                pagination={{
                    current: page,
                    pageSize: 20,
                    total: query.data?.total,
                    onChange: setPage,
                    showSizeChanger: false,
                }}
                columns={[
                    { title: "入队时间", dataIndex: "occurredAt", render: fmt },
                    {
                        title: "状态",
                        render: (_, row) => (
                            <Tag color={statuses[row.status!]?.color}>
                                {statuses[row.status!]?.label || row.status}
                            </Tag>
                        ),
                    },
                    { title: "尝试", dataIndex: "attempts" },
                    {
                        title: "下次尝试",
                        dataIndex: "nextAttemptAt",
                        render: fmt,
                    },
                    {
                        title: "失败原因",
                        render: (_, row) =>
                            row.errorCode === "InvalidPayload"
                                ? "事件格式无效"
                                : row.errorCode
                                  ? "投递处理失败"
                                  : "—",
                    },
                    {
                        title: "追踪 ID",
                        render: (_, row) => (
                            <Typography.Text copyable>
                                {row.correlationId}
                            </Typography.Text>
                        ),
                    },
                    {
                        title: "操作",
                        render: (_, row) =>
                            canReplay &&
                            row.status === "DeadLettered" && (
                                <Button
                                    type="link"
                                    onClick={() => {
                                        form.resetFields();
                                        replay.reset();
                                        setSelected(row);
                                    }}
                                >
                                    重放
                                </Button>
                            ),
                    },
                ]}
            />
            <Modal
                title="重放待发事件"
                open={!!selected}
                onCancel={() => setSelected(undefined)}
                onOk={form.submit}
                okText="重放"
                confirmLoading={replay.isPending}
                forceRender
            >
                <RequestError error={replay.error} />
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={({ reason }) => {
                        if (selected) replay.mutate({ item: selected, reason });
                    }}
                >
                    <Form.Item
                        name="reason"
                        label="重放原因"
                        rules={[{ required: true, whitespace: true }]}
                    >
                        <Input.TextArea maxLength={500} showCount />
                    </Form.Item>
                </Form>
            </Modal>
        </section>
    );
}
