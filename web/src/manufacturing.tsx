import { useEffect, useState } from "react";
import {
    Alert,
    App,
    Button,
    Drawer,
    Form,
    Input,
    InputNumber,
    Select,
    Space,
    Table,
    Tabs,
    Tag,
} from "antd";
import { api, type Me } from "./api";

import { EngineeringExtras } from "./engineeringExtras";

type Field = [string, string, string?];
type Resource = { key: string; title: string; fields: Field[] };
const resources: Resource[] = [
    {
        key: "recipe-applicabilities",
        title: "配方适用性",
        fields: [
            ["recipeVersionId", "配方版本", "recipe-versions"],
            ["routeStepId", "路线步骤", "route-steps"],
            ["equipmentId", "设备", "equipment-options"],
            ["effectiveFrom", "生效时间（ISO 8601）"],
            ["effectiveTo", "失效时间（可空）"],
        ],
    },
    {
        key: "products",
        title: "产品",
        fields: [
            ["code", "编码"],
            ["name", "名称"],
            ["unitOfMeasure", "单位"],
            ["category", "分类"],
            ["isActive", "启用", "boolean"],
        ],
    },
    {
        key: "operations",
        title: "工序",
        fields: [
            ["code", "编码"],
            ["name", "名称"],
            ["segmentType", "类型"],
            ["standardInstruction", "作业说明"],
            ["equipmentTypeId", "设备类型", "equipment-type-options"],
            ["isActive", "启用", "boolean"],
        ],
    },
    {
        key: "routes",
        title: "路线",
        fields: [
            ["code", "编码"],
            ["name", "名称"],
            ["isActive", "启用", "boolean"],
            ["productId", "产品", "products"],
        ],
    },
    {
        key: "route-versions",
        title: "路线版本",
        fields: [
            ["routeId", "路线", "routes"],
            ["version", "版本号"],
            ["changeReason", "变更原因"],
            ["effectiveFrom", "生效时间（ISO 8601）"],
            ["effectiveTo", "失效时间（可空）"],
        ],
    },
    {
        key: "route-steps",
        title: "路线步骤",
        fields: [
            ["routeVersionId", "路线版本", "route-versions"],
            ["operationId", "工序", "operations"],
            ["sequenceNo", "顺序", "number"],
            ["name", "步骤名称"],
            ["instruction", "作业说明"],
            ["equipmentTypeId", "设备类型", "equipment-type-options"],
            ["requiresConfirmation", "需要确认", "boolean"],
        ],
    },
    {
        key: "product-revisions",
        title: "产品修订",
        fields: [
            ["productId", "产品", "products"],
            ["revisionCode", "修订号"],
            ["routeVersionId", "路线版本", "route-versions"],
        ],
    },
    {
        key: "recipes",
        title: "Recipe",
        fields: [
            ["code", "编码"],
            ["name", "名称"],
            ["isActive", "启用", "boolean"],
        ],
    },
    {
        key: "recipe-versions",
        title: "Recipe 版本",
        fields: [
            ["recipeId", "Recipe", "recipes"],
            ["version", "版本号"],
            ["parameters", "参数（JSON）"],
        ],
    },
    {
        key: "number-sequences",
        title: "编号规则",
        fields: [
            ["name", "名称"],
            ["purpose", "用途"],
            ["prefix", "前缀"],
            ["suffix", "后缀"],
            ["padSize", "位宽", "number"],
            ["nextNumber", "下一序号", "number"],
        ],
    },
    {
        key: "checklist-items",
        title: "点检定义",
        fields: [
            ["routeStepId", "步骤", "route-steps"],
            ["label", "检查项"],
            ["isRequired", "必填", "boolean"],
            ["sortOrder", "排序", "number"],
        ],
    },
    {
        key: "output-definitions",
        title: "输出定义",
        fields: [
            ["routeStepId", "步骤", "route-steps"],
            ["key", "字段键"],
            ["label", "名称"],
            ["valueType", "类型"],
            ["unit", "单位"],
            ["options", "枚举选项（JSON）"],
            ["isRequired", "必填", "boolean"],
        ],
    },
];
const order: Resource = {
    key: "work-orders",
    title: "工单",
    fields: [
        ["orderNo", "工单号"],
        ["productId", "产品", "products"],
        ["productRevisionId", "产品修订", "product-revisions"],
        ["plannedQty", "计划片数", "number"],
        ["priority", "优先级", "number"],
        ["dueDate", "交期（YYYY-MM-DD）"],
        ["plannedStartAt", "计划开始（ISO 8601）"],
        ["plannedEndAt", "计划结束（ISO 8601）"],
        ["description", "说明"],
    ],
};

export function Manufacturing({
    me,
    planning = false,
}: {
    me: Me;
    planning?: boolean;
}) {
    const [resource, setResource] = useState(
        planning ? order : resources.find((x) => x.key === "products")!,
    );
    return (
        <section>
            <h2>{planning ? "工单与计划执行" : "产品、工艺与版本管理"}</h2>
            {planning ? (
                <Alert
                    type="info"
                    showIcon
                    message="生产进度按唯一晶圆投放与终点完成事实统计；完整质量处置待后续接通。"
                />
            ) : (
                <Tabs
                    activeKey={resource.key}
                    onChange={(key) =>
                        setResource(resources.find((x) => x.key === key)!)
                    }
                    items={resources.map((x) => ({
                        key: x.key,
                        label: x.title,
                    }))}
                />
            )}
            <ResourceTable
                key={resource.key}
                resource={resource}
                me={me}
                planning={planning}
            />
        </section>
    );
}
function ResourceTable({
    resource,
    me,
    planning,
}: {
    resource: Resource;
    me: Me;
    planning: boolean;
}) {
    const { message, modal } = App.useApp();
    const [rows, setRows] = useState<any[]>([]);
    const [total, setTotal] = useState(0);
    const [page, setPage] = useState(1);
    const [search, setSearch] = useState("");
    const [status, setStatus] = useState<string | undefined>();
    const [editing, setEditing] = useState<any>(null);
    const [choices, setChoices] = useState<Record<string, any[]>>({});
    const [detail, setDetail] = useState<any>(null);
    const [form] = Form.useForm();
    const root = (planning ? "planning/" : "engineering/") + resource.key;
    const can = (action: string) =>
        me.permissions?.includes(
            (planning ? "planning.work-orders." : "engineering.") + action,
        );
    const load = () =>
        api(
            `${root}?page=${page}&orderNo=${encodeURIComponent(search)}${status ? "&status=" + status : ""}`,
        )
            .then((x) => {
                setRows(x.items);
                setTotal(x.total);
            })
            .catch((e) => message.error(e.message));
    useEffect(() => {
        load();
    }, [page, search, status]);
    useEffect(() => {
        Promise.all(
            resource.fields
                .filter((x) => x[2] && !["number", "boolean"].includes(x[2]))
                .map(async (x) => [
                    x[0],
                    (await api(`engineering/${x[2]}?pageSize=100`)).items,
                ]),
        )
            .then((entries) => setChoices(Object.fromEntries(entries)))
            .catch(() => {});
    }, [resource.key]);
    async function edit(row: any) {
        try {
            const values = row.id ? await api(`${root}/${row.id}`) : {};
            setEditing(values);
            form.resetFields();
            form.setFieldsValue(values);
            const entries = await Promise.all(
                resource.fields
                    .filter(
                        (x) => x[2] && !["number", "boolean"].includes(x[2]),
                    )
                    .map(async (x) => [
                        x[0],
                        (await api(`engineering/${x[2]}?pageSize=100`)).items,
                    ]),
            );
            setChoices(Object.fromEntries(entries));
        } catch (e: any) {
            message.error(e.message);
        }
    }
    async function save() {
        try {
            const values = await form.validateFields();
            const body = Object.fromEntries(
                Object.entries(values).filter(
                    ([, v]) => v !== undefined && v !== "",
                ),
            );
            await api(
                root + (editing.id ? "/" + editing.id : ""),
                editing.id ? "PUT" : "POST",
                body,
                editing.etag,
            );
            setEditing(null);
            message.success("已保存");
            load();
        } catch (e: any) {
            if (e.message) message.error(e.message);
        }
    }
    function action(row: any, verb: string) {
        let reason = "";
        modal.confirm({
            title: `确认${{ release: "发布/下达", obsolete: "废止", cancel: "取消", close: "关闭" }[verb]}？`,
            content: (
                <Input.TextArea
                    placeholder="填写操作原因"
                    onChange={(e) => {
                        reason = e.target.value;
                    }}
                />
            ),
            onOk: async () => {
                try {
                    const latest = await api(`${root}/${row.id}`);
                    await api(
                        `${root}/${row.id}/${verb}`,
                        "POST",
                        { reason },
                        latest.etag,
                        crypto.randomUUID(),
                    );
                    message.success("操作完成");
                    load();
                } catch (e: any) {
                    message.error(e.message);
                    throw e;
                }
            },
        });
    }
    return (
        <>
            <Space style={{ marginBlock: 16 }}>
                {planning && (
                    <>
                        <Input.Search
                            placeholder="按工单号搜索"
                            allowClear
                            onSearch={(value) => {
                                setSearch(value);
                                setPage(1);
                            }}
                        />
                        <Select
                            placeholder="工单状态"
                            allowClear
                            style={{ width: 140 }}
                            value={status}
                            onChange={(value) => {
                                setStatus(value);
                                setPage(1);
                            }}
                            options={[
                                "Draft",
                                "Released",
                                "Cancelled",
                                "Closed",
                            ].map((value) => ({ value, label: value }))}
                        />
                    </>
                )}
                <Button onClick={load}>刷新</Button>
                {can("manage") && (
                    <Button type="primary" onClick={() => edit({})}>
                        新建{resource.title}
                    </Button>
                )}
            </Space>
            <Table
                rowKey="id"
                dataSource={rows}
                pagination={{
                    current: page,
                    pageSize: 20,
                    total,
                    onChange: setPage,
                }}
                columns={[
                    ...resource.fields.slice(0, 4).map(([key, label]) => ({
                        title: label,
                        dataIndex: key,
                        render: (v: any) => {
                            if (typeof v === "boolean") return v ? "是" : "否";
                            const related = choices[key]?.find(
                                (x) => x.id === v,
                            );
                            return related
                                ? related.name ||
                                      related.code ||
                                      related.revisionCode ||
                                      related.version
                                : v;
                        },
                    })),
                    {
                        title: "状态",
                        render: (_, row) => (
                            <Tag>
                                {row.lifecycleStatus || row.status || "有效"}
                            </Tag>
                        ),
                    },
                    {
                        title: "操作",
                        render: (_, row) => (
                            <Space wrap>
                                {!planning && (
                                    <EngineeringExtras
                                        row={row}
                                        resource={resource.key}
                                        me={me}
                                        reload={load}
                                    />
                                )}
                                {can("manage") &&
                                    ((!row.lifecycleStatus && !row.status) ||
                                        (row.lifecycleStatus || row.status) ===
                                            "Draft") && (
                                        <Button
                                            size="small"
                                            onClick={() => edit(row)}
                                        >
                                            编辑
                                        </Button>
                                    )}
                                {can("release") &&
                                    (row.lifecycleStatus || row.status) ===
                                        "Draft" && (
                                        <Button
                                            size="small"
                                            onClick={() =>
                                                action(row, "release")
                                            }
                                        >
                                            {planning ? "下达" : "发布"}
                                        </Button>
                                    )}
                                {!planning &&
                                    can("release") &&
                                    row.lifecycleStatus === "Released" && (
                                        <Button
                                            size="small"
                                            onClick={() =>
                                                action(row, "obsolete")
                                            }
                                        >
                                            废止
                                        </Button>
                                    )}
                                {planning &&
                                    ["Draft", "Released"].includes(
                                        row.status,
                                    ) &&
                                    can("cancel") && (
                                        <Button
                                            size="small"
                                            onClick={() =>
                                                action(row, "cancel")
                                            }
                                        >
                                            取消
                                        </Button>
                                    )}
                                {planning &&
                                    row.status === "Released" &&
                                    can("close") && (
                                        <Button
                                            size="small"
                                            onClick={() => action(row, "close")}
                                        >
                                            关闭
                                        </Button>
                                    )}
                                {planning && can("recalculate") && (
                                    <Button
                                        size="small"
                                        onClick={() =>
                                            action(row, "recalculate")
                                        }
                                    >
                                        重算进度缓存
                                    </Button>
                                )}
                                {planning && (
                                    <Button
                                        size="small"
                                        onClick={async () => {
                                            try {
                                                setDetail({
                                                    progress: await api(
                                                        `${root}/${row.id}/progress`,
                                                    ),
                                                    lots: await api(
                                                        `${root}/${row.id}/lots`,
                                                    ),
                                                    snapshots: await api(
                                                        `${root}/${row.id}/snapshots`,
                                                    ),
                                                });
                                            } catch (e: any) {
                                                message.error(e.message);
                                            }
                                        }}
                                    >
                                        执行概况
                                    </Button>
                                )}
                            </Space>
                        ),
                    },
                ]}
            />
            <Drawer
                title={`${editing?.id ? "编辑" : "新建"}${resource.title}`}
                open={editing !== null}
                onClose={() => setEditing(null)}
                extra={
                    <Button type="primary" onClick={save}>
                        保存
                    </Button>
                }
            >
                <Form form={form} layout="vertical">
                    {resource.fields.map(([key, label, type]) => (
                        <Form.Item key={key} name={key} label={label}>
                            {type === "number" ? (
                                <InputNumber style={{ width: "100%" }} />
                            ) : type === "boolean" ? (
                                <Select
                                    options={[
                                        { value: true, label: "是" },
                                        { value: false, label: "否" },
                                    ]}
                                />
                            ) : type ? (
                                <Select
                                    showSearch
                                    optionFilterProp="label"
                                    options={(choices[key] || []).map((x) => ({
                                        value: x.id,
                                        label:
                                            x.name ||
                                            x.code ||
                                            x.revisionCode ||
                                            x.version,
                                    }))}
                                />
                            ) : (
                                <Input />
                            )}
                        </Form.Item>
                    ))}
                </Form>
            </Drawer>
            <Drawer
                title="工单执行概况"
                open={detail !== null}
                onClose={() => setDetail(null)}
                width={720}
            >
                <Alert
                    title={
                        detail?.progress.coverage === "ProductionFacts"
                            ? "生产进度按唯一晶圆事实统计"
                            : "历史生产关联尚待核验"
                    }
                    description={`待集成：${detail?.progress.missingDependencies?.join("、") || "无"}`}
                    type="info"
                />
                <p>
                    计划：{detail?.progress.plannedQty}；投放：
                    {detail?.progress.releasedQty ?? "待核验"}； 在制：
                    {detail?.progress.inProcessQty ?? "待核验"}；阻塞：
                    {detail?.progress.blockedQty ?? "待核验"}； 完工：
                    {detail?.progress.completedQty ?? "待核验"}；报废：
                    {detail?.progress.scrappedQty ?? "待核验"}
                </p>
                <h3>关联 Lot</h3>
                <Table
                    rowKey="id"
                    dataSource={detail?.lots.items}
                    columns={[
                        { title: "批次号", dataIndex: "lotNumber" },
                        { title: "状态", dataIndex: "status" },
                    ]}
                />
                <h3>下达基线</h3>
                <Table
                    rowKey="id"
                    dataSource={detail?.snapshots}
                    columns={[
                        { title: "版本", dataIndex: "version" },
                        { title: "生效时间", dataIndex: "effectiveFrom" },
                    ]}
                />
            </Drawer>
        </>
    );
}
