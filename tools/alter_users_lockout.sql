BEGIN;
ALTER TABLE "AspNetUsers" ALTER COLUMN "LockoutEnd" DROP DEFAULT;
ALTER TABLE "AspNetUsers" ALTER COLUMN "LockoutEnd" TYPE timestamptz USING (CASE WHEN trim("LockoutEnd") ~ '^\\d+$' THEN to_timestamp(trim("LockoutEnd")::bigint) WHEN trim("LockoutEnd") ~ '^\\d{4}-' THEN trim("LockoutEnd")::timestamptz ELSE NULL END);
ALTER TABLE "AspNetUsers" ALTER COLUMN "PhoneNumberConfirmed" DROP DEFAULT;
ALTER TABLE "AspNetUsers" ALTER COLUMN "PhoneNumberConfirmed" TYPE boolean USING (CASE WHEN "PhoneNumberConfirmed" IS NULL THEN false WHEN trim("PhoneNumberConfirmed"::text) ~ '^[01]$' THEN ("PhoneNumberConfirmed"::int <> 0) ELSE "PhoneNumberConfirmed"::boolean END);
COMMIT;
