CREATE SEQUENCE IF NOT EXISTS reviews_id_seq;
ALTER TABLE "Reviews" ALTER COLUMN "Id" SET DEFAULT nextval('public.reviews_id_seq');
GRANT USAGE, SELECT ON SEQUENCE public.reviews_id_seq TO sentpazar;

CREATE SEQUENCE IF NOT EXISTS listingmessages_id_seq;
ALTER TABLE "ListingMessages" ALTER COLUMN "Id" SET DEFAULT nextval('public.listingmessages_id_seq');
GRANT USAGE, SELECT ON SEQUENCE public.listingmessages_id_seq TO sentpazar;

CREATE SEQUENCE IF NOT EXISTS listingpromotions_id_seq;
ALTER TABLE "ListingPromotions" ALTER COLUMN "Id" SET DEFAULT nextval('public.listingpromotions_id_seq');
GRANT USAGE, SELECT ON SEQUENCE public.listingpromotions_id_seq TO sentpazar;

CREATE SEQUENCE IF NOT EXISTS userfavorites_id_seq;
ALTER TABLE "UserFavorites" ALTER COLUMN "Id" SET DEFAULT nextval('public.userfavorites_id_seq');
GRANT USAGE, SELECT ON SEQUENCE public.userfavorites_id_seq TO sentpazar;