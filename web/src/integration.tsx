import { Tabs } from "antd";
import type { Me } from "./api";
import { IntegrationImport } from "./integrationImport";
import { IntegrationSimulator } from "./integrationSimulator";
import { integrationAccess } from "./integrationShared";

export function Integration({ me }: { me: Me }) {
    const access = integrationAccess(me);

    return (
        <>
            <div className="page-heading">
                <h1>数据导入与集成</h1>
            </div>
            <Tabs
                items={[
                    ...(access.simulator
                        ? [
                              {
                                  key: "simulator",
                                  label: "设备模拟",
                                  children: (
                                      <IntegrationSimulator
                                          canPublish={access.publish}
                                      />
                                  ),
                              },
                          ]
                        : []),
                    ...(access.imports
                        ? [
                              {
                                  key: "imports",
                                  label: "工单导入",
                                  children: (
                                      <IntegrationImport
                                          canReadOrders={access.planning}
                                      />
                                  ),
                              },
                          ]
                        : []),
                ]}
            />
        </>
    );
}
