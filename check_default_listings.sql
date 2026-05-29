SELECT column_default FROM information_schema.columns WHERE table_name='Listings' AND column_name='Id';
SELECT nextval('listings_id_seq');
