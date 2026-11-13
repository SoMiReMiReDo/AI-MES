import { useEffect, useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    App,
    Button,
    Descriptions,
    Drawer,
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
import { fmt as formatDate } from "./shared";

type Item = { id: string; etag: string; [key: string]: any };
type Page = { items: Item[]; total: number; page: number; pageSize: number };
const states: Record<string, string> = {
    Draft: "待投放",
    Ready: "等待加工",
    Running: "加工中",
    AwaitingDisposition: "等待处置",
    Completed: "已完成",
    Merged: "已合批转出",
    Cancelled: "已取消",
    Closed: "已收尾",
    Registered: "已登记",
    Active: "在制",
    Scrapped: "已报废",
    Available: "可用",
    Disabled: "停用",
};
const historyTypes: Record<string, string> = {
    Membership: "批次归属",
    Loaded: "装载",
    Unloaded: "卸载",
    Movement: "移位",
    Genealogy: "血缘变更",
    Create: "建批",
    Split: "拆批",
    Merge: "合批",
};
const tag = (value: string) => <Tag>{states[value] || value}</Tag>;
const required = [{ required: true, message: "请填写此项" }];

function useOptions(path: string, enabled = true) {
    return useQuery({
        queryKey: ["production-options", path],
        enabled,
        queryFn: async () => {
            const items: Item[] = [];
            for (let page = 1; page <= 100; page++) {
                const result = await api<Page>(
                    `${path}${path.includes("?") ? "&" : "?"}pageSize=100&page=${page}`,
                );
                items.push(...result.items);
                if (items.length >= result.total) return items;
            }
            return items;
        },
    });
}

function OptionSelect({ path, labelKey, enabled = true, ...props }: any) {
    const query = useOptions(path, enabled);
    return (
        <>
            {query.error && <Alert type="error" title={query.error.message} />}
            <Select
                showSearch
                optionFilterProp="label"
                loading={query.isLoading}
                options={(query.data || []).map((x) => ({
                    value: x.id,
                    label: x[labelKey] || `${x.code || ""} ${x.name || x.id}`,
                }))}
                {...props}
            />
        </>
    );
}

export function Production({ me }: { me: Me }) {
    const can = (p: string) => !!me.permissions?.includes(`production.${p}`);
    const tabs = [
        { key: "lots", label: "批次与血缘", permission: "lots.read" },
        { key: "wafers", label: "晶圆履历", permission: "wafers.read" },
        { key: "carriers", label: "载具与槽位", permission: "carriers.read" },
    ].filter((x) => can(x.permission));
    return (
        <>
            <Typography.Title level={2}>生产管理</Typography.Title>
            <Alert
                type="info"
                showIcon
                title="批次、晶圆与载具"
                description="本页管理投放、归属及载具记录。过站与终点完工请进入生产执行，质量处置与返工将在后续模块提供。"
            />
            <Tabs
                destroyOnHidden
                items={tabs.map((x) => ({
                    key: x.key,
                    label: x.label,
                    children: (
                        <ProductionTable
                            key={x.key}
                            resource={x.key}
                            can={can}
                            me={me}
                        />
                    ),
                }))}
            />
        </>
    );
}

function ProductionTable({
    resource,
    can,
    me,
}: {
    resource: string;
    can: (p: string) => boolean;
    me: Me;
}) {
    const { message } = App.useApp();
    const qc = useQueryClient();
    const [page, setPage] = useState(1);
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState<string>();
    const [selection, setSelection] = useState<Item[]>([]);
    const [detailId, setDetailId] = useState<string>();
    const [dialog, setDialog] = useState<string>();
    const [selectedWafers, setSelectedWafers] = useState<string[]>([]);
    const [preview, setPreview] = useState<any>();
    const [error, setError] = useState("");
    const [busy, setBusy] = useState(false);
    const [form] = Form.useForm();
    const commandKeys = useRef(new Map<string, string>());
    const codeKey =
        resource === "lots"
            ? "lotNumber"
            : resource === "wafers"
              ? "waferCode"
              : "carrierCode";
    const statusKey = resource === "wafers" ? "lifecycleStatus" : "status";
    const path = `production/${resource}`;
    const query = useQuery({
        queryKey: ["production", resource, page, search, status],
        queryFn: () =>
            api<Page>(
                `${path}?page=${page}&${codeKey}=${encodeURIComponent(search)}${status ? `&${statusKey}=${status}` : ""}`,
            ),
    });
    const detailQuery = useQuery({
        queryKey: ["production-detail", resource, detailId],
        enabled: !!detailId,
        queryFn: () => api<Item>(`${path}/${detailId}`),
    });
    const detail = detailQuery.data;
    const waferQuery = useQuery({
        queryKey: ["production-members", detailId],
        enabled: resource === "lots" && !!detailId && can("wafers.read"),
        queryFn: () =>
            api<Page>(`production/lots/${detailId}/wafers?pageSize=100`),
    });
    const historyQuery = useQuery({
        queryKey: ["production-history", resource, detailId],
        enabled: !!detailId && (resource !== "lots" || can("genealogy.read")),
        queryFn: () =>
            api<any>(
                `${path}/${detailId}/${resource === "lots" ? "genealogy?depth=10&pageSize=100" : "history?pageSize=100"}`,
            ),
    });
    const open = (kind: string) => {
        setDialog(kind);
        setError("");
        setPreview(undefined);
    };
    useEffect(() => {
        if (!dialog) return;
        form.resetFields();
        if (dialog === "edit")
            form.setFieldsValue({
                priority: detail?.priority,
                notes: detail?.notes,
            });
        if (dialog === "create")
            form.setFieldsValue({
                priority: 50,
                waferSizeMm: 300,
                slotCapacity: 25,
                carrierType: "Demo",
                allowedWaferSizeMm: 300,
            });
        if (dialog === "loads") form.setFieldsValue({ moves: [{}] });
    }, [dialog, form]);
    const refresh = async () => {
        await qc.invalidateQueries();
        setSelectedWafers([]);
        setSelection([]);
    };
    const command = async (
        target: string,
        body: any,
        etag?: string,
        method = "POST",
    ) => {
        const signature = JSON.stringify({ target, body, etag, method });
        const key = commandKeys.current.get(signature) || crypto.randomUUID();
        commandKeys.current.set(signature, key);
        const result = await api<any>(target, method, body, etag, key);
        commandKeys.current.delete(signature);
        return result;
    };
    const mergeBody = async (values: any) => {
        // Keep the selection's original ETags: a changed source must be explicitly reselected.
        const sources = await Promise.all(
            selection.map(async (lot) => ({
                lotId: lot.id,
                etag: lot.etag,
                waferIds: (
                    await api<Page>(
                        `production/lots/${lot.id}/wafers?pageSize=100`,
                    )
                ).items.map((w) => w.id),
            })),
        );
        return {
            sources,
            reason: values.reason,
            newLotNumber: values.newLotNumber || null,
        };
    };
    const doPreview = async () => {
        setError("");
        setBusy(true);
        try {
            const values = await form.validateFields();
            const body =
                dialog === "merge"
                    ? await mergeBody(values)
                    : {
                          waferIds: selectedWafers,
                          reason: values.reason,
                          newLotNumber: values.newLotNumber || null,
                      };
            const result = await api<any>(
                dialog === "merge"
                    ? "production/lot-merges/preview"
                    : `production/lots/${detailId}/split-preview`,
                "POST",
                body,
            );
            setPreview({ result, body, etag: detail?.etag });
        } catch (e: any) {
            setError(e.message || "请检查输入");
        } finally {
            setBusy(false);
        }
    };
    const submit = async () => {
        setError("");
        setBusy(true);
        try {
            const values = await form.validateFields();
            if (dialog === "create" && resource === "lots") {
                const codes = String(values.waferCodes || "")
                    .split(/\r?\n/)
                    .map((s) => s.trim())
                    .filter(Boolean);
                if (
                    !codes.length ||
                    codes.length > 100 ||
                    new Set(codes).size !== codes.length
                )
                    throw new Error("请逐行填写 1～100 个不重复的晶圆编号");
                await command(path, {
                    workOrderId: values.workOrderId,
                    lotNumber: values.lotNumber || null,
                    priority: values.priority,
                    notes: values.notes || null,
                    wafers: codes.map((waferCode) => ({
                        waferCode,
                        waferSizeMm: values.waferSizeMm,
                        originMaterialLotId: values.originMaterialLotId || null,
                    })),
                });
            } else if (dialog === "create") {
                await command(path, {
                    ...values,
                    currentLocationId: values.currentLocationId || null,
                });
            } else if (dialog === "edit") {
                await command(
                    `${path}/${detailId}`,
                    { priority: values.priority, notes: values.notes || null },
                    detail?.etag,
                    "PUT",
                );
            } else if (dialog === "release" || dialog === "cancel") {
                await command(
                    `${path}/${detailId}/${dialog}`,
                    dialog === "release" ? {} : { reason: values.reason },
                    detail?.etag,
                );
            } else if (dialog === "split" || dialog === "merge") {
                if (!preview) throw new Error("请先预览并核对片集与数量");
                const result = await command(
                    dialog === "merge"
                        ? "production/lot-merges"
                        : `production/lots/${detailId}/split`,
                    preview.body,
                    dialog === "split" ? preview.etag : undefined,
                );
                setDetailId(result.newLot.id);
            } else if (dialog === "status") {
                await command(
                    `${path}/${detailId}`,
                    {
                        status:
                            detail?.status === "Available"
                                ? "Disabled"
                                : "Available",
                    },
                    detail?.etag,
                    "PUT",
                );
            } else if (dialog === "move") {
                await command(`${path}/${detailId}/move`, values, detail?.etag);
            } else if (dialog === "loads") {
                const rows = values.moves as {
                    waferId: string;
                    toSlotId?: string;
                }[];
                if (new Set(rows.map((x) => x.waferId)).size !== rows.length)
                    throw new Error("晶圆不能重复选择");
                // Resolve current source slots and versions once. Retain this exact payload on network retry.
                const signature = JSON.stringify(values);
                let body =
                    preview?.signature === signature ? preview.body : undefined;
                if (!body) {
                    const wafers = await Promise.all(
                        rows.map((x) =>
                            api<Item>(`production/wafers/${x.waferId}`),
                        ),
                    );
                    const lotIds = [
                        ...new Set(
                            wafers.map(
                                (x) => x.currentMembership?.lotId as string,
                            ),
                        ),
                    ];
                    if (lotIds.some((x) => !x))
                        throw new Error("选中的晶圆缺少当前批次归属");
                    const lots = await Promise.all(
                        lotIds.map((id) => api<Item>(`production/lots/${id}`)),
                    );
                    const carrierIds = [
                        ...new Set([
                            detailId!,
                            ...wafers
                                .map((x) => x.currentSlot?.carrierId)
                                .filter(Boolean),
                        ]),
                    ];
                    const carriers = await Promise.all(
                        carrierIds.map((id) =>
                            api<Item>(`production/carriers/${id}`),
                        ),
                    );
                    const moves = rows.map((x, i) => ({
                        waferId: x.waferId,
                        fromSlotId:
                            wafers[i].currentLoad?.carrierSlotId || null,
                        toSlotId: x.toSlotId || null,
                    }));
                    const usedSlots = new Set(
                        moves
                            .flatMap((x) => [x.fromSlotId, x.toSlotId])
                            .filter(Boolean),
                    );
                    body = {
                        reason: values.reason,
                        moves,
                        carrierVersions: carriers
                            .filter((c) =>
                                c.slots.some((s: Item) => usedSlots.has(s.id)),
                            )
                            .map((c) => ({ id: c.id, etag: c.etag })),
                        waferVersions: wafers.map((w) => ({
                            id: w.id,
                            etag: w.etag,
                        })),
                        lotVersions: lots.map((l) => ({
                            id: l.id,
                            etag: l.etag,
                        })),
                    };
                    setPreview({ signature, body });
                }
                await command("production/carrier-load-commands", body);
            }
            setDialog(undefined);
            await refresh();
            message.success("操作已保存");
        } catch (e: any) {
            setError(e.message || "请检查表单");
            if (e instanceof ApiError && [409, 412].includes(e.status)) {
                setPreview(undefined);
                await refresh();
            }
        } finally {
            setBusy(false);
        }
    };
    const errorAlert = (e: any) =>
        e && <Alert type="error" showIcon title={e.message || "加载失败"} />;
    const names: Record<string, string> = {
        create: resource === "lots" ? "工单建批" : "新建载具",
        release: "投放批次",
        cancel: "取消未投放批次",
        split: "按片拆批",
        merge: "合入新批",
        edit: "编辑草稿",
        loads: "装卸与换槽",
        move: "载具人工移位",
        status: "变更载具状态",
    };
    return (
        <>
            <Space wrap style={{ marginBottom: 16 }}>
                <Input.Search
                    aria-label="按编号筛选"
                    placeholder="按编号筛选"
                    allowClear
                    onSearch={(v) => {
                        setSearch(v);
                        setPage(1);
                        setSelection([]);
                    }}
                />
                <Select
                    aria-label="状态筛选"
                    placeholder="全部状态"
                    allowClear
                    style={{ width: 160 }}
                    value={status}
                    onChange={(v) => {
                        setStatus(v);
                        setPage(1);
                        setSelection([]);
                    }}
                    options={(resource === "lots"
                        ? [
                              "Draft",
                              "Ready",
                              "Running",
                              "Completed",
                              "Merged",
                              "Cancelled",
                              "Closed",
                          ]
                        : resource === "wafers"
                          ? [
                                "Registered",
                                "Active",
                                "Completed",
                                "Scrapped",
                                "Cancelled",
                            ]
                          : ["Available", "Disabled"]
                    ).map((v) => ({ value: v, label: states[v] }))}
                />
                {resource === "lots" && can("lots.create") && (
                    <Button type="primary" onClick={() => open("create")}>
                        工单建批
                    </Button>
                )}
                {resource === "carriers" && can("carriers.manage") && (
                    <Button type="primary" onClick={() => open("create")}>
                        新建载具
                    </Button>
                )}
                {resource === "lots" &&
                    can("lots.merge") &&
                    can("wafers.read") && (
                        <Button
                            disabled={selection.length < 2}
                            onClick={() => open("merge")}
                        >
                            合并选中批次
                        </Button>
                    )}
                <Button onClick={refresh}>刷新</Button>
            </Space>
            {errorAlert(query.error)}
            <Table<Item>
                rowKey="id"
                dataSource={query.data?.items || []}
                loading={query.isFetching}
                rowSelection={
                    resource === "lots" && can("lots.merge")
                        ? {
                              selectedRowKeys: selection.map((x) => x.id),
                              onChange: (_, rows) => setSelection(rows),
                          }
                        : undefined
                }
                pagination={{
                    current: page,
                    total: query.data?.total,
                    pageSize: 20,
                    showSizeChanger: false,
                    onChange: (v) => {
                        setPage(v);
                        setSelection([]);
                    },
                }}
                columns={[
                    {
                        title: "编号",
                        dataIndex: codeKey,
                        render: (v, row) => (
                            <Button
                                type="link"
                                onClick={() => {
                                    setDetailId(row.id);
                                    setSelectedWafers([]);
                                }}
                            >
                                {v}
                            </Button>
                        ),
                    },
                    { title: "状态", dataIndex: statusKey, render: tag },
                    ...(resource === "lots"
                        ? [
                              {
                                  title: "当前片数",
                                  render: (_: any, x: Item) =>
                                      x.quantities?.currentWaferCount,
                              },
                              {
                                  title: "阻塞",
                                  render: (_: any, x: Item) =>
                                      x.blockingReasons?.join("；") ||
                                      "无已知阻塞",
                              },
                          ]
                        : resource === "carriers"
                          ? [
                                { title: "类型", dataIndex: "carrierType" },
                                { title: "容量", dataIndex: "slotCapacity" },
                            ]
                          : [
                                {
                                    title: "尺寸（mm）",
                                    dataIndex: "waferSizeMm",
                                },
                            ]),
                ]}
            />
            <Drawer
                title={detail?.[codeKey] || "详情"}
                open={!!detailId}
                size={980}
                onClose={() => setDetailId(undefined)}
            >
                {errorAlert(detailQuery.error)}
                {detail && (
                    <>
                        <Descriptions
                            size="small"
                            bordered
                            column={2}
                            items={[
                                {
                                    key: "state",
                                    label: "状态",
                                    children: tag(detail[statusKey]),
                                },
                                {
                                    key: "id",
                                    label: "编号",
                                    children: detail[codeKey],
                                },
                                ...(resource === "lots"
                                    ? [
                                          {
                                              key: "workOrder",
                                              label: "工单",
                                              children:
                                                  detail.workOrderNo ||
                                                  detail.workOrderId,
                                          },
                                          {
                                              key: "snapshot",
                                              label: "基线版本",
                                              children: detail.snapshotVersion,
                                          },
                                          {
                                              key: "step",
                                              label: "当前步骤",
                                              children:
                                                  detail.currentStep?.name ||
                                                  "—",
                                          },
                                          {
                                              key: "count",
                                              label: "当前归属片数",
                                              children:
                                                  detail.quantities
                                                      .currentWaferCount,
                                          },
                                          {
                                              key: "wip",
                                              label: "在制片数",
                                              children:
                                                  detail.quantities
                                                      .inProcessQty,
                                          },
                                          {
                                              key: "complete",
                                              label: "完工片数",
                                              children: "待执行模块接通",
                                          },
                                      ]
                                    : resource === "wafers"
                                      ? [
                                            {
                                                key: "workOrder",
                                                label: "稳定工单",
                                                children:
                                                    detail.workOrderNo ||
                                                    detail.workOrderId,
                                            },
                                            {
                                                key: "lot",
                                                label: "当前批次",
                                                children:
                                                    detail.currentMembership
                                                        ?.lotId || "无有效归属",
                                            },
                                            {
                                                key: "source",
                                                label: "物料来源",
                                                children:
                                                    detail.originMaterialLotId ||
                                                    "来源未登记",
                                            },
                                            {
                                                key: "slot",
                                                label: "当前槽位",
                                                children:
                                                    detail.currentSlot
                                                        ?.slotNo || "未装载",
                                            },
                                        ]
                                      : [
                                            {
                                                key: "location",
                                                label: "当前位置",
                                                children:
                                                    detail.currentLocationId ||
                                                    "位置未登记",
                                            },
                                            {
                                                key: "size",
                                                label: "适用尺寸（mm）",
                                                children:
                                                    detail.allowedWaferSizeMm ||
                                                    "待登记",
                                            },
                                        ]),
                            ]}
                        />
                        {!!detail.blockingReasons?.length && (
                            <Alert
                                type="warning"
                                title={detail.blockingReasons.join("；")}
                            />
                        )}
                        <Space wrap style={{ margin: "16px 0" }}>
                            {resource === "lots" &&
                                (detail.allowedActions || [])
                                    .filter((a: string) => a !== "merge")
                                    .map((a: string) => (
                                        <Button
                                            key={a}
                                            disabled={
                                                a === "split" &&
                                                !selectedWafers.length
                                            }
                                            onClick={() =>
                                                open(
                                                    a === "manage" ? "edit" : a,
                                                )
                                            }
                                        >
                                            {names[a === "manage" ? "edit" : a]}
                                        </Button>
                                    ))}
                            {resource === "carriers" &&
                                can("carriers.load") &&
                                can("wafers.read") &&
                                can("lots.read") && (
                                    <Button onClick={() => open("loads")}>
                                        装卸与换槽
                                    </Button>
                                )}
                            {resource === "carriers" &&
                                can("carriers.move") && (
                                    <Button onClick={() => open("move")}>
                                        人工移位
                                    </Button>
                                )}
                            {resource === "carriers" &&
                                can("carriers.manage") && (
                                    <Button onClick={() => open("status")}>
                                        {detail.status === "Available"
                                            ? "停用载具"
                                            : "启用载具"}
                                    </Button>
                                )}
                        </Space>
                        {resource === "lots" && can("wafers.read") && (
                            <>
                                {errorAlert(waferQuery.error)}
                                <Table<Item>
                                    size="small"
                                    rowKey="id"
                                    dataSource={waferQuery.data?.items || []}
                                    pagination={false}
                                    rowSelection={{
                                        selectedRowKeys: selectedWafers,
                                        onChange: (keys) =>
                                            setSelectedWafers(keys as string[]),
                                    }}
                                    columns={[
                                        {
                                            title: "晶圆编号",
                                            dataIndex: "waferCode",
                                        },
                                        {
                                            title: "状态",
                                            dataIndex: "lifecycleStatus",
                                            render: tag,
                                        },
                                        {
                                            title: "当前槽位",
                                            render: (_, w) =>
                                                w.currentSlot?.slotNo ||
                                                "未装载",
                                        },
                                    ]}
                                />
                            </>
                        )}
                        {resource === "carriers" && (
                            <Table<Item>
                                rowKey="id"
                                size="small"
                                pagination={false}
                                dataSource={detail.slots}
                                columns={[
                                    { title: "槽位", dataIndex: "slotNo" },
                                    {
                                        title: "晶圆",
                                        render: (_, s) =>
                                            s.wafer?.waferCode ||
                                            (s.occupied
                                                ? "已占用（无晶圆详情权限）"
                                                : "空槽"),
                                    },
                                    {
                                        title: "批次",
                                        render: (_, s) =>
                                            s.wafer?.currentMembership?.lotId ||
                                            "—",
                                    },
                                ]}
                            />
                        )}
                        <Typography.Title level={5}>
                            {resource === "lots" ? "血缘记录" : "时间履历"}
                        </Typography.Title>
                        {errorAlert(historyQuery.error)}
                        {historyQuery.data?.truncated && (
                            <Alert
                                type="info"
                                title="当前展示部分历史，请从来源对象继续查询。"
                            />
                        )}
                        <Table<Item>
                            size="small"
                            rowKey={(r: any) => `${r.id}-${r.type || "event"}`}
                            pagination={{ pageSize: 10 }}
                            dataSource={historyQuery.data?.items || []}
                            expandable={{
                                rowExpandable: (row) =>
                                    !!(
                                        row.details ||
                                        row.data?.details ||
                                        row.type === "Membership"
                                    ),
                                expandedRowRender: (row) => {
                                    const event =
                                        row.type === "Genealogy"
                                            ? row.data
                                            : row;
                                    return event.details ? (
                                        <Table<Item>
                                            rowKey="waferId"
                                            size="small"
                                            pagination={false}
                                            dataSource={event.details}
                                            columns={[
                                                {
                                                    title: "晶圆 ID",
                                                    dataIndex: "waferId",
                                                },
                                                {
                                                    title: "原批次",
                                                    render: (_, item) =>
                                                        event.lotNumbers?.[
                                                            item.fromLotId
                                                        ] || "首次登记",
                                                },
                                                {
                                                    title: "目标批次",
                                                    render: (_, item) =>
                                                        event.lotNumbers?.[
                                                            item.toLotId
                                                        ] || item.toLotId,
                                                },
                                            ]}
                                        />
                                    ) : (
                                        <Descriptions
                                            size="small"
                                            items={[
                                                {
                                                    key: "from",
                                                    label: "归属开始",
                                                    children: formatDate(
                                                        row.data?.validFrom,
                                                    ),
                                                },
                                                {
                                                    key: "to",
                                                    label: "归属结束",
                                                    children: row.data?.validTo
                                                        ? formatDate(
                                                              row.data.validTo,
                                                          )
                                                        : "当前归属",
                                                },
                                            ]}
                                        />
                                    );
                                },
                            }}
                            columns={[
                                {
                                    title: "类型",
                                    render: (_, r) =>
                                        historyTypes[r.type] ||
                                        historyTypes[r.eventType] ||
                                        r.eventType ||
                                        r.type,
                                },
                                {
                                    title: "时间",
                                    dataIndex: "occurredAt",
                                    render: formatDate,
                                },
                                {
                                    title: "来源/原因",
                                    render: (_, r) =>
                                        r.reason ||
                                        r.data?.lotNumber ||
                                        r.data?.lotId ||
                                        r.data?.notes ||
                                        r.id,
                                },
                            ]}
                        />
                    </>
                )}
            </Drawer>
            <Modal
                title={names[dialog || ""]}
                open={!!dialog}
                width={720}
                destroyOnHidden
                maskClosable={!busy}
                confirmLoading={busy}
                onCancel={() => !busy && setDialog(undefined)}
                onOk={submit}
                okText={
                    dialog === "split" || dialog === "merge"
                        ? "确认执行"
                        : "保存"
                }
            >
                {error && (
                    <Alert
                        type="error"
                        showIcon
                        title={error}
                        style={{ marginBottom: 12 }}
                    />
                )}
                <Form
                    form={form}
                    initialValues={
                        dialog === "edit"
                            ? {
                                  priority: detail?.priority,
                                  notes: detail?.notes,
                              }
                            : {
                                  priority: 50,
                                  waferSizeMm: 300,
                                  slotCapacity: 25,
                                  carrierType: "Demo",
                                  allowedWaferSizeMm: 300,
                                  moves: [{}],
                              }
                    }
                    layout="vertical"
                    preserve={false}
                    onValuesChange={() => setPreview(undefined)}
                >
                    {dialog === "create" && resource === "lots" && (
                        <>
                            <Form.Item
                                name="workOrderId"
                                label="已下达工单"
                                rules={required}
                            >
                                <OptionSelect
                                    path="planning/work-orders?status=Released"
                                    labelKey="orderNo"
                                    enabled={
                                        !!me.permissions?.includes(
                                            "planning.work-orders.read",
                                        )
                                    }
                                />
                            </Form.Item>
                            <Form.Item
                                name="lotNumber"
                                label="批号（留空按编号规则分配）"
                            >
                                <Input maxLength={64} />
                            </Form.Item>
                            <Form.Item
                                name="waferSizeMm"
                                label="晶圆尺寸（mm）"
                                rules={required}
                            >
                                <InputNumber min={0.000001} />
                            </Form.Item>
                            <Form.Item
                                name="waferCodes"
                                label="晶圆编号（每行一片）"
                                rules={required}
                            >
                                <Input.TextArea rows={5} />
                            </Form.Item>
                            <Form.Item
                                name="originMaterialLotId"
                                label="来源物料批次 ID（可空）"
                            >
                                <Input placeholder="未登记来源时留空" />
                            </Form.Item>
                        </>
                    )}
                    {(dialog === "edit" ||
                        (dialog === "create" && resource === "lots")) && (
                        <>
                            <Form.Item
                                name="priority"
                                label="优先级"
                                rules={required}
                            >
                                <InputNumber min={0} max={100} />
                            </Form.Item>
                            <Form.Item name="notes" label="备注">
                                <Input.TextArea maxLength={2000} />
                            </Form.Item>
                        </>
                    )}
                    {dialog === "create" && resource === "carriers" && (
                        <>
                            <Form.Item
                                name="carrierCode"
                                label="载具编号"
                                rules={required}
                            >
                                <Input maxLength={64} />
                            </Form.Item>
                            <Form.Item
                                name="carrierType"
                                label="载具类型"
                                rules={required}
                            >
                                <Input maxLength={32} />
                            </Form.Item>
                            <Form.Item
                                name="slotCapacity"
                                label="槽位容量"
                                rules={required}
                            >
                                <InputNumber min={1} max={100} />
                            </Form.Item>
                            <Form.Item
                                name="allowedWaferSizeMm"
                                label="适用晶圆尺寸（mm）"
                                rules={required}
                            >
                                <InputNumber min={0.000001} />
                            </Form.Item>
                            <Form.Item
                                name="currentLocationId"
                                label="初始位置（可空）"
                            >
                                <OptionSelect
                                    allowClear
                                    path="organization/locations"
                                />
                            </Form.Item>
                        </>
                    )}
                    {dialog === "release" && (
                        <Alert
                            type="info"
                            title={`将投放 ${detail?.quantities.currentWaferCount} 片到固定路线首站。此操作不解除 Hold。`}
                        />
                    )}
                    {dialog === "status" && (
                        <Alert
                            type="info"
                            title={
                                detail?.status === "Available"
                                    ? "停用前须卸下全部晶圆。"
                                    : "恢复载具可用状态。"
                            }
                        />
                    )}
                    {(dialog === "split" || dialog === "merge") && (
                        <>
                            <Alert
                                type="info"
                                title={
                                    dialog === "split"
                                        ? `已选 ${selectedWafers.length} 片，源批必须至少保留一片。`
                                        : `选中 ${selection.length} 个源批，将全部合入一个新批。`
                                }
                            />
                            <Form.Item
                                name="newLotNumber"
                                label="新批号（留空按规则分配）"
                            >
                                <Input maxLength={64} />
                            </Form.Item>
                        </>
                    )}
                    {dialog === "move" && (
                        <Form.Item
                            name="toLocationId"
                            label="目标位置"
                            rules={required}
                        >
                            <OptionSelect path="organization/locations" />
                        </Form.Item>
                    )}
                    {dialog === "loads" && (
                        <>
                            <Alert
                                type="info"
                                title="选择晶圆及本载具的目标槽。目标留空表示卸载；跨载具转入时自动核对原槽，交换槽位需在同一清单中列出两片。"
                            />
                            <Form.List name="moves">
                                {(fields, { add, remove }) => (
                                    <>
                                        {fields.map((field) => (
                                            <Space
                                                key={field.key}
                                                align="baseline"
                                            >
                                                <Form.Item
                                                    name={[
                                                        field.name,
                                                        "waferId",
                                                    ]}
                                                    label="晶圆"
                                                    rules={required}
                                                >
                                                    <OptionSelect
                                                        path="production/wafers"
                                                        labelKey="waferCode"
                                                        style={{ width: 300 }}
                                                    />
                                                </Form.Item>
                                                <Form.Item
                                                    name={[
                                                        field.name,
                                                        "toSlotId",
                                                    ]}
                                                    label="目标槽（空为卸载）"
                                                >
                                                    <Select
                                                        allowClear
                                                        style={{ width: 170 }}
                                                        options={detail?.slots.map(
                                                            (s: Item) => ({
                                                                value: s.id,
                                                                label: `槽 ${s.slotNo}${s.occupied ? " · 已占用" : ""}`,
                                                            }),
                                                        )}
                                                    />
                                                </Form.Item>
                                                <Button
                                                    onClick={() =>
                                                        remove(field.name)
                                                    }
                                                >
                                                    移除
                                                </Button>
                                            </Space>
                                        ))}
                                        <Button
                                            onClick={() => add()}
                                            disabled={fields.length >= 100}
                                        >
                                            添加晶圆
                                        </Button>
                                    </>
                                )}
                            </Form.List>
                        </>
                    )}
                    {dialog &&
                        ["cancel", "split", "merge", "loads", "move"].includes(
                            dialog,
                        ) && (
                            <Form.Item
                                name="reason"
                                label="操作原因"
                                rules={required}
                            >
                                <Input.TextArea maxLength={1000} />
                            </Form.Item>
                        )}
                    {(dialog === "split" || dialog === "merge") && (
                        <>
                            <Button loading={busy} onClick={doPreview}>
                                预览并检查
                            </Button>
                            {preview && (
                                <Alert
                                    type="success"
                                    title={`新批 ${preview.result.newCount} 片${preview.result.remainingCount !== undefined ? `，源批保留 ${preview.result.remainingCount} 片` : ""}；提交时将重新校验。`}
                                />
                            )}
                        </>
                    )}
                </Form>
            </Modal>
        </>
    );
}
