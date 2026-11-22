-- Run as the database owner after schema.sql and role creation.
-- Create streamops_app in the Neon console with a fresh strong password; do not reuse the owner credential.
GRANT CONNECT ON DATABASE neondb TO streamops_app;
GRANT USAGE ON SCHEMA streamops TO streamops_app;
GRANT SELECT, INSERT, UPDATE ON ALL TABLES IN SCHEMA streamops TO streamops_app;
REVOKE UPDATE ON TABLE streamops.audit_logs FROM streamops_app;
REVOKE INSERT, UPDATE ON TABLE streamops.asp_net_roles, streamops.asp_net_role_claims FROM streamops_app;
REVOKE INSERT, UPDATE ON TABLE streamops.schema_version FROM streamops_app;
REVOKE DELETE ON ALL TABLES IN SCHEMA streamops FROM streamops_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA streamops TO streamops_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA streamops
  GRANT SELECT, INSERT, UPDATE ON TABLES TO streamops_app;
ALTER DEFAULT PRIVILEGES IN SCHEMA streamops
  GRANT USAGE, SELECT ON SEQUENCES TO streamops_app;
