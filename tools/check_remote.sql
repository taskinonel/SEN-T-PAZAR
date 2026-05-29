-- Remote DB checks for diagnostics (quoted identifiers)
SELECT 'LOGIN_PROVIDERS' AS tag;
SELECT "LoginProvider", COUNT(*) FROM "AspNetUserLogins" GROUP BY "LoginProvider" ORDER BY COUNT(*) DESC;

SELECT 'GOOGLE_USERS' AS tag;
SELECT u."Id", u."Email", l."LoginProvider", l."ProviderKey", l."ProviderDisplayName"
FROM "AspNetUserLogins" l
JOIN "AspNetUsers" u ON u."Id"=l."UserId"
WHERE l."LoginProvider"='Google'
ORDER BY lower(u."Email");

SELECT 'TASKINONEL_ID' AS tag;
SELECT "Id" FROM "AspNetUsers" WHERE lower("Email")=lower('taskinonel@gmail.com');

SELECT 'LISTINGS_TOTAL' AS tag;
SELECT COUNT(*) FROM "Listings";

SELECT 'LISTINGS_VISIBLE' AS tag;
SELECT COUNT(*) FROM "Listings" WHERE NOT "IsClosed" AND "InSite";

SELECT 'LISTINGS_RECENT' AS tag;
SELECT "Id","Title","UserId","CreatedAt","IsClosed","InSite" FROM "Listings" ORDER BY "CreatedAt" DESC LIMIT 50;

SELECT 'LISTINGIMAGES_COUNT' AS tag;
SELECT COUNT(*) FROM "ListingImages";

SELECT 'LISTINGIMAGES_SAMPLE' AS tag;
SELECT "Id","ListingId","FilePath","UserId" FROM "ListingImages" ORDER BY "ListingId" LIMIT 100;
