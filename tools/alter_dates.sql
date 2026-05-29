BEGIN;
-- AdminAuditLogs.CreatedAtUtc
ALTER TABLE "AdminAuditLogs" ALTER COLUMN "CreatedAtUtc" DROP DEFAULT;
ALTER TABLE "AdminAuditLogs" ALTER COLUMN "CreatedAtUtc" TYPE timestamptz USING (CASE WHEN trim("CreatedAtUtc") ~ '^\\d+$' THEN to_timestamp(trim("CreatedAtUtc")::bigint) WHEN trim("CreatedAtUtc") ~ '^\\d{4}-' THEN trim("CreatedAtUtc")::timestamptz ELSE NULL END);
-- AspNetUsers date fields
ALTER TABLE "AspNetUsers" ALTER COLUMN "CorporateApprovalDate" DROP DEFAULT;
ALTER TABLE "AspNetUsers" ALTER COLUMN "CorporateApprovalDate" TYPE timestamptz USING (CASE WHEN trim("CorporateApprovalDate") ~ '^\\d+$' THEN to_timestamp(trim("CorporateApprovalDate")::bigint) WHEN trim("CorporateApprovalDate") ~ '^\\d{4}-' THEN trim("CorporateApprovalDate")::timestamptz ELSE NULL END);
ALTER TABLE "AspNetUsers" ALTER COLUMN "FcmTokenUpdatedAtUtc" DROP DEFAULT;
ALTER TABLE "AspNetUsers" ALTER COLUMN "FcmTokenUpdatedAtUtc" TYPE timestamptz USING (CASE WHEN trim("FcmTokenUpdatedAtUtc") ~ '^\\d+$' THEN to_timestamp(trim("FcmTokenUpdatedAtUtc")::bigint) WHEN trim("FcmTokenUpdatedAtUtc") ~ '^\\d{4}-' THEN trim("FcmTokenUpdatedAtUtc")::timestamptz ELSE NULL END);
ALTER TABLE "AspNetUsers" ALTER COLUMN "SubscriptionEndDate" DROP DEFAULT;
ALTER TABLE "AspNetUsers" ALTER COLUMN "SubscriptionEndDate" TYPE timestamptz USING (CASE WHEN trim("SubscriptionEndDate") ~ '^\\d+$' THEN to_timestamp(trim("SubscriptionEndDate")::bigint) WHEN trim("SubscriptionEndDate") ~ '^\\d{4}-' THEN trim("SubscriptionEndDate")::timestamptz ELSE NULL END);
ALTER TABLE "AspNetUsers" ALTER COLUMN "SubscriptionStartDate" DROP DEFAULT;
ALTER TABLE "AspNetUsers" ALTER COLUMN "SubscriptionStartDate" TYPE timestamptz USING (CASE WHEN trim("SubscriptionStartDate") ~ '^\\d+$' THEN to_timestamp(trim("SubscriptionStartDate")::bigint) WHEN trim("SubscriptionStartDate") ~ '^\\d{4}-' THEN trim("SubscriptionStartDate")::timestamptz ELSE NULL END);
ALTER TABLE "AspNetUsers" ALTER COLUMN "VerifiedAt" DROP DEFAULT;
ALTER TABLE "AspNetUsers" ALTER COLUMN "VerifiedAt" TYPE timestamptz USING (CASE WHEN trim("VerifiedAt") ~ '^\\d+$' THEN to_timestamp(trim("VerifiedAt")::bigint) WHEN trim("VerifiedAt") ~ '^\\d{4}-' THEN trim("VerifiedAt")::timestamptz ELSE NULL END);
-- Documents.UploadDate
ALTER TABLE "Documents" ALTER COLUMN "UploadDate" DROP DEFAULT;
ALTER TABLE "Documents" ALTER COLUMN "UploadDate" TYPE timestamptz USING (CASE WHEN trim("UploadDate") ~ '^\\d+$' THEN to_timestamp(trim("UploadDate")::bigint) WHEN trim("UploadDate") ~ '^\\d{4}-' THEN trim("UploadDate")::timestamptz ELSE NULL END);
-- ListingMessages
ALTER TABLE "ListingMessages" ALTER COLUMN "CreatedAt" DROP DEFAULT;
ALTER TABLE "ListingMessages" ALTER COLUMN "CreatedAt" TYPE timestamptz USING (CASE WHEN trim("CreatedAt") ~ '^\\d+$' THEN to_timestamp(trim("CreatedAt")::bigint) WHEN trim("CreatedAt") ~ '^\\d{4}-' THEN trim("CreatedAt")::timestamptz ELSE NULL END);
ALTER TABLE "ListingMessages" ALTER COLUMN "ReadAt" DROP DEFAULT;
ALTER TABLE "ListingMessages" ALTER COLUMN "ReadAt" TYPE timestamptz USING (CASE WHEN trim("ReadAt") ~ '^\\d+$' THEN to_timestamp(trim("ReadAt")::bigint) WHEN trim("ReadAt") ~ '^\\d{4}-' THEN trim("ReadAt")::timestamptz ELSE NULL END);
-- ListingPromotions
ALTER TABLE "ListingPromotions" ALTER COLUMN "ExpiresAt" DROP DEFAULT;
ALTER TABLE "ListingPromotions" ALTER COLUMN "ExpiresAt" TYPE timestamptz USING (CASE WHEN trim("ExpiresAt") ~ '^\\d+$' THEN to_timestamp(trim("ExpiresAt")::bigint) WHEN trim("ExpiresAt") ~ '^\\d{4}-' THEN trim("ExpiresAt")::timestamptz ELSE NULL END);
ALTER TABLE "ListingPromotions" ALTER COLUMN "StartedAt" DROP DEFAULT;
ALTER TABLE "ListingPromotions" ALTER COLUMN "StartedAt" TYPE timestamptz USING (CASE WHEN trim("StartedAt") ~ '^\\d+$' THEN to_timestamp(trim("StartedAt")::bigint) WHEN trim("StartedAt") ~ '^\\d{4}-' THEN trim("StartedAt")::timestamptz ELSE NULL END);
-- Payments
ALTER TABLE "Payments" ALTER COLUMN "CompletedAt" DROP DEFAULT;
ALTER TABLE "Payments" ALTER COLUMN "CompletedAt" TYPE timestamptz USING (CASE WHEN trim("CompletedAt") ~ '^\\d+$' THEN to_timestamp(trim("CompletedAt")::bigint) WHEN trim("CompletedAt") ~ '^\\d{4}-' THEN trim("CompletedAt")::timestamptz ELSE NULL END);
ALTER TABLE "Payments" ALTER COLUMN "CreatedAt" DROP DEFAULT;
ALTER TABLE "Payments" ALTER COLUMN "CreatedAt" TYPE timestamptz USING (CASE WHEN trim("CreatedAt") ~ '^\\d+$' THEN to_timestamp(trim("CreatedAt")::bigint) WHEN trim("CreatedAt") ~ '^\\d{4}-' THEN trim("CreatedAt")::timestamptz ELSE NULL END);
-- Reviews
ALTER TABLE "Reviews" ALTER COLUMN "CreatedAt" DROP DEFAULT;
ALTER TABLE "Reviews" ALTER COLUMN "CreatedAt" TYPE timestamptz USING (CASE WHEN trim("CreatedAt") ~ '^\\d+$' THEN to_timestamp(trim("CreatedAt")::bigint) WHEN trim("CreatedAt") ~ '^\\d{4}-' THEN trim("CreatedAt")::timestamptz ELSE NULL END);
ALTER TABLE "Reviews" ALTER COLUMN "ModeratedAt" DROP DEFAULT;
ALTER TABLE "Reviews" ALTER COLUMN "ModeratedAt" TYPE timestamptz USING (CASE WHEN trim("ModeratedAt") ~ '^\\d+$' THEN to_timestamp(trim("ModeratedAt")::bigint) WHEN trim("ModeratedAt") ~ '^\\d{4}-' THEN trim("ModeratedAt")::timestamptz ELSE NULL END);
-- UserFavorites
ALTER TABLE "UserFavorites" ALTER COLUMN "CreatedAt" DROP DEFAULT;
ALTER TABLE "UserFavorites" ALTER COLUMN "CreatedAt" TYPE timestamptz USING (CASE WHEN trim("CreatedAt") ~ '^\\d+$' THEN to_timestamp(trim("CreatedAt")::bigint) WHEN trim("CreatedAt") ~ '^\\d{4}-' THEN trim("CreatedAt")::timestamptz ELSE NULL END);
-- UserPackages
ALTER TABLE "UserPackages" ALTER COLUMN "ExpiryDate" DROP DEFAULT;
ALTER TABLE "UserPackages" ALTER COLUMN "ExpiryDate" TYPE timestamptz USING (CASE WHEN trim("ExpiryDate") ~ '^\\d+$' THEN to_timestamp(trim("ExpiryDate")::bigint) WHEN trim("ExpiryDate") ~ '^\\d{4}-' THEN trim("ExpiryDate")::timestamptz ELSE NULL END);
ALTER TABLE "UserPackages" ALTER COLUMN "PurchasedAt" DROP DEFAULT;
ALTER TABLE "UserPackages" ALTER COLUMN "PurchasedAt" TYPE timestamptz USING (CASE WHEN trim("PurchasedAt") ~ '^\\d+$' THEN to_timestamp(trim("PurchasedAt")::bigint) WHEN trim("PurchasedAt") ~ '^\\d{4}-' THEN trim("PurchasedAt")::timestamptz ELSE NULL END);
-- VisitorMessages
ALTER TABLE "VisitorMessages" ALTER COLUMN "CreatedAtUtc" DROP DEFAULT;
ALTER TABLE "VisitorMessages" ALTER COLUMN "CreatedAtUtc" TYPE timestamptz USING (CASE WHEN trim("CreatedAtUtc") ~ '^\\d+$' THEN to_timestamp(trim("CreatedAtUtc")::bigint) WHEN trim("CreatedAtUtc") ~ '^\\d{4}-' THEN trim("CreatedAtUtc")::timestamptz ELSE NULL END);
COMMIT;
