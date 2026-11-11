import { PlusOutlined, ReloadOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    App as AntApp,
    Button,
    Empty,
    Form,
    Input,
    Modal,
    Select,
    Space,
    Table,
    Tabs,
    Tag,
    Tree,
} from "antd";
import { useState } from "react";
import { api, type Me, type Page, type Row } from "./api";
import { qc } from "./queryClient";
import { Heading, RemoteSelect, fmt, labels } from "./shared";

export function Organization({ me }: { me: Me }) {
    const [tab, setTab] = useState("fabs");
    const [areaId, setAreaId] = useState<string>();
    const areas = useQuery({
        queryKey: ["org-tree"],
        queryFn: () => api<Page>("organization/areas?pageSize=100"),
    });

    return (
        <>
            <Heading
                eyebrow="FOUNDATION / ORGANIZATION"
                title="组织结构"
                description="按厂区、区域维护制造场所；停用保留历史引用。"
            />
            <section className="panel organization-tree">
                <div>
                    <h3>厂区与区域</h3>
                    <Tree
                        defaultExpandAll
                        key={areas.data?.total}
                        selectedKeys={areaId ? [areaId] : [me.fab?.id || ""]}
                        treeData={[
                            {
                                title: me.fab?.name || "部署厂区",
                                key: me.fab?.id || "",
                                children: (areas.data?.items || []).map(
                                    (a) => ({
                                        title: `${a.code} · ${a.name}${a.isActive ? "" : "（停用）"}`,
                                        key: a.id,
                                    }),
                                ),
                            },
                        ]}
                        onSelect={(keys) => {
                            setAreaId(
                                keys[0] === me.fab?.id
                                    ? undefined
                                    : String(keys[0] || ""),
                            );
                            if (keys[0] !== me.fab?.id) setTab("lines");
                        }}
                    />
                </div>
                <p className="muted">
                    选择区域可筛选其产线与位置。厂区与区域停用后仍保留历史引用。
                </p>
            </section>
            <Tabs
                activeKey={tab}
                onChange={setTab}
                items={["fabs", "areas", "lines", "locations"].map((r) => ({
                    key: r,
                    label: labels[r],
                    children: (
                        <OrgTable
                            key={r + areaId}
                            resource={r}
                            me={me}
                            embedded
                            areaId={
                                ["lines", "locations"].includes(r)
                                    ? areaId
                                    : undefined
                            }
                        />
                    ),
                }))}
            />
        </>
    );
}

export function OrgTable({
    resource,
    me,
    embedded = false,
    areaId,
}: {
    resource: string;
    me: Me;
    embedded?: boolean;
    areaId?: string;
}) {
    const { message, modal } = AntApp.useApp();
    const [page, setPage] = useState(1);
    const [search, setSearch] = useState("");
    const [active, setActive] = useState<string>();
    const [edit, setEdit] = useState<Row | null | undefined>();
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const [form] = Form.useForm();
    const perm = ["fabs", "areas", "lines", "locations"].includes(resource)
        ? "structure"
        : resource;
    const manage = me.permissions?.includes(`org.${perm}.manage`);
    const query = useQuery({
        queryKey: ["org", resource, page, search, active, areaId],
        queryFn: () =>
            api<Page>(
                `organization/${resource}?page=${page}&keyword=${encodeURIComponent(search)}${active ? "&isActive=" + active : ""}${areaId ? "&areaId=" + areaId : ""}`,
            ),
    });
    const refresh = () => {
        qc.invalidateQueries({ queryKey: ["org"] });
        qc.invalidateQueries({ queryKey: ["org-tree"] });
        qc.invalidateQueries({ queryKey: ["options"] });
    };
    const open = async (row?: Row) => {
        setError("");
        form.resetFields();
        const detail = row
            ? await api<Row>(`organization/${resource}/${row.id}`)
            : null;
        setEdit(detail);
        form.setFieldsValue(
            detail || {
                fabId: me.fab?.id,
                locationType: "Other",
                deviceUuid: crypto.randomUUID(),
            },
        );
    };
    const changeState = async (row: Row) => {
        const impact = await api<any>(
            `organization/${resource}/${row.id}/deactivation-impact`,
        );
        let reason = "";
        modal.confirm({
            title: row.isActive ? "停用" + row.name : "启用" + row.name,
            content: (
                <>
                    <p>
                        {impact.dependencies.length
                            ? `发现 ${impact.dependencies.length} 类依赖，请先处理：${impact.dependencies.map((x: any) => x.type).join("、")}`
                            : "停用后历史仍可查询，新的业务绑定将被禁止。"}
                    </p>
                    <Input.TextArea
                        placeholder="请填写操作原因"
                        onChange={(e) => (reason = e.target.value)}
                    />
                </>
            ),
            onOk: async () => {
                try {
                    await api(
                        `organization/${resource}/${row.id}/${row.isActive ? "deactivate" : "activate"}`,
                        "POST",
                        { reason },
                        row.etag,
                    );
                    message.success("状态已更新");
                    refresh();
                } catch (e) {
                    message.error((e as Error).message);
                    throw e;
                }
            },
        });
    };
    const bind = async (row: Row) => {
        let workstationId: string | null = row.workstationId ?? null;
        let reason = "";
        modal.confirm({
            title: "绑定终端工位",
            content: (
                <Space direction="vertical" style={{ width: "100%" }}>
                    <RemoteSelect
                        resource="organization/workstations"
                        defaultValue={row.workstationId}
                        style={{ width: "100%" }}
                        onChange={(v: string) => (workstationId = v || null)}
                    />
                    <Input.TextArea
                        placeholder="变更原因（关联会话将失效）"
                        onChange={(e) => (reason = e.target.value)}
                    />
                </Space>
            ),
            onOk: async () => {
                try {
                    await api(
                        `organization/terminals/${row.id}/bind-workstation`,
                        "POST",
                        { workstationId, reason },
                        row.etag,
                    );
                    refresh();
                    message.success("已更新工位绑定");
                } catch (e) {
                    message.error((e as Error).message);
                    throw e;
                }
            },
        });
    };

    return (
        <>
            {!embedded && (
                <Heading
                    eyebrow="FOUNDATION / RESOURCES"
                    title={labels[resource] + "管理"}
                    description={
                        resource === "workers"
                            ? "员工是业务责任身份，可与一个登录账号关联。"
                            : "维护操作场所与终端；工位、终端和物理设备独立标识。"
                    }
                />
            )}
            <section className="panel">
                <div className="toolbar">
                    <Space>
                        <Input.Search
                            placeholder="搜索编码或名称"
                            allowClear
                            onSearch={(v) => {
                                setSearch(v);
                                setPage(1);
                            }}
                            style={{ width: 250 }}
                        />
                        <Select
                            placeholder="全部状态"
                            allowClear
                            options={[
                                { value: "true", label: "已启用" },
                                { value: "false", label: "已停用" },
                            ]}
                            value={active}
                            onChange={(v) => {
                                setActive(v);
                                setPage(1);
                            }}
                            style={{ width: 130 }}
                        />
                    </Space>
                    <Space>
                        <Button
                            icon={<ReloadOutlined />}
                            onClick={() => query.refetch()}
                        >
                            刷新
                        </Button>
                        {manage && resource !== "fabs" && (
                            <Button
                                type="primary"
                                icon={<PlusOutlined />}
                                onClick={() => open()}
                            >
                                新增{labels[resource]}
                            </Button>
                        )}
                    </Space>
                </div>
                {query.error && (
                    <Alert type="error" message={query.error.message} />
                )}
                <Table
                    rowKey="id"
                    loading={query.isLoading}
                    dataSource={query.data?.items}
                    pagination={{
                        current: page,
                        pageSize: 20,
                        total: query.data?.total,
                        onChange: setPage,
                        showSizeChanger: false,
                    }}
                    columns={[
                        {
                            title: "编码",
                            dataIndex: "code",
                            render: (v) => <span className="code">{v}</span>,
                        },
                        { title: "名称", dataIndex: "name" },
                        {
                            title: "状态",
                            dataIndex: "isActive",
                            render: (v) => (
                                <Tag color={v ? "success" : "default"}>
                                    {v ? "已启用" : "已停用"}
                                </Tag>
                            ),
                        },
                        {
                            title:
                                resource === "terminals" ? "最后联系" : "说明",
                            dataIndex:
                                resource === "terminals"
                                    ? "lastSeenAt"
                                    : "description",
                            render: (v) =>
                                resource === "terminals" ? fmt(v) : v || "—",
                        },
                        {
                            title: "操作",
                            width: 220,
                            render: (_, row) => (
                                <Space>
                                    <Button
                                        type="link"
                                        onClick={() => open(row)}
                                    >
                                        {manage ? "编辑" : "详情"}
                                    </Button>
                                    {manage && (
                                        <>
                                            <Button
                                                type="link"
                                                onClick={() => changeState(row)}
                                            >
                                                {row.isActive ? "停用" : "启用"}
                                            </Button>
                                            {resource === "terminals" && (
                                                <Button
                                                    type="link"
                                                    onClick={() => bind(row)}
                                                >
                                                    绑定
                                                </Button>
                                            )}
                                        </>
                                    )}
                                </Space>
                            ),
                        },
                    ]}
                    locale={{
                        emptyText: (
                            <Empty description="暂无记录，从新增开始建立基础信息" />
                        ),
                    }}
                />
            </section>
            <Modal
                title={`${edit ? "编辑" : "新增"}${labels[resource]}`}
                open={edit !== undefined}
                onCancel={() => setEdit(undefined)}
                onOk={() => form.submit()}
                confirmLoading={busy}
                okButtonProps={{ disabled: !manage }}
                destroyOnHidden
            >
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={async (values) => {
                        setBusy(true);
                        setError("");
                        try {
                            const body = edit
                                ? {
                                      name: values.name,
                                      description: values.description || null,
                                      ...(resource === "locations"
                                          ? {
                                                locationType:
                                                    values.locationType,
                                            }
                                          : {}),
                                      ...(resource === "workstations"
                                          ? { lineId: values.lineId || null }
                                          : {}),
                                  }
                                : values;
                            await api(
                                `organization/${resource}${edit ? "/" + edit.id : ""}`,
                                edit ? "PATCH" : "POST",
                                body,
                                edit?.etag,
                            );
                            setEdit(undefined);
                            refresh();
                            message.success("已保存");
                        } catch (e) {
                            setError((e as Error).message);
                        } finally {
                            setBusy(false);
                        }
                    }}
                >
                    {error && <Alert type="error" message={error} showIcon />}
                    <Form.Item
                        name="code"
                        label="编码"
                        rules={[{ required: true }]}
                    >
                        <Input disabled={!!edit} maxLength={64} />
                    </Form.Item>
                    <Form.Item
                        name="name"
                        label="名称"
                        rules={[{ required: true }]}
                    >
                        <Input maxLength={200} />
                    </Form.Item>
                    {!edit &&
                        ["areas", "workers", "terminals"].includes(
                            resource,
                        ) && (
                            <Form.Item
                                name="fabId"
                                label="厂区"
                                rules={[{ required: true }]}
                            >
                                <Select
                                    options={[
                                        {
                                            value: me.fab?.id,
                                            label: me.fab?.name,
                                        },
                                    ]}
                                />
                            </Form.Item>
                        )}
                    {!edit &&
                        ["lines", "locations", "workstations"].includes(
                            resource,
                        ) && (
                            <Form.Item
                                name="areaId"
                                label="所属区域"
                                rules={[{ required: true }]}
                            >
                                <RemoteSelect resource="organization/areas" />
                            </Form.Item>
                        )}
                    {resource === "workstations" && (
                        <Form.Item name="lineId" label="产线（可选，同区域）">
                            <RemoteSelect resource="organization/lines" />
                        </Form.Item>
                    )}
                    {resource === "locations" && (
                        <Form.Item
                            name="locationType"
                            label="位置类型"
                            rules={[{ required: true }]}
                        >
                            <Select
                                options={[
                                    "Storage",
                                    "Buffer",
                                    "Process",
                                    "Other",
                                ].map((v, i) => ({
                                    value: v,
                                    label: ["存储", "缓冲", "加工", "其它"][i],
                                }))}
                            />
                        </Form.Item>
                    )}
                    {resource === "terminals" && !edit && (
                        <>
                            <Form.Item
                                name="deviceUuid"
                                label="设备 UUID"
                                rules={[{ required: true }]}
                            >
                                <Input />
                            </Form.Item>
                            <Form.Item
                                name="workstationId"
                                label="工位（可选）"
                            >
                                <RemoteSelect resource="organization/workstations" />
                            </Form.Item>
                        </>
                    )}
                    {resource === "fabs" && edit && (
                        <Alert
                            message={`时区：${edit.timezone}（创建后固定）`}
                            type="info"
                        />
                    )}
                    <Form.Item name="description" label="说明">
                        <Input.TextArea rows={3} maxLength={2000} />
                    </Form.Item>
                </Form>
            </Modal>
        </>
    );
}
