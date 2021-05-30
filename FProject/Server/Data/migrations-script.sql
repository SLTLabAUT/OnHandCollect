﻿CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

CREATE TABLE "AspNetRoles" (
    "Id" text NOT NULL,
    "Name" character varying(256) NULL,
    "NormalizedName" character varying(256) NULL,
    "ConcurrencyStamp" text NULL,
    CONSTRAINT "PK_AspNetRoles" PRIMARY KEY ("Id")
);

CREATE TABLE "AspNetUsers" (
    "Id" text NOT NULL,
    "UserName" character varying(256) NULL,
    "NormalizedUserName" character varying(256) NULL,
    "Email" character varying(256) NULL,
    "NormalizedEmail" character varying(256) NULL,
    "EmailConfirmed" boolean NOT NULL,
    "PasswordHash" text NULL,
    "SecurityStamp" text NULL,
    "ConcurrencyStamp" text NULL,
    "PhoneNumber" text NULL,
    "PhoneNumberConfirmed" boolean NOT NULL,
    "TwoFactorEnabled" boolean NOT NULL,
    "LockoutEnd" timestamp with time zone NULL,
    "LockoutEnabled" boolean NOT NULL,
    "AccessFailedCount" integer NOT NULL,
    CONSTRAINT "PK_AspNetUsers" PRIMARY KEY ("Id")
);

CREATE TABLE "DeviceCodes" (
    "UserCode" character varying(200) NOT NULL,
    "DeviceCode" character varying(200) NOT NULL,
    "SubjectId" character varying(200) NULL,
    "SessionId" character varying(100) NULL,
    "ClientId" character varying(200) NOT NULL,
    "Description" character varying(200) NULL,
    "CreationTime" timestamp without time zone NOT NULL,
    "Expiration" timestamp without time zone NOT NULL,
    "Data" character varying(50000) NOT NULL,
    CONSTRAINT "PK_DeviceCodes" PRIMARY KEY ("UserCode")
);

CREATE TABLE "PersistedGrants" (
    "Key" character varying(200) NOT NULL,
    "Type" character varying(50) NOT NULL,
    "SubjectId" character varying(200) NULL,
    "SessionId" character varying(100) NULL,
    "ClientId" character varying(200) NOT NULL,
    "Description" character varying(200) NULL,
    "CreationTime" timestamp without time zone NOT NULL,
    "Expiration" timestamp without time zone NULL,
    "ConsumedTime" timestamp without time zone NULL,
    "Data" character varying(50000) NOT NULL,
    CONSTRAINT "PK_PersistedGrants" PRIMARY KEY ("Key")
);

CREATE TABLE "Text" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "Type" integer NOT NULL,
    "Content" text NULL,
    CONSTRAINT "PK_Text" PRIMARY KEY ("Id")
);

CREATE TABLE "AspNetRoleClaims" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "RoleId" text NOT NULL,
    "ClaimType" text NULL,
    "ClaimValue" text NULL,
    CONSTRAINT "PK_AspNetRoleClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetRoleClaims_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserClaims" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "UserId" text NOT NULL,
    "ClaimType" text NULL,
    "ClaimValue" text NULL,
    CONSTRAINT "PK_AspNetUserClaims" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_AspNetUserClaims_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserLogins" (
    "LoginProvider" character varying(128) NOT NULL,
    "ProviderKey" character varying(128) NOT NULL,
    "ProviderDisplayName" text NULL,
    "UserId" text NOT NULL,
    CONSTRAINT "PK_AspNetUserLogins" PRIMARY KEY ("LoginProvider", "ProviderKey"),
    CONSTRAINT "FK_AspNetUserLogins_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserRoles" (
    "UserId" text NOT NULL,
    "RoleId" text NOT NULL,
    CONSTRAINT "PK_AspNetUserRoles" PRIMARY KEY ("UserId", "RoleId"),
    CONSTRAINT "FK_AspNetUserRoles_AspNetRoles_RoleId" FOREIGN KEY ("RoleId") REFERENCES "AspNetRoles" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_AspNetUserRoles_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "AspNetUserTokens" (
    "UserId" text NOT NULL,
    "LoginProvider" character varying(128) NOT NULL,
    "Name" character varying(128) NOT NULL,
    "Value" text NULL,
    CONSTRAINT "PK_AspNetUserTokens" PRIMARY KEY ("UserId", "LoginProvider", "Name"),
    CONSTRAINT "FK_AspNetUserTokens_AspNetUsers_UserId" FOREIGN KEY ("UserId") REFERENCES "AspNetUsers" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Writepads" (
    "Id" integer NOT NULL GENERATED BY DEFAULT AS IDENTITY,
    "IsDeleted" boolean NOT NULL,
    "PointerType" integer NOT NULL,
    "LastModified" timestamp with time zone NOT NULL,
    "Status" integer NOT NULL,
    "TextId" integer NOT NULL,
    "OwnerId" text NULL,
    CONSTRAINT "PK_Writepads" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Writepads_AspNetUsers_OwnerId" FOREIGN KEY ("OwnerId") REFERENCES "AspNetUsers" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Writepads_Text_TextId" FOREIGN KEY ("TextId") REFERENCES "Text" ("Id") ON DELETE CASCADE
);

CREATE TABLE "Points" (
    "WritepadId" integer NOT NULL,
    "Number" integer NOT NULL,
    "IsDeleted" boolean NOT NULL,
    "Type" integer NOT NULL,
    "TimeStamp" double precision NOT NULL,
    "X" real NOT NULL,
    "Y" real NOT NULL,
    "Width" real NOT NULL,
    "Height" real NOT NULL,
    "Pressure" real NOT NULL,
    "TangentialPressure" real NOT NULL,
    "TiltX" real NOT NULL,
    "TiltY" real NOT NULL,
    "Twist" smallint NOT NULL,
    CONSTRAINT "PK_Points" PRIMARY KEY ("WritepadId", "Number"),
    CONSTRAINT "FK_Points_Writepads_WritepadId" FOREIGN KEY ("WritepadId") REFERENCES "Writepads" ("Id") ON DELETE CASCADE
);

CREATE INDEX "IX_AspNetRoleClaims_RoleId" ON "AspNetRoleClaims" ("RoleId");

CREATE UNIQUE INDEX "RoleNameIndex" ON "AspNetRoles" ("NormalizedName");

CREATE INDEX "IX_AspNetUserClaims_UserId" ON "AspNetUserClaims" ("UserId");

CREATE INDEX "IX_AspNetUserLogins_UserId" ON "AspNetUserLogins" ("UserId");

CREATE INDEX "IX_AspNetUserRoles_RoleId" ON "AspNetUserRoles" ("RoleId");

CREATE INDEX "EmailIndex" ON "AspNetUsers" ("NormalizedEmail");

CREATE UNIQUE INDEX "UserNameIndex" ON "AspNetUsers" ("NormalizedUserName");

CREATE UNIQUE INDEX "IX_DeviceCodes_DeviceCode" ON "DeviceCodes" ("DeviceCode");

CREATE INDEX "IX_DeviceCodes_Expiration" ON "DeviceCodes" ("Expiration");

CREATE INDEX "IX_PersistedGrants_Expiration" ON "PersistedGrants" ("Expiration");

CREATE INDEX "IX_PersistedGrants_SubjectId_ClientId_Type" ON "PersistedGrants" ("SubjectId", "ClientId", "Type");

CREATE INDEX "IX_PersistedGrants_SubjectId_SessionId_Type" ON "PersistedGrants" ("SubjectId", "SessionId", "Type");

CREATE INDEX "IX_Writepads_OwnerId" ON "Writepads" ("OwnerId");

CREATE INDEX "IX_Writepads_TextId" ON "Writepads" ("TextId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210228103453_Initial', '5.0.6');

COMMIT;

START TRANSACTION;

INSERT INTO "AspNetRoles" ("Id", "ConcurrencyStamp", "Name", "NormalizedName")
VALUES ('afc9f911-04ae-4bc2-88a6-900ce65eca92', 'd3198c4c-4dd9-4d8c-8d35-e76757529aac', 'User', 'USER');
INSERT INTO "AspNetRoles" ("Id", "ConcurrencyStamp", "Name", "NormalizedName")
VALUES ('1c6b33d2-a1d8-42fa-924b-43449867f115', 'c0a582f7-49de-43f6-9314-d24b0879ce22', 'Admin', 'ADMIN');

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210228162153_IdentityRole', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "AspNetUsers" ADD "BirthDate" timestamp without time zone NULL;

ALTER TABLE "AspNetUsers" ADD "Sex" integer NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210312222610_UserUpdate', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "Text" ADD "WordCount" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210318232825_TextWordCount', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "Text" DROP COLUMN "Type";

ALTER TABLE "Writepads" ADD "Type" integer NOT NULL DEFAULT 0;

ALTER TABLE "Text" ADD "Rank" real NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210319130338_TextTypeRefactor', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "Text" RENAME COLUMN "Rank" TO "Rarity";

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210319145635_TextRankRename', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "Writepads" ADD "UserSpecifiedNumber" integer NOT NULL DEFAULT 0;

CREATE INDEX "IX_Writepads_UserSpecifiedNumber_OwnerId" ON "Writepads" ("UserSpecifiedNumber", "OwnerId");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210319220441_UserSpecifiedNumber', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "Writepads" DROP CONSTRAINT "FK_Writepads_Text_TextId";

ALTER TABLE "Writepads" ALTER COLUMN "TextId" DROP NOT NULL;

ALTER TABLE "Writepads" ADD CONSTRAINT "FK_Writepads_Text_TextId" FOREIGN KEY ("TextId") REFERENCES "Text" ("Id") ON DELETE RESTRICT;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210517182016_OptionalText', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "AspNetUsers" DROP COLUMN "BirthDate";

ALTER TABLE "Writepads" ADD "LastCheck" timestamp with time zone NULL;

ALTER TABLE "AspNetUsers" ADD "BirthYear" smallint NULL;

ALTER TABLE "AspNetUsers" ADD "Education" integer NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210525142152_UserAndWritepadStatus', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "Text" ADD "Type" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210527142018_TextType', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "AspNetUsers" ADD "Handedness" integer NOT NULL DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210530023534_Handedness', '5.0.6');

COMMIT;

START TRANSACTION;

ALTER TABLE "Writepads" ADD "Hand" integer NULL;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210530051306_Hand', '5.0.6');

COMMIT;

START TRANSACTION;

UPDATE "Writepads" SET "Hand" = 0
WHERE "Hand" IS NULL;

ALTER TABLE "Writepads" ALTER COLUMN "Hand" SET NOT NULL;
ALTER TABLE "Writepads" ALTER COLUMN "Hand" SET DEFAULT 0;

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20210530062438_HandRequired', '5.0.6');

COMMIT;

