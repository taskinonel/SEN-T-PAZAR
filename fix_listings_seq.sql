BEGIN;
CREATE SEQUENCE IF NOT EXISTS listings_id_seq;
SELECT setval('listings_id_seq', COALESCE((SELECT MAX("Id") FROM "Listings"),0));
ALTER SEQUENCE listings_id_seq OWNED BY "Listings"."Id";
ALTER TABLE "Listings" ALTER COLUMN "Id" SET DEFAULT nextval('listings_id_seq');
COMMIT;
