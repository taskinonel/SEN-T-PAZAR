BEGIN;
INSERT INTO "ListingImages" ("Id","FilePath", "UserId", "ListingId")
SELECT (COALESCE((SELECT MAX("Id") FROM "ListingImages"),0)+1), '/uploads/8fe0becb012c46a186e6ed216f2b5392.jpg', NULL, 22
WHERE NOT EXISTS (SELECT 1 FROM "ListingImages" WHERE "FilePath" = '/uploads/8fe0becb012c46a186e6ed216f2b5392.jpg');

INSERT INTO "ListingImages" ("Id","FilePath", "UserId", "ListingId")
SELECT (COALESCE((SELECT MAX("Id") FROM "ListingImages"),0)+1), '/uploads/621bef4f-e3cf-4521-a894-136aa1b676df.jpg', NULL, 22
WHERE NOT EXISTS (SELECT 1 FROM "ListingImages" WHERE "FilePath" = '/uploads/621bef4f-e3cf-4521-a894-136aa1b676df.jpg');

INSERT INTO "ListingImages" ("Id","FilePath", "UserId", "ListingId")
SELECT (COALESCE((SELECT MAX("Id") FROM "ListingImages"),0)+1), '/uploads/69c9ca5d3ce3482a84d8dafcc84857d0.jpg', NULL, 22
WHERE NOT EXISTS (SELECT 1 FROM "ListingImages" WHERE "FilePath" = '/uploads/69c9ca5d3ce3482a84d8dafcc84857d0.jpg');
COMMIT;
