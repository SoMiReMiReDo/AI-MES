import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    App as AntApp,
    Button,
    Form,
    Input,
    Space,
    Table,
    Tag,
} from "antd";
import { api, type Me } from "./api";
import { PasswordForm } from "./auth";
import { Heading, fmt } from "./shared";

export function Profile({
    me,
    reload,
    logout,
}: {
    me: Me;
    reload: () => Promise<void>;
    logout: () => Promise<void>;
}) {
    const { message } = AntApp.useApp();
    const sessions = useQuery({
        queryKey: ["sessions"],
        queryFn: () => api<any[]>("account/sessions"),
    });

    return (
        <>
            <Heading
                eyebrow="ACCOUNT / SETTINGS"
                title="个人设置"
                description="管理你的显示名、密码与登录会话。"
            />
            <div className="profile-grid">
                <section className="panel">
                    <h3>个人资料</h3>
                    <p className="muted">用户名：{me.username}</p>
                    <Form
                        layout="vertical"
                        initialValues={{ displayName: me.displayName }}
                        onFinish={async (v) => {
                            try {
                                await api(
                                    "account/profile",
                                    "PATCH",
                                    v,
                                    me.etag,
                                );
                                await reload();
                                message.success("已更新");
                            } catch (e) {
                                message.error((e as Error).message);
                            }
                        }}
                    >
                        <Form.Item
                            name="displayName"
                            label="显示名"
                            rules={[{ required: true }]}
                        >
                            <Input />
                        </Form.Item>
                        <Button htmlType="submit">保存资料</Button>
                    </Form>
                </section>
                <section className="panel">
                    <h3>修改密码</h3>
                    <PasswordForm done={logout} />
                </section>
            </div>
            <section className="panel">
                <div className="toolbar">
                    <h3>登录会话</h3>
                    <Button
                        danger
                        onClick={async () => {
                            await api(
                                "account/sessions/revoke-all",
                                "POST",
                                {},
                            );
                            await logout();
                        }}
                    >
                        退出全部会话
                    </Button>
                </div>
                {sessions.error && (
                    <Alert type="error" message={sessions.error.message} />
                )}
                <Table
                    rowKey="id"
                    dataSource={sessions.data}
                    columns={[
                        {
                            title: "创建时间",
                            dataIndex: "createdAt",
                            render: fmt,
                        },
                        {
                            title: "最后活动",
                            dataIndex: "lastActivityAt",
                            render: fmt,
                        },
                        {
                            title: "会话状态",
                            render: (_, r) => (
                                <Space>
                                    {r.current && <Tag color="cyan">当前</Tag>}
                                    {r.revokedAt
                                        ? "已撤销"
                                        : new Date(r.idleExpiresAt) < new Date()
                                          ? "已到期"
                                          : "有效"}
                                </Space>
                            ),
                        },
                        {
                            title: "操作",
                            render: (_, r) => (
                                <Button
                                    type="link"
                                    disabled={!!r.revokedAt}
                                    onClick={async () => {
                                        try {
                                            await api(
                                                `account/sessions/${r.id}/revoke`,
                                                "POST",
                                                {},
                                            );
                                            if (r.current) await logout();
                                            else sessions.refetch();
                                        } catch (e) {
                                            message.error((e as Error).message);
                                        }
                                    }}
                                >
                                    退出会话
                                </Button>
                            ),
                        },
                    ]}
                />
            </section>
        </>
    );
}
