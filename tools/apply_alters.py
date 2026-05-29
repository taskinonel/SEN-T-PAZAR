#!/usr/bin/env python3
import tempfile,os,subprocess,sys
sql = '''BEGIN;
ALTER TABLE "Listings" ALTER COLUMN "AllowWhatsApp" TYPE boolean USING (CASE WHEN "AllowWhatsApp" IS NULL THEN false WHEN trim("AllowWhatsApp"::text) ~ '^[01]$' THEN ("AllowWhatsApp"::int <> 0) ELSE "AllowWhatsApp"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "AllowMessages" TYPE boolean USING (CASE WHEN "AllowMessages" IS NULL THEN false WHEN trim("AllowMessages"::text) ~ '^[01]$' THEN ("AllowMessages"::int <> 0) ELSE "AllowMessages"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "Negotiable" TYPE boolean USING (CASE WHEN "Negotiable" IS NULL THEN false WHEN trim("Negotiable"::text) ~ '^[01]$' THEN ("Negotiable"::int <> 0) ELSE "Negotiable"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "TradeIn" TYPE boolean USING (CASE WHEN "TradeIn" IS NULL THEN false WHEN trim("TradeIn"::text) ~ '^[01]$' THEN ("TradeIn"::int <> 0) ELSE "TradeIn"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "EstateFurnished" TYPE boolean USING (CASE WHEN "EstateFurnished" IS NULL THEN false WHEN trim("EstateFurnished"::text) ~ '^[01]$' THEN ("EstateFurnished"::int <> 0) ELSE "EstateFurnished"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "InSite" TYPE boolean USING (CASE WHEN "InSite" IS NULL THEN false WHEN trim("InSite"::text) ~ '^[01]$' THEN ("InSite"::int <> 0) ELSE "InSite"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "HasBalcony" TYPE boolean USING (CASE WHEN "HasBalcony" IS NULL THEN false WHEN trim("HasBalcony"::text) ~ '^[01]$' THEN ("HasBalcony"::int <> 0) ELSE "HasBalcony"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "HasElevator" TYPE boolean USING (CASE WHEN "HasElevator" IS NULL THEN false WHEN trim("HasElevator"::text) ~ '^[01]$' THEN ("HasElevator"::int <> 0) ELSE "HasElevator"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "HasParking" TYPE boolean USING (CASE WHEN "HasParking" IS NULL THEN false WHEN trim("HasParking"::text) ~ '^[01]$' THEN ("HasParking"::int <> 0) ELSE "HasParking"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "HasPool" TYPE boolean USING (CASE WHEN "HasPool" IS NULL THEN false WHEN trim("HasPool"::text) ~ '^[01]$' THEN ("HasPool"::int <> 0) ELSE "HasPool"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "HasSecurity" TYPE boolean USING (CASE WHEN "HasSecurity" IS NULL THEN false WHEN trim("HasSecurity"::text) ~ '^[01]$' THEN ("HasSecurity"::int <> 0) ELSE "HasSecurity"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "IsFeatured" TYPE boolean USING (CASE WHEN "IsFeatured" IS NULL THEN false WHEN trim("IsFeatured"::text) ~ '^[01]$' THEN ("IsFeatured"::int <> 0) ELSE "IsFeatured"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "IsVitrin" TYPE boolean USING (CASE WHEN "IsVitrin" IS NULL THEN false WHEN trim("IsVitrin"::text) ~ '^[01]$' THEN ("IsVitrin"::int <> 0) ELSE "IsVitrin"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "IsImported" TYPE boolean USING (CASE WHEN "IsImported" IS NULL THEN false WHEN trim("IsImported"::text) ~ '^[01]$' THEN ("IsImported"::int <> 0) ELSE "IsImported"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "SellerIsCorporate" TYPE boolean USING (CASE WHEN "SellerIsCorporate" IS NULL THEN false WHEN trim("SellerIsCorporate"::text) ~ '^[01]$' THEN ("SellerIsCorporate"::int <> 0) ELSE "SellerIsCorporate"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "UnderWarranty" TYPE boolean USING (CASE WHEN "UnderWarranty" IS NULL THEN false WHEN trim("UnderWarranty"::text) ~ '^[01]$' THEN ("UnderWarranty"::int <> 0) ELSE "UnderWarranty"::boolean END);
ALTER TABLE "Listings" ALTER COLUMN "CreatedAt" TYPE timestamptz USING (CASE WHEN trim("CreatedAt") ~ '^\\\d+$' THEN to_timestamp(trim("CreatedAt")::bigint) WHEN trim("CreatedAt") ~ '^\\d{4}-' THEN trim("CreatedAt")::timestamptz ELSE NULL END);
ALTER TABLE "Listings" ALTER COLUMN "PublishUntil" TYPE timestamptz USING (CASE WHEN trim("PublishUntil") ~ '^\\\d+$' THEN to_timestamp(trim("PublishUntil")::bigint) WHEN trim("PublishUntil") ~ '^\\d{4}-' THEN trim("PublishUntil")::timestamptz ELSE NULL END);
ALTER TABLE "Listings" ALTER COLUMN "FeaturedExpiryDate" TYPE timestamptz USING (CASE WHEN trim("FeaturedExpiryDate") ~ '^\\\d+$' THEN to_timestamp(trim("FeaturedExpiryDate")::bigint) WHEN trim("FeaturedExpiryDate") ~ '^\\d{4}-' THEN trim("FeaturedExpiryDate")::timestamptz ELSE NULL END);
ALTER TABLE "Listings" ALTER COLUMN "VitrinExpiryDate" TYPE timestamptz USING (CASE WHEN trim("VitrinExpiryDate") ~ '^\\\d+$' THEN to_timestamp(trim("VitrinExpiryDate")::bigint) WHEN trim("VitrinExpiryDate") ~ '^\\d{4}-' THEN trim("VitrinExpiryDate")::timestamptz ELSE NULL END);
ALTER TABLE "Listings" ALTER COLUMN "PublishedAt" TYPE timestamptz USING (CASE WHEN trim("PublishedAt") ~ '^\\\d+$' THEN to_timestamp(trim("PublishedAt")::bigint) WHEN trim("PublishedAt") ~ '^\\d{4}-' THEN trim("PublishedAt")::timestamptz ELSE NULL END);
COMMIT;
'''
fd, path = tempfile.mkstemp(prefix='alters_', suffix='.sql', dir='/tmp', text=True)
with os.fdopen(fd, 'w', encoding='utf-8') as f:
    f.write(sql)
os.chmod(path, 0o644)
print('WROTE', path)
proc = subprocess.run(['sudo','-u','postgres','psql','-d','sentpazar','-v','ON_ERROR_STOP=1','-f',path], stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)
print('RC', proc.returncode)
print('STDOUT:\n', proc.stdout)
print('STDERR:\n', proc.stderr)
os.remove(path)
if proc.returncode!=0:
    sys.exit(proc.returncode)
