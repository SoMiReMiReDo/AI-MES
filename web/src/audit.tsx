import { ReloadOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    Button,
    DatePicker,
    Input,
    Modal,
    Space,
    Table,
    Tabs,
} from "antd";
import { useState } from "react";
import { api, type Me, type Page, type Row } from "./api";
import { OperationAudit, Outbox } from "./reliability";
import { Heading, fmt } from "./shared";

export function Logs({ me }: { me: Me }) {
    const can = (p: string) => me.permissions?.includes(p);
    const tabs = [];
    if (can("security.audit.read"))
        tabs.push(
            {
                key: "audit-logs",
                label: "操作审计",
                children: <SecurityLogs tab="audit-logs" />,
            },
            {
                key: "events",
                label: "安全事件",
                children: <SecurityLogs tab="events" />,
            },
        );
    if (can("audit.operations.read"))
        tabs.push({
            key: "business",
            label: "业务审计",
            children: <OperationAudit />,
        });
    if (can("security.outbox.read"))
        tabs.push({
            key: "outbox",
            label: "事件投递",
            children: <Outbox canReplay={!!can("security.outbox.replay")} />,
        });

    return (
        <>
            <Heading
                eyebrow="GOVERNANCE / AUDIT"
                title="审计与安全事件"
                description="审计默认显示最近 7 天的记录。"
            />
            <Tabs items={tabs} destroyOnHidden />
        </>
    );
}

function SecurityLogs({ tab }: { tab: "audit-logs" | "events" }) {
    const [page, setPage] = useState(1);
    const [detail, setDetail] = useState<Row>();
    const [range, setRange] = useState<string[]>([]);
    const [filter, setFilter] = useState("");
    const query = useQuery({
        queryKey: ["logs", tab, page, range, filter],
        queryFn: () =>
            api<Page>(
                `security/${tab}?page=${page}${range.length ? `&from=${encodeURIComponent(range[0])}&to=${encodeURIComponent(range[1])}` : ""}${filter ? `&${tab === "events" ? "eventType" : "action"}=${encodeURIComponent(filter)}` : ""}`,
            ),
    });

    return (
        <>
            <section className="panel">
                <div className="toolbar">
                    <Space wrap>
                        <DatePicker.RangePicker
                            showTime
                            onChange={(v) => {
                                setRange(
                                    v?.[0] && v?.[1]
                                        ? [
                                              v[0].toISOString(),
                                              v[1].toISOString(),
                                          ]
                                        : [],
                                );
                                setPage(1);
                            }}
                        />
                        <Input.Search
                            placeholder="按动作或事件类型筛选"
                            onSearch={(v) => {
                                setFilter(v);
                                setPage(1);
                            }}
                            style={{ width: 220 }}
                        />
                        <span>{query.data?.total || 0} 条记录</span>
                    </Space>
                    <Button
                        icon={<ReloadOutlined />}
                        onClick={() => query.refetch()}
                    >
                        刷新
                    </Button>
                </div>
                {query.error && (
                    <Alert type="error" message={query.error.message} />
                )}
                <Table
                    rowKey="id"
                    dataSource={query.data?.items}
                    loading={query.isLoading}
                    scroll={{ x: 650 }}
                    pagination={{
                        current: page,
                        pageSize: 20,
                        total: query.data?.total,
                        onChange: setPage,
                        showSizeChanger: false,
                    }}
                    columns={[
                        { title: "时间", dataIndex: "occurredAt", render: fmt },
                        {
                            title: tab === "events" ? "事件" : "对象",
                            dataIndex:
                                tab === "events" ? "eventType" : "entityType",
                        },
                        {
                            title: "动作 / 结果",
                            dataIndex: tab === "events" ? "outcome" : "action",
                        },
                        {
                            title: "追踪 ID",
                            dataIndex: "correlationId",
                            render: (v) => (
                                <span className="code">{v?.slice(0, 12)}…</span>
                            ),
                        },
                        {
                            title: "详情",
                            render: (_, r) => (
                                <Button
                                    type="link"
                                    onClick={() => setDetail(r)}
                                >
                                    查看
                                </Button>
                            ),
                        },
                    ]}
                />
            </section>
            <Modal
                title="审计详情"
                open={!!detail}
                footer={null}
                onCancel={() => setDetail(undefined)}
                width={800}
            >
                <pre className="json-detail">
                    {JSON.stringify(detail, null, 2)}
                </pre>
            </Modal>
        </>
    );
}
