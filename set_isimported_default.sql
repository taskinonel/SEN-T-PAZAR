ALTER TABLE "Listings" ALTER COLUMN "IsImported" SET DEFAULT false;
UPDATE "Listings" SET "IsImported" = false WHERE "IsImported" IS NULL;
