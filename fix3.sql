CREATE SEQUENCE IF NOT EXISTS adminaudilogs_id_seq;
ALTER TABLE "AdminAuditLogs" ALTER COLUMN "Id" SET DEFAULT nextval('public.adminaudilogs_id_seq');
GRANT USAGE, SELECT ON SEQUENCE public.adminaudilogs_id_seq TO sentpazar;