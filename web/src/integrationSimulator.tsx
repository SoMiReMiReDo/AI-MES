import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    Button,
    Card,
    Descriptions,
    Drawer,
    Form,
    Input,
    Modal,
    Select,
    Space,
    Table,
} from "antd";
import { Link } from "react-router-dom";
import { api, type Row } from "./api";
import { fmt } from "./shared";
import {
    IntegrationStatus,
    integrationError,
    integrationPageSize,
    useIntegrationCommand,
    type IntegrationPage,
    type IntegrationSchema,
} from "./integrationShared";

type Connection = IntegrationSchema["SimulatorConnectionView"];
type Summary = IntegrationSchema["SimulatorMessageSummary"];
type Message = IntegrationSchema["SimulatorMessageView"];
type Registration = IntegrationSchema["RegisterSimulatorConnection"];
type Delivery = IntegrationSchema["ReceiveSimulatorMessage"];
const root = "integration/connections";
const required = [{ required: true, message: "请填写此项" }];

function ConnectionForm({
    onClose,
    onSaved,
}: {
    onClose: () => void;
    onSaved: (row: Connection) => void;
}) {
    const [search, setSearch] = useState("");
    const [form] = Form.useForm<Registration>();
    const command = useIntegrationCommand();
    const equipment = useQuery({
        queryKey: ["integration", "equipment-options", search],
        queryFn: () =>
            api<IntegrationPage<Row>>(
                `equipment/tools?pageSize=50&q=${encodeURIComponent(search)}`,
            ),
    });

    return (
        <Modal
            open
            title="登记模拟连接"
            okText="登记"
            onOk={form.submit}
            onCancel={onClose}
            confirmLoading={command.busy}
            cancelButtonProps={{ disabled: command.busy }}
            closable={!command.busy}
            mask={{ closable: false }}
        >
            {command.error && <Alert type="error" title={command.error} />}
            <Form<Registration>
                form={form}
                layout="vertical"
                disabled={command.busy}
                onFinish={async (values) => {
                    const row = await command.send<Connection>(root, values);
                    if (row) onSaved(row);
                }}
            >
                <Form.Item name="equipmentId" label="设备" rules={required}>
                    <Select
                        showSearch={{
                            onSearch: setSearch,
                            filterOption: false,
                        }}
                        placeholder="搜索已登记设备编号"
                        loading={equipment.isFetching}
                        notFoundContent={
                            equipment.isError ? "设备加载失败" : "无匹配设备"
                        }
                        options={equipment.data?.items
                            .filter((row) => row.isActive)
                            .map((row) => ({
                                value: row.id,
                                label: `${row.code} · ${row.name}`,
                            }))}
                    />
                </Form.Item>
                <Form.Item name="name" label="连接名称" rules={required}>
                    <Input maxLength={200} />
                </Form.Item>
            </Form>
        </Modal>
    );
}

function MessageForm({
    connection,
    onClose,
    onSaved,
}: {
    connection: Connection;
    onClose: () => void;
    onSaved: (message: Message) => void;
}) {
    const [form] = Form.useForm<Delivery>();
    const command = useIntegrationCommand();
    const [initial] = useState(() => ({
        sourceMessageId: crypto.randomUUID(),
        rawPayload: JSON.stringify(
            {
                occurredAt: new Date().toISOString(),
                kind: "Connection",
                state: "Online",
                reason: "模拟通信在线",
            },
            null,
            2,
        ),
    }));

    return (
        <Modal
            open
            title={`投递模拟报文 · ${connection.equipmentCode}`}
            okText="投递"
            onOk={form.submit}
            onCancel={onClose}
            confirmLoading={command.busy}
            cancelButtonProps={{ disabled: command.busy }}
            closable={!command.busy}
            mask={{ closable: false }}
        >
            {command.error && <Alert type="error" title={command.error} />}
            <Form<Delivery>
                form={form}
                layout="vertical"
                initialValues={initial}
                disabled={command.busy}
                onFinish={async (values) => {
                    const row = await command.send<Message>(
                        `${root}/${connection.id}/messages`,
                        values,
                    );
                    if (row) onSaved(row);
                }}
            >
                <Form.Item
                    name="sourceMessageId"
                    label="来源编号"
                    rules={required}
                >
                    <Input maxLength={128} />
                </Form.Item>
                <Form.Item
                    name="rawPayload"
                    label="原始报文"
                    rules={required}
                    extra="发生时间使用 UTC。Connection 报告通信状态，State 报告运行条件。"
                >
                    <Input.TextArea rows={8} spellCheck={false} />
                </Form.Item>
            </Form>
        </Modal>
    );
}

export function IntegrationSimulator({ canPublish }: { canPublish: boolean }) {
    const client = useQueryClient();
    const [page, setPage] = useState(1);
    const [messagePage, setMessagePage] = useState(1);
    const [connection, setConnection] = useState<Connection>();
    const [selected, setSelected] = useState<string>();
    const [dialog, setDialog] = useState<"connection" | "message">();
    const connectionList = useQuery({
        queryKey: ["integration", "connections", page],
        queryFn: () => api<IntegrationPage<Connection>>(`${root}?page=${page}`),
    });
    const messages = connection ? `${root}/${connection.id}/messages` : "";
    const messageList = useQuery({
        queryKey: ["integration", messages, messagePage],
        enabled: Boolean(connection),
        queryFn: () =>
            api<IntegrationPage<Summary>>(`${messages}?page=${messagePage}`),
    });
    const detail = useQuery({
        queryKey: ["integration", messages, selected],
        enabled: Boolean(connection && selected),
        queryFn: () => api<Message>(`${messages}/${selected}`),
    });

    function openConnection(row: Connection) {
        setSelected(undefined);
        setConnection(row);
        setMessagePage(1);
    }

    return (
        <Space orientation="vertical" size="middle" style={{ width: "100%" }}>
            <Card
                title="模拟连接"
                extra={
                    canPublish && (
                        <Button onClick={() => setDialog("connection")}>
                            登记模拟连接
                        </Button>
                    )
                }
            >
                {connectionList.isError && (
                    <Alert
                        type="error"
                        title={integrationError(connectionList.error)}
                    />
                )}
                <Table<Connection>
                    rowKey="id"
                    loading={connectionList.isFetching}
                    dataSource={connectionList.data?.items}
                    scroll={{ x: 600 }}
                    pagination={{
                        current: page,
                        pageSize: integrationPageSize,
                        total: Number(connectionList.data?.total ?? 0),
                        showSizeChanger: false,
                        onChange: setPage,
                    }}
                    columns={[
                        { title: "设备", dataIndex: "equipmentCode" },
                        { title: "连接名称", dataIndex: "name" },
                        {
                            title: "模拟通信状态",
                            render: (_, row) => (
                                <IntegrationStatus status={row.status} />
                            ),
                        },
                        {
                            title: "最近在线",
                            render: (_, row) => fmt(row.lastConnectedAt ?? ""),
                        },
                        {
                            title: "操作",
                            render: (_, row) => (
                                <Button
                                    type="link"
                                    onClick={() => openConnection(row)}
                                >
                                    查看报文
                                </Button>
                            ),
                        },
                    ]}
                />
            </Card>
            {connection && (
                <Card
                    title={`报文履历 · ${connection.name}`}
                    extra={
                        canPublish && (
                            <Button
                                type="primary"
                                onClick={() => setDialog("message")}
                            >
                                投递模拟报文
                            </Button>
                        )
                    }
                >
                    {messageList.isError && (
                        <Alert
                            type="error"
                            title={integrationError(messageList.error)}
                        />
                    )}
                    <Table<Summary>
                        rowKey="id"
                        loading={messageList.isFetching}
                        dataSource={messageList.data?.items}
                        scroll={{ x: 560 }}
                        pagination={{
                            current: messagePage,
                            pageSize: integrationPageSize,
                            total: Number(messageList.data?.total ?? 0),
                            showSizeChanger: false,
                            onChange: setMessagePage,
                        }}
                        columns={[
                            {
                                title: "来源编号",
                                dataIndex: "sourceMessageId",
                                ellipsis: true,
                            },
                            {
                                title: "接收时间",
                                render: (_, row) => fmt(row.receivedAt),
                            },
                            {
                                title: "结果",
                                render: (_, row) => (
                                    <IntegrationStatus status={row.status} />
                                ),
                            },
                            {
                                title: "操作",
                                render: (_, row) => (
                                    <Button
                                        type="link"
                                        onClick={() => setSelected(row.id)}
                                    >
                                        报文详情
                                    </Button>
                                ),
                            },
                        ]}
                    />
                </Card>
            )}
            <Drawer
                open={Boolean(selected)}
                title="模拟报文详情"
                size="min(680px, 100vw)"
                onClose={() => setSelected(undefined)}
                loading={detail.isFetching}
            >
                {detail.isError && (
                    <Alert
                        type="error"
                        title={integrationError(detail.error)}
                    />
                )}
                {detail.data && (
                    <>
                        <Descriptions
                            column={1}
                            items={[
                                {
                                    key: "source",
                                    label: "来源编号",
                                    children: detail.data.sourceMessageId,
                                },
                                {
                                    key: "received",
                                    label: "接收时间",
                                    children: fmt(detail.data.receivedAt),
                                },
                                {
                                    key: "status",
                                    label: "结果",
                                    children: (
                                        <IntegrationStatus
                                            status={detail.data.status}
                                        />
                                    ),
                                },
                                {
                                    key: "event",
                                    label: "设备事件",
                                    children: detail.data.equipmentEventId ? (
                                        <Link
                                            to={`/equipment?view=events&record=${detail.data.equipmentEventId}`}
                                        >
                                            查看设备事件
                                        </Link>
                                    ) : (
                                        "—"
                                    ),
                                },
                            ]}
                        />
                        {detail.data.error && (
                            <Alert type="warning" title={detail.data.error} />
                        )}
                        <h3>原始报文</h3>
                        <pre className="integration-payload">
                            {detail.data.rawPayload}
                        </pre>
                        {detail.data.parsedData && (
                            <>
                                <h3>解析结果</h3>
                                <pre className="integration-payload">
                                    {JSON.stringify(
                                        detail.data.parsedData,
                                        null,
                                        2,
                                    )}
                                </pre>
                            </>
                        )}
                    </>
                )}
            </Drawer>
            {dialog === "connection" && (
                <ConnectionForm
                    onClose={() => setDialog(undefined)}
                    onSaved={(row) => {
                        setDialog(undefined);
                        openConnection(row);
                        void client.invalidateQueries({
                            queryKey: ["integration", "connections"],
                        });
                    }}
                />
            )}
            {dialog === "message" && connection && (
                <MessageForm
                    connection={connection}
                    onClose={() => setDialog(undefined)}
                    onSaved={(row) => {
                        setDialog(undefined);
                        setMessagePage(1);
                        client.setQueryData(
                            ["integration", messages, row.id],
                            row,
                        );
                        setSelected(row.id);
                        void client.invalidateQueries({
                            queryKey: ["integration", "connections"],
                        });
                        void client.invalidateQueries({
                            queryKey: ["integration", messages],
                        });
                        void client.invalidateQueries({
                            queryKey: ["equipment"],
                        });
                    }}
                />
            )}
        </Space>
    );
}
