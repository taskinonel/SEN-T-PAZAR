SELECT column_name, data_type, column_default
FROM information_schema.columns
WHERE table_name='Listings' AND is_nullable='NO'
ORDER BY ordinal_position;