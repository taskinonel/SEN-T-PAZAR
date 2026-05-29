CREATE SEQUENCE IF NOT EXISTS listings_id_seq;
SELECT setval('listings_id_seq', COALESCE((SELECT MAX("Id") FROM "Listings"),0));
ALTER SEQUENCE listings_id_seq OWNER TO sentpazar;
