import { useState } from "react";
import { App, Button, Drawer, Input, Table } from "antd";
import { api, type Me } from "./api";

export function EngineeringExtras({
    row,
    resource,
    me,
    reload,
}: {
    row: any;
    resource: string;
    me: Me;
    reload: () => void;
}) {
    const { message, modal } = App.useApp();
    const [documents, setDocuments] = useState<any[] | null>(null);
    const can = (name: string) =>
        me.permissions?.includes("engineering." + name);
    const path = `engineering/${resource}/${row.id}`;
    return (
        <>
            {resource === "product-revisions" && (
                <Button
                    size="small"
                    onClick={async () => {
                        try {
                            const result = await api(`${path}/impact`);
                            modal.info({
                                title: "影响范围",
                                width: 720,
                                content: (
                                    <>
                                        <h4>关联工单</h4>
                                        <Table
                                            rowKey="id"
                                            dataSource={
                                                result.workOrders.items ||
                                                result.workOrders
                                            }
                                            columns={[
                                                {
                                                    title: "工单号",
                                                    dataIndex: "orderNo",
                                                },
                                                {
                                                    title: "状态",
                                                    dataIndex: "status",
                                                },
                                            ]}
                                        />
                                        <h4>关联批次</h4>
                                        <Table
                                            rowKey="id"
                                            dataSource={
                                                result.lots.items || result.lots
                                            }
                                            columns={[
                                                {
                                                    title: "批次号",
                                                    dataIndex: "lotNumber",
                                                },
                                            ]}
                                        />
                                    </>
                                ),
                            });
                        } catch (e: any) {
                            message.error(e.message);
                        }
                    }}
                >
                    影响范围
                </Button>
            )}
            {resource === "recipe-applicabilities" && (
                <Button
                    size="small"
                    onClick={async () => {
                        try {
                            const result = await api(
                                `engineering/recipe-candidates?routeStepId=${row.routeStepId}&equipmentId=${row.equipmentId}`,
                            );
                            modal.info({
                                title: "当前候选配方",
                                content: (
                                    <Table
                                        rowKey={(x) => x.applicability.id}
                                        dataSource={result}
                                        columns={[
                                            {
                                                title: "版本",
                                                render: (_, x) =>
                                                    x.version.version,
                                            },
                                            {
                                                title: "生效时间",
                                                render: (_, x) =>
                                                    x.applicability
                                                        .effectiveFrom,
                                            },
                                        ]}
                                    />
                                ),
                            });
                        } catch (e: any) {
                            message.error(e.message);
                        }
                    }}
                >
                    查询候选
                </Button>
            )}
            {resource === "number-sequences" && can("numbers.allocate") && (
                <Button
                    size="small"
                    onClick={() => {
                        const key = crypto.randomUUID();
                        modal.confirm({
                            title: "正式分配编号",
                            content: "此操作会占用一个编号。",
                            onOk: async () => {
                                try {
                                    const latest = await api(path);
                                    const result = await api(
                                        `${path}/allocate`,
                                        "POST",
                                        {},
                                        latest.etag,
                                        key,
                                    );
                                    modal.info({
                                        title: "已分配编号",
                                        content: result.number,
                                    });
                                    reload();
                                } catch (e: any) {
                                    message.error(e.message);
                                    throw e;
                                }
                            },
                        });
                    }}
                >
                    分配编号
                </Button>
            )}

            {[
                "route-versions",
                "recipe-versions",
                "product-revisions",
            ].includes(resource) &&
                can("manage") && (
                    <Button
                        size="small"
                        onClick={() => {
                            let version = "";
                            modal.confirm({
                                title: "复制为新草稿",
                                content: (
                                    <Input
                                        placeholder="新版本号"
                                        onChange={(e) => {
                                            version = e.target.value;
                                        }}
                                    />
                                ),
                                onOk: async () => {
                                    try {
                                        await api(`${path}/copy`, "POST", {
                                            version,
                                            reason: "复制新版本",
                                        });
                                        reload();
                                    } catch (e: any) {
                                        message.error(e.message);
                                        throw e;
                                    }
                                },
                            });
                        }}
                    >
                        复制版本
                    </Button>
                )}
            {resource === "number-sequences" && can("manage") && (
                <Button
                    size="small"
                    onClick={async () => {
                        try {
                            const result = await api(
                                `${path}/preview`,
                                "POST",
                                {},
                            );
                            modal.info({
                                title: "编号预览（不占号）",
                                content: result.number,
                            });
                        } catch (e: any) {
                            message.error(e.message);
                        }
                    }}
                >
                    预览
                </Button>
            )}
            {resource === "route-steps" && (
                <Button
                    size="small"
                    onClick={async () => {
                        try {
                            const result = await api(
                                `engineering/documents?entityId=${row.id}&pageSize=100`,
                            );
                            setDocuments(
                                result.items.filter(
                                    (x: any) => x.entityId === row.id,
                                ),
                            );
                        } catch (e: any) {
                            message.error(e.message);
                        }
                    }}
                >
                    指导书
                </Button>
            )}
            <Drawer
                title="作业指导书"
                open={documents !== null}
                onClose={() => setDocuments(null)}
                width={650}
            >
                {can("documents.manage") && (
                    <input
                        type="file"
                        accept="application/pdf"
                        onChange={async (e) => {
                            const file = e.target.files?.[0];
                            if (!file) return;
                            if (file.size > 10485760) {
                                message.error("文件不得超过 10 MiB");
                                return;
                            }
                            const reader = new FileReader();
                            reader.onload = () => {
                                let revision = "";
                                modal.confirm({
                                    title: "上传指导书",
                                    content: (
                                        <Input
                                            placeholder="文档修订号"
                                            onChange={(event) => {
                                                revision = event.target.value;
                                            }}
                                        />
                                    ),
                                    onOk: async () => {
                                        try {
                                            const doc = await api(
                                                "engineering/documents",
                                                "POST",
                                                {
                                                    entityType: "RouteStep",
                                                    entityId: row.id,
                                                    revision,
                                                    filename: file.name,
                                                    content: String(
                                                        reader.result,
                                                    ).split(",")[1],
                                                },
                                            );
                                            setDocuments([
                                                ...(documents || []),
                                                doc,
                                            ]);
                                            message.success("已上传草稿");
                                        } catch (error: any) {
                                            message.error(error.message);
                                            throw error;
                                        }
                                    },
                                });
                            };
                            reader.readAsDataURL(file);
                        }}
                    />
                )}
                <Table
                    rowKey="id"
                    dataSource={documents || []}
                    columns={[
                        {
                            title: "文件",
                            render: (_, doc) => (
                                <a
                                    href={`/api/v1/engineering/documents/${doc.id}/content`}
                                >
                                    {doc.originalFilename}
                                </a>
                            ),
                        },
                        { title: "修订", dataIndex: "revision" },
                        { title: "状态", dataIndex: "lifecycleStatus" },
                        {
                            title: "操作",
                            render: (_, doc) =>
                                doc.lifecycleStatus === "Draft" &&
                                can("documents.manage") && (
                                    <Button
                                        onClick={async () => {
                                            try {
                                                const saved = await api(
                                                    `engineering/documents/${doc.id}/release`,
                                                    "POST",
                                                    { reason: "发布指导书" },
                                                    doc.etag,
                                                    crypto.randomUUID(),
                                                );
                                                setDocuments(
                                                    (documents || []).map(
                                                        (x) =>
                                                            x.id === doc.id
                                                                ? saved
                                                                : x,
                                                    ),
                                                );
                                            } catch (e: any) {
                                                message.error(e.message);
                                            }
                                        }}
                                    >
                                        发布
                                    </Button>
                                ),
                        },
                    ]}
                />
            </Drawer>
        </>
    );
}
