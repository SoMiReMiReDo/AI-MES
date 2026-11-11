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
    Select,
    Space,
    Table,
    Tag,
} from "antd";
import { useState } from "react";
import { api, type Me, type Page, type Row } from "./api";
import { qc } from "./queryClient";
import { Heading, RemoteSelect, fmt } from "./shared";

export function Users({ me }: { me: Me }) {
    const { message, modal } = AntApp.useApp();
    const [page, setPage] = useState(1),
        [key, setKey] = useState("");
    const [edit, setEdit] = useState<Row | null | undefined>();
    const [form] = Form.useForm();
    const [error, setError] = useState("");
    const [busy, setBusy] = useState(false);
    const [sessions, setSessions] = useState<any[]>();
    const query = useQuery({
        queryKey: ["users", page, key],
        queryFn: () =>
            api<Page>(
                `security/users?page=${page}&keyword=${encodeURIComponent(key)}`,
            ),
    });
    const refresh = () => qc.invalidateQueries({ queryKey: ["users"] });
    const open = async (row?: Row) => {
        form.resetFields();
        setError("");
        const d = row ? await api<Row>("security/users/" + row.id) : null;
        setEdit(d);
        form.setFieldsValue(
            d || { fabIds: me.fab ? [me.fab.id] : [], roleIds: [] },
        );
    };
    const perform = (row: Row, action: string, title: string) => {
        let reason = "",
            temporaryPassword = "";
        modal.confirm({
            title,
            content: (
                <Space direction="vertical" style={{ width: "100%" }}>
                    <p>
                        目标账号：{row.username}。授权或密码变更会使旧会话失效。
                    </p>
                    {action === "reset-password" && (
                        <Input.Password
                            placeholder="新临时密码（12～128 字符）"
                            onChange={(e) =>
                                (temporaryPassword = e.target.value)
                            }
                        />
                    )}
                    <Input.TextArea
                        placeholder="操作原因"
                        onChange={(e) => (reason = e.target.value)}
                    />
                </Space>
            ),
            onOk: async () => {
                try {
                    await api(
                        `security/users/${row.id}/${action}`,
                        "POST",
                        {
                            reason,
                            ...(action === "reset-password"
                                ? { temporaryPassword }
                                : {}),
                            ...(action === "lock"
                                ? {
                                      until: new Date(
                                          Date.now() + 15 * 60000,
                                      ).toISOString(),
                                  }
                                : {}),
                        },
                        row.etag,
                    );
                    message.success("操作成功");
                    refresh();
                } catch (e) {
                    message.error((e as Error).message);
                    throw e;
                }
            },
        });
    };
    const access = (row: Row) => {
        let roleIds = row.roleIds as string[],
            fabIds = row.fabIds as string[],
            reason = "";
        modal.confirm({
            title: "配置账号权限与范围",
            width: 560,
            content: (
                <Space direction="vertical" style={{ width: "100%" }}>
                    <p>{row.username} · 角色与厂区范围同时生效</p>
                    <RemoteSelect
                        resource="security/roles"
                        mode="multiple"
                        defaultValue={roleIds}
                        style={{ width: "100%" }}
                        onChange={(v: string[]) => (roleIds = v)}
                    />
                    <Checkbox
                        defaultChecked={fabIds.includes(me.fab?.id || "")}
                        onChange={(e) =>
                            (fabIds =
                                e.target.checked && me.fab ? [me.fab.id] : [])
                        }
                    >
                        {me.fab?.name}
                    </Checkbox>
                    <Input.TextArea
                        placeholder="授权变更原因"
                        onChange={(e) => (reason = e.target.value)}
                    />
                    <Alert
                        type="info"
                        message="提交后目标用户的现有会话将被撤销。"
                    />
                </Space>
            ),
            onOk: async () => {
                try {
                    await api(
                        `security/users/${row.id}/access`,
                        "PUT",
                        { roleIds, fabIds, reason },
                        row.etag,
                    );
                    refresh();
                    message.success("授权已更新");
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
                eyebrow="ACCESS / PEOPLE"
                title="账号与访问"
                description="管理员创建个人账号，按角色与厂区范围授予访问能力。"
            />
            <section className="panel">
                <div className="toolbar">
                    <Input.Search
                        placeholder="搜索用户名或姓名"
                        onSearch={(v) => {
                            setKey(v);
                            setPage(1);
                        }}
                        style={{ width: 280 }}
                    />
                    <Button
                        type="primary"
                        icon={<PlusOutlined />}
                        onClick={() => open()}
                    >
                        创建账号
                    </Button>
                </div>
                {query.error && (
                    <Alert type="error" message={query.error.message} />
                )}
                <Table
                    rowKey="id"
                    dataSource={query.data?.items}
                    loading={query.isLoading}
                    scroll={{ x: 1100 }}
                    pagination={{
                        current: page,
                        pageSize: 20,
                        total: query.data?.total,
                        onChange: setPage,
                        showSizeChanger: false,
                    }}
                    columns={[
                        {
                            title: "用户名",
                            dataIndex: "username",
                            render: (v) => <span className="code">{v}</span>,
                        },
                        { title: "显示名", dataIndex: "displayName" },
                        {
                            title: "状态",
                            render: (_, r) => (
                                <Space wrap>
                                    <Tag
                                        color={
                                            r.isActive ? "success" : "default"
                                        }
                                    >
                                        {r.isActive ? "启用" : "停用"}
                                    </Tag>
                                    {r.forcePasswordChange && (
                                        <Tag color="gold">需改密</Tag>
                                    )}
                                    {r.lockoutEnd &&
                                        new Date(r.lockoutEnd) > new Date() && (
                                            <Tag color="red">锁定</Tag>
                                        )}
                                </Space>
                            ),
                        },
                        {
                            title: "最后登录",
                            dataIndex: "lastLoginAt",
                            render: fmt,
                        },
                        {
                            title: "操作",
                            width: 370,
                            render: (_, r) => (
                                <Space wrap size={2}>
                                    <Button type="link" onClick={() => open(r)}>
                                        资料
                                    </Button>
                                    <Button
                                        type="link"
                                        onClick={() => access(r)}
                                    >
                                        授权
                                    </Button>
                                    <Button
                                        type="link"
                                        disabled={r.id === me.userId}
                                        onClick={() =>
                                            perform(
                                                r,
                                                "reset-password",
                                                "重置密码",
                                            )
                                        }
                                    >
                                        重置
                                    </Button>
                                    <Button
                                        type="link"
                                        onClick={() =>
                                            perform(
                                                r,
                                                r.isActive
                                                    ? "deactivate"
                                                    : "activate",
                                                r.isActive
                                                    ? "停用账号"
                                                    : "启用账号",
                                            )
                                        }
                                    >
                                        {r.isActive ? "停用" : "启用"}
                                    </Button>
                                    <Button
                                        type="link"
                                        onClick={() =>
                                            perform(
                                                r,
                                                r.lockoutEnd &&
                                                    new Date(r.lockoutEnd) >
                                                        new Date()
                                                    ? "unlock"
                                                    : "lock",
                                                "调整账号锁定",
                                            )
                                        }
                                    >
                                        {r.lockoutEnd &&
                                        new Date(r.lockoutEnd) > new Date()
                                            ? "解锁"
                                            : "锁定"}
                                    </Button>
                                    <Button
                                        type="link"
                                        onClick={async () =>
                                            setSessions(
                                                await api(
                                                    `security/users/${r.id}/sessions`,
                                                ),
                                            )
                                        }
                                    >
                                        会话
                                    </Button>
                                    <Button
                                        type="link"
                                        onClick={() =>
                                            perform(
                                                r,
                                                "sessions/revoke-all",
                                                "撤销全部会话",
                                            )
                                        }
                                    >
                                        撤销会话
                                    </Button>
                                </Space>
                            ),
                        },
                    ]}
                />
            </section>
            <Modal
                title={edit ? "编辑账号资料" : "创建个人账号"}
                open={edit !== undefined}
                onCancel={() => setEdit(undefined)}
                onOk={() => form.submit()}
                confirmLoading={busy}
                destroyOnHidden
            >
                <Form
                    form={form}
                    layout="vertical"
                    onFinish={async (v) => {
                        setBusy(true);
                        setError("");
                        try {
                            const body = edit
                                ? {
                                      displayName: v.displayName,
                                      workerId: v.workerId || null,
                                  }
                                : { ...v, workerId: v.workerId || null };
                            await api(
                                "security/users" + (edit ? "/" + edit.id : ""),
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
                    {error && <Alert type="error" message={error} />}
                    <Form.Item
                        name="username"
                        label="用户名"
                        rules={[{ required: true }]}
                    >
                        <Input disabled={!!edit} autoComplete="off" />
                    </Form.Item>
                    <Form.Item
                        name="displayName"
                        label="显示名"
                        rules={[{ required: true }]}
                    >
                        <Input />
                    </Form.Item>
                    <Form.Item name="workerId" label="关联员工（可选）">
                        <RemoteSelect resource="organization/workers" />
                    </Form.Item>
                    {!edit && (
                        <>
                            <Form.Item
                                name="temporaryPassword"
                                label="临时密码"
                                rules={[{ required: true, min: 12, max: 128 }]}
                            >
                                <Input.Password autoComplete="new-password" />
                            </Form.Item>
                            <Form.Item name="roleIds" label="角色">
                                <RemoteSelect
                                    resource="security/roles"
                                    mode="multiple"
                                />
                            </Form.Item>
                            <Form.Item name="fabIds" label="厂区范围">
                                <Select
                                    mode="multiple"
                                    options={
                                        me.fab
                                            ? [
                                                  {
                                                      value: me.fab.id,
                                                      label: me.fab.name,
                                                  },
                                              ]
                                            : []
                                    }
                                />
                            </Form.Item>
                            <Alert
                                type="info"
                                message="首次登录必须改密。请通过安全渠道交付临时密码。"
                            />
                        </>
                    )}
                </Form>
            </Modal>
            <Modal
                title="账号会话"
                open={!!sessions}
                footer={null}
                onCancel={() => setSessions(undefined)}
                width={850}
            >
                <Table
                    rowKey="id"
                    dataSource={sessions}
                    columns={[
                        { title: "创建", dataIndex: "createdAt", render: fmt },
                        {
                            title: "最后活动",
                            dataIndex: "lastActivityAt",
                            render: fmt,
                        },
                        {
                            title: "状态",
                            render: (_, r) =>
                                r.revokedAt
                                    ? "已撤销"
                                    : new Date(r.idleExpiresAt) < new Date()
                                      ? "已到期"
                                      : "有效",
                        },
                        { title: "终止原因", dataIndex: "revokeReason" },
                    ]}
                />
            </Modal>
        </>
    );
}
