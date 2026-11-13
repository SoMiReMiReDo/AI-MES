import { SafetyCertificateOutlined } from "@ant-design/icons";
import { Alert, Button, Form, Input } from "antd";
import { useState } from "react";
import { api, refreshCsrf, signIn } from "./api";
import { Brand } from "./shared";
import { ThemeToggle } from "./theme";

export function Login({
    onDone,
    expired,
}: {
    onDone: () => Promise<void>;
    expired: boolean;
}) {
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");

    return (
        <div className="login-scene">
            <ThemeToggle className="theme-corner" />
            <section className="login-intro">
                <div className="eyebrow">CONNECTED MANUFACTURING</div>
                <h1>
                    每一次操作，
                    <br />
                    从可信身份开始。
                </h1>
                <p>
                    连接人员、场所与制造流程。
                    <br />
                    让工厂基础信息清晰，让每项授权有据可循。
                </p>
                <div className="wafer">
                    <div />
                    <div />
                    <div />
                </div>
                <small>MESagent / FAB OPERATIONS</small>
            </section>
            <div className="login-card">
                <Brand />
                <h2>登录工作台</h2>
                <p>使用管理员分配的个人账号</p>
                {(error || expired) && (
                    <Alert
                        type="warning"
                        showIcon
                        message={error || "会话已失效，请重新登录"}
                    />
                )}
                <Form
                    layout="vertical"
                    onFinish={async (v) => {
                        setBusy(true);
                        setError("");
                        try {
                            await signIn(v);
                            await onDone();
                        } catch (e) {
                            setError((e as Error).message);
                            await refreshCsrf();
                        } finally {
                            setBusy(false);
                        }
                    }}
                >
                    <Form.Item
                        name="username"
                        label="用户名"
                        rules={[{ required: true, message: "请输入用户名" }]}
                    >
                        <Input autoComplete="username" size="large" />
                    </Form.Item>
                    <Form.Item
                        name="password"
                        label="密码"
                        rules={[{ required: true, message: "请输入密码" }]}
                    >
                        <Input.Password
                            autoComplete="current-password"
                            size="large"
                        />
                    </Form.Item>
                    <Button
                        block
                        type="primary"
                        htmlType="submit"
                        size="large"
                        loading={busy}
                    >
                        登录
                    </Button>
                </Form>
                <div className="login-note">
                    <SafetyCertificateOutlined />{" "}
                    账号由管理员创建。忘记密码请联系管理员。
                </div>
            </div>
        </div>
    );
}

export function PasswordForm({ done }: { done: () => Promise<void> }) {
    const [busy, setBusy] = useState(false),
        [error, setError] = useState("");

    return (
        <Form
            layout="vertical"
            onFinish={async (v) => {
                setBusy(true);
                setError("");
                try {
                    await api("account/change-password", "POST", {
                        currentPassword: v.currentPassword,
                        newPassword: v.newPassword,
                    });
                    await done();
                } catch (e) {
                    setError((e as Error).message);
                } finally {
                    setBusy(false);
                }
            }}
        >
            {error && <Alert type="error" message={error} />}
            <Form.Item
                name="currentPassword"
                label="当前密码"
                rules={[{ required: true }]}
            >
                <Input.Password autoComplete="current-password" />
            </Form.Item>
            <Form.Item
                name="newPassword"
                label="新密码（12～128 字符）"
                rules={[{ required: true, min: 12, max: 128 }]}
            >
                <Input.Password autoComplete="new-password" />
            </Form.Item>
            <Form.Item
                name="confirm"
                label="确认新密码"
                dependencies={["newPassword"]}
                rules={[
                    { required: true },
                    ({ getFieldValue }) => ({
                        validator(_, v) {
                            return v === getFieldValue("newPassword")
                                ? Promise.resolve()
                                : Promise.reject(new Error("两次密码不一致"));
                        },
                    }),
                ]}
            >
                <Input.Password autoComplete="new-password" />
            </Form.Item>
            <Button type="primary" htmlType="submit" loading={busy}>
                更新密码并重新登录
            </Button>
        </Form>
    );
}
