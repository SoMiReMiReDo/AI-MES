import { useState } from "react";
import { useQuery, useQueryClient } from "@tanstack/react-query";
import {
    Alert,
    Button,
    Card,
    Col,
    Collapse,
    Descriptions,
    Drawer,
    Form,
    Input,
    Popconfirm,
    Row,
    Space,
    Table,
} from "antd";
import { Link } from "react-router-dom";
import { api } from "./api";
import { fmt } from "./shared";
import {
    IntegrationStatus,
    integrationError,
    integrationPageSize,
    useIntegrationCommand,
    type IntegrationPage,
    type IntegrationSchema,
} from "./integrationShared";

type Preview = IntegrationSchema["PreviewWorkOrderCsv"];
type Job = IntegrationSchema["WorkOrderImportView"];
type Summary = IntegrationSchema["WorkOrderImportSummary"];
type Draft = IntegrationSchema["WorkOrderImportRow"];
type Issue = IntegrationSchema["WorkOrderImportIssue"];
type CsvFormValues = { mapping?: Partial<Preview["mapping"]> };
const root = "integration/work-order-imports";

const fields = [
    ["orderNo", "工单号"],
    ["productCode", "产品代码"],
    ["revisionCode", "产品修订"],
    ["plannedQty", "计划数量"],
    ["priority", "优先级"],
    ["dueDate", "交期"],
    ["plannedStartAt", "计划开始"],
    ["plannedEndAt", "计划结束"],
    ["description", "描述"],
];
const fieldLabels = Object.fromEntries(fields);

function ImportForm({ onSaved }: { onSaved: (job: Job) => void }) {
    const [file, setFile] = useState<File>();
    const [reading, setReading] = useState(false);
    const command = useIntegrationCommand();
    const disabled = reading || command.busy;

    async function preview(values: CsvFormValues) {
        if (!file) {
            command.setError("请选择 CSV 文件");
            return;
        }
        if (file.size > 256 * 1024) {
            command.setError("CSV 文件超过 256 KiB");
            return;
        }

        command.setError("");
        setReading(true);
        let csv: string;
        try {
            csv = new TextDecoder("utf-8", { fatal: true }).decode(
                await file.arrayBuffer(),
            );
        } catch {
            command.setError("文件读取失败，请使用 UTF-8 CSV");
            return;
        } finally {
            setReading(false);
        }

        const mapping = Object.fromEntries(
            Object.entries(values.mapping ?? {})
                .map(([key, value]) => [key, value?.trim() ?? ""] as const)
                .filter(([, value]) => value),
        );
        const job = await command.send<Job>(root, {
            filename: file.name,
            csv,
            mapping,
        } satisfies Preview);
        if (job) onSaved(job);
    }

    return (
        <Card title="预检 CSV 工单">
            {command.error && <Alert type="error" title={command.error} />}
            <Form<CsvFormValues>
                layout="vertical"
                disabled={disabled}
                onFinish={preview}
            >
                <Form.Item
                    label="工单 CSV"
                    htmlFor="work-order-csv"
                    extra="UTF-8，最多 256 KiB、100 条记录。预检通过后可整批创建草稿。"
                >
                    <Input
                        id="work-order-csv"
                        type="file"
                        accept=".csv,text/csv"
                        onChange={(e) => setFile(e.target.files?.[0])}
                    />
                </Form.Item>
                <Collapse
                    items={[
                        {
                            key: "mapping",
                            label: "字段映射",
                            children: (
                                <>
                                    <p>
                                        填写 CSV
                                        中的实际表头，留空时按占位符中的字段名匹配。
                                    </p>
                                    <Row gutter={[16, 0]}>
                                        {fields.map(([field, label], index) => (
                                            <Col
                                                xs={24}
                                                sm={12}
                                                lg={8}
                                                key={field}
                                            >
                                                <Form.Item
                                                    name={["mapping", field]}
                                                    label={
                                                        index < 4
                                                            ? `${label}（必需列）`
                                                            : label
                                                    }
                                                >
                                                    <Input
                                                        maxLength={255}
                                                        placeholder={field}
                                                    />
                                                </Form.Item>
                                            </Col>
                                        ))}
                                    </Row>
                                </>
                            ),
                        },
                    ]}
                />
                <Button
                    style={{ marginTop: 16 }}
                    type="primary"
                    htmlType="submit"
                    loading={disabled}
                    disabled={!file}
                >
                    运行预检
                </Button>
            </Form>
        </Card>
    );
}

function JobRows({ job, canReadOrders }: { job: Job; canReadOrders: boolean }) {
    const imported = new Set(job.orders.map((order) => order.row));

    return (
        <>
            {job.errors.length > 0 && (
                <Table<Issue>
                    size="small"
                    rowKey={(error, index) =>
                        `${error.row}-${error.field}-${index}`
                    }
                    title={() => "预检或复核错误"}
                    dataSource={job.errors}
                    scroll={{ x: 480 }}
                    pagination={{ pageSize: 10, showSizeChanger: false }}
                    columns={[
                        { title: "记录", dataIndex: "row", width: 65 },
                        {
                            title: "字段",
                            render: (_, error) =>
                                fieldLabels[error.field] ?? error.field,
                            width: 100,
                        },
                        { title: "原因", dataIndex: "message" },
                    ]}
                />
            )}
            {job.rows.length > 0 && (
                <Table<Draft>
                    size="small"
                    rowKey={(row) => String(row.row)}
                    title={() => "工单记录"}
                    dataSource={job.rows}
                    scroll={{ x: 740 }}
                    pagination={{ pageSize: 10, showSizeChanger: false }}
                    expandable={{
                        expandedRowRender: (row) => (
                            <Descriptions
                                size="small"
                                column={1}
                                items={[
                                    {
                                        key: "start",
                                        label: "计划开始",
                                        children: fmt(row.plannedStartAt ?? ""),
                                    },
                                    {
                                        key: "end",
                                        label: "计划结束",
                                        children: fmt(row.plannedEndAt ?? ""),
                                    },
                                    {
                                        key: "description",
                                        label: "描述",
                                        children: row.description ?? "—",
                                    },
                                ]}
                            />
                        ),
                    }}
                    columns={[
                        { title: "记录", dataIndex: "row", width: 65 },
                        {
                            title: "工单号",
                            render: (_, row) =>
                                canReadOrders && imported.has(row.row) ? (
                                    <Link
                                        to={`/planning?orderNo=${encodeURIComponent(row.orderNo)}`}
                                    >
                                        {row.orderNo}
                                    </Link>
                                ) : (
                                    row.orderNo
                                ),
                        },
                        { title: "产品", dataIndex: "productCode" },
                        { title: "修订", dataIndex: "revisionCode" },
                        { title: "计划数量", dataIndex: "plannedQty" },
                        { title: "优先级", dataIndex: "priority" },
                        {
                            title: "交期",
                            render: (_, row) => row.dueDate ?? "—",
                        },
                    ]}
                />
            )}
        </>
    );
}

export function IntegrationImport({
    canReadOrders,
}: {
    canReadOrders: boolean;
}) {
    const client = useQueryClient();
    const [selected, setSelected] = useState<string>();
    const [page, setPage] = useState(1);
    const command = useIntegrationCommand();
    const history = useQuery({
        queryKey: ["integration", "imports", page],
        queryFn: () => api<IntegrationPage<Summary>>(`${root}?page=${page}`),
    });
    const detail = useQuery({
        queryKey: ["integration", "import-detail", selected],
        enabled: Boolean(selected),
        queryFn: () => api<Job>(`${root}/${selected}`),
    });
    const job = detail.data;

    function open(id: string) {
        command.setError("");
        setSelected(id);
    }

    function saved(row: Job) {
        client.setQueryData(["integration", "import-detail", row.id], row);
        setPage(1);
        open(row.id);
        void client.invalidateQueries({ queryKey: ["integration", "imports"] });
    }

    return (
        <Space orientation="vertical" size="middle" style={{ width: "100%" }}>
            <ImportForm onSaved={saved} />
            <Card title="我的导入任务">
                {history.isError && (
                    <Alert
                        type="error"
                        title={integrationError(history.error)}
                    />
                )}
                <Table<Summary>
                    rowKey="id"
                    loading={history.isFetching}
                    dataSource={history.data?.items}
                    scroll={{ x: 680 }}
                    pagination={{
                        current: page,
                        pageSize: integrationPageSize,
                        total: Number(history.data?.total ?? 0),
                        showSizeChanger: false,
                        onChange: setPage,
                    }}
                    columns={[
                        {
                            title: "文件名",
                            dataIndex: "filename",
                            ellipsis: true,
                        },
                        {
                            title: "状态",
                            render: (_, row) => (
                                <IntegrationStatus status={row.status} />
                            ),
                        },
                        { title: "记录数", dataIndex: "totalRows" },
                        { title: "已创建", dataIndex: "successfulRows" },
                        {
                            title: "创建时间",
                            render: (_, row) => fmt(row.createdAt),
                        },
                        {
                            title: "操作",
                            render: (_, row) => (
                                <Button
                                    type="link"
                                    onClick={() => open(row.id)}
                                >
                                    查看任务
                                </Button>
                            ),
                        },
                    ]}
                />
            </Card>
            <Drawer
                open={Boolean(selected)}
                title={job?.filename ?? "导入任务"}
                size="min(920px, 100vw)"
                onClose={() => setSelected(undefined)}
                closable={!command.busy}
                mask={{ closable: false }}
                loading={!job && detail.isFetching}
                extra={
                    <Space>
                        <Button
                            size="small"
                            onClick={() => {
                                command.setError("");
                                void detail.refetch();
                            }}
                            disabled={command.busy}
                        >
                            刷新
                        </Button>
                        {job?.status === "Ready" && (
                            <Popconfirm
                                title={`创建 ${job.totalRows} 张工单草稿？`}
                                okText="确认创建"
                                cancelText="取消"
                                onConfirm={async () => {
                                    const created = await command.send<Job>(
                                        `${root}/${job.id}/confirm`,
                                        {},
                                        job.etag,
                                    );
                                    if (created) saved(created);
                                }}
                            >
                                <Button
                                    type="primary"
                                    size="small"
                                    loading={command.busy}
                                >
                                    确认导入
                                </Button>
                            </Popconfirm>
                        )}
                    </Space>
                }
            >
                {command.error && <Alert type="error" title={command.error} />}
                {detail.isError && (
                    <Alert
                        type="error"
                        title={integrationError(detail.error)}
                    />
                )}
                {job && (
                    <>
                        <Descriptions
                            size="small"
                            column={{ xs: 1, sm: 2 }}
                            items={[
                                {
                                    key: "status",
                                    label: "状态",
                                    children: (
                                        <IntegrationStatus
                                            status={job.status}
                                        />
                                    ),
                                },
                                {
                                    key: "rows",
                                    label: "记录数",
                                    children: job.totalRows,
                                },
                                {
                                    key: "success",
                                    label: "已创建",
                                    children: job.successfulRows,
                                },
                                {
                                    key: "failed",
                                    label: "错误记录",
                                    children: job.failedRows,
                                },
                            ]}
                        />
                        <JobRows job={job} canReadOrders={canReadOrders} />
                    </>
                )}
            </Drawer>
        </Space>
    );
}
