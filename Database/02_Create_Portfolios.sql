/* ============================================================
   Portfolio Builder - Database-first schema
   Script 02: Create Portfolios table
   Run AFTER 01_Create_Users.sql (it needs dbo.Users to exist).

   A Portfolio is the PUBLIC-FACING content for one user.
   Relationship: 1 User  ->  0..1 Portfolio  (linked by Username).
   NOTE: this table is for the NEXT phase (CV -> AI -> portfolio).
   Creating it now so the schema is complete; the API will start
   by only touching Users.
   ============================================================ */

USE [Portfolio_Builder];
GO

IF OBJECT_ID(N'dbo.Portfolios', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.Portfolios
    (
        Id            UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT DF_Portfolios_Id DEFAULT (NEWSEQUENTIALID()),

        -- Links to the owning account. Username is UNIQUE in Users,
        -- so it is safe to reference and it is what the URL uses.
        Username      NVARCHAR(39)   NOT NULL,

        -- Denormalized "First Last" for quick display in the admin list.
        -- (Kept in sync in code when the user edits their name.)
        FullName      NVARCHAR(201)  NOT NULL,

        -- e.g. https://junaidjavedjj.com/PortfolioBuilder/<username>
        PortfolioLink NVARCHAR(256)  NOT NULL,

        CreatedAt     DATETIME2(3)   NOT NULL
            CONSTRAINT DF_Portfolios_CreatedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_Portfolios PRIMARY KEY (Id),

        -- One portfolio per user, and each link is unique.
        CONSTRAINT UQ_Portfolios_Username      UNIQUE (Username),
        CONSTRAINT UQ_Portfolios_PortfolioLink UNIQUE (PortfolioLink),

        -- Enforce the User <-> Portfolio link at the DB level.
        -- If a user is deleted, their portfolio goes with them.
        CONSTRAINT FK_Portfolios_Users_Username
            FOREIGN KEY (Username) REFERENCES dbo.Users (Username)
            ON DELETE CASCADE ON UPDATE CASCADE
    );
END
GO
