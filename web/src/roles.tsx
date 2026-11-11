import { PlusOutlined } from "@ant-design/icons";
import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    App as AntApp,
    Button,
    Checkbox,
    Form,
    Input,
    Modal,
    Space,
    Table,
    Tag,
} from "antd";
import { useState } from "react";
import { api, type Me, type Page, type Row } from "./api";
import { qc } from "./queryClient";
import { Heading, permissionLabels, roleLabels } from "./shared";

export function Roles({ me }: { me: Me }) {
    const { message, modal } = AntApp.useApp();
    const [edit, setEdit] = useState<Row | null | undefined>();
    const [form] = Form.useForm();
    const [error, setError] = useState("");
    const query = useQuery({
        queryKey: ["roles"],
        queryFn: () => api<Page>("security/roles?pageSize=100"),
    });
    const permissions = useQuery({
        queryKey: ["permissions"],
        queryFn: () => api<any[]>("security/permissions"),
    });
    const open = async (row?: Row) => {
        form.resetFields();
        setError("");
        const d = row ? await api<Row>("security/roles/" + row.id) : null;
        setEdit(d);
        form.setFieldsValue(d || { permissionCodes: [] });
    };
    const state = (row: Row) => {
        let reason = "";
        modal.confirm({
            title: row.isActive ? "停用角色" : "启用角色",
            content: (
                <Input.TextArea
                    placeholder="原因（变更将撤销成员会话）"
                    onChange={(e) => (reason = e.target.value)}
                />
            ),
            onOk: async () => {
                try {
                    await api(
                        `security/roles/${row.id}/${row.isActive ? "deactivate" : "activate"}`,
                        "POST",
                        { reason },
                        row.etag,
                    );
                    qc.invalidateQueries({ queryKey: ["roles"] });
                } catch (e) {
                    message.error((e as Error).message);
                    throw e;
                }
            },
        });
    };
    const rename = (row: Row) => {
        let name = row.name || "";
        modal.confirm({
            title: "角色更名",
            content: (
                <Input
                    defaultValue={name}
                    onChange={(e) => (name = e.target.value)}
                />
            ),
            onOk: async () => {
                try {
                    await api(
                        `security/roles/${row.id}`,
                        "PATCH",
                        { name },
                        row.etag,
                    );
                    qc.invalidateQueries({ queryKey: ["roles"] });
                    qc.invalidateQueries({ queryKey: ["options"] });
                    message.success("已更新角色名称");
                } catch (e) {
                    message.error((e as Error).message);
                    throw e;
                }
            },
        });
    };

    return (
        <>
            <Heading
                eyebrow="ACCESS / ROLES"
                title="角色与权限"
                description="用明确的职责集合控制操作；系统管理权限不等于生产操作授权。"
            />
            <section className="panel">
                <div className="toolbar">
                    <span>{query.data?.total || 0} 个角色</span>
                    <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={() => open()}
                    >
                        新增角色
                    </Button>
                </div>
                {query.error && (
                    <Alert type="error" message={query.error.message} />
                )}
                <Table
                    rowKey="id"
                    loading={query.isLoading}
                    dataSource={query.data?.items}
                    columns={[
                        {
                            title: "角色",
                            dataIndex: "name",
                            render: (v, r) => (
                                <Space>
                                    {roleLabels[v] || v}
                                    {r.isSystem && <Tag>系统保护</Tag>}
                                </Space>
                            ),
                        },
                        { title: "成员数", dataIndex: "memberCount" },
                        {
                            title: "权限数",
                            dataIndex: "permissionCodes",
                            render: (v) => v.length,
                        },
                        {
                            title: "状态",
                            dataIndex: "isActive",
                            render: (v) => (
                                <Tag color={v ? "success" : "default"}>
                                    {v ? "启用" : "停用"}
                                </Tag>
                            ),
                        },
                        {
                            title: "操作",
                            render: (_, r) => (
                                <Space>
                                    {!r.isSystem && (
                                        <Button
                                            type="link"
                                            onClick={() => rename(r)}
                                        >
                                            更名
                                        </Button>
                                    )}
                                    <Button type="link" onClick={() => open(r)}>
                                        {r.isSystem ? "查看" : "配置权限"}
                                    </Button>
                                    <Button
                                        type="link"
                                        disabled={r.isSystem}
                                        onClick={() => state(r)}
                                    >
                                        {r.isActive ? "停用" : "启用"}
                                    </Button>
                                </Space>
                            ),
                        },
                    ]}
                />
            </section>
            <Modal
                title={edit ? "角色权限" : "新增角色"}
                open={edit !== undefined}
                onCancel={() => setEdit(undefined)}
                onOk={() => form.submit()}
                okButtonProps={{ disabled: edit?.isSystem }}
                width={680}
                destroyOnHidden
            >
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={async (v) => {
                        try {
                            setError("");
                            if (edit) {
                                await api(
                                    `security/roles/${edit.id}/permissions`,
                                    "PUT",
                                    {
                                        permissionCodes: v.permissionCodes,
                                        reason: v.reason,
                                    },
                                    edit.etag,
                                );
                            } else
                                await api("security/roles", "POST", {
                                    name: v.name,
                                    permissionCodes: v.permissionCodes,
                                });
                            setEdit(undefined);
                            qc.invalidateQueries({ queryKey: ["roles"] });
                            qc.invalidateQueries({ queryKey: ["options"] });
                            message.success("角色已保存");
                        } catch (e) {
                            setError((e as Error).message);
                        }
                    }}
                >
                    {error && <Alert type="error" message={error} />}
                    <Form.Item
                        name="name"
                        label="角色名称"
                        rules={[{ required: true }]}
                    >
                        <Input disabled={!!edit} />
                    </Form.Item>
                    <Form.Item name="permissionCodes" label="动作权限">
                        <Checkbox.Group disabled={edit?.isSystem}>
                            <div className="permission-grid">
                                {permissions.data
                                    ?.filter(
                                        (x) =>
                                            edit?.isSystem ||
                                            !x.code.startsWith("security."),
                                    )
                                    .map((x) => (
                                        <Checkbox key={x.code} value={x.code}>
                                            {permissionLabels[x.code] || x.code}
                                        </Checkbox>
                                    ))}
                            </div>
                        </Checkbox.Group>
                    </Form.Item>
                    {edit && !edit.isSystem && (
                        <Form.Item
                            name="reason"
                            label="变更原因"
                            rules={[{ required: true }]}
                        >
                            <Input.TextArea />
                        </Form.Item>
                    )}
                    <Alert
                        type="info"
                        message="个人直接授权暂不开放；查询和修改权限分别授予。"
                    />
                </Form>
            </Modal>
        </>
    );
}
