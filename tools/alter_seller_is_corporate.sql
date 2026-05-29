BEGIN;
ALTER TABLE "Listings" ALTER COLUMN "SellerIsCorporate" DROP DEFAULT;
ALTER TABLE "Listings" ALTER COLUMN "SellerIsCorporate" TYPE boolean USING (CASE WHEN "SellerIsCorporate" IS NULL THEN false WHEN trim("SellerIsCorporate"::text) ~ '^[01]$' THEN ("SellerIsCorporate"::int <> 0) ELSE "SellerIsCorporate"::boolean END);
COMMIT;
