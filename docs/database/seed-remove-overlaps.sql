-- Run after seed-dev.sql and seed-posters.sql (scripts/reset-demo-screenings.sh, deploy/reset-demo-data.sh).
-- seed-posters.sql gives the running times of TMDB, sometimes longer than those of seed-dev.sql (Amadeus: 160, then 180 minutes):
-- a movie may then overlap the next demo screening of its room (bug 0002). The rule of the API applies here too
-- (US 5.2: a room is busy from the start of a screening to the end of its movie, plus 15 minutes of cleaning):
-- the later demo screening is removed, with its demo sale. Screenings created in the programming screen are never removed.
CREATE TEMP TABLE overlapping_demo_screenings AS
SELECT DISTINCT later."Id"
FROM "Screenings" later
JOIN "Screenings" earlier
    ON earlier."RoomId" = later."RoomId"
   AND earlier."Id" <> later."Id"
   AND earlier."CancelledAt" IS NULL
   AND earlier."StartsAt" <= later."StartsAt"
JOIN "Movies" m ON m."Id" = earlier."MovieId"
WHERE later."Id"::text LIKE '33333333-%'
  AND later."CancelledAt" IS NULL
  AND later."StartsAt" < earlier."StartsAt" + make_interval(mins => m."DurationMinutes" + 15)
  AND (earlier."StartsAt" < later."StartsAt" OR earlier."Id" < later."Id");

DELETE FROM "Reservations" WHERE "ScreeningId" IN (SELECT "Id" FROM overlapping_demo_screenings);
DELETE FROM "Screenings" WHERE "Id" IN (SELECT "Id" FROM overlapping_demo_screenings);
DROP TABLE overlapping_demo_screenings;
