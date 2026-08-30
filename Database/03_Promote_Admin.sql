/* ============================================================
   Portfolio Builder - Script 03: Promote a user to admin
   Run AFTER you have registered your own account through the
   /create-user API (so the password is hashed correctly).

   There is intentionally NO "register as admin" endpoint:
   admin is granted here, in the DB, by you.
   ============================================================ */

USE [Portfolio_Builder];
GO

-- Change this to YOUR username (the account you registered).
DECLARE @AdminUsername NVARCHAR(39) = N'junaidjaved';

UPDATE dbo.Users
SET Role = N'admin'
WHERE Username = @AdminUsername;

-- Confirm it worked
SELECT Username, Email, Role
FROM dbo.Users
WHERE Username = @AdminUsername;
GO
