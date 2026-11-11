import { ApartmentOutlined } from "@ant-design/icons";
import { Tag } from "antd";
import { type Me } from "./api";
import { Heading, roleLabels } from "./shared";

export function Dashboard({ me }: { me: Me }) {
    return (
        <>
            <Heading
                eyebrow="WORKSPACE / OVERVIEW"
                title={`你好，${me.displayName}`}
                description="从这里维护制造基础信息，管理人员身份与访问范围。"
            />
            <div className="overview-banner">
                <div>
                    <Tag>当前工作空间</Tag>
                    <h2>{me.fab?.name || "等待配置厂区范围"}</h2>
                    <p>
                        {me.fab
                            ? `${me.fab.code} · ${me.fab.timezone} · ${me.fab.isActive ? "已启用" : "已停用（可查询历史）"}`
                            : "请联系管理员分配厂区范围，之后即可进入对应业务工作台。"}
                    </p>
                </div>
                <ApartmentOutlined />
            </div>
            <div className="cards">
                <div className="stat">
                    <span>我的角色</span>
                    <strong>
                        {me.roles?.map((x) => roleLabels[x] || x).join(" / ") ||
                            "尚未分配"}
                    </strong>
                    <p>职责通过角色授予</p>
                </div>
                <div className="stat">
                    <span>有效权限</span>
                    <strong>
                        {me.permissions?.length || 0}
                        <em> 项</em>
                    </strong>
                    <p>每次请求由服务端重新校验</p>
                </div>
                <div className="stat">
                    <span>厂区范围</span>
                    <strong>
                        {me.fab ? "1" : "0"}
                        <em> 个</em>
                    </strong>
                    <p>仅显示当前授权的部署厂区</p>
                </div>
            </div>
            <section className="panel">
                <h3>基础管理指南</h3>
                <div className="steps">
                    <div>
                        <b>01</b>
                        <h4>组织与场所</h4>
                        <p>维护区域、产线与位置，明确工位和终端归属。</p>
                    </div>
                    <div>
                        <b>02</b>
                        <h4>员工与账号</h4>
                        <p>区分员工身份和登录账号，建立可追溯的人员关联。</p>
                    </div>
                    <div>
                        <b>03</b>
                        <h4>角色与范围</h4>
                        <p>按职责配置权限，变更后重新登录使授权生效。</p>
                    </div>
                </div>
            </section>
        </>
    );
}
