import { useSearchParams } from "react-router-dom";
import type { components } from "./generated/api";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    Button,
    Descriptions,
    Drawer,
    Empty,
    Form,
    Input,
    Modal,
    Select,
    Space,
    Table,
    Tabs,
    Typography,
} from "antd";
import { api, type Me } from "./api";
import { fmt, Heading } from "./shared";
import { QualityConfiguration } from "./qualityConfiguration";
import { QualityInspection } from "./qualityInspection";
import {
    choice,
    qualityOptions,
    qualityStatus,
    QualitySelect,
    required,
    useQualityCommand,
    useQualityOptions,
    type QualityItem,
} from "./qualityShared";

type Dialog = {
    kind: string;
    title: string;
    path: string;
    row?: QualityItem;
    initial?: any;
};
const resourceTitles: Record<string, string> = {
    issues: "异常",
    lots: "批次",
    checks: "检验记录",
    scraps: "报废记录",
};

export function Quality({ me }: { me: Me }) {
    const [routeParams] = useSearchParams();
    const view = routeParams.get("view");
    const record = routeParams.get("record");
    const initialTab =
        view && ["issues", "holds", "checks", "lots"].includes(view)
            ? view
            : "issues";
    const [tab, setTab] = useState(initialTab);
    const [page, setPage] = useState(1);
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState("");
    const [selected, setSelected] = useState<
        | {
              resource: string;
              id: string;
          }
        | undefined
    >(record ? { resource: initialTab, id: record } : undefined);
    const [dialog, setDialog] = useState<Dialog>();
    const [inspection, setInspection] = useState<{ lotId?: string }>();
    const [form] = Form.useForm();
    const can = (p: string) => me.permissions?.includes(`quality.${p}`);
    const { send, busy } = useQualityCommand();
    const catalogLots = useQualityOptions("lots");
    const types = useQualityOptions("issue-types");
    const selectedLotId = Form.useWatch("lotId", form);
    const issueLot = useQuery<QualityItem>({
        queryKey: ["quality", "lots", selectedLotId],
        queryFn: () => api(`quality/lots/${selectedLotId}`),
        enabled: dialog?.kind === "issue" && !!selectedLotId,
    });
    const list = useQuery<{ items: QualityItem[]; total: number }>({
        queryKey: ["quality", tab, page, search, status],
        queryFn: () =>
            api(
                `quality/${tab}?${new URLSearchParams({ page: String(page), q: search, status })}`,
            ),
        enabled: tab !== "configuration",
    });
    const detail = useQuery<QualityItem>({
        queryKey: ["quality", selected?.resource, selected?.id],
        queryFn: () => api(`quality/${selected!.resource}/${selected!.id}`),
        enabled: !!selected,
    });
    const show = (d: Dialog) => {
        form.resetFields();
        form.setFieldsValue(d.initial || {});
        setDialog(d);
    };
    const reason = (title: string, path: string, row: QualityItem) =>
        show({ kind: "reason", title, path, row });
    const save = async () => {
        if (!dialog) return;
        try {
            const v = await form.validateFields();
            let body:
                | components["schemas"]["ReportQualityIssue"]
                | components["schemas"]["HoldQualityLot"]
                | components["schemas"]["ReleaseQualityLot"]
                | components["schemas"]["ScrapQualityWafers"]
                | components["schemas"]["AssignQualityIssue"]
                | components["schemas"]["ResolveQualityIssue"]
                | components["schemas"]["CreateQualityAction"]
                | components["schemas"]["ReasonRequest"];
            if (dialog.kind === "issue")
                body = {
                    issueTypeId: v.issueTypeId,
                    title: v.title,
                    description: v.description,
                    lotId: v.lotId || null,
                    workOrderId: v.workOrderId || null,
                    waferId: v.waferId || null,
                    stepExecutionId: v.stepExecutionId || null,
                    equipmentId: v.equipmentId || null,
                    holdReasonCodeId: v.holdReasonCodeId || null,
                };
            else if (dialog.kind === "hold")
                body = {
                    reasonCodeId: v.reasonCodeId,
                    issueId: v.issueId || null,
                };
            else if (dialog.kind === "release")
                body = { reason: v.reason, action: v.action };
            else if (dialog.kind === "scrap")
                body = {
                    reasonCodeId: v.reasonCodeId,
                    waferIds: v.waferIds,
                    quantity: v.waferIds.length,
                    notes: v.notes,
                };
            else if (dialog.kind === "assign")
                body = { assignedToId: v.assignedToId };
            else if (dialog.kind === "resolve")
                body = {
                    disposition: v.disposition,
                    rootCause: v.rootCause,
                    containmentAction: v.containmentAction,
                    notes: v.notes,
                };
            else if (dialog.kind === "action")
                body = {
                    title: v.title,
                    actionType: v.actionType,
                    assignedToId: v.assignedToId,
                    description: v.description || null,
                    dueDate: v.dueDate || null,
                };
            else body = { reason: v.reason };
            await send(dialog.path, body, dialog.row?.etag);
            setDialog(undefined);
        } catch {
            /* Keep the reviewable command and inputs available. */
        }
    };
    const lotName = (id: string) =>
        catalogLots.data?.items.find((x) => x.id === id)?.lotNumber ||
        "历史批次";
    const row = detail.data;
    const openIssue = () =>
        show({ kind: "issue", title: "登记异常", path: "quality/issues" });
    const columns: any[] =
        tab === "issues"
            ? [
                  { title: "异常", dataIndex: "title" },
                  {
                      title: "批次",
                      dataIndex: "lotId",
                      render: (id: string) =>
                          id ? lotName(id) : "工单 / 晶圆 / 设备",
                  },
                  { title: "状态", dataIndex: "status", render: qualityStatus },
                  { title: "报告时间", dataIndex: "reportedAt", render: fmt },
              ]
            : tab === "lots"
              ? [
                    { title: "批次号", dataIndex: "lotNumber" },
                    {
                        title: "状态",
                        dataIndex: "status",
                        render: qualityStatus,
                    },
                    {
                        title: "投放时间",
                        dataIndex: "releasedToRouteAt",
                        render: fmt,
                    },
                ]
              : tab === "checks"
                ? [
                      { title: "批次", dataIndex: "lotId", render: lotName },
                      {
                          title: "检验时间",
                          dataIndex: "checkedAt",
                          render: fmt,
                      },
                      { title: "检验数量", dataIndex: "productionQuantity" },
                      {
                          title: "判定",
                          dataIndex: "allPassed",
                          render: (v: boolean) =>
                              qualityStatus(v ? "Passed" : "Failed"),
                      },
                  ]
                : [
                      {
                          title: "报废时间",
                          dataIndex: "reportedAt",
                          render: fmt,
                      },
                      { title: "报废数量", dataIndex: "quantity" },
                      { title: "说明", dataIndex: "notes" },
                  ];
    columns.push({
        title: "操作",
        render: (_: unknown, x: QualityItem) => (
            <Button
                size="small"
                onClick={() => setSelected({ resource: tab, id: x.id })}
            >
                查看{resourceTitles[tab]}
            </Button>
        ),
    });
    return (
        <>
            <Heading
                eyebrow="QUALITY"
                title="质量管理"
                description="异常、冻结、检验与逐片处置；质量审核后继续生产。"
            />
            <Tabs
                activeKey={tab}
                onChange={(key) => {
                    setTab(key);
                    setPage(1);
                    setStatus("");
                    setSearch("");
                }}
                items={[
                    { key: "issues", label: "异常管理" },
                    { key: "lots", label: "冻结与放行" },
                    { key: "checks", label: "检验记录" },
                    { key: "scraps", label: "逐片报废" },
                    { key: "configuration", label: "质量配置" },
                ]}
            />
            {tab === "configuration" ? (
                <QualityConfiguration me={me} />
            ) : (
                <>
                    <Space wrap style={{ marginBottom: 16 }}>
                        {["issues", "lots"].includes(tab) && (
                            <Input.Search
                                aria-label="质量搜索"
                                placeholder={
                                    tab === "issues"
                                        ? "搜索异常标题"
                                        : "搜索批次号"
                                }
                                allowClear
                                onSearch={(v) => {
                                    setSearch(v);
                                    setPage(1);
                                }}
                            />
                        )}
                        {tab !== "scraps" && (
                            <Select
                                aria-label="质量状态"
                                style={{ width: 160 }}
                                allowClear
                                placeholder="全部状态"
                                value={status || undefined}
                                options={choice(
                                    tab === "issues"
                                        ? [
                                              "Open",
                                              "Acknowledged",
                                              "Resolved",
                                              "Closed",
                                          ]
                                        : tab === "lots"
                                          ? [
                                                "Draft",
                                                "Ready",
                                                "Running",
                                                "AwaitingDisposition",
                                                "Completed",
                                                "Closed",
                                            ]
                                          : ["Passed", "Failed"],
                                )}
                                onChange={(v) => {
                                    setStatus(v || "");
                                    setPage(1);
                                }}
                            />
                        )}
                        <Button onClick={() => list.refetch()}>刷新</Button>
                        {tab === "issues" && can("issues") && (
                            <Button type="primary" onClick={openIssue}>
                                登记异常
                            </Button>
                        )}
                        {tab === "checks" && can("inspect") && (
                            <Button
                                type="primary"
                                onClick={() => setInspection({})}
                            >
                                录入检验
                            </Button>
                        )}
                        {tab === "scraps" && (
                            <Typography.Text type="secondary">
                                从“冻结与放行”的批次详情选择晶圆并报废。
                            </Typography.Text>
                        )}
                    </Space>
                    {list.error && (
                        <Alert
                            type="error"
                            showIcon
                            title={(list.error as Error).message}
                        />
                    )}
                    <Table
                        rowKey="id"
                        columns={columns}
                        loading={list.isLoading}
                        dataSource={list.data?.items}
                        scroll={{ x: 780 }}
                        pagination={{
                            current: page,
                            pageSize: 20,
                            total: list.data?.total,
                            onChange: setPage,
                            showSizeChanger: false,
                        }}
                    />
                </>
            )}
            <Drawer
                title={
                    selected
                        ? `${resourceTitles[selected.resource]}详情`
                        : "详情"
                }
                open={!!selected}
                onClose={() => setSelected(undefined)}
                width={980}
            >
                {detail.error && (
                    <Alert
                        type="error"
                        title={(detail.error as Error).message}
                    />
                )}
                {!row ? (
                    <Empty
                        description={detail.isLoading ? "加载中" : "暂无数据"}
                    />
                ) : (
                    <>
                        <Space wrap style={{ marginBottom: 16 }}>
                            <Button onClick={() => detail.refetch()}>
                                刷新详情
                            </Button>
                            {selected?.resource === "issues" &&
                                can("issues") && (
                                    <>
                                        {row.status === "Open" && (
                                            <Button
                                                onClick={() =>
                                                    reason(
                                                        "确认异常",
                                                        `quality/issues/${row.id}/acknowledge`,
                                                        row,
                                                    )
                                                }
                                            >
                                                确认异常
                                            </Button>
                                        )}
                                        {row.status === "Acknowledged" && (
                                            <>
                                                <Button
                                                    onClick={() =>
                                                        show({
                                                            kind: "assign",
                                                            title: "指派责任人",
                                                            path: `quality/issues/${row.id}/assign`,
                                                            row,
                                                        })
                                                    }
                                                >
                                                    指派
                                                </Button>
                                                <Button
                                                    onClick={() =>
                                                        show({
                                                            kind: "action",
                                                            title: "新增整改",
                                                            path: `quality/issues/${row.id}/actions`,
                                                            row,
                                                            initial: {
                                                                actionType:
                                                                    "Corrective",
                                                            },
                                                        })
                                                    }
                                                >
                                                    新增整改
                                                </Button>
                                                <Button
                                                    onClick={() =>
                                                        show({
                                                            kind: "resolve",
                                                            title: "登记处置结论",
                                                            path: `quality/issues/${row.id}/resolve`,
                                                            row,
                                                            initial: {
                                                                disposition:
                                                                    "Continue",
                                                            },
                                                        })
                                                    }
                                                >
                                                    处置异常
                                                </Button>
                                            </>
                                        )}
                                        {row.status === "Resolved" && (
                                            <Button
                                                onClick={() =>
                                                    reason(
                                                        "关闭异常",
                                                        `quality/issues/${row.id}/close`,
                                                        row,
                                                    )
                                                }
                                            >
                                                关闭异常
                                            </Button>
                                        )}
                                    </>
                                )}
                            {selected?.resource === "lots" && (
                                <>
                                    {can("hold") &&
                                        [
                                            "Draft",
                                            "Ready",
                                            "Running",
                                            "AwaitingDisposition",
                                        ].includes(row.status) && (
                                            <Button
                                                onClick={() =>
                                                    show({
                                                        kind: "hold",
                                                        title: `冻结 ${row.lotNumber}`,
                                                        path: `quality/lots/${row.id}/holds`,
                                                        row,
                                                    })
                                                }
                                            >
                                                添加 Hold
                                            </Button>
                                        )}
                                    {can("release") &&
                                        [
                                            "Draft",
                                            "Ready",
                                            "AwaitingDisposition",
                                        ].includes(row.status) && (
                                            <Button
                                                onClick={() =>
                                                    show({
                                                        kind: "release",
                                                        title: `质量放行 ${row.lotNumber}`,
                                                        path: `quality/lots/${row.id}/release`,
                                                        row,
                                                        initial: {
                                                            action:
                                                                row.status ===
                                                                "AwaitingDisposition"
                                                                    ? "Continue"
                                                                    : "None",
                                                        },
                                                    })
                                                }
                                            >
                                                质量放行
                                            </Button>
                                        )}
                                    {can("scrap") &&
                                        [
                                            "Ready",
                                            "AwaitingDisposition",
                                        ].includes(row.status) && (
                                            <Button
                                                danger
                                                onClick={() =>
                                                    show({
                                                        kind: "scrap",
                                                        title: `逐片报废 ${row.lotNumber}`,
                                                        path: `quality/lots/${row.id}/scrap`,
                                                        row,
                                                    })
                                                }
                                            >
                                                逐片报废
                                            </Button>
                                        )}
                                    {can("inspect") &&
                                        [
                                            "Ready",
                                            "Running",
                                            "AwaitingDisposition",
                                        ].includes(row.status) && (
                                            <Button
                                                onClick={() =>
                                                    setInspection({
                                                        lotId: row.id,
                                                    })
                                                }
                                            >
                                                录入检验
                                            </Button>
                                        )}
                                </>
                            )}
                        </Space>
                        <Descriptions
                            bordered
                            size="small"
                            column={2}
                            items={
                                selected?.resource === "lots"
                                    ? [
                                          {
                                              key: "lot",
                                              label: "批次号",
                                              children: row.lotNumber,
                                          },
                                          {
                                              key: "order",
                                              label: "工单",
                                              children: row.workOrderNo,
                                          },
                                          {
                                              key: "status",
                                              label: "状态",
                                              children: qualityStatus(
                                                  row.status,
                                              ),
                                          },
                                          {
                                              key: "step",
                                              label: "当前步骤 / 到访",
                                              children: row.step
                                                  ? `${row.step.name} / ${row.step.visitNo}`
                                                  : "—",
                                          },
                                      ]
                                    : selected?.resource === "issues"
                                      ? [
                                            {
                                                key: "title",
                                                label: "异常",
                                                children: row.title,
                                            },
                                            {
                                                key: "status",
                                                label: "状态",
                                                children: qualityStatus(
                                                    row.status,
                                                ),
                                            },
                                            {
                                                key: "description",
                                                label: "说明",
                                                children: row.description,
                                                span: 2,
                                            },
                                            {
                                                key: "disposition",
                                                label: "处置结论",
                                                children: row.disposition
                                                    ? qualityStatus(
                                                          row.disposition,
                                                      )
                                                    : "—",
                                            },
                                            {
                                                key: "root",
                                                label: "原因",
                                                children: row.rootCause || "—",
                                            },
                                            {
                                                key: "assignee",
                                                label: "责任人",
                                                children:
                                                    row.assignedToName ||
                                                    "未指派",
                                            },
                                            {
                                                key: "reporter",
                                                label: "报告人",
                                                children:
                                                    row.reportedByName || "—",
                                            },
                                            {
                                                key: "containment",
                                                label: "遏制措施",
                                                children:
                                                    row.containmentAction ||
                                                    "—",
                                            },
                                            {
                                                key: "notes",
                                                label: "处置说明",
                                                children:
                                                    row.resolutionNotes || "—",
                                            },
                                        ]
                                      : [
                                            {
                                                key: "time",
                                                label: "记录时间",
                                                children: fmt(
                                                    row.checkedAt ||
                                                        row.reportedAt,
                                                ),
                                            },
                                            {
                                                key: "qty",
                                                label: "数量",
                                                children:
                                                    row.quantity ||
                                                    row.productionQuantity,
                                            },
                                            {
                                                key: "notes",
                                                label: "说明",
                                                children: row.notes || "—",
                                                span: 2,
                                            },
                                        ]
                            }
                        />
                        {row.wafers && (
                            <>
                                <Typography.Title level={5}>
                                    当前晶圆
                                </Typography.Title>
                                <Table
                                    size="small"
                                    rowKey="id"
                                    pagination={false}
                                    dataSource={row.wafers}
                                    columns={[
                                        {
                                            title: "晶圆号",
                                            dataIndex: "waferCode",
                                        },
                                        {
                                            title: "生命周期",
                                            dataIndex: "lifecycleStatus",
                                            render: qualityStatus,
                                        },
                                    ]}
                                />
                            </>
                        )}
                        {row.actions && (
                            <>
                                <Typography.Title level={5}>
                                    整改任务
                                </Typography.Title>
                                <Table
                                    size="small"
                                    rowKey="id"
                                    pagination={false}
                                    dataSource={row.actions}
                                    columns={[
                                        { title: "整改", dataIndex: "title" },
                                        {
                                            title: "状态",
                                            dataIndex: "status",
                                            render: qualityStatus,
                                        },
                                        {
                                            title: "截止日期",
                                            dataIndex: "dueDate",
                                        },
                                        {
                                            title: "操作",
                                            render: (_, x: QualityItem) =>
                                                can("issues") &&
                                                x.status !== "Verified" ? (
                                                    <Button
                                                        size="small"
                                                        onClick={() =>
                                                            reason(
                                                                x.status ===
                                                                    "Open"
                                                                    ? "完成整改"
                                                                    : "复核整改",
                                                                `quality/actions/${x.id}/${x.status === "Open" ? "complete" : "verify"}`,
                                                                x,
                                                            )
                                                        }
                                                    >
                                                        {x.status === "Open"
                                                            ? "完成"
                                                            : "复核"}
                                                    </Button>
                                                ) : (
                                                    "—"
                                                ),
                                        },
                                    ]}
                                />
                            </>
                        )}
                        {row.holds && (
                            <>
                                <Typography.Title level={5}>
                                    Hold 记录
                                </Typography.Title>
                                <Table
                                    size="small"
                                    rowKey="id"
                                    pagination={false}
                                    dataSource={row.holds}
                                    columns={[
                                        {
                                            title: "冻结时间",
                                            dataIndex: "heldAt",
                                            render: fmt,
                                        },
                                        {
                                            title: "冻结原因",
                                            dataIndex: "reasonName",
                                        },
                                        {
                                            title: "状态",
                                            render: (_, x: QualityItem) =>
                                                x.releasedAt
                                                    ? "已解除"
                                                    : "有效冻结",
                                        },
                                        {
                                            title: "解除原因",
                                            dataIndex: "releaseReason",
                                        },
                                        {
                                            title: "操作",
                                            render: (_, x: QualityItem) =>
                                                !x.releasedAt &&
                                                can("release") ? (
                                                    <Button
                                                        size="small"
                                                        onClick={() =>
                                                            reason(
                                                                "解除该条 Hold",
                                                                `quality/holds/${x.id}/release`,
                                                                x,
                                                            )
                                                        }
                                                    >
                                                        解除 Hold
                                                    </Button>
                                                ) : (
                                                    "—"
                                                ),
                                        },
                                    ]}
                                />
                            </>
                        )}
                        {row.releases && (
                            <>
                                <Typography.Title level={5}>
                                    质量放行记录
                                </Typography.Title>
                                <Table
                                    size="small"
                                    rowKey="id"
                                    pagination={false}
                                    dataSource={row.releases}
                                    columns={[
                                        {
                                            title: "放行时间",
                                            dataIndex: "releasedAt",
                                            render: fmt,
                                        },
                                        {
                                            title: "动作",
                                            dataIndex: "recoveryAction",
                                            render: qualityStatus,
                                        },
                                        {
                                            title: "审核原因",
                                            dataIndex: "reason",
                                        },
                                    ]}
                                />
                            </>
                        )}
                        {row.measurements && (
                            <>
                                <Typography.Title level={5}>
                                    量测与规格快照
                                </Typography.Title>
                                <Table
                                    size="small"
                                    rowKey="id"
                                    pagination={false}
                                    dataSource={row.measurements}
                                    columns={[
                                        {
                                            title: "参数",
                                            dataIndex: "parameterCode",
                                        },
                                        {
                                            title: "检验对象",
                                            dataIndex: "waferCode",
                                            render: (value?: string) =>
                                                value || "批次",
                                        },
                                        {
                                            title: "样本 / 样点",
                                            render: (_, x: QualityItem) =>
                                                `${x.sampleNumber} / ${x.samplePoint || "—"}`,
                                        },
                                        {
                                            title: "实际值",
                                            render: (_, x: QualityItem) =>
                                                String(
                                                    x.valueNumeric ??
                                                        x.valueBoolean ??
                                                        x.valueText,
                                                ),
                                        },
                                        {
                                            title: "规格限 / 单位",
                                            render: (_, x: QualityItem) =>
                                                x.parameterType === "boolean"
                                                    ? `期望：${x.expectedBoolean ? "是" : "否"}`
                                                    : x.parameterType ===
                                                        "string"
                                                      ? `期望：${x.expectedText}`
                                                      : `${x.specMin ?? "—"} ～ ${x.specMax ?? "—"} ${x.unit || ""}`,
                                        },
                                        {
                                            title: "判定",
                                            render: (_, x: QualityItem) =>
                                                qualityStatus(
                                                    x.isPassed
                                                        ? "Passed"
                                                        : "Failed",
                                                ),
                                        },
                                    ]}
                                />
                                {row.issueId && (
                                    <Button
                                        onClick={() =>
                                            setSelected({
                                                resource: "issues",
                                                id: row.issueId,
                                            })
                                        }
                                    >
                                        打开关联异常
                                    </Button>
                                )}
                            </>
                        )}
                        {row.dispositions && (
                            <>
                                <Typography.Title level={5}>
                                    逐片报废凭据
                                </Typography.Title>
                                <Table
                                    size="small"
                                    rowKey="id"
                                    pagination={false}
                                    dataSource={row.dispositions}
                                    columns={[
                                        {
                                            title: "晶圆号",
                                            dataIndex: "waferCode",
                                        },
                                        {
                                            title: "原批次",
                                            dataIndex: "lotIdAtEvent",
                                            render: lotName,
                                        },
                                        {
                                            title: "报废时间",
                                            dataIndex: "occurredAt",
                                            render: fmt,
                                        },
                                    ]}
                                />
                            </>
                        )}
                    </>
                )}
            </Drawer>
            <Modal
                title={dialog?.title}
                open={!!dialog}
                onCancel={() => setDialog(undefined)}
                onOk={save}
                confirmLoading={busy}
                destroyOnHidden
            >
                <Form form={form} layout="vertical">
                    {dialog?.kind === "issue" && (
                        <>
                            <Form.Item
                                name="issueTypeId"
                                label="异常类型"
                                rules={required}
                            >
                                <Select
                                    options={qualityOptions(
                                        types.data?.items.filter(
                                            (x) =>
                                                x.isActive &&
                                                x.code !==
                                                    "SYS-QUALITY-CHECK-FAILED",
                                        ),
                                    )}
                                />
                            </Form.Item>
                            <Form.Item
                                name="title"
                                label="异常标题"
                                rules={required}
                            >
                                <Input maxLength={200} />
                            </Form.Item>
                            <Form.Item
                                name="description"
                                label="异常说明"
                                rules={required}
                            >
                                <Input.TextArea maxLength={4000} />
                            </Form.Item>
                            <Form.Item name="lotId" label="关联批次">
                                <QualitySelect
                                    resource="lots"
                                    onChange={() =>
                                        form.resetFields([
                                            "waferId",
                                            "stepExecutionId",
                                        ])
                                    }
                                />
                            </Form.Item>
                            <Form.Item
                                name="workOrderId"
                                label="关联工单（选择批次后自动推导）"
                            >
                                <QualitySelect resource="work-orders" />
                            </Form.Item>
                            {selectedLotId && (
                                <>
                                    <Form.Item name="waferId" label="关联晶圆">
                                        <Select
                                            allowClear
                                            options={issueLot.data?.wafers?.map(
                                                (x: QualityItem) => ({
                                                    value: x.id,
                                                    label: x.waferCode,
                                                }),
                                            )}
                                        />
                                    </Form.Item>
                                    <Form.Item
                                        name="stepExecutionId"
                                        label="关联当前步骤"
                                    >
                                        <Select
                                            allowClear
                                            options={
                                                issueLot.data?.step
                                                    ? [
                                                          {
                                                              value: issueLot
                                                                  .data.step.id,
                                                              label: issueLot
                                                                  .data.step
                                                                  .name,
                                                          },
                                                      ]
                                                    : []
                                            }
                                        />
                                    </Form.Item>
                                </>
                            )}
                            <Form.Item name="equipmentId" label="关联设备">
                                <QualitySelect resource="equipment" />
                            </Form.Item>
                            <Form.Item
                                name="holdReasonCodeId"
                                label="冻结原因（阻塞类型必填）"
                            >
                                <QualitySelect resource="reason-codes?category=Hold" />
                            </Form.Item>
                        </>
                    )}
                    {["assign", "action"].includes(dialog?.kind || "") && (
                        <Form.Item
                            name="assignedToId"
                            label="责任人"
                            rules={required}
                        >
                            <QualitySelect resource="assignees" />
                        </Form.Item>
                    )}
                    {dialog?.kind === "action" && (
                        <>
                            <Form.Item
                                name="title"
                                label="整改标题"
                                rules={required}
                            >
                                <Input maxLength={200} />
                            </Form.Item>
                            <Form.Item
                                name="actionType"
                                label="整改类别"
                                rules={required}
                            >
                                <Select
                                    options={choice([
                                        "Containment",
                                        "Corrective",
                                        "Preventive",
                                    ])}
                                />
                            </Form.Item>
                            <Form.Item name="description" label="整改说明">
                                <Input.TextArea maxLength={2000} />
                            </Form.Item>
                            <Form.Item name="dueDate" label="截止日期">
                                <Input type="date" />
                            </Form.Item>
                        </>
                    )}
                    {dialog?.kind === "resolve" && (
                        <>
                            <Alert
                                type="info"
                                title="处置结论仅记录审核；报废、解除 Hold 和质量恢复需分别执行。"
                            />
                            <Form.Item
                                name="disposition"
                                label="处置结论"
                                rules={required}
                            >
                                <Select
                                    options={choice([
                                        "Continue",
                                        "Retry",
                                        "Scrap",
                                    ])}
                                />
                            </Form.Item>
                            <Form.Item
                                name="rootCause"
                                label="原因分析"
                                rules={required}
                            >
                                <Input.TextArea maxLength={2000} />
                            </Form.Item>
                            <Form.Item
                                name="containmentAction"
                                label="遏制措施"
                                rules={required}
                            >
                                <Input.TextArea maxLength={2000} />
                            </Form.Item>
                            <Form.Item
                                name="notes"
                                label="处置说明"
                                rules={required}
                            >
                                <Input.TextArea maxLength={2000} />
                            </Form.Item>
                        </>
                    )}
                    {dialog?.kind === "hold" && (
                        <Form.Item
                            name="reasonCodeId"
                            label="冻结原因"
                            rules={required}
                        >
                            <QualitySelect resource="reason-codes?category=Hold" />
                        </Form.Item>
                    )}
                    {dialog?.kind === "release" && (
                        <>
                            <Alert
                                type="info"
                                title="须先解除全部 Hold、处置阻塞异常。继续校验剩余片已通过；重试生成当前站的新到访。"
                            />
                            <Form.Item
                                name="action"
                                label="放行动作"
                                rules={required}
                            >
                                <Select
                                    options={choice(
                                        dialog.row?.status ===
                                            "AwaitingDisposition"
                                            ? ["Continue", "Retry"]
                                            : ["None"],
                                    )}
                                />
                            </Form.Item>
                        </>
                    )}
                    {dialog?.kind === "scrap" && (
                        <>
                            <Alert
                                type="warning"
                                title="报废将关闭所选晶圆的当前归属和载具槽位，保留原生产履历。请核对逐片清单。"
                            />
                            <Form.Item
                                name="waferIds"
                                label="报废晶圆"
                                rules={required}
                            >
                                <Select
                                    mode="multiple"
                                    options={dialog.row?.wafers
                                        ?.filter(
                                            (x: QualityItem) =>
                                                x.lifecycleStatus === "Active",
                                        )
                                        .map((x: QualityItem) => ({
                                            value: x.id,
                                            label: x.waferCode,
                                        }))}
                                />
                            </Form.Item>
                            <Form.Item
                                name="reasonCodeId"
                                label="报废原因"
                                rules={required}
                            >
                                <QualitySelect resource="reason-codes?category=Scrap" />
                            </Form.Item>
                            <Form.Item
                                name="notes"
                                label="报废说明"
                                rules={required}
                            >
                                <Input.TextArea maxLength={2000} />
                            </Form.Item>
                        </>
                    )}
                    {["reason", "release"].includes(dialog?.kind || "") && (
                        <Form.Item
                            name="reason"
                            label="审核说明"
                            rules={required}
                        >
                            <Input.TextArea maxLength={2000} />
                        </Form.Item>
                    )}
                </Form>
            </Modal>
            {inspection && (
                <QualityInspection
                    initialLotId={inspection.lotId}
                    onClose={() => setInspection(undefined)}
                />
            )}
        </>
    );
}
