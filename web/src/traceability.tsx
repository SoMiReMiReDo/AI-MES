import {
    DeleteOutlined,
    HistoryOutlined,
    RightOutlined,
    SearchOutlined,
} from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    Button,
    Card,
    DatePicker,
    Descriptions,
    Drawer,
    Input,
    Select,
    Space,
    Spin,
    Table,
    Tag,
    Typography,
    type InputRef,
} from "antd";
import {
    useCallback,
    useEffect,
    useRef,
    useState,
    type ReactNode,
} from "react";
import { Link, useSearchParams } from "react-router-dom";
import { api, type Me } from "./api";
import type { components } from "./generated/api";
import { fmt } from "./shared";
import {
    emptyTraceHistory,
    readTraceHistory,
    rememberTrace,
    writeTraceHistory,
    type TraceHistoryEntry,
    type ViewedTrace,
} from "./traceHistory";
import "./traceability.css";

type TraceObject = components["schemas"]["TraceObject"];
type TraceOverview = components["schemas"]["TraceOverview"];
type TraceLink = components["schemas"]["TraceLink"];
type TraceEvent = components["schemas"]["TraceEvent"];
type TraceEvidence = components["schemas"]["TraceEvidence"];

const kinds = [
    {
        value: "work-order",
        label: "工单",
        permission: "planning.work-orders.read",
    },
    { value: "lot", label: "Lot（批次）", permission: "production.lots.read" },
    {
        value: "wafer",
        label: "Wafer（晶圆）",
        permission: "production.wafers.read",
    },
    { value: "carrier", label: "载具", permission: "production.carriers.read" },
    { value: "equipment", label: "设备", permission: "equipment.read" },
];

const groups: Record<string, string> = {
    orders: "工单",
    lots: "批次",
    wafers: "晶圆归属与生命周期",
    genealogy: "拆批与合批",
    carriers: "载具",
    execution: "加工",
    quality: "质量",
    equipment: "设备状态",
};

const events: Record<string, string> = {
    "order-created": "工单创建",
    "order-released": "工单下达",
    "order-completed": "工单完成",
    "lot-created": "批次创建",
    "lot-closed": "批次关闭",
    membership: "批次归属区间",
    "wafer-released": "晶圆首次投放",
    "wafer-completed": "晶圆终点完成",
    genealogy: "拆合批血缘",
    "carrier-loaded": "载具装载",
    "carrier-unloaded": "载具卸载",
    "carrier-moved": "载具移位",
    "step-visit": "工序到访",
    "run-started": "Track-in",
    "run-ended": "Run 结束",
    "run-wafer": "加工参与片",
    inspection: "批级检验",
    measurement: "量测",
    hold: "批次冻结",
    "hold-released": "解除冻结",
    "quality-release": "质量放行",
    disposition: "晶圆处置",
    "equipment-state": "设备状态区间",
    "equipment-event": "设备事件",
};

const statuses: Record<string, string> = {
    Active: "有效",
    Aborted: "已中止",
    Busy: "加工中",
    Cancelled: "已取消",
    Closed: "已关闭",
    Completed: "已完成",
    Disabled: "已停用",
    Down: "停机",
    Draft: "草稿",
    Failed: "失败",
    Idle: "空闲",
    InProcess: "加工中",
    Merged: "已合并",
    Merge: "合批",
    Passed: "通过",
    Ready: "待加工",
    Released: "已投放",
    Running: "加工中",
    Scrap: "报废",
    Scrapped: "已报废",
    Split: "拆批",
    Waiting: "等待",
    AwaitingDisposition: "等待处置",
};

const statusName = (status: string) => statuses[status] ?? status;

export function canTrace(me: Me) {
    return kinds.some((x) => me.permissions?.includes(x.permission));
}

export function traceUrl(kind: string, id: string) {
    return `/traceability?${new URLSearchParams({ kind, id })}`;
}

function Related({ items }: { items?: TraceLink[] }) {
    if (!items?.length)
        return <Typography.Text type="secondary">—</Typography.Text>;
    return (
        <Space wrap>
            {items.map(({ kind, id, code, relation }) => (
                <Link
                    key={`${kind}-${id}-${relation}`}
                    to={
                        kind === "material-lot"
                            ? `/materials?lot=${id}`
                            : traceUrl(kind, id)
                    }
                >
                    {relation} {code}
                </Link>
            ))}
        </Space>
    );
}

export function Traceability({ me }: { me: Me }) {
    const [params, setParams] = useSearchParams();
    const allowed = kinds.filter((x) => me.permissions?.includes(x.permission));
    const historyScope = `${me.userId}/${me.fab?.id ?? ""}`;
    const [recent, setRecent] = useState(() => readTraceHistory(historyScope));
    const requested = params.get("kind");
    const selected = params.get("id");
    const kind = allowed.find((x) => x.value === requested)?.value;
    const code = params.get("q") ?? "";
    const [draft, setDraft] = useState(code);
    const searchInput = useRef<InputRef>(null);
    useEffect(() => {
        // Navigation may finish after the user starts editing the next query.
        if (document.activeElement !== searchInput.current?.input) {
            setDraft(code);
        }
    }, [code]);
    useEffect(
        () => writeTraceHistory(historyScope, recent),
        [historyScope, recent],
    );

    const rememberSearch = useCallback((kind: string, code: string) => {
        const entry = { kind, code, at: new Date().toISOString() };
        setRecent((current) => ({
            ...current,
            searched: rememberTrace(
                current.searched,
                entry,
                (previous) =>
                    previous.kind === kind &&
                    previous.code.toLowerCase() === code.toLowerCase(),
            ),
        }));
    }, []);
    const rememberView = useCallback(
        (kind: string, id: string, code: string) => {
            const entry = { kind, id, code, at: new Date().toISOString() };
            setRecent((current) => ({
                ...current,
                viewed: rememberTrace(
                    current.viewed,
                    entry,
                    (previous) => previous.kind === kind && previous.id === id,
                ),
            }));
        },
        [],
    );

    if (selected && !kind) {
        return <Alert type="warning" showIcon title="没有此类对象的追溯权限" />;
    }
    const searchKind = kind ?? allowed[0].value;

    return (
        <div className="traceability-page">
            <Typography.Title className="trace-title" level={2}>
                生产追溯
            </Typography.Title>
            <div
                className="trace-search"
                role="search"
                aria-label="生产追溯检索"
            >
                <div className="trace-search-field">
                    <label htmlFor="trace-object-kind">对象类型</label>
                    <Select
                        id="trace-object-kind"
                        size="large"
                        aria-label="追溯对象类型"
                        value={searchKind}
                        options={allowed}
                        onChange={(value) => {
                            setDraft("");
                            setParams({ kind: value });
                        }}
                    />
                </div>
                <div className="trace-search-field">
                    <label htmlFor="trace-object-code">追溯编号</label>
                    <Input.Search
                        id="trace-object-code"
                        ref={searchInput}
                        size="large"
                        aria-label="追溯编号"
                        classNames={{ button: { root: "trace-search-submit" } }}
                        value={draft}
                        onChange={(event) => setDraft(event.target.value)}
                        maxLength={120}
                        allowClear
                        placeholder="输入完整或部分编号"
                        enterButton="查询"
                        onSearch={(value) => {
                            const q = value.trim();
                            setDraft(q);
                            setParams({ kind: searchKind, q });
                        }}
                    />
                </div>
            </div>
            {selected && kind ? (
                <TraceTimeline
                    key={`${kind}/${selected}`}
                    kind={kind}
                    id={selected}
                    timezone={me.fab?.timezone}
                    onViewed={rememberView}
                />
            ) : code ? (
                <TraceMatches
                    key={`${searchKind}:${code}`}
                    kind={searchKind}
                    code={code}
                    onSearched={rememberSearch}
                />
            ) : (
                <RecentTraces
                    viewed={recent.viewed.filter((entry) =>
                        allowed.some((kind) => kind.value === entry.kind),
                    )}
                    searched={recent.searched.filter((entry) =>
                        allowed.some((kind) => kind.value === entry.kind),
                    )}
                    onClear={() => setRecent(emptyTraceHistory())}
                />
            )}
        </div>
    );
}

function RecentTraces({
    viewed,
    searched,
    onClear,
}: {
    viewed: ViewedTrace[];
    searched: TraceHistoryEntry[];
    onClear: () => void;
}) {
    return (
        <section
            className="trace-history"
            aria-labelledby="trace-history-title"
        >
            <div className="trace-history-heading">
                <Typography.Title id="trace-history-title" level={3}>
                    追溯历史
                </Typography.Title>
                {(viewed.length > 0 || searched.length > 0) && (
                    <Button
                        type="text"
                        icon={<DeleteOutlined />}
                        onClick={onClear}
                    >
                        清空历史
                    </Button>
                )}
            </div>
            <div className="trace-history-grid">
                <TraceHistoryList
                    title="最近查看"
                    icon={<HistoryOutlined />}
                    entries={viewed}
                    emptyMessage="打开对象履历后，可在这里快速重开。"
                    destination={(entry) => traceUrl(entry.kind, entry.id)}
                />
                <TraceHistoryList
                    title="最近查询"
                    icon={<SearchOutlined />}
                    entries={searched}
                    emptyMessage="按编号检索后，可在这里再次查询。"
                    destination={({ kind, code }) =>
                        `/traceability?${new URLSearchParams({ kind, q: code })}`
                    }
                />
            </div>
        </section>
    );
}

function TraceHistoryList<T extends TraceHistoryEntry>({
    title,
    icon,
    entries,
    emptyMessage,
    destination,
}: {
    title: string;
    icon: ReactNode;
    entries: T[];
    emptyMessage: string;
    destination: (entry: T) => string;
}) {
    return (
        <section className="trace-history-card" aria-label={title}>
            <h4>
                {icon}
                {title}
            </h4>
            {entries.length ? (
                <ul>
                    {entries.map((entry) => {
                        const link = destination(entry);
                        return (
                            <li key={link}>
                                <Link to={link} className="trace-history-link">
                                    <span className="trace-history-object">
                                        <span>{entry.code}</span>
                                        <small>
                                            {
                                                kinds.find(
                                                    (kind) =>
                                                        kind.value ===
                                                        entry.kind,
                                                )?.label
                                            }
                                        </small>
                                    </span>
                                    <time dateTime={entry.at}>
                                        {fmt(entry.at)}
                                    </time>
                                    <RightOutlined aria-hidden />
                                </Link>
                            </li>
                        );
                    })}
                </ul>
            ) : (
                <p className="trace-history-empty">{emptyMessage}</p>
            )}
        </section>
    );
}

function TraceMatches({
    kind,
    code,
    onSearched,
}: {
    kind: string;
    code: string;
    onSearched: (kind: string, code: string) => void;
}) {
    const [page, setPage] = useState(1);
    const query = useQuery({
        queryKey: ["traceability", "search", kind, code, page],
        queryFn: () =>
            api<components["schemas"]["PageViewOfTraceObject"]>(
                `traceability/search?${new URLSearchParams({ kind, q: code, page: String(page) })}`,
            ),
    });
    useEffect(() => {
        if (query.isSuccess) onSearched(kind, code);
    }, [query.isSuccess, kind, code, onSearched]);
    if (query.error)
        return <Alert type="error" showIcon title={query.error.message} />;
    return (
        <Table<TraceObject>
            rowKey="id"
            loading={query.isPending}
            dataSource={query.data?.items ?? []}
            locale={{ emptyText: "没有匹配的编号" }}
            pagination={{
                current: page,
                pageSize: 20,
                total: Number(query.data?.total ?? 0),
                onChange: setPage,
                showSizeChanger: false,
            }}
            columns={[
                {
                    title: "编号",
                    dataIndex: "code",
                    render: (value, record) => (
                        <Link
                            to={`/traceability?${new URLSearchParams({ kind, q: code, id: record.id! })}`}
                        >
                            {value}
                        </Link>
                    ),
                },
                ...(kind === "equipment"
                    ? [{ title: "名称", dataIndex: "name" }]
                    : []),
                {
                    title: "当前状态",
                    dataIndex: "status",
                    render: (value) => <Tag>{statusName(value)}</Tag>,
                },
            ]}
        />
    );
}

function TraceTimeline({
    kind,
    id,
    timezone,
    onViewed,
}: {
    kind: string;
    id: string;
    timezone?: string | null;
    onViewed: (kind: string, id: string, code: string) => void;
}) {
    const [params, setParams] = useSearchParams();
    const [group, setGroup] = useState<string>();
    const [range, setRange] = useState<[string, string]>();
    const [page, setPage] = useState(1);
    const path = `traceability/${kind}/${id}`;
    const overview = useQuery({
        queryKey: [path],
        queryFn: () => api<TraceOverview>(path),
    });
    const subjectCode = overview.data?.subject.code;
    useEffect(() => {
        if (subjectCode) onViewed(kind, id, subjectCode);
    }, [kind, id, subjectCode, onViewed]);
    const filters = new URLSearchParams({ page: String(page) });
    if (group) filters.set("group", group);
    if (range) {
        filters.set("from", range[0]);
        filters.set("to", range[1]);
    }
    const history = useQuery({
        queryKey: [path, "events", filters.toString()],
        enabled: overview.isSuccess,
        queryFn: () =>
            api<components["schemas"]["PageViewOfTraceEvent"]>(
                `${path}/events?${filters}`,
            ),
    });
    const source = params.get("source");
    const eventType = params.get("event");
    if (overview.error)
        return <Alert type="error" showIcon title={overview.error.message} />;
    if (!overview.data?.subject) return <Spin />;
    const { subject, originCoverage, related, asOf } = overview.data;
    const returnTo = new URLSearchParams(params);
    for (const key of ["id", "event", "source"]) returnTo.delete(key);

    return (
        <Space orientation="vertical" size="middle" style={{ width: "100%" }}>
            <Link to={`/traceability?${returnTo}`}>返回检索</Link>
            <Card
                title={`${kinds.find((x) => x.value === kind)?.label} ${subject.code}`}
                extra={<Tag>{statusName(subject.status)}</Tag>}
            >
                <Space orientation="vertical">
                    {subject.name && (
                        <Typography.Text>{subject.name}</Typography.Text>
                    )}
                    {!!related?.length && <Related items={related} />}
                    {originCoverage === "Unregistered" && (
                        <Typography.Text type="secondary">
                            来料来源未登记
                        </Typography.Text>
                    )}
                    {originCoverage === "Unavailable" && (
                        <Typography.Text type="secondary">
                            来料来源记录不可用
                        </Typography.Text>
                    )}
                    <Typography.Text type="secondary">
                        状态读取于 {fmt(asOf)} · {timezone}
                    </Typography.Text>
                </Space>
            </Card>
            <Typography.Title level={4} style={{ marginBottom: 0 }}>
                事件履历
            </Typography.Title>
            <Space wrap className="table-toolbar">
                <Select
                    aria-label="履历类别"
                    value={group ?? "all"}
                    options={[
                        { value: "all", label: "全部履历" },
                        ...(overview.data.groups ?? []).map((value) => ({
                            value,
                            label: groups[value],
                        })),
                    ]}
                    onChange={(value) => {
                        setPage(1);
                        setGroup(value === "all" ? undefined : value);
                    }}
                />
                <DatePicker.RangePicker
                    showTime
                    placeholder={["开始时间（本机）", "结束时间（本机）"]}
                    onChange={(value) => {
                        setPage(1);
                        setRange(
                            value?.[0] && value[1]
                                ? [
                                      value[0].toISOString(),
                                      value[1].toISOString(),
                                  ]
                                : undefined,
                        );
                    }}
                />
            </Space>
            {history.error ? (
                <Alert
                    type="error"
                    showIcon
                    title={history.error.message}
                    action={
                        <Button onClick={() => history.refetch()}>重试</Button>
                    }
                />
            ) : (
                <Table<TraceEvent>
                    rowKey={(row) => `${row.type}-${row.sourceId}`}
                    loading={history.isPending}
                    dataSource={history.data?.items ?? []}
                    scroll={{ x: 920 }}
                    locale={{ emptyText: "此范围内暂无已登记的事件" }}
                    pagination={{
                        current: page,
                        pageSize: 20,
                        total: Number(history.data?.total ?? 0),
                        onChange: setPage,
                        showSizeChanger: false,
                    }}
                    columns={[
                        {
                            title: "发生时间",
                            dataIndex: "occurredAt",
                            width: 172,
                            render: (value) => fmt(value),
                        },
                        {
                            title: "事件",
                            dataIndex: "type",
                            width: 145,
                            render: (type: string, row) => {
                                const link = new URLSearchParams(params);
                                link.set("event", type);
                                link.set("source", row.sourceId);
                                return (
                                    <Link to={`/traceability?${link}`}>
                                        {events[type] ?? type}
                                    </Link>
                                );
                            },
                        },
                        {
                            title: "事实",
                            dataIndex: "summary",
                            render: (value, row) => (
                                <Space size="small" wrap>
                                    {value}
                                    {row.result && (
                                        <Tag>{statusName(row.result)}</Tag>
                                    )}
                                    {row.endedAt && (
                                        <Typography.Text type="secondary">
                                            至 {fmt(row.endedAt)}
                                        </Typography.Text>
                                    )}
                                </Space>
                            ),
                        },
                        {
                            title: "操作人",
                            dataIndex: "actor",
                            width: 110,
                            render: (actor) => actor ?? "—",
                        },
                        {
                            title: "关联对象",
                            dataIndex: "related",
                            render: (items) => <Related items={items} />,
                        },
                    ]}
                />
            )}
            {eventType && source && (
                <EvidenceDrawer
                    key={`${eventType}/${source}`}
                    path={`${path}/events/${encodeURIComponent(eventType)}/${encodeURIComponent(source)}`}
                    close={() => {
                        const next = new URLSearchParams(params);
                        next.delete("event");
                        next.delete("source");
                        setParams(next, { replace: true });
                    }}
                />
            )}
        </Space>
    );
}

function EvidenceDrawer({ path, close }: { path: string; close: () => void }) {
    const query = useQuery({
        queryKey: [path],
        queryFn: () => api<TraceEvidence>(path),
    });
    const event = query.data?.event;
    return (
        <Drawer title="原始记录" open onClose={close} size={560}>
            {query.error && (
                <Alert type="error" showIcon title={query.error.message} />
            )}
            {query.isPending && <Spin />}
            {event && (
                <Descriptions
                    column={1}
                    bordered
                    size="small"
                    items={[
                        {
                            label: "事件",
                            children: events[event.type] ?? event.type,
                        },
                        { label: "发生时间", children: fmt(event.occurredAt) },
                        ...(event.endedAt
                            ? [
                                  {
                                      label: "结束时间",
                                      children: fmt(event.endedAt),
                                  },
                              ]
                            : []),
                        ...(event.summary
                            ? [{ label: "事实", children: event.summary }]
                            : []),
                        ...(event.result
                            ? [
                                  {
                                      label: "结果",
                                      children: statusName(event.result),
                                  },
                              ]
                            : []),
                        ...(event.actor
                            ? [{ label: "操作人", children: event.actor }]
                            : []),
                        ...(query.data?.fields ?? []).map(
                            ({ label, value }) => ({ label, children: value }),
                        ),
                        ...(event.related?.length
                            ? [
                                  {
                                      label: "关联对象",
                                      children: (
                                          <Related items={event.related} />
                                      ),
                                  },
                              ]
                            : []),
                    ]}
                />
            )}
        </Drawer>
    );
}
