-- list non-nullable columns for Listings
SELECT column_name, is_nullable, column_default
FROM information_schema.columns
WHERE table_name='Listings' AND is_nullable='NO'
ORDER BY ordinal_position;
