import { useRef, useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    App,
    Button,
    Descriptions,
    Drawer,
    Form,
    Input,
    Modal,
    Select,
    Space,
    Table,
    Typography,
} from "antd";
import { Link, useSearchParams } from "react-router-dom";
import { api, type Me } from "./api";
import { fmt } from "./shared";

type Page<T> = { items: T[]; total: number };
type Reference = {
    id: string;
    code: string;
    name: string;
    materialCode?: string;
};
type OriginOption = {
    id: string;
    lotNumber: string;
    supplierLotNo: string;
    materialCode: string;
};
type OriginLot = OriginOption & {
    sourceCode: string;
    sourceName: string;
    materialName: string;
    receivedAt: string;
    expiryDate?: string;
    selectable: boolean;
};
type OriginWafer = {
    id: string;
    waferCode: string;
    supplierWaferCode?: string;
    lifecycleStatus: string;
    currentLotId?: string;
    currentLotNumber?: string;
};

const read = "materials.wafer-sources.read";
const manage = "materials.wafer-sources.manage";
const required = [{ required: true, message: "请填写此项" }];
const pageSize = 20;

export const originLotLink = (id: string) => `/materials?lot=${id}`;

function useOptions<T>(resource: string, search: string) {
    return useQuery({
        queryKey: ["materials", resource, search],
        queryFn: () =>
            api<Page<T>>(
                `materials/${resource}?pageSize=30&search=${encodeURIComponent(search)}`,
            ),
    });
}

export function WaferOriginSelect({
    id,
    value,
    onChange,
}: {
    id?: string;
    value?: string;
    onChange?: (value?: string) => void;
}) {
    const [search, setSearch] = useState("");
    const query = useOptions<OriginOption>("wafer-origin-options", search);
    return (
        <Select
            id={id}
            aria-label="硅片来料批"
            allowClear
            showSearch={{ filterOption: false, onSearch: setSearch }}
            value={value}
            onChange={onChange}
            loading={query.isFetching}
            placeholder="按来料批号、供应方批号或物料搜索"
            notFoundContent={query.isError ? "来料批加载失败" : undefined}
            options={query.data?.items.map((row) => ({
                value: row.id,
                label: `${row.lotNumber} · ${row.materialCode} · ${row.supplierLotNo}`,
            }))}
        />
    );
}

function ReferenceSelect({
    resource,
    id,
    value,
    onChange,
}: {
    id?: string;
    resource: "wafer-materials" | "wafer-sources";
    value?: string;
    onChange?: (value: string) => void;
}) {
    const [search, setSearch] = useState("");
    const query = useOptions<Reference>(resource, search);
    return (
        <Select
            id={id}
            showSearch={{ filterOption: false, onSearch: setSearch }}
            value={value}
            onChange={onChange}
            loading={query.isFetching}
            placeholder="按代码或名称搜索"
            notFoundContent={query.isError ? "选项加载失败" : undefined}
            options={query.data?.items.map((row) => ({
                value: row.id,
                label: [row.code, row.name, row.materialCode]
                    .filter(Boolean)
                    .join(" · "),
            }))}
        />
    );
}

const registrations = {
    "wafer-materials": "登记物料",
    "wafer-sources": "登记来源",
    "wafer-lots": "登记来料批",
};
type Registration = keyof typeof registrations;

function RegistrationDialog({
    kind,
    onClose,
    onSaved,
}: {
    kind: Registration;
    onClose: () => void;
    onSaved: (id: string) => void;
}) {
    const [form] = Form.useForm();
    const [busy, setBusy] = useState(false);
    const [error, setError] = useState("");
    const command = useRef<{ payload: string; key: string }>(undefined);
    const submit = async (values: Record<string, string>) => {
        const body = { ...values, expiryDate: values.expiryDate || undefined };
        const payload = JSON.stringify(body);
        if (command.current?.payload !== payload)
            command.current = { payload, key: crypto.randomUUID() };
        setBusy(true);
        setError("");
        try {
            const row = await api<{ id: string }>(
                `materials/${kind}`,
                "POST",
                body,
                null,
                command.current.key,
            );
            onSaved(row.id);
        } catch (e) {
            setError(e instanceof Error ? e.message : "登记失败");
        } finally {
            setBusy(false);
        }
    };
    return (
        <Modal
            open
            title={registrations[kind]}
            onCancel={onClose}
            onOk={() => form.submit()}
            confirmLoading={busy}
            cancelButtonProps={{ disabled: busy }}
            closable={!busy}
            mask={{ closable: !busy }}
        >
            {error && <Alert type="error" title={error} showIcon />}
            <Form form={form} layout="vertical" onFinish={submit}>
                {kind === "wafer-sources" && (
                    <Form.Item
                        name="materialId"
                        label="硅片物料"
                        rules={required}
                    >
                        <ReferenceSelect resource="wafer-materials" />
                    </Form.Item>
                )}
                {kind === "wafer-lots" ? (
                    <>
                        <Form.Item
                            name="sourceId"
                            label="来源"
                            rules={required}
                        >
                            <ReferenceSelect resource="wafer-sources" />
                        </Form.Item>
                        <Form.Item
                            name="lotNumber"
                            label="来料批号"
                            rules={required}
                        >
                            <Input maxLength={64} />
                        </Form.Item>
                        <Form.Item
                            name="supplierLotNo"
                            label="供应方批号"
                            rules={required}
                        >
                            <Input maxLength={128} />
                        </Form.Item>
                        <Form.Item name="expiryDate" label="有效截止日（可选）">
                            <Input type="date" />
                        </Form.Item>
                    </>
                ) : (
                    <>
                        <Form.Item name="code" label="代码" rules={required}>
                            <Input
                                maxLength={
                                    kind === "wafer-materials" ? 64 : 128
                                }
                            />
                        </Form.Item>
                        <Form.Item name="name" label="名称" rules={required}>
                            <Input maxLength={200} />
                        </Form.Item>
                    </>
                )}
            </Form>
        </Modal>
    );
}

function OriginDetails({
    id,
    showWafers,
    onClose,
}: {
    id: string;
    showWafers: boolean;
    onClose: () => void;
}) {
    const [page, setPage] = useState(1);
    const path = `materials/wafer-lots/${id}`;
    const detail = useQuery({
        queryKey: ["materials", "lot", id],
        queryFn: () => api<OriginLot>(path),
    });
    const lot = detail.data;
    const wafers = useQuery({
        queryKey: ["materials", "wafer-trace", id, page],
        enabled: showWafers && !!lot,
        queryFn: () =>
            api<Page<OriginWafer>>(
                `${path}/wafers?page=${page}&pageSize=${pageSize}`,
            ),
    });

    return (
        <Drawer
            title={lot?.lotNumber ?? "来料批详情"}
            open
            size="large"
            onClose={onClose}
            loading={detail.isLoading}
        >
            {detail.error && (
                <Alert type="error" title={detail.error.message} />
            )}
            {lot && (
                <>
                    <Descriptions column={1} size="small">
                        <Descriptions.Item label="硅片物料">
                            {lot.materialCode} · {lot.materialName}
                        </Descriptions.Item>
                        <Descriptions.Item label="来源">
                            {lot.sourceCode} · {lot.sourceName}
                        </Descriptions.Item>
                        <Descriptions.Item label="供应方批号">
                            {lot.supplierLotNo}
                        </Descriptions.Item>
                        <Descriptions.Item label="登记时间">
                            {fmt(lot.receivedAt)}
                        </Descriptions.Item>
                        <Descriptions.Item label="有效截止日">
                            {lot.expiryDate ?? "未登记"}
                        </Descriptions.Item>
                        <Descriptions.Item label="建批引用">
                            {lot.selectable ? "可引用" : "不可引用"}
                        </Descriptions.Item>
                    </Descriptions>
                    {showWafers && (
                        <>
                            <Typography.Title level={4}>
                                来源晶圆
                            </Typography.Title>
                            {wafers.error && (
                                <Alert
                                    type="error"
                                    title={wafers.error.message}
                                />
                            )}
                            <Table<OriginWafer>
                                rowKey="id"
                                size="small"
                                loading={wafers.isFetching}
                                dataSource={wafers.data?.items}
                                pagination={{
                                    current: page,
                                    pageSize,
                                    total: wafers.data?.total,
                                    onChange: setPage,
                                    showSizeChanger: false,
                                }}
                                columns={[
                                    {
                                        title: "晶圆编号",
                                        render: (_, row) => (
                                            <Link
                                                to={`/production?view=wafers&detail=${row.id}`}
                                            >
                                                {row.waferCode}
                                            </Link>
                                        ),
                                    },
                                    {
                                        title: "供应方片号",
                                        dataIndex: "supplierWaferCode",
                                    },
                                    {
                                        title: "当前生产批",
                                        render: (_, row) =>
                                            row.currentLotId ? (
                                                <Link
                                                    to={`/production?view=lots&detail=${row.currentLotId}`}
                                                >
                                                    {row.currentLotNumber}
                                                </Link>
                                            ) : (
                                                "—"
                                            ),
                                    },
                                ]}
                            />
                        </>
                    )}
                </>
            )}
        </Drawer>
    );
}

export function Materials({ me }: { me: Me }) {
    const permissions = me.permissions ?? [];
    const { message } = App.useApp();
    const qc = useQueryClient();
    const [params, setParams] = useSearchParams();
    const detail = params.get("lot");
    const [search, setSearch] = useState("");
    const [page, setPage] = useState(1);
    const [dialog, setDialog] = useState<Registration>();
    const query = useQuery({
        queryKey: ["materials", "lot-list", search, page],
        enabled: permissions.includes(read),
        queryFn: () =>
            api<Page<OriginLot>>(
                `materials/wafer-lots?page=${page}&pageSize=${pageSize}&search=${encodeURIComponent(search)}`,
            ),
    });

    return (
        <>
            <Typography.Title level={2}>硅片来料</Typography.Title>
            <Space wrap className="table-toolbar">
                <Input.Search
                    aria-label="检索来料批"
                    allowClear
                    placeholder="检索批号、物料或来源"
                    onSearch={(value) => {
                        setPage(1);
                        setSearch(value);
                    }}
                />
                {permissions.includes(manage) &&
                    (Object.keys(registrations) as Registration[]).map(
                        (kind) => (
                            <Button
                                key={kind}
                                type={
                                    kind === "wafer-lots"
                                        ? "primary"
                                        : "default"
                                }
                                onClick={() => setDialog(kind)}
                            >
                                {registrations[kind]}
                            </Button>
                        ),
                    )}
            </Space>
            {query.error && <Alert type="error" title={query.error.message} />}
            <Table<OriginLot>
                rowKey="id"
                loading={query.isFetching}
                dataSource={query.data?.items}
                pagination={{
                    current: page,
                    pageSize,
                    total: query.data?.total,
                    onChange: setPage,
                    showSizeChanger: false,
                }}
                columns={[
                    {
                        title: "来料批号",
                        render: (_, row) => (
                            <Link to={originLotLink(row.id)}>
                                {row.lotNumber}
                            </Link>
                        ),
                    },
                    { title: "物料代码", dataIndex: "materialCode" },
                    { title: "来源代码", dataIndex: "sourceCode" },
                    { title: "供应方批号", dataIndex: "supplierLotNo" },
                    {
                        title: "登记时间",
                        render: (_, row) => fmt(row.receivedAt),
                    },
                    {
                        title: "有效截止日",
                        render: (_, row) => row.expiryDate ?? "—",
                    },
                ]}
            />
            {detail && (
                <OriginDetails
                    key={detail}
                    id={detail}
                    showWafers={permissions.includes("production.wafers.read")}
                    onClose={() => setParams({}, { replace: true })}
                />
            )}
            {dialog && (
                <RegistrationDialog
                    kind={dialog}
                    onClose={() => setDialog(undefined)}
                    onSaved={(id) => {
                        void qc.invalidateQueries({ queryKey: ["materials"] });
                        void message.success("登记成功");
                        if (dialog === "wafer-lots") setParams({ lot: id });
                        setDialog(undefined);
                    }}
                />
            )}
        </>
    );
}
