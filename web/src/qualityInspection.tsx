import type { components } from "./generated/api";
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import {
    Alert,
    Form,
    Input,
    InputNumber,
    Modal,
    Select,
    Space,
    Typography,
} from "antd";
import { api } from "./api";
import {
    qualityOptions,
    templateSpecificationIds,
    QualitySelect,
    required,
    useQualityCommand,
    useQualityOptions,
    type QualityItem,
} from "./qualityShared";

export function QualityInspection({
    onClose,
    initialLotId,
}: {
    onClose: () => void;
    initialLotId?: string;
}) {
    const [form] = Form.useForm();
    const [lotId, setLotId] = useState(initialLotId);
    const [templateId, setTemplateId] = useState<string>();
    const [waferIds, setWaferIds] = useState<string[]>([]);
    const [mode, setMode] = useState("Lot");
    const { send, busy } = useQualityCommand();
    const lot = useQuery<QualityItem>({
        queryKey: ["quality", "lots", lotId],
        queryFn: () => api(`quality/lots/${lotId}`),
        enabled: !!lotId,
    });
    const templates = useQualityOptions("templates?status=Released");
    const specs = useQualityOptions("specifications?status=Released");
    const equipment = useQualityOptions("equipment");
    const template = templates.data?.items.find((x) => x.id === templateId);
    const ids: string[] = template
        ? templateSpecificationIds(template.parameters)
        : [];
    const objects = mode === "Lot" ? [null] : waferIds;
    const measurements = objects
        .flatMap((waferId) =>
            Array.from({ length: template?.samplesPerCheck || 0 }, (_, i) =>
                ids.map((specId) => ({
                    waferId,
                    sampleNumber: i + 1,
                    spec: specs.data?.items.find((x) => x.id === specId),
                })),
            ),
        )
        .flat();
    const resetValues = () => form.setFieldsValue({ values: [], points: [] });
    const save = async () => {
        try {
            const v = await form.validateFields();
            if (
                !lot.data ||
                !template ||
                measurements.length === 0 ||
                measurements.some((x) => !x.spec)
            )
                return;
            const body: components["schemas"]["RecordQualityCheck"] = {
                lotId: lot.data.id,
                lotEtag: lot.data.etag,
                qualityCheckTemplateId: template.id,
                notes: v.notes || null,
                processRunId: v.processRunId || null,
                measurements: measurements.map((row, i) => ({
                    specificationVersionId: row.spec!.id,
                    waferId: row.waferId,
                    sampleNumber: row.sampleNumber,
                    samplePoint:
                        v.points?.[
                            `${row.waferId || "lot"}-${row.sampleNumber}`
                        ] || null,
                    unit: row.spec!.unit || null,
                    measurementEquipmentId: v.equipmentId || null,
                    ...(row.spec!.valueType === "boolean"
                        ? { valueBoolean: v.values[i] }
                        : row.spec!.valueType === "string"
                          ? { valueText: v.values[i] }
                          : { valueNumeric: v.values[i] }),
                })),
            };
            await send("quality/checks", body);
            onClose();
        } catch {
            /* Keep the complete measurement form after a failed submission. */
        }
    };
    return (
        <Modal
            title="录入检验结果"
            open
            onCancel={onClose}
            onOk={save}
            confirmLoading={busy}
            width={850}
            destroyOnHidden
        >
            <Alert
                type="info"
                showIcon
                title="后端依据发布规格判定；不合格会自动创建异常并冻结批次。复检通过后仍须人工处置旧异常。"
                style={{ marginBottom: 16 }}
            />
            {lot.error && (
                <Alert type="error" title={(lot.error as Error).message} />
            )}
            <Form
                form={form}
                layout="vertical"
                initialValues={{ lotId: initialLotId, mode: "Lot" }}
            >
                <Form.Item name="lotId" label="检验批次" rules={required}>
                    <QualitySelect
                        resource="lots"
                        onChange={(id: string) => {
                            setLotId(id);
                            setTemplateId(undefined);
                            setWaferIds([]);
                            form.resetFields([
                                "templateId",
                                "waferIds",
                                "processRunId",
                            ]);
                            resetValues();
                        }}
                    />
                </Form.Item>
                <Form.Item
                    name="templateId"
                    label="已发布模板"
                    rules={required}
                >
                    <Select
                        options={qualityOptions(
                            templates.data?.items.filter(
                                (x) =>
                                    x.routeVersionId ===
                                    lot.data?.routeVersionId,
                            ),
                        )}
                        onChange={(id) => {
                            setTemplateId(id);
                            resetValues();
                        }}
                    />
                </Form.Item>
                <Space align="start">
                    <Form.Item name="mode" label="检验对象" rules={required}>
                        <Select
                            style={{ width: 160 }}
                            options={[
                                { value: "Lot", label: "批次抽样" },
                                { value: "Wafer", label: "逐片检验" },
                            ]}
                            onChange={(v) => {
                                setMode(v);
                                resetValues();
                            }}
                        />
                    </Form.Item>
                    {mode === "Wafer" && (
                        <Form.Item
                            name="waferIds"
                            label="选择晶圆"
                            rules={required}
                        >
                            <Select
                                mode="multiple"
                                style={{ minWidth: 300 }}
                                options={lot.data?.wafers?.map(
                                    (x: QualityItem) => ({
                                        value: x.id,
                                        label: x.waferCode,
                                    }),
                                )}
                                onChange={(v) => {
                                    setWaferIds(v);
                                    resetValues();
                                }}
                            />
                        </Form.Item>
                    )}
                </Space>
                <Form.Item name="processRunId" label="关联当前加工（可选）">
                    <Select
                        allowClear
                        options={
                            lot.data?.run
                                ? [
                                      {
                                          value: lot.data.run.id,
                                          label: `当前加工 · ${lot.data.step?.name}`,
                                      },
                                  ]
                                : []
                        }
                    />
                </Form.Item>
                <Form.Item name="equipmentId" label="量测设备（可选）">
                    <Select
                        allowClear
                        options={qualityOptions(equipment.data?.items)}
                    />
                </Form.Item>
                {measurements.map(
                    (row, i) =>
                        row.spec && (
                            <div
                                key={`${templateId}-${row.waferId}-${row.sampleNumber}-${row.spec.id}`}
                            >
                                {i % ids.length === 0 && (
                                    <>
                                        <Typography.Text strong>
                                            {row.waferId
                                                ? lot.data?.wafers?.find(
                                                      (x: QualityItem) =>
                                                          x.id === row.waferId,
                                                  )?.waferCode
                                                : "批次"}{" "}
                                            · 样本 {row.sampleNumber}
                                        </Typography.Text>
                                        <Form.Item
                                            name={[
                                                "points",
                                                `${row.waferId || "lot"}-${row.sampleNumber}`,
                                            ]}
                                            label="样点（可选）"
                                            preserve={false}
                                        >
                                            <Input maxLength={200} />
                                        </Form.Item>
                                    </>
                                )}
                                <Form.Item
                                    name={["values", i]}
                                    preserve={false}
                                    label={`${row.spec.parameterCode} · ${row.spec.unit || "无单位"} · ${row.spec.valueType === "boolean" ? `期望${row.spec.expectedBoolean ? "是" : "否"}` : row.spec.valueType === "string" ? `期望 ${row.spec.expectedText}` : `${row.spec.specMin ?? "无下限"} ～ ${row.spec.specMax ?? "无上限"}`}`}
                                    rules={required}
                                >
                                    {row.spec.valueType === "boolean" ? (
                                        <Select
                                            options={[
                                                { value: true, label: "是" },
                                                { value: false, label: "否" },
                                            ]}
                                        />
                                    ) : row.spec.valueType === "string" ? (
                                        <Input maxLength={2000} />
                                    ) : (
                                        <InputNumber
                                            style={{ width: "100%" }}
                                        />
                                    )}
                                </Form.Item>
                            </div>
                        ),
                )}
                <Form.Item name="notes" label="检验说明">
                    <Input.TextArea maxLength={2000} />
                </Form.Item>
            </Form>
        </Modal>
    );
}
