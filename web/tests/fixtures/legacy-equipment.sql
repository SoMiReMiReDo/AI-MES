-- Disposable browser-test database only: explicit historical source, no fabricated Run.
DO $$
DECLARE
    area_id uuid := uuidv7();
    group_id uuid := uuidv7();
    type_id uuid := uuidv7();
    tool_id uuid := uuidv7();
BEGIN
    INSERT INTO organization.areas(id,fab_id,code,name)
    VALUES(area_id,'{{FAB_ID}}','LEGACY-AREA','历史测试区域');
    INSERT INTO equipment.equipment_groups(id,fab_id,code,name)
    VALUES(group_id,'{{FAB_ID}}','LEGACY-GROUP','历史设备组');
    INSERT INTO equipment.equipment_types(id,code,name)
    VALUES(type_id,'LEGACY-TYPE','历史设备类型');
    INSERT INTO equipment.equipment(id,area_id,equipment_group_id,equipment_type_id,code,name,operational_state,created_at)
    VALUES(tool_id,area_id,group_id,type_id,'LEGACY-EQUIPMENT','历史设备','Idle',CURRENT_TIMESTAMP-interval '3 days');
    INSERT INTO equipment.equipment_states(equipment_id,state,started_at,ended_at,source)
    VALUES(tool_id,'Down',CURRENT_TIMESTAMP-interval '2 hours',CURRENT_TIMESTAMP-interval '1 hour','Legacy'),
        (tool_id,'Idle',CURRENT_TIMESTAMP-interval '1 hour',NULL,'Legacy');
END $$;
