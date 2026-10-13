CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;
CREATE TABLE "Movies" (
    "Id" uuid NOT NULL,
    "Title" character varying(200) NOT NULL,
    "Synopsis" text NOT NULL,
    "DurationMinutes" integer NOT NULL,
    "Genre" character varying(50) NOT NULL,
    "ReleaseDate" date NOT NULL,
    "PosterUrl" character varying(500) NOT NULL,
    CONSTRAINT "PK_Movies" PRIMARY KEY ("Id")
);

CREATE TABLE "Rooms" (
    "Id" uuid NOT NULL,
    "Name" character varying(50) NOT NULL,
    "Capacity" integer NOT NULL,
    CONSTRAINT "PK_Rooms" PRIMARY KEY ("Id"),
    CONSTRAINT "CK_Rooms_Capacity" CHECK ("Capacity" > 0)
);

CREATE TABLE "Users" (
    "Id" uuid NOT NULL,
    "Email" character varying(256) NOT NULL,
    "PasswordHash" character varying(256) NOT NULL,
    "FirstName" character varying(100) NOT NULL,
    "LastName" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "Role" character varying(20) NOT NULL,
    CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
);

CREATE TABLE "Screenings" (
    "Id" uuid NOT NULL,
    "StartsAt" timestamp without time zone NOT NULL,
    "Price" numeric(10,2) NOT NULL,
    "MovieId" uuid NOT NULL,
    "RoomId" uuid NOT NULL,
    CONSTRAINT "PK_Screenings" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Screenings_Movies_MovieId" FOREIGN KEY ("MovieId") REFERENCES "Movies" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Screenings_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Seats" (
    "Id" uuid NOT NULL,
    "RoomId" uuid NOT NULL,
    "Row" character varying(5) NOT NULL,
    "Number" integer NOT NULL,
    CONSTRAINT "PK_Seats" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Seats_Rooms_RoomId" FOREIGN KEY ("RoomId") REFERENCES "Rooms" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "Reservations" (
    "Id" uuid NOT NULL,
    "UserId" uuid NOT NULL,
    "ScreeningId" uuid NOT NULL,
    "Status" character varying(20) NOT NULL,
    "CreatedAt" timestamp with time zone NOT NULL,
    "ExpiresAt" timestamp with time zone,
    "TotalAmount" numeric(10,2) NOT NULL,
    CONSTRAINT "PK_Reservations" PRIMARY KEY ("Id"),
    CONSTRAINT "FK_Reservations_Screenings_ScreeningId" FOREIGN KEY ("ScreeningId") REFERENCES "Screenings" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_Reservations_Users_UserId" FOREIGN KEY ("UserId") REFERENCES "Users" ("Id") ON DELETE RESTRICT
);

CREATE TABLE "ReservedSeats" (
    "ReservationId" uuid NOT NULL,
    "SeatId" uuid NOT NULL,
    "ScreeningId" uuid NOT NULL,
    CONSTRAINT "PK_ReservedSeats" PRIMARY KEY ("ReservationId", "SeatId"),
    CONSTRAINT "FK_ReservedSeats_Reservations_ReservationId" FOREIGN KEY ("ReservationId") REFERENCES "Reservations" ("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ReservedSeats_Screenings_ScreeningId" FOREIGN KEY ("ScreeningId") REFERENCES "Screenings" ("Id") ON DELETE RESTRICT,
    CONSTRAINT "FK_ReservedSeats_Seats_SeatId" FOREIGN KEY ("SeatId") REFERENCES "Seats" ("Id") ON DELETE RESTRICT
);

CREATE INDEX "IX_Reservations_ScreeningId" ON "Reservations" ("ScreeningId");

CREATE INDEX "IX_Reservations_UserId" ON "Reservations" ("UserId");

CREATE UNIQUE INDEX "IX_ReservedSeats_ScreeningId_SeatId" ON "ReservedSeats" ("ScreeningId", "SeatId");

CREATE INDEX "IX_ReservedSeats_SeatId" ON "ReservedSeats" ("SeatId");

CREATE UNIQUE INDEX "IX_Rooms_Name" ON "Rooms" ("Name");

CREATE INDEX "IX_Screenings_MovieId" ON "Screenings" ("MovieId");

CREATE INDEX "IX_Screenings_RoomId_StartsAt" ON "Screenings" ("RoomId", "StartsAt");

CREATE UNIQUE INDEX "IX_Seats_RoomId_Row_Number" ON "Seats" ("RoomId", "Row", "Number");

CREATE UNIQUE INDEX "IX_Users_Email" ON "Users" ("Email");

INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20261013202034_InitialSchema', '10.0.12');

COMMIT;

