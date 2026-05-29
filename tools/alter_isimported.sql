BEGIN;
ALTER TABLE "Listings" ALTER COLUMN "IsImported" DROP DEFAULT;
ALTER TABLE "Listings" ALTER COLUMN "IsImported" TYPE boolean USING (CASE WHEN "IsImported" IS NULL THEN false WHEN trim("IsImported"::text) ~ '^[01]$' THEN ("IsImported"::int <> 0) ELSE "IsImported"::boolean END);
COMMIT;
