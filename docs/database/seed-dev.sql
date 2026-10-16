-- Demo data for local development: one room, two movies, screenings today and tomorrow, two demo accounts.
-- Safe to run several times: rows already present are skipped.

INSERT INTO "Rooms" ("Id", "Name", "Capacity")
VALUES ('11111111-1111-1111-1111-111111111111', 'Salle 1', 170)
ON CONFLICT DO NOTHING;

INSERT INTO "Movies" ("Id", "Title", "Synopsis", "DurationMinutes", "Genre", "ReleaseDate", "PosterUrl")
VALUES
    ('22222222-2222-2222-2222-222222222221', 'Blue Note', 'A documentary about the jazz label', 136, 'Documentary', '2023-10-01', 'https://example.com/blue-note.jpg'),
    ('22222222-2222-2222-2222-222222222222', 'Metropolis', 'A futuristic city divided between workers and planners', 153, 'Science fiction', '1927-01-10', 'https://example.com/metropolis.jpg')
ON CONFLICT DO NOTHING;

INSERT INTO "Screenings" ("Id", "MovieId", "RoomId", "StartsAt", "Price")
VALUES
    ('33333333-3333-3333-3333-333333333331', '22222222-2222-2222-2222-222222222221', '11111111-1111-1111-1111-111111111111', CURRENT_DATE + TIME '18:30', 9.00),
    ('33333333-3333-3333-3333-333333333332', '22222222-2222-2222-2222-222222222221', '11111111-1111-1111-1111-111111111111', CURRENT_DATE + TIME '21:00', 9.00),
    ('33333333-3333-3333-3333-333333333333', '22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', CURRENT_DATE + TIME '14:00', 7.50),
    ('33333333-3333-3333-3333-333333333334', '22222222-2222-2222-2222-222222222222', '11111111-1111-1111-1111-111111111111', CURRENT_DATE + 1 + TIME '20:00', 9.00)
ON CONFLICT DO NOTHING;

-- Demo accounts (US 2.1). Passwords, for the demonstration only:
--   manager@grand-cinema.test  /  Manager-Demo-2026!   (role Manager)
--   client@grand-cinema.test   /  Client-Demo-2026!    (role Customer)
-- Hashes produced by ASP.NET Core Identity's PasswordHasher format (v3: PBKDF2, HMAC-SHA512, 100,000 iterations, random salt).
INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FirstName", "LastName", "CreatedAt", "Role")
VALUES
    ('44444444-4444-4444-4444-444444444441', 'manager@grand-cinema.test', 'AQAAAAIAAYagAAAAELa1KX2ovC0OtXLAcj0RNezw1+z35bTvMdcWWzGbf/KDMxe0tQ29/GimwyPWa8hBUg==', 'Paul', 'Durand', NOW(), 'Manager'),
    ('44444444-4444-4444-4444-444444444442', 'client@grand-cinema.test', 'AQAAAAIAAYagAAAAEDpvNboqLNY/zxtSnOafj8FM0tN85JSIlBLpiTysMTkLBR5C2l5qqyxQ9HeESAhqoQ==', 'Léa', 'Martin', NOW(), 'Customer')
ON CONFLICT DO NOTHING;
