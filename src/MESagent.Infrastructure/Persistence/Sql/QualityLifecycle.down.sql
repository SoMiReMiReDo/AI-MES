DROP TRIGGER protect_issue_context ON quality.issues;
DROP FUNCTION quality.protect_issue_context();
DROP TRIGGER protect_hold ON quality.hold_records;
DROP FUNCTION quality.protect_hold();
DROP TRIGGER immutable_history ON quality.lot_releases;
DROP TRIGGER immutable_history ON quality.quality_checks;
DROP TRIGGER protect_released_template ON quality.quality_check_templates;
-- Keep security assignments and system codes on rollback to avoid deleting administrator-maintained configuration.
