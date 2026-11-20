import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    App,
    Badge,
    Button,
    Card,
    Descriptions,
    Form,
    Input,
    Radio,
    Spin,
    Tabs,
} from "antd";
import { api, ApiError } from "./api";
import type { components } from "./generated/api";
import { Brand, fmt, Heading } from "./shared";
import { useSiteAppearance, type SiteAppearance } from "./siteAppearance";
import "./websiteSettings.css";

type Schemas = components["schemas"];

function SaveError({ error, reload }: { error: Error; reload: () => void }) {
    const stale = error instanceof ApiError && error.status === 412;
    return (
        <Alert
            type={stale ? "warning" : "error"}
            showIcon
            title={
                stale
                    ? "设置已被其他操作更新，请重新加载后再保存"
                    : error.message
            }
            action={
                stale && (
                    <Button size="small" onClick={reload}>
                        重新加载
                    </Button>
                )
            }
        />
    );
}

function AppearanceForm({
    settings,
    editable,
    reload,
}: {
    settings: Schemas["WebsiteAppearanceSettings"];
    editable: boolean;
    reload: () => void;
}) {
    const [form] = Form.useForm<SiteAppearance>();
    const watched = Form.useWatch([], form);
    const appearance = settings.appearance as SiteAppearance;
    const draft = { ...appearance, ...watched };
    const { updateAppearance } = useSiteAppearance();
    const { message } = App.useApp();
    const query = useQueryClient();
    const save = useMutation({
        mutationFn: (value: SiteAppearance) =>
            api<Schemas["WebsiteAppearanceSettings"]>(
                "settings/appearance",
                "PUT",
                value,
                settings.etag,
            ),
        onSuccess: (result) => {
            updateAppearance(result.appearance as SiteAppearance);
            query.setQueryData(["settings", "appearance"], result);
            message.success("站点外观已保存");
        },
    });

    return (
        <div className="settings-appearance">
            <Form
                form={form}
                layout="vertical"
                initialValues={appearance}
                onFinish={(values) => save.mutate(values)}
                disabled={!editable || save.isPending}
            >
                <Form.Item
                    label="站点名称"
                    name="name"
                    rules={[
                        {
                            required: true,
                            whitespace: true,
                            message: "请输入站点名称",
                        },
                    ]}
                >
                    <Input maxLength={60} showCount />
                </Form.Item>
                <Form.Item label="副标题" name="subtitle">
                    <Input maxLength={100} showCount />
                </Form.Item>
                <Form.Item label="默认主题" name="defaultTheme">
                    <Radio.Group
                        options={[
                            { label: "浅色", value: "light" },
                            { label: "深色", value: "dark" },
                        ]}
                    />
                </Form.Item>
                {save.error && (
                    <Form.Item>
                        <SaveError error={save.error} reload={reload} />
                    </Form.Item>
                )}
                {editable && (
                    <Button
                        type="primary"
                        htmlType="submit"
                        loading={save.isPending}
                    >
                        保存站点外观
                    </Button>
                )}
            </Form>
            <div
                className="settings-preview"
                data-preview-theme={draft.defaultTheme}
            >
                <span>品牌预览</span>
                <Brand preview={draft} />
            </div>
        </div>
    );
}

function Appearance({ editable }: { editable: boolean }) {
    const settings = useQuery({
        queryKey: ["settings", "appearance"],
        queryFn: () =>
            api<Schemas["WebsiteAppearanceSettings"]>("settings/appearance"),
        staleTime: Infinity,
    });

    return (
        <Card size="small" loading={settings.isPending}>
            {settings.error && (
                <Alert type="error" showIcon title={settings.error.message} />
            )}
            {!editable && (
                <Alert
                    type="info"
                    showIcon
                    title="修改站点外观需要全厂区授权及写权限"
                />
            )}
            {settings.data && (
                <AppearanceForm
                    key={settings.data.etag}
                    settings={settings.data}
                    editable={editable}
                    reload={() => void settings.refetch()}
                />
            )}
        </Card>
    );
}

function ScheduleForm({
    fabId,
    settings,
    editable,
    reload,
}: {
    fabId: string;
    settings: Schemas["FabScheduleSettings"];
    editable: boolean;
    reload: () => void;
}) {
    const { message } = App.useApp();
    const query = useQueryClient();
    const save = useMutation({
        mutationFn: (input: Schemas["FabScheduleSettingsInput"]) =>
            api<Schemas["FabScheduleSettings"]>(
                "settings/schedule",
                "PUT",
                input,
                settings.etag,
            ),
        onSuccess: (result) => {
            query.setQueryData(["settings", "schedule", fabId], result);
            void query.invalidateQueries({
                queryKey: ["workbench", "workbench/options?"],
            });
            message.success("排程设置已保存");
        },
    });

    return (
        <Form
            layout="vertical"
            initialValues={{ defaultView: settings.defaultView }}
            onFinish={(value: Schemas["FabScheduleSettingsInput"]) =>
                save.mutate(value)
            }
            disabled={!editable || save.isPending}
        >
            <Form.Item label="默认排程视图" name="defaultView">
                <Radio.Group
                    options={[
                        { label: "周视图", value: "week" },
                        { label: "日视图", value: "day" },
                    ]}
                />
            </Form.Item>
            {save.error && (
                <Form.Item>
                    <SaveError error={save.error} reload={reload} />
                </Form.Item>
            )}
            {editable && (
                <Button
                    type="primary"
                    htmlType="submit"
                    loading={save.isPending}
                >
                    保存排程设置
                </Button>
            )}
        </Form>
    );
}

function Schedule({ info }: { info: Schemas["WebsiteSystemInfo"] }) {
    const settings = useQuery({
        queryKey: ["settings", "schedule", info.fabId],
        queryFn: () => api<Schemas["FabScheduleSettings"]>("settings/schedule"),
        staleTime: Infinity,
    });

    return (
        <Card
            size="small"
            title={`厂区 · ${info.fabCode}`}
            loading={settings.isPending}
        >
            {settings.error && (
                <Alert type="error" showIcon title={settings.error.message} />
            )}
            {settings.data && (
                <ScheduleForm
                    key={settings.data.etag}
                    fabId={info.fabId}
                    settings={settings.data}
                    editable={info.canManageSchedule}
                    reload={() => void settings.refetch()}
                />
            )}
        </Card>
    );
}

function SystemInformation({ info }: { info: Schemas["WebsiteSystemInfo"] }) {
    const readiness = useQuery({
        queryKey: ["settings", "readiness", info.fabId],
        queryFn: async () => {
            const response = await fetch("/health/ready", {
                credentials: "omit",
                cache: "no-store",
                signal: AbortSignal.timeout(5000),
            }).catch(() => null);
            return {
                ready: response?.ok === true,
                checkedAt: new Date().toISOString(),
            };
        },
    });

    return (
        <Card
            size="small"
            extra={
                <Button
                    size="small"
                    loading={readiness.isFetching}
                    onClick={() => void readiness.refetch()}
                >
                    重新检查
                </Button>
            }
        >
            <Descriptions
                column={{ xs: 1, sm: 1, md: 2 }}
                items={[
                    {
                        key: "fab",
                        label: "当前厂区",
                        children: `${info.fabCode} · ${info.fabName}`,
                    },
                    {
                        key: "timezone",
                        label: "厂区时区",
                        children: info.timezone,
                    },
                    {
                        key: "idle",
                        label: "会话空闲时限",
                        children: `${info.idleMinutes} 分钟`,
                    },
                    {
                        key: "absolute",
                        label: "会话最长时限",
                        children: `${info.absoluteHours} 小时`,
                    },
                    {
                        key: "ready",
                        label: "服务就绪状态",
                        children: readiness.data ? (
                            <Badge
                                status={
                                    readiness.data.ready ? "success" : "error"
                                }
                                text={
                                    readiness.data.ready ? "就绪" : "暂不可用"
                                }
                            />
                        ) : (
                            "检查中"
                        ),
                    },
                    {
                        key: "checked",
                        label: "检查时间",
                        children: fmt(readiness.data?.checkedAt),
                    },
                ]}
            />
        </Card>
    );
}

export function WebsiteSettings() {
    const system = useQuery({
        queryKey: ["settings", "system"],
        queryFn: () => api<Schemas["WebsiteSystemInfo"]>("settings/system"),
    });

    return (
        <section className="website-settings">
            <Heading
                eyebrow="SYSTEM SETTINGS"
                title="系统设置"
                description="设置站点外观和厂区的排程默认视图，查看当前运行信息。"
            />
            {system.isPending && <Spin />}
            {system.error && (
                <Alert type="error" showIcon title={system.error.message} />
            )}
            {system.data && (
                <Tabs
                    items={[
                        {
                            key: "appearance",
                            label: "站点外观",
                            children: (
                                <Appearance
                                    editable={system.data.canManageAppearance}
                                />
                            ),
                        },
                        {
                            key: "schedule",
                            label: "排程",
                            children: <Schedule info={system.data} />,
                        },
                        {
                            key: "system",
                            label: "系统信息",
                            children: <SystemInformation info={system.data} />,
                        },
                    ]}
                />
            )}
        </section>
    );
}
