import { Button, Empty, Space, Tag } from "antd";
import { useEffect, useRef, useState } from "react";
import { metric } from "./workbenchShared";
import {
    ratioText,
    reasonLabel,
    type AnalyticsSchemas,
} from "./analyticsShared";

type Point = AnalyticsSchemas["AnalyticsOeePoint"];

export const series = [
    { key: "oee", label: "OEE", name: "综合设备效率", color: "oee" },
    { key: "availability", label: "A", name: "可用率", color: "a" },
    { key: "performance", label: "P", name: "性能率", color: "p" },
    { key: "quality", label: "Q", name: "良品率", color: "q" },
] as const;

export function OeeTrend({
    days,
    selected,
    choose,
}: {
    days: Point[];
    selected?: string;
    choose: (value: string) => void;
}) {
    const [hidden, setHidden] = useState<string[]>([]);
    const [width, setWidth] = useState(700);
    const container = useRef<HTMLDivElement>(null);
    const hasDays = days.length > 0;
    useEffect(() => {
        const target = container.current;
        if (!target) return;
        const observer = new ResizeObserver(([entry]) => {
            setWidth(Math.floor(entry.contentRect.width));
        });
        observer.observe(target);
        return () => observer.disconnect();
    }, [hasDays]);

    if (!hasDays) return <Empty description="暂无设备趋势" />;
    const active = series.filter((item) => !hidden.includes(item.key));
    const compact = width < 440;
    const size = {
        width: Math.max(width, 250, 60 + days.length * 25),
        height: compact ? 208 : 250,
        x: compact ? 44 : 57,
        y: 16,
        right: compact ? 19 : 26,
        base: compact ? 174 : 216,
    };
    const x = (index: number) =>
        size.x +
        (size.width - size.x - size.right) *
            (days.length === 1 ? 0.5 : index / (days.length - 1));
    const maximum = Math.max(
        1,
        ...days.flatMap(({ metrics }) =>
            active.map(({ key }) => metrics?.[key] ?? 0),
        ),
    );
    const ceiling = Math.ceil(maximum * 4) / 4;
    const y = (value: number) =>
        size.base - ((size.base - size.y) * value) / ceiling;
    const selectedIndex = days.findIndex((item) => item.date === selected);
    const defaultIndex = days.reduce(
        (last, { metrics }, i) => (metrics?.oee != null ? i : last),
        -1,
    );
    const index =
        selectedIndex >= 0
            ? selectedIndex
            : defaultIndex >= 0
              ? defaultIndex
              : days.length - 1;
    const focus = days[index];
    const ticks = Array.from({ length: 5 }, (_, i) => (ceiling * i) / 4);
    const labelEvery = Math.max(1, Math.ceil(days.length / 7));

    return (
        <>
            <Space wrap className="analytics-legend">
                {series.map((item) => (
                    <Button
                        size="small"
                        key={item.key}
                        className={`analytics-series analytics-series-${item.color}`}
                        aria-pressed={!hidden.includes(item.key)}
                        title={item.name}
                        onClick={() =>
                            setHidden((previous) =>
                                previous.includes(item.key)
                                    ? previous.filter((key) => key !== item.key)
                                    : previous.length < series.length - 1
                                      ? [...previous, item.key]
                                      : previous,
                            )
                        }
                    >
                        <i /> {item.label} {item.key === "oee" ? "" : item.name}
                    </Button>
                ))}
            </Space>
            <div ref={container} className="analytics-chart-scroll">
                <svg
                    className="analytics-trend"
                    style={{ width: size.width }}
                    viewBox={`0 0 ${size.width} ${size.height}`}
                    role="group"
                    aria-label="按日 OEE 和 A／P／Q 趋势，空白处表示无法计算"
                >
                    {ticks.map((tick) => (
                        <g key={tick} className="analytics-grid-line">
                            <line
                                x1={size.x}
                                x2={size.width - size.right}
                                y1={y(tick)}
                                y2={y(tick)}
                            />
                            <text
                                x={size.x - 7}
                                y={y(tick) + 4}
                                textAnchor="end"
                            >
                                {ratioText(tick)}
                            </text>
                        </g>
                    ))}
                    <line
                        className="analytics-guide"
                        x1={x(index)}
                        x2={x(index)}
                        y1={size.y}
                        y2={size.base}
                    />
                    {active.map((item) => {
                        const path = days
                            .map(({ metrics }, i) => {
                                const value = metrics?.[item.key];
                                if (value == null) return "";
                                const command =
                                    i === 0 ||
                                    days[i - 1].metrics?.[item.key] == null
                                        ? "M"
                                        : "L";
                                return `${command}${x(i)} ${y(value)}`;
                            })
                            .join(" ");
                        return (
                            <g
                                className={`analytics-series analytics-series-${item.color}`}
                                key={item.key}
                            >
                                <path d={path} fill="none" />
                                {days.map(({ date, metrics }, i) => {
                                    const value = metrics?.[item.key];
                                    return value == null ? null : (
                                        <circle
                                            key={date}
                                            cx={x(i)}
                                            cy={y(value)}
                                            r={i === index ? 5 : 2.5}
                                        >
                                            <title>
                                                {date} · {item.name}{" "}
                                                {ratioText(value)}
                                            </title>
                                        </circle>
                                    );
                                })}
                            </g>
                        );
                    })}
                    {days.map(({ date }, i) => (
                        <g
                            key={date}
                            className="analytics-chart-hit"
                            role="button"
                            tabIndex={0}
                            aria-label={`查看 ${date} 指标`}
                            onClick={() => date && choose(date)}
                            onKeyDown={(event) => {
                                if (
                                    date &&
                                    ["Enter", " "].includes(event.key)
                                ) {
                                    event.preventDefault();
                                    choose(date);
                                }
                            }}
                        >
                            <rect
                                x={Math.max(
                                    size.x - 8,
                                    x(i) - (x(1) - x(0) || 24) / 2,
                                )}
                                y={size.y}
                                height={size.base - size.y}
                                width={
                                    days.length === 1 ? 24 : (x(1) - x(0)) * 0.9
                                }
                                fill="transparent"
                            />
                            {(i % labelEvery === 0 ||
                                (i === days.length - 1 && days.length < 9)) && (
                                <text
                                    x={x(i)}
                                    y={size.base + 21}
                                    textAnchor="middle"
                                >
                                    {date?.slice(5)}
                                </text>
                            )}
                        </g>
                    ))}
                </svg>
            </div>
            <div className="analytics-trend-detail" aria-live="polite">
                <strong>{focus.date}</strong>
                {series.map((item) => (
                    <span
                        className={`analytics-series analytics-series-${item.color}`}
                        key={item.key}
                    >
                        <i /> {item.label}{" "}
                        <b>{ratioText(focus.metrics?.[item.key])}</b>
                    </span>
                ))}
                <span>
                    Run / 计划 {metric(focus.metrics?.runMinutes, 1)} /{" "}
                    {metric(focus.metrics?.plannedMinutes, 1)} 分钟
                </span>
                {focus.metrics?.missing?.map((code) => (
                    <Tag key={code}>{reasonLabel(code)}</Tag>
                ))}
            </div>
        </>
    );
}
