import {
    App,
    Button,
    Checkbox,
    Form,
    Input,
    InputNumber,
    Modal,
    Segmented,
    Select,
    Space,
    Switch,
    Table,
    Tag,
} from "antd";
import { useQueryClient } from "@tanstack/react-query";
import { useState } from "react";
import { api, type Me } from "./api";
import {
    asLocalInput,
    has,
    RecordLabel,
    sourceNames,
    TimeCell,
    useWorkbench,
    WorkbenchError,
    workbenchPermissions as permission,
    workbenchStatus,
    type WorkbenchSchemas,
} from "./workbenchShared";

type AlertRow = WorkbenchSchemas["WorkbenchAlert"];
type Subscription = WorkbenchSchemas["WorkbenchSubscriptionView"];
type SubscriptionInput = WorkbenchSchemas["WorkbenchSubscriptionInput"];

function SubscriptionForm({
    row,
    submit,
}: {
    row: Subscription;
    submit: (kind: string, value: SubscriptionInput) => Promise<void>;
}) {
    const [busy, setBusy] = useState(false);
    const [form] = Form.useForm<SubscriptionInput>();
    const save = async (value: SubscriptionInput) => {
        setBusy(true);
        try {
            await submit(row.kind, {
                enabled: value.enabled,
                escalationMinutes: value.escalationMinutes ?? null,
                escalateToId: value.escalateToId ?? null,
            });
        } finally {
            setBusy(false);
        }
    };
    return (
        <Form
            form={form}
            layout="vertical"
            className="workbench-subscription"
            initialValues={{
                enabled: row.enabled,
                escalationMinutes: row.escalationMinutes,
                escalateToId: row.escalateToId ?? undefined,
            }}
            onFinish={save}
        >
            <Form.Item
                name="enabled"
                label={sourceNames[row.kind]}
                valuePropName="checked"
            >
                <Switch aria-label={`订阅${sourceNames[row.kind]}`} />
            </Form.Item>
            <Form.Item name="escalateToId" label="升级收件人">
                <Select
                    aria-label={`${sourceNames[row.kind]}升级收件人`}
                    allowClear
                    showSearch={{ optionFilterProp: "label" }}
                    placeholder="不升级"
                    options={row.recipients.map((x) => ({
                        value: x.id,
                        label: x.name,
                    }))}
                />
            </Form.Item>
            <Form.Item name="escalationMinutes" label="未确认分钟数">
                <InputNumber
                    aria-label={`${sourceNames[row.kind]}未确认分钟数`}
                    min={1}
                    max={10080}
                    precision={0}
                />
            </Form.Item>
            <Button htmlType="submit" loading={busy}>
                保存订阅
            </Button>
        </Form>
    );
}

export function WorkbenchAlerts({ me }: { me: Me }) {
    const [mode, setMode] = useState("tasks");
    const [kind, setKind] = useState("");
    const [page, setPage] = useState(1);
    const [includeSilenced, setIncludeSilenced] = useState(false);
    const [silence, setSilence] = useState<AlertRow>();
    const [until, setUntil] = useState("");
    const [busy, setBusy] = useState(false);
    const client = useQueryClient();
    const { message } = App.useApp();
    const notifications = mode === "notifications";
    const list = useWorkbench<WorkbenchSchemas["WorkbenchAlerts"]>(
        "alerts",
        {
            mode,
            kind,
            page: String(page),
            includeSilenced: String(includeSilenced),
        },
        mode !== "subscriptions",
    );
    const subscriptions = useWorkbench<Subscription[]>(
        "subscriptions",
        {},
        mode === "subscriptions",
    );
    const kinds = Object.entries(sourceNames).filter(([key]) =>
        has(
            me,
            key === "downtime"
                ? permission.equipment
                : key === "disposition"
                  ? permission.execution
                  : permission.quality,
        ),
    );
    async function saveSubscription(kind: string, body: SubscriptionInput) {
        try {
            await api(`workbench/subscriptions/${kind}`, "PUT", body);
            await client.invalidateQueries({ queryKey: ["workbench"] });
            message.success("订阅已保存");
        } catch (error) {
            message.error((error as Error).message);
        }
    }
    async function act(row: AlertRow, action: string, date?: string) {
        setBusy(true);
        try {
            await api(
                `workbench/alerts/${row.source.kind}/${row.source.id}`,
                "PUT",
                { action, until: date ? new Date(date).toISOString() : null },
            );
            setSilence(undefined);
            await client.invalidateQueries({ queryKey: ["workbench"] });
        } catch (error) {
            message.error((error as Error).message);
        } finally {
            setBusy(false);
        }
    }

    return (
        <Space orientation="vertical" size="middle" className="workbench-stack">
            <div className="workbench-filters">
                <Segmented
                    value={mode}
                    onChange={(value) => {
                        setMode(value);
                        setPage(1);
                    }}
                    options={[
                        { value: "tasks", label: "当前待办" },
                        { value: "notifications", label: "站内通知" },
                        { value: "subscriptions", label: "我的订阅" },
                    ]}
                />
                {mode !== "subscriptions" && (
                    <Select
                        aria-label="告警来源"
                        value={kind}
                        onChange={(v) => {
                            setKind(v);
                            setPage(1);
                        }}
                        options={[
                            { value: "", label: "全部来源" },
                            ...kinds.map(([value, label]) => ({
                                value,
                                label,
                            })),
                        ]}
                    />
                )}
                {notifications && (
                    <Checkbox
                        checked={includeSilenced}
                        onChange={(event) => {
                            setIncludeSilenced(event.target.checked);
                            setPage(1);
                        }}
                    >
                        显示已静默
                    </Checkbox>
                )}
            </div>
            <WorkbenchError
                error={
                    mode === "subscriptions" ? subscriptions.error : list.error
                }
            />
            {mode === "subscriptions" ? (
                <div className="workbench-subscriptions">
                    {subscriptions.data?.map((row) => (
                        <SubscriptionForm
                            key={row.kind}
                            row={row}
                            submit={saveSubscription}
                        />
                    ))}
                </div>
            ) : (
                <Table
                    rowKey={(row) => `${row.source.kind}/${row.source.id}`}
                    size="small"
                    dataSource={list.data?.records.items}
                    loading={list.isLoading}
                    scroll={{ x: notifications ? 840 : 550 }}
                    pagination={{
                        current: page,
                        total: list.data?.records.total,
                        pageSize: 20,
                        onChange: setPage,
                        showSizeChanger: false,
                    }}
                    columns={[
                        {
                            title: "类型",
                            render: (_, row) => (
                                <Tag>{sourceNames[row.source.kind]}</Tag>
                            ),
                        },
                        {
                            title: "来源",
                            render: (_, row) => (
                                <RecordLabel me={me} row={row.source} />
                            ),
                        },
                        {
                            title: "状态",
                            render: (_, { source }) =>
                                workbenchStatus[source.status] ?? source.status,
                        },
                        {
                            title: "发生时间",
                            render: (_, row) => (
                                <TimeCell value={row.source.occurredAt} />
                            ),
                        },
                        ...(notifications
                            ? [
                                  {
                                      title: "通知状态",
                                      render: (_: unknown, row: AlertRow) => (
                                          <Space wrap>
                                              {row.escalated && (
                                                  <Tag color="orange">
                                                      已升级
                                                  </Tag>
                                              )}
                                              {row.acknowledgedAt ? (
                                                  <Tag>已确认</Tag>
                                              ) : row.silencedUntil &&
                                                new Date(row.silencedUntil) >
                                                    new Date() ? (
                                                  <Tag>
                                                      静默至{" "}
                                                      <TimeCell
                                                          value={
                                                              row.silencedUntil
                                                          }
                                                      />
                                                  </Tag>
                                              ) : (
                                                  <Tag color="red">待确认</Tag>
                                              )}
                                          </Space>
                                      ),
                                  },
                                  {
                                      title: "操作",
                                      render: (_: unknown, row: AlertRow) => (
                                          <Space wrap>
                                              {!row.acknowledgedAt && (
                                                  <Button
                                                      size="small"
                                                      disabled={busy}
                                                      onClick={() =>
                                                          void act(
                                                              row,
                                                              "acknowledge",
                                                          )
                                                      }
                                                  >
                                                      确认
                                                  </Button>
                                              )}
                                              {row.silencedUntil &&
                                              new Date(row.silencedUntil) >
                                                  new Date() ? (
                                                  <Button
                                                      size="small"
                                                      disabled={busy}
                                                      onClick={() =>
                                                          void act(
                                                              row,
                                                              "unsilence",
                                                          )
                                                      }
                                                  >
                                                      取消静默
                                                  </Button>
                                              ) : (
                                                  !row.acknowledgedAt && (
                                                      <Button
                                                          size="small"
                                                          disabled={busy}
                                                          onClick={() => {
                                                              setUntil(
                                                                  asLocalInput(
                                                                      new Date(
                                                                          Date.now() +
                                                                              3600000,
                                                                      ),
                                                                  ),
                                                              );
                                                              setSilence(row);
                                                          }}
                                                      >
                                                          定时静默
                                                      </Button>
                                                  )
                                              )}
                                          </Space>
                                      ),
                                  },
                              ]
                            : []),
                    ]}
                />
            )}
            <Modal
                open={!!silence}
                title="定时静默"
                okText="保存静默"
                okButtonProps={{
                    disabled: !until || Number.isNaN(Date.parse(until)),
                }}
                confirmLoading={busy}
                onCancel={() => setSilence(undefined)}
                onOk={() => silence && void act(silence, "silence", until)}
            >
                <label className="workbench-filter">
                    <span>静默截止时间（浏览器时区）</span>
                    <Input
                        aria-label="静默截止时间"
                        type="datetime-local"
                        value={until}
                        onChange={(event) => setUntil(event.target.value)}
                    />
                </label>
            </Modal>
        </Space>
    );
}
