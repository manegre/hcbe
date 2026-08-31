-- Migration pour ajouter la table TeamMembers
CREATE TABLE IF NOT EXISTS "TeamMembers" (
    "Id" TEXT NOT NULL CONSTRAINT "PK_TeamMembers" PRIMARY KEY,
    "Name" TEXT NOT NULL,
    "Position" TEXT NOT NULL,
    "Region" TEXT NOT NULL,
    "Zone" TEXT NOT NULL,
    "Photo" TEXT NULL,
    "Bio" TEXT NULL,
    "Email" TEXT NULL,
    "IsActive" INTEGER NOT NULL,
    "Order" INTEGER NOT NULL,
    "CreatedAt" TEXT NOT NULL,
    "UpdatedAt" TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS "IX_TeamMembers_Order" ON "TeamMembers" ("Order");
CREATE INDEX IF NOT EXISTS "IX_TeamMembers_IsActive" ON "TeamMembers" ("IsActive");
