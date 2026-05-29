SELECT "Id","Email","EmailConfirmed" FROM "AspNetUsers" WHERE lower("Email")=lower('taskinonel@gmail.com');
SELECT "LoginProvider","ProviderDisplayName","ProviderKey" FROM "AspNetUserLogins" WHERE "UserId"=(SELECT "Id" FROM "AspNetUsers" WHERE lower("Email")=lower('taskinonel@gmail.com')) LIMIT 50;
