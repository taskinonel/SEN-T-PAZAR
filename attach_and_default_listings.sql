ALTER SEQUENCE listings_id_seq OWNED BY "Listings"."Id";
ALTER TABLE "Listings" ALTER COLUMN "Id" SET DEFAULT nextval('listings_id_seq');
