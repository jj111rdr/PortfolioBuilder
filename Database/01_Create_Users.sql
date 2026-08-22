/* ============================================================
   Portfolio Builder - Database-first schema
   Script 01: Create database + Users table
   Run this in SSMS against your SQL Server (the 'JJ' or
   'localhost\SQLEXPRESS01' instance you used before).
   After running, scaffold the EF model from this DB.
   ============================================================ */

-- 1. Create the database
IF DB_ID(N'Portfolio_Builder') IS NULL
BEGIN
    CREATE DATABASE [Portfolio_Builder];
END
GO

USE [Portfolio_Builder];
GO

-- 2. Users table = the ACCOUNT only. Portfolio content comes later
--    in its own table (Portfolios), 1 user -> 1 portfolio.
IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Users
    (
        Id                     UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Users_Id DEFAULT (NEWSEQUENTIALID()),

        FirstName              NVARCHAR(100)  NOT NULL,
        LastName               NVARCHAR(100)  NOT NULL,
        DateOfBirth            DATE           NOT NULL,   -- store DOB, compute age in code
        Gender                 NVARCHAR(20)   NOT NULL,
        Email                  NVARCHAR(256)  NOT NULL,
        Username               NVARCHAR(39)   NOT NULL,   -- 39 = GitHub max length

        PasswordHash           NVARCHAR(MAX)  NOT NULL,   -- PasswordHasher<User> output; never the raw password
        Role                   NVARCHAR(20)   NOT NULL
            CONSTRAINT DF_Users_Role DEFAULT (N'user'),

        -- Refresh-token flow, same as your prior JWT project
        RefreshToken           NVARCHAR(MAX)  NULL,
        RefreshTokenExpireTime DATETIME2(3)   NULL,

        CreatedAt              DATETIME2(3)   NOT NULL
            CONSTRAINT DF_Users_CreatedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Users PRIMARY KEY (Id),

        -- Both must be unique (login + public portfolio URL)
        CONSTRAINT UQ_Users_Email    UNIQUE (Email),
        CONSTRAINT UQ_Users_Username UNIQUE (Username),

        -- Only the 3 allowed genders
        CONSTRAINT CK_Users_Gender CHECK (Gender IN (N'Male', N'Female', N'Prefer not to say')),

        -- Only two roles in the whole system
        CONSTRAINT CK_Users_Role CHECK (Role IN (N'user', N'admin')),

        /* GitHub-style username rule enforced at the DB level as a safety net
           (the API will validate too, with a nicer message):
             - 1 to 39 characters
             - letters, digits, single hyphens only
             - cannot start or end with a hyphen
             - no consecutive hyphens
           Case-insensitivity for uniqueness is handled below. */
        CONSTRAINT CK_Users_Username_Format CHECK
        (
            Username NOT LIKE N'%[^0-9A-Za-z-]%'   -- only alnum + hyphen
            AND Username NOT LIKE N'-%'            -- no leading hyphen
            AND Username NOT LIKE N'%-'            -- no trailing hyphen
            AND Username NOT LIKE N'%--%'          -- no double hyphen
            AND LEN(Username) >= 1
        )
    );
END
GO

/* ------------------------------------------------------------
   Case-insensitive uniqueness:
   Default SQL Server collation is already case-INsensitive, so
   'Junaid' and 'junaid' collide on the UNIQUE constraints above.
   That is what we want for both Email and Username. No extra work
   needed unless your server uses a case-sensitive collation.
   ------------------------------------------------------------ */
GO
