import { useSearchParams } from "react-router-dom";
import { useRef, useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    App as AntApp,
    Button,
    Descriptions,
    Drawer,
    Form,
    Input,
    InputNumber,
    Modal,
    Select,
    Space,
    Switch,
    Table,
    Tabs,
    Tag,
    Typography,
} from "antd";
import { api, ApiError, type Me } from "./api";
import { fmt, Heading } from "./shared";
import { useQualityCommand, type QualityItem as Item } from "./qualityShared";

type Field = {
    key: string;
    label: string;
    kind?: string;
    options?: string[];
    reference?: string;
    required?: boolean;
    initial?: unknown;
};
type Dialog = {
    title: string;
    path: string;
    fields: Field[];
    row?: Item;
    method?: string;
    fixed?: Record<string, unknown>;
    equipmentVersionId?: string;
};
const names: Record<string, string> = {
    Idle: "空闲",
    Running: "加工中",
    Down: "故障停机",
    Maintenance: "维护",
    Offline: "离线",
    Online: "在线",
    Unknown: "未知",
    Applied: "已应用",
    Rejected: "已拒绝",
    Planned: "已计划",
    InProgress: "维护中",
    Completed: "待复机",
    Released: "已复机",
    Cancelled: "已取消",
    Available: "可用",
    Blocked: "禁用",
    Qualified: "已认证",
    Revoked: "已撤销",
    Hours: "小时",
    Days: "天",
    State: "运行条件",
    Connection: "通信状态",
    Manual: "人工",
    Simulator: "模拟器",
    Execution: "生产执行",
    Migration: "启用校准",
    Legacy: "历史登记",
};
const runNames: Record<string, string> = {
    Running: "加工中",
    Completed: "已完成",
    Aborted: "已中止",
};
const titles: Record<string, string> = {
    tools: "设备",
    types: "设备类型",
    groups: "设备组",
    chambers: "腔体",
    states: "状态区间",
    events: "事件",
    downtimes: "停机",
    reasons: "停机原因",
    shifts: "班次",
    resources: "工具",
    "maintenance-plans": "维护计划",
    maintenance: "维护工单",
    "equipment-qualifications": "设备资格",
    "worker-qualifications": "人员资格",
    policies: "工序门禁",
    reticles: "光罩",
    "reticle-mounts": "光罩装卸",
    consumables: "耗材定义",
    "consumable-lots": "耗材批次",
    "material-mounts": "耗材上机",
    statistics: "时长统计",
};
const groups = [
    ["registry", "台账", ["tools", "types", "groups", "chambers"]],
    ["events", "状态与事件", ["events", "states"]],
    [
        "downtime",
        "停机与统计",
        ["downtimes", "statistics", "reasons", "shifts"],
    ],
    [
        "maintenance",
        "工具与维护",
        ["maintenance", "maintenance-plans", "resources"],
    ],
    [
        "qualifications",
        "资格与门禁",
        ["equipment-qualifications", "worker-qualifications", "policies"],
    ],
    ["reticles", "光罩", ["reticles", "reticle-mounts"]],
    [
        "materials",
        "耗材",
        ["material-mounts", "consumable-lots", "consumables"],
    ],
] as const;
const field = (
    key: string,
    label: string,
    kind = "text",
    required = true,
): Field => ({ key, label, kind, required });
const ref = (
    key: string,
    label: string,
    reference: string,
    required = true,
): Field => ({ key, label, reference, required });
const choice = (key: string, label: string, options: string[]): Field => ({
    key,
    label,
    options,
    required: true,
});
const reason = field("reason", "操作说明");
const codeName = [field("code", "编码"), field("name", "名称")];
const target = [
    ref("equipmentId", "设备（与工具二选一）", "equipment", false),
    ref("toolId", "工具", "resources", false),
];
const qualification = [
    ref("operationId", "工序", "operations"),
    ref("recipeVersionId", "专项配方（可选）", "recipes", false),
    field("validFrom", "生效时间", "date"),
    field("validTo", "到期时间（可选）", "date", false),
];
const policy = [
    ref("operationId", "工序", "operations"),
    field("requireEquipmentQualification", "启用设备与人员资格", "bool", false),
    field("requiredReticleLayer", "必需光罩层次（可选）", "text", false),
    {
        ...ref("requiredMaterialIds", "必需耗材（可选）", "materials", false),
        kind: "multi",
    },
];
const createFields: Record<string, Field[]> = {
    types: codeName,
    groups: codeName,
    reasons: codeName,
    tools: [
        ...codeName,
        ref("areaId", "区域", "areas"),
        ref("lineId", "产线（须属于该区域）", "lines", false),
        ref("equipmentGroupId", "设备组", "groups"),
        ref("equipmentTypeId", "设备类型", "types"),
    ],
    chambers: [...codeName, ref("equipmentId", "设备", "equipment")],
    shifts: [
        ...codeName,
        field("startTime", "开始时间", "time"),
        field("endTime", "结束时间", "time"),
    ],
    events: [
        ref("equipmentId", "设备", "equipment"),
        field("source", "模拟来源标识"),
        field("sourceEventId", "来源事件编号"),
        field("occurredAt", "发生时间", "date"),
        choice("kind", "事件类别", ["State", "Connection"]),
        choice("state", "目标状态", [
            "Idle",
            "Down",
            "Maintenance",
            "Offline",
            "Online",
            "Unknown",
        ]),
        reason,
        ref("reasonCodeId", "停机原因（可后补）", "reasons", false),
        ref("shiftId", "班次（可选）", "shifts", false),
    ],
    resources: [
        ...codeName,
        ref("equipmentId", "绑定设备（可选）", "equipment", false),
        ref("equipmentTypeId", "设备类型（可选）", "types", false),
    ],
    "maintenance-plans": [
        field("name", "计划名称"),
        field("eventType", "维护类型"),
        ...target,
        choice("frequency", "周期单位", ["Hours", "Days"]),
        field("intervalValue", "周期数", "number"),
        field("nextDueAt", "下次到期", "date"),
        ref("assignedToId", "负责人（可选）", "users", false),
    ],
    maintenance: [
        field("title", "标题"),
        field("eventType", "维护类型"),
        ...target,
        ref("scheduleId", "关联计划（可选，目标需一致）", "plans", false),
        field("scheduledAt", "计划时间", "date"),
        ref("assignedToId", "负责人（可选）", "users", false),
    ],
    "equipment-qualifications": [
        ref("equipmentId", "设备", "equipment"),
        ...qualification,
    ],
    "worker-qualifications": [
        ref("workerId", "人员", "workers"),
        ...qualification,
    ],
    policies: policy,
    reticles: [
        ...codeName,
        field("revision", "版本"),
        field("layerCode", "层次"),
        field("maxUsageCount", "寿命（启动次数）", "number"),
        field("usableUntil", "有效截止（可选）", "date", false),
    ],
    "reticle-mounts": [
        ref("equipmentId", "设备", "equipment"),
        ref("reticleId", "光罩", "reticles"),
    ],
    consumables: [...codeName, field("unit", "单位")],
    "consumable-lots": [
        ref("materialId", "耗材", "materials"),
        field("lotNumber", "批次编号"),
        field("expiryAt", "批次有效截止（可选）", "date", false),
    ],
    "material-mounts": [
        ref("equipmentId", "设备", "equipment"),
        ref("materialLotId", "耗材批次", "materialLots"),
        field("openedAt", "开封时间", "date"),
        field("usableUntil", "上机有效截止", "date"),
    ],
};
const permission: Record<string, string> = {
    events: "simulate",
    reasons: "downtime",
    shifts: "downtime",
    downtimes: "downtime",
    resources: "maintenance",
    maintenance: "maintenance",
    "maintenance-plans": "maintenance",
    "equipment-qualifications": "qualify",
    "worker-qualifications": "qualify",
    policies: "qualify",
    reticles: "reticles",
    "reticle-mounts": "reticles",
    consumables: "materials",
    "consumable-lots": "materials",
    "material-mounts": "materials",
};
const labels: Record<string, string> = {
    code: "编码",
    name: "名称",
    lotNumber: "批次编号",
    title: "标题",
    state: "状态",
    operationalState: "运行条件",
    communicationState: "通信",
    source: "来源",
    sourceEventId: "来源编号",
    processingStatus: "处理结果",
    rejectionReason: "拒绝原因",
    eventType: "类型",
    stateFrom: "原状态",
    stateTo: "目标状态",
    isSimulated: "模拟事件",
    needsReason: "待归因",
    status: "状态",
    qualificationStatus: "资格",
    startedAt: "开始",
    endedAt: "结束",
    eventTimestamp: "发生时间",
    receivedAt: "接收时间",
    validFrom: "生效",
    validTo: "到期",
    nextDueAt: "下次到期",
    frequency: "周期单位",
    intervalValue: "周期数",
    scheduledAt: "计划时间",
    completedAt: "完成",
    releasedAt: "复机确认",
    isActive: "启用",
    revision: "版本",
    layerCode: "层次",
    usageCount: "已使用次数",
    maxUsageCount: "寿命次数",
    usableUntil: "有效截止",
    loadedAt: "装入",
    unloadedAt: "卸下",
    mountedAt: "上机",
    unmountedAt: "卸下",
    openedAt: "开封",
    resourceExpiryAt: "批次有效截止",
    unit: "单位",
    unitOfMeasure: "单位",
    requireEquipmentQualification: "设备资格门禁",
    requireWorkerQualification: "人员资格门禁",
    requiredReticleLayer: "必需光罩层次",
    requiredMaterialIds: "必需耗材",
    notes: "说明",
    resolutionNotes: "维护结论",
    description: "说明",
    areaId: "区域",
    lineId: "产线",
    equipmentGroupId: "设备组",
    equipmentTypeId: "设备类型",
    equipmentId: "设备",
    toolId: "工具",
    operationId: "工序",
    recipeVersionId: "配方版本",
    workerId: "人员",
    reasonCodeId: "停机原因",
    shiftId: "班次",
    reticleId: "光罩",
    materialId: "耗材",
    materialLotId: "耗材批次",
    scheduleId: "维护计划",
};
const references: Record<string, string> = {
    areaId: "areas",
    lineId: "lines",
    equipmentGroupId: "groups",
    equipmentTypeId: "types",
    equipmentId: "equipment",
    toolId: "resources",
    operationId: "operations",
    recipeVersionId: "recipes",
    workerId: "workers",
    reasonCodeId: "reasons",
    shiftId: "shifts",
    reticleId: "reticles",
    materialId: "materials",
    materialLotId: "materialLots",
    scheduleId: "plans",
};
const columns: Record<string, string[]> = {
    tools: [
        "code",
        "name",
        "operationalState",
        "communicationState",
        "equipmentGroupId",
        "isActive",
    ],
    types: ["code", "name", "isActive"],
    groups: ["code", "name", "isActive"],
    chambers: ["code", "name", "equipmentId", "isActive"],
    events: [
        "equipmentId",
        "eventType",
        "stateFrom",
        "stateTo",
        "eventTimestamp",
        "processingStatus",
        "rejectionReason",
    ],
    states: ["equipmentId", "state", "startedAt", "endedAt", "source"],
    downtimes: [
        "equipmentId",
        "startedAt",
        "endedAt",
        "reasonCodeId",
        "shiftId",
        "needsReason",
    ],
    resources: ["code", "name", "equipmentId", "status", "isActive"],
    "maintenance-plans": [
        "name",
        "equipmentId",
        "toolId",
        "frequency",
        "intervalValue",
        "nextDueAt",
        "isActive",
    ],
    maintenance: ["title", "equipmentId", "toolId", "scheduledAt", "status"],
    "equipment-qualifications": [
        "equipmentId",
        "operationId",
        "recipeVersionId",
        "validFrom",
        "validTo",
        "qualificationStatus",
    ],
    "worker-qualifications": [
        "workerId",
        "operationId",
        "recipeVersionId",
        "validFrom",
        "validTo",
        "qualificationStatus",
    ],
    policies: [
        "operationId",
        "requireEquipmentQualification",
        "requireWorkerQualification",
        "requiredReticleLayer",
    ],
    reticles: [
        "code",
        "name",
        "revision",
        "layerCode",
        "usageCount",
        "maxUsageCount",
        "usableUntil",
        "status",
    ],
    "reticle-mounts": ["equipmentId", "reticleId", "loadedAt", "unloadedAt"],
    "material-mounts": [
        "equipmentId",
        "materialLotId",
        "openedAt",
        "usableUntil",
        "mountedAt",
        "unmountedAt",
    ],
    consumables: ["code", "name", "unitOfMeasure", "isActive"],
    "consumable-lots": [
        "lotNumber",
        "materialId",
        "unitOfMeasure",
        "resourceExpiryAt",
        "status",
    ],
    reasons: ["code", "name", "isActive"],
    shifts: ["code", "name"],
};

export function Equipment({ me }: { me: Me }) {
    const [routeParams] = useSearchParams();
    const isDowntime = routeParams.get("view") === "downtimes";
    const isEvent = routeParams.get("view") === "events";
    const initialGroup = isDowntime
        ? "downtime"
        : isEvent
          ? "events"
          : "registry";
    const initialResource = isDowntime
        ? "downtimes"
        : isEvent
          ? "events"
          : "tools";
    const record = routeParams.get("record");
    const [group, setGroup] = useState(initialGroup);
    const [resource, setResource] = useState(initialResource);
    const [page, setPage] = useState(1);
    const [q, setQ] = useState("");
    const [equipmentId, setEquipmentId] = useState("");
    const [needsReason, setNeedsReason] = useState(false);
    const [from, setFrom] = useState("");
    const [until, setUntil] = useState("");
    const [dialog, setDialog] = useState<Dialog>();
    const [detail, setDetail] = useState<
        | {
              path: string;
              title: string;
              kind: string;
              page: number;
          }
        | undefined
    >(
        record
            ? {
                  path:
                      isDowntime || isEvent
                          ? `equipment/${initialResource}?id=${record}`
                          : `equipment/tools/${record}`,
                  title: isDowntime
                      ? "停机"
                      : isEvent
                        ? "设备事件"
                        : "设备详情",
                  kind: isDowntime || isEvent ? "record" : "equipment",
                  page: 1,
              }
            : undefined,
    );
    const [form] = Form.useForm();
    const equipmentVersion = useRef<{ signature: string; etag: string } | null>(
        null,
    );
    const { send, busy } = useQualityCommand();
    const { message } = AntApp.useApp();
    const can = (p: string) => me.permissions?.includes(`equipment.${p}`);
    const options = useQuery<Record<string, Item[]>>({
        queryKey: ["equipment", "options"],
        queryFn: () => api("equipment/options"),
    });
    const list = useQuery<{ items: Item[]; total: number }>({
        queryKey: ["equipment", resource, page, q, equipmentId, needsReason],
        queryFn: () =>
            api(
                `equipment/${resource}?${new URLSearchParams({ page: String(page), q, equipmentId, needsReason: String(needsReason) })}`,
            ),
        enabled: resource !== "statistics",
    });
    const stats = useQuery<any>({
        queryKey: ["equipment", "statistics", equipmentId, from, until],
        queryFn: () =>
            api(
                `equipment/statistics?${new URLSearchParams({
                    equipmentId,
                    from: from ? new Date(from).toISOString() : "",
                    until: until ? new Date(until).toISOString() : "",
                })}`,
            ),
        enabled: resource === "statistics",
    });
    const details = useQuery<any>({
        queryKey: ["equipment", "detail", detail],
        queryFn: () =>
            api(
                `${detail!.path}${detail!.kind === "impact" ? `?page=${detail!.page}` : ""}`,
            ),
        enabled: !!detail,
    });
    const values = (key: string) =>
        (options.data?.[key] || []).map((x) => ({
            value: x.id,
            label: [x.code, x.name].filter(Boolean).join(" · "),
        }));
    const label = (key: string, value: unknown): any => {
        if (value == null || value === "") return "—";
        if (typeof value === "boolean") return value ? "是" : "否";
        if (
            key === "status" &&
            resource === "consumable-lots" &&
            value === "Released"
        )
            return <Tag>已放行</Tag>;
        if (key === "requiredMaterialIds" && Array.isArray(value))
            return (
                value
                    .map(
                        (id) =>
                            values("materials").find(
                                (item) => item.value === id,
                            )?.label || "历史耗材",
                    )
                    .join("、") || "—"
            );
        if (references[key])
            return (
                values(references[key]).find((x) => x.value === value)?.label ||
                "历史对象（当前选项不可用）"
            );
        if (
            /At$|^validFrom$|^validTo$|^usableUntil$|^eventTimestamp$/.test(key)
        )
            return fmt(String(value));
        if (names[String(value)]) return <Tag>{names[String(value)]}</Tag>;
        return String(value);
    };
    const open = (d: Dialog, initial: Record<string, any> = {}) => {
        equipmentVersion.current = null;
        form.resetFields();
        form.setFieldsValue(initial);
        setDialog(d);
    };
    const action = (
        row: Item,
        kind: string,
        title: string,
        fields: Field[] = [reason],
    ) =>
        open({
            title,
            path: `equipment/${resource}/${row.id}/${kind}`,
            fields,
            row,
        });
    const edit = (row: Item) => {
        const fields =
            resource === "tools"
                ? [
                      ...createFields.tools.slice(1),
                      field("isActive", "启用", "bool", false),
                      field("description", "说明", "text", false),
                  ]
                : resource === "policies"
                  ? policy.filter((x) => x.key !== "operationId")
                  : [
                        field("name", "名称"),
                        field("isActive", "启用", "bool", false),
                        ...(resource === "reticles" || resource === "resources"
                            ? [
                                  choice("status", "状态", [
                                      "Available",
                                      "Blocked",
                                  ]),
                              ]
                            : []),
                    ];
        if (resource === "resources")
            fields.push(
                ref(
                    "equipmentId",
                    "绑定设备（清空解除绑定）",
                    "equipment",
                    false,
                ),
            );
        const initial = {
            ...row,
            requiredMaterialIds: row.requiredMaterialIds || [],
        };
        open(
            {
                title: `修改${titles[resource]}`,
                path: `equipment/${resource}/${row.id}`,
                method: "PUT",
                fields,
                row,
            },
            initial,
        );
    };
    const submit = async () => {
        if (!dialog) return;
        let commandSent = false;
        try {
            const v = await form.validateFields();
            const body: Record<string, any> = { ...dialog.fixed };
            for (const f of dialog.fields) {
                const value = v[f.key];
                body[f.key] =
                    f.kind === "bool"
                        ? !!value
                        : f.kind === "multi"
                          ? value || []
                          : f.kind === "date" && value
                            ? new Date(value).toISOString()
                            : f.kind === "time" && value
                              ? value + (value.length === 5 ? ":00" : "")
                              : (value ?? null);
            }
            if (resource === "policies")
                body.requireWorkerQualification =
                    body.requireEquipmentQualification;
            const versionFor = async (id: string) => {
                const signature = JSON.stringify([
                    dialog.path,
                    dialog.method,
                    body,
                    id,
                ]);
                if (equipmentVersion.current?.signature !== signature) {
                    const current = await api<any>(`equipment/tools/${id}`);
                    equipmentVersion.current = {
                        signature,
                        etag: current.equipment.etag,
                    };
                }
                return equipmentVersion.current!.etag;
            };
            let path = dialog.path;
            let etag = dialog.row?.etag;
            if (dialog.equipmentVersionId) {
                etag = await versionFor(dialog.equipmentVersionId);
            }
            if (
                dialog.title.startsWith("新增") &&
                ["events", "reticle-mounts", "material-mounts"].includes(
                    resource,
                )
            ) {
                const id = body.equipmentId;
                etag = await versionFor(id);
                path += `/${id}`;
                delete body.equipmentId;
            }
            if (dialog.method === "PUT" && resource === "policies")
                body.operationId = dialog.row?.operationId;
            commandSent = true;
            await send(path, body, etag, dialog.method || "POST");
            equipmentVersion.current = null;
            setDialog(undefined);
        } catch (error) {
            if (
                error instanceof ApiError &&
                error.status >= 400 &&
                error.status < 500
            )
                equipmentVersion.current = null;
            if (!commandSent && error instanceof Error)
                message.error(error.message);
        }
    };
    const showEquipment = (row: Item) =>
        setDetail({
            path: `equipment/tools/${row.id}`,
            title: `${row.code} · ${row.name}`,
            kind: "equipment",
            page: 1,
        });
    const tableColumns = (columns[resource] || ["code", "name"]).map((key) => ({
        title: labels[key] || key,
        dataIndex: key,
        render: (v: unknown) => label(key, v),
    }));
    return (
        <>
            <Heading
                eyebrow="EQUIPMENT"
                title="设备与生产资源"
                description="设备状态、维护、资格、光罩与耗材的统一工作台。时间按厂区时区显示。"
            />
            <Tabs
                activeKey={group}
                onChange={(key) => {
                    setGroup(key);
                    setResource(groups.find((x) => x[0] === key)![2][0]);
                    setPage(1);
                    setQ("");
                }}
                items={groups.map((g) => ({ key: g[0], label: g[1] }))}
            />
            <Space wrap style={{ marginBottom: 16 }}>
                <Select
                    aria-label="资源类别"
                    value={resource}
                    style={{ width: 170 }}
                    onChange={(value) => {
                        setResource(value);
                        setPage(1);
                    }}
                    options={groups
                        .find((x) => x[0] === group)![2]
                        .map((value) => ({ value, label: titles[value] }))}
                />
                {[
                    "tools",
                    "types",
                    "groups",
                    "chambers",
                    "reasons",
                    "shifts",
                    "resources",
                    "maintenance-plans",
                    "maintenance",
                    "reticles",
                    "consumables",
                    "consumable-lots",
                ].includes(resource) && (
                    <Input.Search
                        placeholder="名称或编码"
                        allowClear
                        onSearch={(value) => {
                            setQ(value);
                            setPage(1);
                        }}
                        style={{ width: 220 }}
                    />
                )}
                {[
                    "tools",
                    "chambers",
                    "events",
                    "states",
                    "downtimes",
                    "resources",
                    "maintenance-plans",
                    "maintenance",
                    "equipment-qualifications",
                    "reticle-mounts",
                    "material-mounts",
                    "statistics",
                ].includes(resource) && (
                    <Select
                        aria-label="筛选设备"
                        value={equipmentId || undefined}
                        placeholder="全部设备"
                        allowClear
                        style={{ width: 230 }}
                        options={values("equipment")}
                        onChange={(value) => {
                            setEquipmentId(value || "");
                            setPage(1);
                        }}
                    />
                )}
                {resource === "statistics" && (
                    <>
                        <Input
                            aria-label="统计开始（本地时间）"
                            type="datetime-local"
                            value={from}
                            onChange={(event) => setFrom(event.target.value)}
                            style={{ width: 220 }}
                        />
                        <Input
                            aria-label="统计截止（本地时间）"
                            type="datetime-local"
                            value={until}
                            onChange={(event) => setUntil(event.target.value)}
                            style={{ width: 220 }}
                        />
                    </>
                )}
                {resource === "downtimes" && (
                    <Space>
                        <Switch
                            checked={needsReason}
                            onChange={setNeedsReason}
                        />
                        仅待归因
                    </Space>
                )}
                {createFields[resource] &&
                    can(permission[resource] || "manage") && (
                        <Button
                            type="primary"
                            onClick={() =>
                                open({
                                    title: `新增${titles[resource]}`,
                                    path: `equipment/${resource}`,
                                    fields: createFields[resource],
                                })
                            }
                        >
                            新增{titles[resource]}
                        </Button>
                    )}
                <Button
                    onClick={() => {
                        list.refetch();
                        options.refetch();
                        stats.refetch();
                    }}
                >
                    刷新
                </Button>
            </Space>
            {list.error && (
                <Alert type="error" title={(list.error as Error).message} />
            )}
            {resource === "statistics" ? (
                <>
                    {stats.error && (
                        <Alert
                            type="error"
                            title={(stats.error as Error).message}
                        />
                    )}
                    <Alert
                        type="info"
                        title={
                            stats.data?.note ||
                            "最近30天状态时长；仅 Down 计停机。"
                        }
                    />
                    <Typography.Paragraph>
                        统计区间：{fmt(stats.data?.from)} ～{" "}
                        {fmt(stats.data?.until)}
                    </Typography.Paragraph>
                    <Table
                        rowKey={(x: any) => x.equipmentId + x.state}
                        loading={stats.isLoading}
                        dataSource={stats.data?.states}
                        columns={[
                            {
                                title: "设备",
                                dataIndex: "equipmentId",
                                render: (v) => label("equipmentId", v),
                            },
                            {
                                title: "状态",
                                dataIndex: "state",
                                render: (v) => label("state", v),
                            },
                            {
                                title: "时长（分钟）",
                                dataIndex: "seconds",
                                render: (v) => (v / 60).toFixed(2),
                            },
                        ]}
                    />
                    <Table
                        rowKey={(x: any) =>
                            [x.equipmentId, x.reasonCodeId, x.shiftId].join(":")
                        }
                        dataSource={stats.data?.downtimes}
                        columns={[
                            {
                                title: "设备",
                                dataIndex: "equipmentId",
                                render: (v) => label("equipmentId", v),
                            },
                            {
                                title: "原因",
                                dataIndex: "reasonCodeId",
                                render: (v) => label("reasonCodeId", v),
                            },
                            {
                                title: "班次",
                                dataIndex: "shiftId",
                                render: (v) => label("shiftId", v),
                            },
                            {
                                title: "停机分钟",
                                dataIndex: "seconds",
                                render: (v) => (v / 60).toFixed(2),
                            },
                        ]}
                    />
                </>
            ) : (
                <Table<Item>
                    rowKey="id"
                    scroll={{ x: "max-content" }}
                    loading={list.isLoading}
                    dataSource={list.data?.items}
                    pagination={{
                        current: page,
                        total: list.data?.total,
                        pageSize: 20,
                        showSizeChanger: false,
                        onChange: setPage,
                    }}
                    columns={[
                        ...tableColumns,
                        {
                            title: "操作",
                            render: (_, row) => (
                                <Space wrap>
                                    <Button
                                        size="small"
                                        onClick={() =>
                                            resource === "tools"
                                                ? showEquipment(row)
                                                : setDetail({
                                                      path: `equipment/${resource}?id=${row.id}`,
                                                      title: titles[resource],
                                                      kind: "record",
                                                      page: 1,
                                                  })
                                        }
                                    >
                                        详情
                                    </Button>
                                    {[
                                        "tools",
                                        "types",
                                        "groups",
                                        "chambers",
                                        "reasons",
                                        "resources",
                                        "reticles",
                                        "policies",
                                    ].includes(resource) &&
                                        can(
                                            permission[resource] || "manage",
                                        ) && (
                                            <Button
                                                size="small"
                                                onClick={() => edit(row)}
                                            >
                                                修改
                                            </Button>
                                        )}
                                    {resource === "tools" && can("state") && (
                                        <Button
                                            size="small"
                                            onClick={() =>
                                                action(
                                                    row,
                                                    "state",
                                                    "变更设备状态",
                                                    [
                                                        choice(
                                                            "state",
                                                            "设备运行条件",
                                                            [
                                                                "Idle",
                                                                "Down",
                                                                "Maintenance",
                                                                "Offline",
                                                            ],
                                                        ),
                                                        reason,
                                                        ref(
                                                            "reasonCodeId",
                                                            "停机原因（可后补）",
                                                            "reasons",
                                                            false,
                                                        ),
                                                        ref(
                                                            "shiftId",
                                                            "班次（可选）",
                                                            "shifts",
                                                            false,
                                                        ),
                                                    ],
                                                )
                                            }
                                        >
                                            状态
                                        </Button>
                                    )}
                                    {resource === "states" &&
                                        row.state === "Down" &&
                                        !row.downtimeId &&
                                        can("downtime") && (
                                            <Button
                                                size="small"
                                                onClick={() =>
                                                    open({
                                                        title: "补登记历史停机",
                                                        path: "equipment/downtimes",
                                                        equipmentVersionId:
                                                            row.equipmentId,
                                                        fixed: {
                                                            equipmentStateId:
                                                                row.id,
                                                        },
                                                        fields: [
                                                            ref(
                                                                "reasonCodeId",
                                                                "停机原因（可后补）",
                                                                "reasons",
                                                                false,
                                                            ),
                                                            ref(
                                                                "shiftId",
                                                                "班次（可选）",
                                                                "shifts",
                                                                false,
                                                            ),
                                                            field(
                                                                "notes",
                                                                "登记说明",
                                                                "text",
                                                                false,
                                                            ),
                                                        ],
                                                    })
                                                }
                                            >
                                                补登记
                                            </Button>
                                        )}
                                    {resource === "downtimes" &&
                                        can("downtime") && (
                                            <Button
                                                size="small"
                                                onClick={() =>
                                                    action(
                                                        row,
                                                        "classify",
                                                        "停机归因",
                                                        [
                                                            ref(
                                                                "reasonCodeId",
                                                                "停机原因",
                                                                "reasons",
                                                            ),
                                                            ref(
                                                                "shiftId",
                                                                "班次（可选）",
                                                                "shifts",
                                                                false,
                                                            ),
                                                            field(
                                                                "notes",
                                                                "归因说明",
                                                            ),
                                                        ],
                                                    )
                                                }
                                            >
                                                归因
                                            </Button>
                                        )}
                                    {resource === "maintenance-plans" &&
                                        can("maintenance") && (
                                            <>
                                                <Button
                                                    size="small"
                                                    onClick={() =>
                                                        action(
                                                            row,
                                                            "defer",
                                                            "延期维护计划",
                                                            [
                                                                field(
                                                                    "until",
                                                                    "新到期时间",
                                                                    "date",
                                                                ),
                                                                reason,
                                                            ],
                                                        )
                                                    }
                                                >
                                                    延期
                                                </Button>
                                                <Button
                                                    size="small"
                                                    onClick={() =>
                                                        action(
                                                            row,
                                                            "toggle",
                                                            "启用或停用计划",
                                                            [
                                                                field(
                                                                    "isActive",
                                                                    "启用",
                                                                    "bool",
                                                                    false,
                                                                ),
                                                                reason,
                                                            ],
                                                        )
                                                    }
                                                >
                                                    启停
                                                </Button>
                                            </>
                                        )}
                                    {resource === "maintenance" &&
                                        [
                                            "Planned",
                                            "InProgress",
                                            "Completed",
                                        ].includes(row.status) &&
                                        can(
                                            row.status === "Completed"
                                                ? "release"
                                                : "maintenance",
                                        ) && (
                                            <Button
                                                size="small"
                                                onClick={() =>
                                                    action(
                                                        row,
                                                        (
                                                            {
                                                                Planned:
                                                                    "start",
                                                                InProgress:
                                                                    "complete",
                                                                Completed:
                                                                    "release",
                                                            } as Record<
                                                                string,
                                                                string
                                                            >
                                                        )[row.status],
                                                        (
                                                            {
                                                                Planned:
                                                                    "开始维护",
                                                                InProgress:
                                                                    "完成维护",
                                                                Completed:
                                                                    "确认复机",
                                                            } as Record<
                                                                string,
                                                                string
                                                            >
                                                        )[row.status],
                                                    )
                                                }
                                            >
                                                {
                                                    (
                                                        {
                                                            Planned: "开始",
                                                            InProgress: "完成",
                                                            Completed: "复机",
                                                        } as Record<
                                                            string,
                                                            string
                                                        >
                                                    )[row.status]
                                                }
                                            </Button>
                                        )}
                                    {resource === "maintenance" &&
                                        row.status === "Planned" &&
                                        can("maintenance") && (
                                            <>
                                                <Button
                                                    size="small"
                                                    onClick={() =>
                                                        action(
                                                            row,
                                                            "defer",
                                                            "延期工单",
                                                            [
                                                                field(
                                                                    "until",
                                                                    "新计划时间",
                                                                    "date",
                                                                ),
                                                                reason,
                                                            ],
                                                        )
                                                    }
                                                >
                                                    延期
                                                </Button>
                                                <Button
                                                    size="small"
                                                    onClick={() =>
                                                        action(
                                                            row,
                                                            "cancel",
                                                            "取消工单",
                                                        )
                                                    }
                                                >
                                                    取消
                                                </Button>
                                            </>
                                        )}
                                    {resource.endsWith("-qualifications") &&
                                        row.qualificationStatus ===
                                            "Qualified" &&
                                        can("qualify") && (
                                            <Button
                                                size="small"
                                                onClick={() =>
                                                    action(
                                                        row,
                                                        "revoke",
                                                        "撤销资格",
                                                    )
                                                }
                                            >
                                                撤销
                                            </Button>
                                        )}
                                    {[
                                        "reticle-mounts",
                                        "material-mounts",
                                    ].includes(resource) &&
                                        !row.unloadedAt &&
                                        !row.unmountedAt &&
                                        can(permission[resource]) && (
                                            <Button
                                                size="small"
                                                onClick={() =>
                                                    action(
                                                        row,
                                                        "unload",
                                                        "卸下资源",
                                                    )
                                                }
                                            >
                                                卸下
                                            </Button>
                                        )}
                                    {["reticles", "material-mounts"].includes(
                                        resource,
                                    ) && (
                                        <Button
                                            size="small"
                                            onClick={() =>
                                                setDetail({
                                                    path: `equipment/${resource}/${row.id}/impact`,
                                                    title: "实际加工影响",
                                                    kind: "impact",
                                                    page: 1,
                                                })
                                            }
                                        >
                                            影响查询
                                        </Button>
                                    )}
                                </Space>
                            ),
                        },
                    ]}
                />
            )}
            <Modal
                okText="保存"
                title={dialog?.title}
                open={!!dialog}
                onCancel={() => setDialog(undefined)}
                onOk={submit}
                confirmLoading={busy}
                destroyOnHidden
                width={640}
            >
                <Form form={form} layout="vertical">
                    {dialog?.fields.map((f) => (
                        <Form.Item
                            key={f.key}
                            name={f.key}
                            label={
                                f.label +
                                (f.kind === "date" ? "（本地时间）" : "")
                            }
                            valuePropName={
                                f.kind === "bool" ? "checked" : "value"
                            }
                            rules={
                                f.required
                                    ? [
                                          {
                                              required: true,
                                              message: "请填写此项",
                                          },
                                      ]
                                    : []
                            }
                        >
                            {f.reference ? (
                                <Select
                                    showSearch
                                    optionFilterProp="label"
                                    mode={
                                        f.kind === "multi"
                                            ? "multiple"
                                            : undefined
                                    }
                                    allowClear
                                    options={values(f.reference)}
                                />
                            ) : f.options ? (
                                <Select
                                    options={f.options.map((value) => ({
                                        value,
                                        label: names[value] || value,
                                    }))}
                                />
                            ) : f.kind === "bool" ? (
                                <Switch />
                            ) : f.kind === "number" ? (
                                <InputNumber
                                    min={1}
                                    precision={0}
                                    style={{ width: "100%" }}
                                />
                            ) : (
                                <Input
                                    step={f.kind === "date" ? 0.001 : undefined}
                                    type={
                                        f.kind === "date"
                                            ? "datetime-local"
                                            : f.kind === "time"
                                              ? "time"
                                              : "text"
                                    }
                                    maxLength={
                                        f.key === "reason" || f.key === "notes"
                                            ? 500
                                            : 200
                                    }
                                />
                            )}
                        </Form.Item>
                    ))}
                </Form>
            </Modal>
            <Drawer
                title={detail?.title}
                open={!!detail}
                onClose={() => setDetail(undefined)}
                size="large"
            >
                {details.error && (
                    <Alert
                        type="error"
                        title={(details.error as Error).message}
                    />
                )}
                {detail?.kind === "record" && (
                    <Descriptions
                        column={1}
                        bordered
                        items={Object.entries(details.data?.items?.[0] || {})
                            .filter(([key]) => labels[key])
                            .map(([key, value]) => ({
                                key,
                                label: labels[key],
                                children: label(key, value),
                            }))}
                    />
                )}
                {detail?.kind === "equipment" && details.data && (
                    <>
                        <Descriptions
                            column={1}
                            bordered
                            items={Object.entries(details.data.equipment || {})
                                .filter(([key]) => labels[key])
                                .map(([key, value]) => ({
                                    key,
                                    label: labels[key],
                                    children: label(key, value),
                                }))}
                        />
                        <Typography.Title level={5}>当前加工</Typography.Title>
                        <Typography.Paragraph>
                            {details.data.currentRun
                                ? `${details.data.currentRun.lotNumber} · ${fmt(details.data.currentRun.trackInAt)}`
                                : "无运行中的加工"}
                        </Typography.Paragraph>
                        <Typography.Title level={5}>
                            工艺能力（具体启动条件由执行模块校验）
                        </Typography.Title>
                        <Table
                            size="small"
                            rowKey="id"
                            dataSource={details.data.capabilities}
                            columns={[
                                { title: "步骤", dataIndex: "name" },
                                {
                                    title: "配方版本",
                                    dataIndex: "recipeVersionId",
                                    render: (v) => label("recipeVersionId", v),
                                },
                                {
                                    title: "生效",
                                    dataIndex: "effectiveFrom",
                                    render: fmt,
                                },
                                {
                                    title: "截止",
                                    dataIndex: "effectiveTo",
                                    render: fmt,
                                },
                            ]}
                        />
                        <Typography.Title level={5}>状态履历</Typography.Title>
                        <Table
                            size="small"
                            rowKey="id"
                            dataSource={details.data.states}
                            columns={[
                                {
                                    title: "状态",
                                    dataIndex: "state",
                                    render: (v) => label("state", v),
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
                {detail?.kind === "impact" && (
                    <>
                        <Alert
                            type="info"
                            title="按实际 Run 与参与片关联查询；仅覆盖已记录的资源使用事实。"
                        />
                        <Table
                            rowKey={(x: any) => x.run.id}
                            loading={details.isLoading}
                            dataSource={details.data?.items}
                            pagination={{
                                current: detail.page,
                                total: details.data?.total,
                                pageSize: 20,
                                showSizeChanger: false,
                                onChange: (value) =>
                                    setDetail({ ...detail, page: value }),
                            }}
                            columns={[
                                {
                                    title: "Lot",
                                    render: (_, x: any) => x.lot.lotNumber,
                                },
                                {
                                    title: "开始",
                                    render: (_, x: any) => fmt(x.run.trackInAt),
                                },
                                {
                                    title: "结果",
                                    render: (_, x: any) =>
                                        runNames[x.run.status] || x.run.status,
                                },
                                {
                                    title: "实际参与晶圆",
                                    render: (_, x: any) =>
                                        x.wafers
                                            .map((w: any) => w.waferCode)
                                            .join("、"),
                                },
                            ]}
                        />
                    </>
                )}
            </Drawer>
        </>
    );
}
