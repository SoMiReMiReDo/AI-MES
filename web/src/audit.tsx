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
import { api, type Page, type Row } from "./api";
import { Heading, fmt } from "./shared";

export function Logs() {
    const [tab, setTab] = useState("audit-logs"),
        [page, setPage] = useState(1);
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
            <Heading
                eyebrow="GOVERNANCE / AUDIT"
                title="审计与安全事件"
                description="查看最近 7 天的管理变更与登录事件。凭据不会进入审计内容。"
            />
            <Tabs
                activeKey={tab}
                onChange={(v) => {
                    setTab(v);
                    setPage(1);
                }}
                items={[
                    { key: "audit-logs", label: "操作审计" },
                    { key: "events", label: "安全事件" },
                ]}
            />
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
