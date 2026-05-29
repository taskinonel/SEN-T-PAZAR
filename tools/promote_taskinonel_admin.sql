WITH target_user AS (
  SELECT "Id" AS user_id, "Email" AS email
  FROM "AspNetUsers"
  WHERE lower("Email") = lower('taskinonel@gmail.com')
  LIMIT 1
), admin_role AS (
  SELECT "Id" AS role_id, "Name" AS name
  FROM "AspNetRoles"
  WHERE upper("NormalizedName") = 'ADMIN'
     OR upper("Name") = 'ADMIN'
  LIMIT 1
), inserted AS (
  INSERT INTO "AspNetUserRoles" ("UserId", "RoleId")
  SELECT u.user_id, r.role_id
  FROM target_user u
  CROSS JOIN admin_role r
  WHERE NOT EXISTS (
    SELECT 1 FROM "AspNetUserRoles" ur
    WHERE ur."UserId" = u.user_id
      AND ur."RoleId" = r.role_id
  )
  RETURNING "UserId", "RoleId"
)
SELECT
  (SELECT email FROM target_user) AS user_email,
  (SELECT user_id FROM target_user) AS user_id,
  (SELECT name FROM admin_role) AS role_name,
  (SELECT role_id FROM admin_role) AS role_id,
  (SELECT count(*) FROM inserted) AS inserted_count;

SELECT r."Name" AS role
FROM "AspNetUsers" u
JOIN "AspNetUserRoles" ur ON ur."UserId" = u."Id"
JOIN "AspNetRoles" r ON r."Id" = ur."RoleId"
WHERE lower(u."Email") = lower('taskinonel@gmail.com')
ORDER BY r."Name";
