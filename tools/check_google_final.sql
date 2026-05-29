SELECT id,email,emailconfirmed FROM "AspNetUsers" WHERE lower(email)=lower('taskinonel@gmail.com');
SELECT loginprovider,providername,providerkey FROM "AspNetUserLogins" WHERE userid=(SELECT id FROM "AspNetUsers" WHERE lower(email)=lower('taskinonel@gmail.com')) LIMIT 50;
