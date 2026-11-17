import type { components } from "./generated/api";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    Button,
    Form,
    Input,
    InputNumber,
    Modal,
    Select,
    Space,
    Switch,
    Table,
    Tabs,
} from "antd";
import { api, type Me } from "./api";
import {
    choice,
    qualityOptions,
    templateSpecificationIds,
    qualityStatus,
    required,
    useQualityCommand,
    useQualityOptions,
    type QualityItem,
} from "./qualityShared";

export function QualityConfiguration({ me }: { me: Me }) {
    const [kind, setKind] = useState("reason-codes");
    const [page, setPage] = useState(1);
    const [editing, setEditing] = useState<QualityItem | null | undefined>();
    const [form] = Form.useForm();
    const valueType = Form.useWatch("valueType", form);
    const routeId = Form.useWatch("routeVersionId", form);
    const routes = useQualityOptions("routes");
    const specs = useQualityOptions("specifications?status=Released");
    const { send, busy } = useQualityCommand();
    const can = me.permissions?.includes("quality.configuration");
    const data = useQuery<{ items: QualityItem[]; total: number }>({
        queryKey: ["quality", kind, page],
        queryFn: () => api(`quality/${kind}?page=${page}`),
    });
    const open = (row: QualityItem | null, copy = false) => {
        form.resetFields();
        const values = row
            ? {
                  ...row,
                  specificationVersionIds:
                      kind === "templates"
                          ? templateSpecificationIds(row.parameters)
                          : undefined,
              }
            : {
                  version: 1,
                  valueType: "decimal",
                  samplesPerCheck: 1,
                  category: "Hold",
                  severity: "Major",
                  isActive: true,
                  isBlocking: false,
              };
        if (copy && row) values.version = row.version + 1;
        form.setFieldsValue(values);
        setEditing(copy ? null : row);
    };
    const save = async () => {
        try {
            const v = await form.validateFields();
            let body:
                | components["schemas"]["SaveQualityReason"]
                | components["schemas"]["SaveQualityIssueType"]
                | components["schemas"]["SaveQualitySpecification"]
                | components["schemas"]["SaveQualityTemplate"];
            if (kind === "reason-codes")
                body = {
                    code: v.code,
                    name: v.name,
                    category: v.category,
                    description: v.description,
                    isActive: v.isActive,
                };
            else if (kind === "issue-types")
                body = {
                    code: v.code,
                    name: v.name,
                    severity: v.severity,
                    isBlocking: v.isBlocking,
                    description: v.description,
                    isActive: v.isActive,
                };
            else if (kind === "specifications")
                body = {
                    parameterCode: v.parameterCode,
                    version: v.version,
                    valueType: v.valueType,
                    unit: v.unit || null,
                    specMin: ["decimal", "integer"].includes(v.valueType)
                        ? (v.specMin ?? null)
                        : null,
                    specMax: ["decimal", "integer"].includes(v.valueType)
                        ? (v.specMax ?? null)
                        : null,
                    expectedBoolean:
                        v.valueType === "boolean" ? v.expectedBoolean : null,
                    expectedText:
                        v.valueType === "string" ? v.expectedText : null,
                };
            else
                body = {
                    routeVersionId: v.routeVersionId,
                    name: v.name,
                    version: v.version,
                    samplesPerCheck: v.samplesPerCheck,
                    specificationVersionIds: v.specificationVersionIds,
                    routeStepId: v.routeStepId || null,
                };
            await send(
                `quality/${kind}${editing ? `/${editing.id}` : ""}`,
                body,
                editing?.etag,
                editing ? "PUT" : "POST",
            );
            setEditing(undefined);
        } catch {
            /* Keep inputs available after validation or server errors. */
        }
    };
    const columns: any[] = [
        {
            title: "代码 / 名称",
            render: (_: unknown, x: QualityItem) =>
                x.parameterCode || `${x.code || ""} ${x.name || ""}`,
        },
        {
            title: "版本 / 类别",
            render: (_: unknown, x: QualityItem) =>
                x.version ||
                (x.category && qualityStatus(x.category)) ||
                qualityStatus(x.severity),
        },
        {
            title: "状态",
            render: (_: unknown, x: QualityItem) =>
                x.lifecycleStatus
                    ? qualityStatus(x.lifecycleStatus)
                    : x.isActive
                      ? "启用"
                      : "停用",
        },
        {
            title: "规格 / 规则",
            render: (_: unknown, x: QualityItem) =>
                kind === "specifications"
                    ? x.valueType === "boolean"
                        ? `期望：${x.expectedBoolean ? "是" : "否"}`
                        : x.valueType === "string"
                          ? `期望：${x.expectedText}`
                          : `${x.specMin ?? "无下限"} ～ ${x.specMax ?? "无上限"} ${x.unit || ""}`
                    : kind === "templates"
                      ? `每对象 ${x.samplesPerCheck} 个样本`
                      : kind === "issue-types"
                        ? x.isBlocking
                            ? "阻塞并冻结 Lot"
                            : "普通异常"
                        : x.description || "—",
        },
        {
            title: "操作",
            render: (_: unknown, x: QualityItem) =>
                can && x.code !== "SYS-QUALITY-CHECK-FAILED" ? (
                    <Space>
                        {(!x.lifecycleStatus ||
                            x.lifecycleStatus === "Draft") && (
                            <Button size="small" onClick={() => open(x)}>
                                编辑
                            </Button>
                        )}
                        {x.lifecycleStatus === "Released" && (
                            <Button size="small" onClick={() => open(x, true)}>
                                复制新版本
                            </Button>
                        )}
                        {x.lifecycleStatus === "Draft" && (
                            <Button
                                size="small"
                                onClick={() =>
                                    Modal.confirm({
                                        title: "发布后内容冻结，确认发布？",
                                        onOk: () =>
                                            send(
                                                `quality/${kind}/${x.id}/release`,
                                                { reason: "审核发布质量配置" },
                                                x.etag,
                                            ),
                                    })
                                }
                            >
                                发布
                            </Button>
                        )}
                    </Space>
                ) : null,
        },
    ];
    const title = {
        "reason-codes": "原因码",
        "issue-types": "异常类型",
        specifications: "规格版本",
        templates: "检验模板",
    }[kind];
    return (
        <>
            <Tabs
                activeKey={kind}
                onChange={(k) => {
                    setKind(k);
                    setPage(1);
                }}
                items={[
                    { key: "reason-codes", label: "原因码" },
                    { key: "issue-types", label: "异常类型" },
                    { key: "specifications", label: "规格版本" },
                    { key: "templates", label: "检验模板" },
                ]}
            />
            <Space style={{ marginBottom: 16 }}>
                <Button onClick={() => data.refetch()}>刷新</Button>
                {can && (
                    <Button type="primary" onClick={() => open(null)}>
                        新增{title}
                    </Button>
                )}
            </Space>
            {data.error && (
                <Alert
                    type="error"
                    showIcon
                    title={(data.error as Error).message}
                />
            )}
            <Table
                rowKey="id"
                loading={data.isLoading}
                dataSource={data.data?.items}
                columns={columns}
                scroll={{ x: 850 }}
                pagination={{
                    current: page,
                    pageSize: 20,
                    total: data.data?.total,
                    onChange: setPage,
                    showSizeChanger: false,
                }}
            />
            <Modal
                title={`${editing ? "编辑" : "新增"}${title}`}
                open={editing !== undefined}
                onCancel={() => setEditing(undefined)}
                onOk={save}
                confirmLoading={busy}
                destroyOnHidden
            >
                <Form form={form} layout="vertical">
                    {kind === "specifications" ? (
                        <>
                            <Form.Item
                                name="parameterCode"
                                label="参数代码"
                                rules={required}
                            >
                                <Input disabled={!!editing} />
                            </Form.Item>
                            <Form.Item
                                name="version"
                                label="版本号"
                                rules={required}
                            >
                                <InputNumber
                                    min={1}
                                    precision={0}
                                    disabled={!!editing}
                                />
                            </Form.Item>
                            <Form.Item
                                name="valueType"
                                label="参数类型"
                                rules={required}
                            >
                                <Select
                                    options={[
                                        { value: "decimal", label: "数值" },
                                        { value: "integer", label: "整数" },
                                        { value: "boolean", label: "是 / 否" },
                                        { value: "string", label: "文本" },
                                    ]}
                                />
                            </Form.Item>
                            <Form.Item name="unit" label="单位">
                                <Input maxLength={32} />
                            </Form.Item>
                            {["decimal", "integer"].includes(valueType) && (
                                <Space>
                                    <Form.Item name="specMin" label="规格下限">
                                        <InputNumber />
                                    </Form.Item>
                                    <Form.Item name="specMax" label="规格上限">
                                        <InputNumber />
                                    </Form.Item>
                                </Space>
                            )}
                            {valueType === "boolean" && (
                                <Form.Item
                                    name="expectedBoolean"
                                    label="期望值"
                                    rules={required}
                                >
                                    <Select
                                        options={[
                                            { value: true, label: "是" },
                                            { value: false, label: "否" },
                                        ]}
                                    />
                                </Form.Item>
                            )}
                            {valueType === "string" && (
                                <Form.Item
                                    name="expectedText"
                                    label="期望文本"
                                    rules={required}
                                >
                                    <Input maxLength={2000} />
                                </Form.Item>
                            )}
                        </>
                    ) : (
                        <>
                            {kind !== "templates" && (
                                <Form.Item
                                    name="code"
                                    label="代码"
                                    rules={required}
                                >
                                    <Input disabled={!!editing} />
                                </Form.Item>
                            )}
                            <Form.Item
                                name="name"
                                label="名称"
                                rules={required}
                            >
                                <Input
                                    disabled={kind === "templates" && !!editing}
                                    maxLength={200}
                                />
                            </Form.Item>
                            {kind === "templates" ? (
                                <>
                                    <Form.Item
                                        name="routeVersionId"
                                        label="路线版本"
                                        rules={required}
                                    >
                                        <Select
                                            disabled={!!editing}
                                            options={qualityOptions(
                                                routes.data?.items,
                                            )}
                                            onChange={() =>
                                                form.setFieldValue(
                                                    "routeStepId",
                                                    undefined,
                                                )
                                            }
                                        />
                                    </Form.Item>
                                    <Form.Item
                                        name="routeStepId"
                                        label="适用步骤（可选）"
                                    >
                                        <Select
                                            allowClear
                                            options={qualityOptions(
                                                routes.data?.items.find(
                                                    (x) => x.id === routeId,
                                                )?.steps,
                                            )}
                                        />
                                    </Form.Item>
                                    <Form.Item
                                        name="version"
                                        label="模板版本"
                                        rules={required}
                                    >
                                        <InputNumber
                                            min={1}
                                            precision={0}
                                            disabled={!!editing}
                                        />
                                    </Form.Item>
                                    <Form.Item
                                        name="samplesPerCheck"
                                        label="每对象样本数"
                                        rules={required}
                                    >
                                        <InputNumber
                                            min={1}
                                            max={100}
                                            precision={0}
                                        />
                                    </Form.Item>
                                    <Form.Item
                                        name="specificationVersionIds"
                                        label="绑定已发布规格"
                                        rules={required}
                                    >
                                        <Select
                                            mode="multiple"
                                            options={qualityOptions(
                                                specs.data?.items,
                                            )}
                                        />
                                    </Form.Item>
                                </>
                            ) : (
                                <>
                                    {kind === "reason-codes" ? (
                                        <Form.Item
                                            name="category"
                                            label="类别"
                                            rules={required}
                                        >
                                            <Select
                                                disabled={!!editing}
                                                options={choice([
                                                    "Hold",
                                                    "Scrap",
                                                    "Disposition",
                                                ])}
                                            />
                                        </Form.Item>
                                    ) : (
                                        <>
                                            <Form.Item
                                                name="severity"
                                                label="严重度"
                                                rules={required}
                                            >
                                                <Select
                                                    options={choice([
                                                        "Minor",
                                                        "Major",
                                                        "Critical",
                                                    ])}
                                                />
                                            </Form.Item>
                                            <Form.Item
                                                name="isBlocking"
                                                label="登记时自动冻结 Lot"
                                                valuePropName="checked"
                                            >
                                                <Switch />
                                            </Form.Item>
                                        </>
                                    )}
                                    <Form.Item name="description" label="说明">
                                        <Input.TextArea maxLength={2000} />
                                    </Form.Item>
                                    <Form.Item
                                        name="isActive"
                                        label="启用"
                                        valuePropName="checked"
                                    >
                                        <Switch />
                                    </Form.Item>
                                </>
                            )}
                        </>
                    )}
                </Form>
            </Modal>
        </>
    );
}
