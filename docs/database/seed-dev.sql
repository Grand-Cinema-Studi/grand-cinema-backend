-- Demo data for local development and acceptance testing, generated relative to the current date:
-- 10 rooms (1,700 seats), 125 movies (114 showing in the coming weeks, 11 no longer showing),
-- screenings over several weeks (1 to 3 a day per movie), two demo accounts.
-- Safe to run several times. To move the screenings to today's date, run scripts/reset-demo-screenings.sh.
-- Posters, release dates and running times come from TMDB: see scripts/fetch-tmdb-posters.mjs and seed-posters.sql.

INSERT INTO "Rooms" ("Id", "Name", "Capacity")
VALUES
    ('11111111-1111-1111-1111-111111111111', 'Salle 1', 170),
    ('11111111-1111-1111-1111-000000000002', 'Salle 2', 300),
    ('11111111-1111-1111-1111-000000000003', 'Salle 3', 250),
    ('11111111-1111-1111-1111-000000000004', 'Salle 4', 220),
    ('11111111-1111-1111-1111-000000000005', 'Salle 5', 200),
    ('11111111-1111-1111-1111-000000000006', 'Salle 6', 160),
    ('11111111-1111-1111-1111-000000000007', 'Salle 7', 130),
    ('11111111-1111-1111-1111-000000000008', 'Salle 8', 110),
    ('11111111-1111-1111-1111-000000000009', 'Salle 9', 90),
    ('11111111-1111-1111-1111-000000000010', 'Salle 10', 70)
ON CONFLICT ("Id") DO UPDATE SET "Name" = EXCLUDED."Name", "Capacity" = EXCLUDED."Capacity";

-- One movie per line. The trailing comment is read by scripts/fetch-tmdb-posters.mjs: original title (year).
-- ReleaseDate is a placeholder (1 January) and PosterUrl is empty until seed-posters.sql is applied.
INSERT INTO "Movies" ("Id", "Title", "Synopsis", "DurationMinutes", "Genre", "ReleaseDate", "PosterUrl")
VALUES
    ('22222222-2222-2222-2222-222222222221', 'Blue Note Records : Beyond the Notes', 'L''histoire du label de jazz Blue Note, de ses fondateurs exilés à New York jusqu''aux artistes d''aujourd''hui.', 85, 'Documentaire', '2018-01-01', ''), -- tmdb: Blue Note Records: Beyond the Notes (2018)
    ('22222222-2222-2222-2222-222222222222', 'Metropolis', 'Dans une cité futuriste, le fils du maître de la ville découvre le sort des ouvriers qui vivent sous terre.', 153, 'Science-fiction', '1927-01-01', ''), -- tmdb: Metropolis (1927)
    ('22222222-2222-2222-2222-000000000003', 'Whiplash', 'Un jeune batteur de jazz entre au conservatoire le plus réputé du pays et se heurte à un professeur tyrannique.', 107, 'Drame', '2014-01-01', ''), -- tmdb: Whiplash (2014)
    ('22222222-2222-2222-2222-000000000004', 'La La Land', 'À Los Angeles, un pianiste de jazz et une aspirante actrice s''aiment tout en poursuivant chacun leur rêve.', 128, 'Comédie musicale', '2016-01-01', ''), -- tmdb: La La Land (2016)
    ('22222222-2222-2222-2222-000000000005', 'Bird', 'La vie du saxophoniste Charlie Parker, génie du bebop rongé par ses excès.', 161, 'Biopic', '1988-01-01', ''), -- tmdb: Bird (1988)
    ('22222222-2222-2222-2222-000000000006', 'Autour de minuit', 'Dans le Paris des années 1950, un saxophoniste américain vieillissant trouve un ami fidèle chez un jeune admirateur.', 133, 'Drame', '1986-01-01', ''), -- tmdb: 'Round Midnight (1986)
    ('22222222-2222-2222-2222-000000000007', 'Ray', 'Le parcours de Ray Charles, devenu aveugle enfant, jusqu''à la gloire de la soul.', 152, 'Biopic', '2004-01-01', ''), -- tmdb: Ray (2004)
    ('22222222-2222-2222-2222-000000000008', 'Green Book : Sur les routes du sud', 'En 1962, un pianiste virtuose part en tournée dans le Sud ségrégationniste avec un chauffeur italo-américain.', 130, 'Comédie dramatique', '2018-01-01', ''), -- tmdb: Green Book (2018)
    ('22222222-2222-2222-2222-000000000009', 'Soul', 'Un professeur de musique passionné de jazz se retrouve séparé de son corps à la veille du concert de sa vie.', 100, 'Animation', '2020-01-01', ''), -- tmdb: Soul (2020)
    ('22222222-2222-2222-2222-000000000010', 'Le Blues de Ma Rainey', 'Chicago, 1927 : une séance d''enregistrement tendue entre la « mère du blues » et son jeune trompettiste ambitieux.', 94, 'Drame', '2020-01-01', ''), -- tmdb: Ma Rainey's Black Bottom (2020)
    ('22222222-2222-2222-2222-000000000011', 'Cotton Club', 'Dans le Harlem de la Prohibition, musiciens et gangsters se croisent autour d''un célèbre club de jazz.', 127, 'Policier', '1984-01-01', ''), -- tmdb: The Cotton Club (1984)
    ('22222222-2222-2222-2222-000000000012', 'Miles Ahead', 'Retiré de la scène à la fin des années 1970, Miles Davis tente de récupérer un enregistrement inédit.', 100, 'Biopic', '2015-01-01', ''), -- tmdb: Miles Ahead (2015)
    ('22222222-2222-2222-2222-000000000013', 'Born to Be Blue', 'Le trompettiste Chet Baker tente de remonter sur scène après une agression qui a abîmé sa mâchoire.', 97, 'Biopic', '2015-01-01', ''), -- tmdb: Born to Be Blue (2015)
    ('22222222-2222-2222-2222-000000000014', 'Elvis', 'L''ascension d''Elvis Presley, racontée à travers sa relation avec son manager, le colonel Parker.', 159, 'Biopic', '2022-01-01', ''), -- tmdb: Elvis (2022)
    ('22222222-2222-2222-2222-000000000015', 'Bohemian Rhapsody', 'De ses débuts au concert du Live Aid, le parcours de Freddie Mercury et du groupe Queen.', 134, 'Biopic', '2018-01-01', ''), -- tmdb: Bohemian Rhapsody (2018)
    ('22222222-2222-2222-2222-000000000016', 'Les Choristes', 'En 1949, un surveillant d''internat redonne goût à la vie à des enfants difficiles en créant une chorale.', 97, 'Drame', '2004-01-01', ''), -- tmdb: Les Choristes (2004)
    ('22222222-2222-2222-2222-000000000017', 'Amadeus', 'À Vienne, le compositeur Salieri se ronge de jalousie face au génie du jeune Mozart.', 160, 'Biopic', '1984-01-01', ''), -- tmdb: Amadeus (1984)
    ('22222222-2222-2222-2222-000000000018', 'West Side Story', 'À New York, l''amour impossible de Tony et Maria entre deux bandes rivales.', 153, 'Comédie musicale', '1961-01-01', ''), -- tmdb: West Side Story (1961)
    ('22222222-2222-2222-2222-000000000019', 'Les Demoiselles de Rochefort', 'Deux sœurs jumelles rêvent du grand amour et de Paris pendant un week-end de fête à Rochefort.', 120, 'Comédie musicale', '1967-01-01', ''), -- tmdb: Les Demoiselles de Rochefort (1967)
    ('22222222-2222-2222-2222-000000000020', 'Les Parapluies de Cherbourg', 'À Cherbourg, l''amour de Geneviève et Guy est mis à l''épreuve par la guerre d''Algérie.', 91, 'Comédie musicale', '1964-01-01', ''), -- tmdb: Les Parapluies de Cherbourg (1964)
    ('22222222-2222-2222-2222-000000000021', 'Chantons sous la pluie', 'À Hollywood, le passage au cinéma parlant bouleverse la carrière d''une vedette du muet.', 103, 'Comédie musicale', '1952-01-01', ''), -- tmdb: Singin' in the Rain (1952)
    ('22222222-2222-2222-2222-000000000022', 'Parasite', 'Une famille pauvre s''infiltre peu à peu au service d''une famille riche de Séoul.', 132, 'Thriller', '2019-01-01', ''), -- tmdb: Parasite (2019)
    ('22222222-2222-2222-2222-000000000023', 'Intouchables', 'Un aristocrate tétraplégique engage comme aide à domicile un jeune homme de banlieue tout juste sorti de prison.', 112, 'Comédie', '2011-01-01', ''), -- tmdb: Intouchables (2011)
    ('22222222-2222-2222-2222-000000000024', 'Le Fabuleux Destin d''Amélie Poulain', 'À Montmartre, une jeune serveuse timide décide de changer en secret la vie des gens qui l''entourent.', 122, 'Comédie', '2001-01-01', ''), -- tmdb: Le Fabuleux Destin d'Amélie Poulain (2001)
    ('22222222-2222-2222-2222-000000000025', 'Les Évadés', 'Condamné à tort, un banquier noue une amitié durable avec un détenu au fil de longues années de prison.', 142, 'Drame', '1994-01-01', ''), -- tmdb: The Shawshank Redemption (1994)
    ('22222222-2222-2222-2222-000000000026', 'Forrest Gump', 'Un homme simple et généreux traverse sans le vouloir trente ans d''histoire américaine.', 142, 'Drame', '1994-01-01', ''), -- tmdb: Forrest Gump (1994)
    ('22222222-2222-2222-2222-000000000027', 'Pulp Fiction', 'Les destins croisés de tueurs à gages, d''un boxeur et d''un couple de braqueurs à Los Angeles.', 154, 'Policier', '1994-01-01', ''), -- tmdb: Pulp Fiction (1994)
    ('22222222-2222-2222-2222-000000000028', 'Inception', 'Un voleur capable de s''introduire dans les rêves doit, pour une fois, y implanter une idée.', 148, 'Science-fiction', '2010-01-01', ''), -- tmdb: Inception (2010)
    ('22222222-2222-2222-2222-000000000029', 'Interstellar', 'Sur une Terre mourante, un ancien pilote part à travers une faille spatiale chercher une nouvelle planète.', 169, 'Science-fiction', '2014-01-01', ''), -- tmdb: Interstellar (2014)
    ('22222222-2222-2222-2222-000000000030', 'The Dark Knight : Le Chevalier noir', 'Batman affronte le Joker, un criminel imprévisible qui plonge Gotham dans le chaos.', 152, 'Action', '2008-01-01', ''), -- tmdb: The Dark Knight (2008)
    ('22222222-2222-2222-2222-000000000031', 'Gladiator', 'Trahi et réduit en esclavage, un général romain devient gladiateur pour se venger de l''empereur.', 155, 'Aventure', '2000-01-01', ''), -- tmdb: Gladiator (2000)
    ('22222222-2222-2222-2222-000000000032', 'Matrix', 'Un pirate informatique découvre que le monde qu''il connaît n''est qu''une simulation.', 136, 'Science-fiction', '1999-01-01', ''), -- tmdb: The Matrix (1999)
    ('22222222-2222-2222-2222-000000000033', 'Retour vers le futur', 'Propulsé en 1955 par une voiture à remonter le temps, un adolescent doit réunir ses futurs parents.', 116, 'Science-fiction', '1985-01-01', ''), -- tmdb: Back to the Future (1985)
    ('22222222-2222-2222-2222-000000000034', 'Jurassic Park', 'Sur une île, un parc d''attractions peuplé de dinosaures clonés échappe à tout contrôle.', 127, 'Aventure', '1993-01-01', ''), -- tmdb: Jurassic Park (1993)
    ('22222222-2222-2222-2222-000000000035', 'Le Roi lion', 'Chassé de son royaume après la mort de son père, le jeune lion Simba doit un jour reprendre sa place.', 88, 'Animation', '1994-01-01', ''), -- tmdb: The Lion King (1994)
    ('22222222-2222-2222-2222-000000000036', 'Toy Story', 'Un cow-boy en plastique voit d''un mauvais œil l''arrivée d''un nouveau jouet, un astronaute.', 81, 'Animation', '1995-01-01', ''), -- tmdb: Toy Story (1995)
    ('22222222-2222-2222-2222-000000000037', 'Le Voyage de Chihiro', 'Égarée dans un monde d''esprits, une fillette travaille dans des bains publics pour sauver ses parents.', 125, 'Animation', '2001-01-01', ''), -- tmdb: Sen to Chihiro no Kamikakushi (2001)
    ('22222222-2222-2222-2222-000000000038', 'Mon voisin Totoro', 'Deux sœurs installées à la campagne se lient d''amitié avec un grand esprit de la forêt.', 86, 'Animation', '1988-01-01', ''), -- tmdb: Tonari no Totoro (1988)
    ('22222222-2222-2222-2222-000000000039', 'Princesse Mononoké', 'Un jeune prince maudit se retrouve au cœur d''une guerre entre les hommes et les dieux de la forêt.', 134, 'Animation', '1997-01-01', ''), -- tmdb: Mononoke-hime (1997)
    ('22222222-2222-2222-2222-000000000040', 'Le Château ambulant', 'Transformée en vieille femme par un sort, une jeune chapelière trouve refuge chez un magicien.', 119, 'Animation', '2004-01-01', ''), -- tmdb: Hauru no Ugoku Shiro (2004)
    ('22222222-2222-2222-2222-000000000041', 'Ratatouille', 'Un rat doué pour la cuisine aide un jeune commis à devenir chef dans un grand restaurant parisien.', 111, 'Animation', '2007-01-01', ''), -- tmdb: Ratatouille (2007)
    ('22222222-2222-2222-2222-000000000042', 'Là-haut', 'Un veuf grincheux s''envole avec sa maison accrochée à des ballons, un jeune scout caché sur le perron.', 96, 'Animation', '2009-01-01', ''), -- tmdb: Up (2009)
    ('22222222-2222-2222-2222-000000000043', 'Coco', 'Un garçon qui rêve de musique se retrouve au pays des morts le jour de la fête des défunts.', 105, 'Animation', '2017-01-01', ''), -- tmdb: Coco (2017)
    ('22222222-2222-2222-2222-000000000044', 'Vice-versa', 'Dans la tête d''une fillette qui déménage, ses émotions s''affrontent pour la guider.', 95, 'Animation', '2015-01-01', ''), -- tmdb: Inside Out (2015)
    ('22222222-2222-2222-2222-000000000045', 'Vice-versa 2', 'À l''adolescence, de nouvelles émotions, dont l''Anxiété, prennent place dans la tête de Riley.', 96, 'Animation', '2024-01-01', ''), -- tmdb: Inside Out 2 (2024)
    ('22222222-2222-2222-2222-000000000046', 'Spider-Man : New Generation', 'Un adolescent de Brooklyn devient Spider-Man et rencontre ses homologues venus d''autres univers.', 117, 'Animation', '2018-01-01', ''), -- tmdb: Spider-Man: Into the Spider-Verse (2018)
    ('22222222-2222-2222-2222-000000000047', 'Spider-Man : Across the Spider-Verse', 'Miles Morales traverse le multivers et se heurte à une société d''hommes-araignées.', 140, 'Animation', '2023-01-01', ''), -- tmdb: Spider-Man: Across the Spider-Verse (2023)
    ('22222222-2222-2222-2222-000000000048', 'WALL-E', 'Sur une Terre désertée, un petit robot nettoyeur tombe amoureux d''une sonde venue de l''espace.', 98, 'Animation', '2008-01-01', ''), -- tmdb: WALL·E (2008)
    ('22222222-2222-2222-2222-000000000049', 'Les Indestructibles', 'Une famille de super-héros contraints à une vie ordinaire reprend du service.', 115, 'Animation', '2004-01-01', ''), -- tmdb: The Incredibles (2004)
    ('22222222-2222-2222-2222-000000000050', 'Your Name', 'Un lycéen de Tokyo et une lycéenne de la campagne se réveillent régulièrement dans le corps de l''autre.', 106, 'Animation', '2016-01-01', ''), -- tmdb: Kimi no Na wa. (2016)
    ('22222222-2222-2222-2222-000000000051', 'Le Garçon et le Héron', 'Après la mort de sa mère, un garçon suit un mystérieux héron dans un monde fantastique.', 124, 'Animation', '2023-01-01', ''), -- tmdb: Kimitachi wa Dou Ikiru ka (2023)
    ('22222222-2222-2222-2222-000000000052', 'Le Robot sauvage', 'Échouée sur une île, une robot apprend à survivre et adopte un oison orphelin.', 102, 'Animation', '2024-01-01', ''), -- tmdb: The Wild Robot (2024)
    ('22222222-2222-2222-2222-000000000053', 'Flow, le chat qui n''avait plus peur de l''eau', 'Après une grande inondation, un chat solitaire embarque sur un bateau avec d''autres animaux.', 85, 'Animation', '2024-01-01', ''), -- tmdb: Flow (2024)
    ('22222222-2222-2222-2222-000000000054', 'Dune', 'Héritier d''une grande famille, Paul Atréides arrive sur Arrakis, la planète la plus dangereuse de l''univers.', 155, 'Science-fiction', '2021-01-01', ''), -- tmdb: Dune (2021)
    ('22222222-2222-2222-2222-000000000055', 'Dune : Deuxième partie', 'Paul Atréides s''allie aux Fremen pour venger sa famille, au risque de déclencher une guerre sainte.', 166, 'Science-fiction', '2024-01-01', ''), -- tmdb: Dune: Part Two (2024)
    ('22222222-2222-2222-2222-000000000056', 'Top Gun : Maverick', 'Trente ans après, le pilote Maverick forme une nouvelle génération pour une mission périlleuse.', 131, 'Action', '2022-01-01', ''), -- tmdb: Top Gun: Maverick (2022)
    ('22222222-2222-2222-2222-000000000057', 'Everything Everywhere All at Once', 'Une gérante de laverie découvre qu''elle doit sauver le multivers en explorant ses autres vies.', 139, 'Science-fiction', '2022-01-01', ''), -- tmdb: Everything Everywhere All at Once (2022)
    ('22222222-2222-2222-2222-000000000058', 'Mad Max : Fury Road', 'Dans un désert post-apocalyptique, une guerrière fuit un tyran avec ses captives, aidée par Max.', 120, 'Action', '2015-01-01', ''), -- tmdb: Mad Max: Fury Road (2015)
    ('22222222-2222-2222-2222-000000000059', 'Get Out', 'Un jeune homme noir rend visite à la famille de sa petite amie blanche et découvre un terrible secret.', 104, 'Horreur', '2017-01-01', ''), -- tmdb: Get Out (2017)
    ('22222222-2222-2222-2222-000000000060', 'Le Silence des agneaux', 'Une jeune recrue du FBI sollicite l''aide d''un psychiatre cannibale pour arrêter un tueur en série.', 118, 'Thriller', '1991-01-01', ''), -- tmdb: The Silence of the Lambs (1991)
    ('22222222-2222-2222-2222-000000000061', 'Seven', 'Deux inspecteurs traquent un tueur qui s''inspire des sept péchés capitaux.', 127, 'Thriller', '1995-01-01', ''), -- tmdb: Se7en (1995)
    ('22222222-2222-2222-2222-000000000062', 'Fight Club', 'Un employé insomniaque fonde avec un vendeur de savon un club de combat clandestin.', 139, 'Drame', '1999-01-01', ''), -- tmdb: Fight Club (1999)
    ('22222222-2222-2222-2222-000000000063', 'Les Affranchis', 'Trente ans dans la vie d''un petit truand de New York, de l''ascension à la chute.', 146, 'Policier', '1990-01-01', ''), -- tmdb: Goodfellas (1990)
    ('22222222-2222-2222-2222-000000000064', 'Fenêtre sur cour', 'Immobilisé par une jambe cassée, un photographe soupçonne un voisin d''avoir commis un meurtre.', 112, 'Thriller', '1954-01-01', ''), -- tmdb: Rear Window (1954)
    ('22222222-2222-2222-2222-000000000065', 'Les Quatre Cents Coups', 'Incompris par ses parents et ses professeurs, un garçon de Paris multiplie les fugues.', 99, 'Drame', '1959-01-01', ''), -- tmdb: Les Quatre Cents Coups (1959)
    ('22222222-2222-2222-2222-000000000066', 'À bout de souffle', 'Un petit voyou en fuite après le meurtre d''un policier retrouve à Paris une étudiante américaine.', 90, 'Drame', '1960-01-01', ''), -- tmdb: À bout de souffle (1960)
    ('22222222-2222-2222-2222-000000000067', 'La Haine', 'Vingt-quatre heures dans la vie de trois jeunes de banlieue au lendemain d''une émeute.', 98, 'Drame', '1995-01-01', ''), -- tmdb: La Haine (1995)
    ('22222222-2222-2222-2222-000000000068', 'Les Visiteurs', 'Un chevalier du Moyen Âge et son écuyer sont propulsés par erreur à la fin du XXe siècle.', 107, 'Comédie', '1993-01-01', ''), -- tmdb: Les Visiteurs (1993)
    ('22222222-2222-2222-2222-000000000069', 'La Grande Vadrouille', 'En 1942, un chef d''orchestre et un peintre en bâtiment aident des aviateurs anglais à rejoindre la zone libre.', 132, 'Comédie', '1966-01-01', ''), -- tmdb: La Grande Vadrouille (1966)
    ('22222222-2222-2222-2222-000000000070', 'Bienvenue chez les Ch''tis', 'Un directeur de La Poste muté dans le Nord découvre une région bien plus chaleureuse que prévu.', 106, 'Comédie', '2008-01-01', ''), -- tmdb: Bienvenue chez les Ch'tis (2008)
    ('22222222-2222-2222-2222-000000000071', 'Léon', 'Un tueur à gages solitaire recueille une fillette dont la famille vient d''être assassinée.', 110, 'Policier', '1994-01-01', ''), -- tmdb: Léon (1994)
    ('22222222-2222-2222-2222-000000000072', 'Le Cinquième Élément', 'Au XXIIIe siècle, un chauffeur de taxi doit protéger une mystérieuse jeune femme capable de sauver la Terre.', 126, 'Science-fiction', '1997-01-01', ''), -- tmdb: The Fifth Element (1997)
    ('22222222-2222-2222-2222-000000000073', 'Anatomie d''une chute', 'Une romancière est soupçonnée d''avoir tué son mari ; leur fils malvoyant est le seul témoin.', 152, 'Drame', '2023-01-01', ''), -- tmdb: Anatomie d'une chute (2023)
    ('22222222-2222-2222-2222-000000000074', 'Portrait de la jeune fille en feu', 'En 1770, une peintre doit réaliser en secret le portrait de mariage d''une jeune femme qui refuse de poser.', 122, 'Romance', '2019-01-01', ''), -- tmdb: Portrait de la jeune fille en feu (2019)
    ('22222222-2222-2222-2222-000000000075', 'The Artist', 'En 1927, une star du cinéma muet voit sa carrière s''effondrer avec l''arrivée du parlant.', 100, 'Comédie dramatique', '2011-01-01', ''), -- tmdb: The Artist (2011)
    ('22222222-2222-2222-2222-000000000076', 'Un p''tit truc en plus', 'Deux braqueurs en fuite se cachent dans une colonie de vacances pour jeunes adultes en situation de handicap.', 99, 'Comédie', '2024-01-01', ''), -- tmdb: Un p'tit truc en plus (2024)
    ('22222222-2222-2222-2222-000000000077', 'L''Amour ouf', 'Des années 1980 aux années 1990, l''amour fou de Jackie et Clotaire, que la violence va séparer.', 166, 'Romance', '2024-01-01', ''), -- tmdb: L'Amour ouf (2024)
    ('22222222-2222-2222-2222-000000000078', 'Avatar', 'Sur la planète Pandora, un ancien marine paraplégique se mêle au peuple autochtone des Na''vi.', 162, 'Science-fiction', '2009-01-01', ''), -- tmdb: Avatar (2009)
    ('22222222-2222-2222-2222-000000000079', 'E.T. l''extra-terrestre', 'Un garçon recueille un extraterrestre égaré et l''aide à rentrer chez lui.', 115, 'Science-fiction', '1982-01-01', ''), -- tmdb: E.T. the Extra-Terrestrial (1982)
    ('22222222-2222-2222-2222-000000000080', 'Les Dents de la mer', 'Un grand requin blanc sème la terreur dans une station balnéaire au début de l''été.', 124, 'Thriller', '1975-01-01', ''), -- tmdb: Jaws (1975)
    ('22222222-2222-2222-2222-000000000081', 'Alien, le huitième passager', 'L''équipage d''un vaisseau cargo est traqué par une créature extraterrestre.', 117, 'Science-fiction', '1979-01-01', ''), -- tmdb: Alien (1979)
    ('22222222-2222-2222-2222-000000000082', 'Blade Runner', 'En 2019, un policier est chargé de traquer des androïdes devenus fugitifs à Los Angeles.', 117, 'Science-fiction', '1982-01-01', ''), -- tmdb: Blade Runner (1982)
    ('22222222-2222-2222-2222-000000000083', '2001 : l''Odyssée de l''espace', 'De l''aube de l''humanité à une mission vers Jupiter, l''énigme d''un mystérieux monolithe.', 149, 'Science-fiction', '1968-01-01', ''), -- tmdb: 2001: A Space Odyssey (1968)
    ('22222222-2222-2222-2222-000000000084', 'Django Unchained', 'Un esclave affranchi s''associe à un chasseur de primes pour libérer sa femme.', 165, 'Western', '2012-01-01', ''), -- tmdb: Django Unchained (2012)
    ('22222222-2222-2222-2222-000000000085', 'Inglourious Basterds', 'En France occupée, un commando juif et une jeune exploitante de cinéma préparent chacun leur vengeance.', 153, 'Guerre', '2009-01-01', ''), -- tmdb: Inglourious Basterds (2009)
    ('22222222-2222-2222-2222-000000000086', 'Il faut sauver le soldat Ryan', 'Après le Débarquement, un groupe de soldats part à la recherche d''un parachutiste dont les frères sont morts.', 169, 'Guerre', '1998-01-01', ''), -- tmdb: Saving Private Ryan (1998)
    ('22222222-2222-2222-2222-000000000087', '1917', 'Deux soldats britanniques doivent traverser les lignes ennemies pour porter un message vital.', 119, 'Guerre', '2019-01-01', ''), -- tmdb: 1917 (2019)
    ('22222222-2222-2222-2222-000000000088', 'Dunkerque', 'Mai 1940 : sur terre, en mer et dans les airs, l''évacuation des soldats alliés encerclés.', 106, 'Guerre', '2017-01-01', ''), -- tmdb: Dunkirk (2017)
    ('22222222-2222-2222-2222-000000000089', 'Joker', 'À Gotham, un comédien raté et isolé bascule peu à peu dans la folie et la violence.', 122, 'Drame', '2019-01-01', ''), -- tmdb: Joker (2019)
    ('22222222-2222-2222-2222-000000000090', 'The Social Network', 'La création de Facebook et les procès qui ont opposé ses fondateurs.', 120, 'Drame', '2010-01-01', ''), -- tmdb: The Social Network (2010)
    ('22222222-2222-2222-2222-000000000091', 'Premier Contact', 'Une linguiste est chargée de communiquer avec des visiteurs venus d''ailleurs.', 116, 'Science-fiction', '2016-01-01', ''), -- tmdb: Arrival (2016)
    ('22222222-2222-2222-2222-000000000092', 'Her', 'Un homme solitaire tombe amoureux d''un système d''exploitation doté d''une voix et d''une personnalité.', 126, 'Romance', '2013-01-01', ''), -- tmdb: Her (2013)
    ('22222222-2222-2222-2222-000000000093', 'Drive', 'Cascadeur le jour et chauffeur pour des braqueurs la nuit, un homme se met en danger pour protéger sa voisine.', 100, 'Thriller', '2011-01-01', ''), -- tmdb: Drive (2011)
    ('22222222-2222-2222-2222-000000000094', 'Moonlight', 'Trois moments de la vie d''un jeune homme noir de Miami qui cherche sa place.', 111, 'Drame', '2016-01-01', ''), -- tmdb: Moonlight (2016)
    ('22222222-2222-2222-2222-000000000095', 'Les Misérables', 'Un policier nouvellement affecté à Montfermeil découvre les tensions d''une cité.', 104, 'Drame', '2019-01-01', ''), -- tmdb: Les Misérables (2019)
    ('22222222-2222-2222-2222-000000000096', 'Le Pianiste', 'Un pianiste juif polonais survit caché dans le ghetto de Varsovie puis dans la ville en ruines.', 150, 'Drame', '2002-01-01', ''), -- tmdb: The Pianist (2002)
    ('22222222-2222-2222-2222-000000000097', 'The Grand Budapest Hotel', 'Le concierge d''un grand hôtel d''Europe centrale et son jeune groom sont mêlés au vol d''un tableau.', 99, 'Comédie', '2014-01-01', ''), -- tmdb: The Grand Budapest Hotel (2014)
    ('22222222-2222-2222-2222-000000000098', 'Les Aventuriers de l''arche perdue', 'En 1936, l''archéologue Indiana Jones affronte les nazis pour retrouver l''Arche d''alliance.', 115, 'Aventure', '1981-01-01', ''), -- tmdb: Raiders of the Lost Ark (1981)
    ('22222222-2222-2222-2222-000000000099', 'La Guerre des étoiles', 'Un jeune fermier rejoint la rébellion pour sauver une princesse et affronter l''Empire.', 121, 'Science-fiction', '1977-01-01', ''), -- tmdb: Star Wars (1977)
    ('22222222-2222-2222-2222-000000000100', 'L''Empire contre-attaque', 'Pourchassés par l''Empire, les rebelles se dispersent tandis que Luke s''initie à la Force.', 124, 'Science-fiction', '1980-01-01', ''), -- tmdb: The Empire Strikes Back (1980)
    ('22222222-2222-2222-2222-000000000101', 'Paddington 2', 'Accusé à tort d''un vol, l''ourson Paddington se retrouve en prison.', 103, 'Comédie', '2017-01-01', ''), -- tmdb: Paddington 2 (2017)
    ('22222222-2222-2222-2222-000000000102', 'À couteaux tirés', 'Un détective enquête sur la mort d''un célèbre romancier au sein de sa famille.', 130, 'Policier', '2019-01-01', ''), -- tmdb: Knives Out (2019)
    ('22222222-2222-2222-2222-000000000103', 'Le Prestige', 'Dans le Londres victorien, deux magiciens rivaux se livrent une guerre sans merci.', 130, 'Thriller', '2006-01-01', ''), -- tmdb: The Prestige (2006)
    ('22222222-2222-2222-2222-000000000104', 'The Truman Show', 'Un homme découvre que sa vie entière est une émission de télévision.', 103, 'Comédie dramatique', '1998-01-01', ''), -- tmdb: The Truman Show (1998)
    ('22222222-2222-2222-2222-000000000105', 'Wonka', 'Les débuts du jeune Willy Wonka, chocolatier rêveur face à un cartel de confiseurs.', 116, 'Comédie musicale', '2023-01-01', ''), -- tmdb: Wonka (2023)
    ('22222222-2222-2222-2222-000000000106', 'Past Lives', 'Deux amis d''enfance séparés par l''émigration se retrouvent vingt ans plus tard à New York.', 106, 'Romance', '2023-01-01', ''), -- tmdb: Past Lives (2023)
    ('22222222-2222-2222-2222-000000000107', 'La Zone d''intérêt', 'Le quotidien du commandant d''Auschwitz et de sa famille, dans leur maison voisine du camp.', 105, 'Guerre', '2023-01-01', ''), -- tmdb: The Zone of Interest (2023)
    ('22222222-2222-2222-2222-000000000108', 'Perfect Days', 'À Tokyo, un agent d''entretien des toilettes publiques savoure une vie simple et réglée.', 124, 'Drame', '2023-01-01', ''), -- tmdb: Perfect Days (2023)
    ('22222222-2222-2222-2222-000000000109', 'Wicked', 'Au pays d''Oz, l''amitié improbable entre la future Méchante Sorcière de l''Ouest et Glinda.', 160, 'Comédie musicale', '2024-01-01', ''), -- tmdb: Wicked (2024)
    ('22222222-2222-2222-2222-000000000110', 'Emilia Pérez', 'Une avocate mexicaine aide un chef de cartel à disparaître pour devenir la femme qu''il a toujours voulu être.', 132, 'Comédie musicale', '2024-01-01', ''), -- tmdb: Emilia Pérez (2024)
    ('22222222-2222-2222-2222-000000000111', 'Conclave', 'À la mort du pape, un cardinal organise le conclave et découvre des secrets compromettants.', 120, 'Thriller', '2024-01-01', ''), -- tmdb: Conclave (2024)
    ('22222222-2222-2222-2222-000000000112', 'Anora', 'Une jeune strip-teaseuse de Brooklyn épouse sur un coup de tête le fils d''un oligarque russe.', 139, 'Comédie dramatique', '2024-01-01', ''), -- tmdb: Anora (2024)
    ('22222222-2222-2222-2222-000000000113', 'Les Trois Mousquetaires : D''Artagnan', 'Le jeune d''Artagnan rejoint Athos, Porthos et Aramis au cœur d''un complot contre le royaume.', 121, 'Aventure', '2023-01-01', ''), -- tmdb: Les Trois Mousquetaires : D'Artagnan (2023)
    ('22222222-2222-2222-2222-000000000114', 'Le Règne animal', 'Dans un monde où des humains se transforment en animaux, un père cherche sa femme disparue avec leur fils.', 128, 'Fantastique', '2023-01-01', ''), -- tmdb: Le Règne animal (2023)
    ('22222222-2222-2222-2222-000000000115', 'Casablanca', 'Au Maroc pendant la guerre, un patron de bar retrouve la femme qu''il a aimée, accompagnée d''un résistant.', 102, 'Romance', '1942-01-01', ''), -- tmdb: Casablanca (1942)
    ('22222222-2222-2222-2222-000000000116', 'Psychose', 'Une secrétaire en fuite s''arrête dans un motel isolé tenu par un jeune homme étrange.', 109, 'Horreur', '1960-01-01', ''), -- tmdb: Psycho (1960)
    ('22222222-2222-2222-2222-000000000117', 'Les Temps modernes', 'Charlot, ouvrier à la chaîne, se débat dans un monde industriel qui le dépasse.', 87, 'Comédie', '1936-01-01', ''), -- tmdb: Modern Times (1936)
    ('22222222-2222-2222-2222-000000000118', 'Sueurs froides', 'Un policier sujet au vertige est chargé de suivre une femme au comportement étrange.', 128, 'Thriller', '1958-01-01', ''), -- tmdb: Vertigo (1958)
    ('22222222-2222-2222-2222-000000000119', 'Shrek', 'Un ogre bougon part délivrer une princesse pour récupérer la tranquillité de son marais.', 90, 'Animation', '2001-01-01', ''), -- tmdb: Shrek (2001)
    ('22222222-2222-2222-2222-000000000120', 'Barbie', 'Chassée de son monde parfait, Barbie part découvrir le monde réel avec Ken.', 114, 'Comédie', '2023-01-01', ''), -- tmdb: Barbie (2023)
    ('22222222-2222-2222-2222-000000000121', 'Le Dîner de cons', 'Un éditeur qui organise des dîners de cons invite un homme qui va lui gâcher la soirée.', 80, 'Comédie', '1998-01-01', ''), -- tmdb: Le Dîner de cons (1998)
    ('22222222-2222-2222-2222-000000000122', 'Mamma Mia !', 'Sur une île grecque, une jeune femme invite à son mariage les trois hommes qui pourraient être son père.', 108, 'Comédie musicale', '2008-01-01', ''), -- tmdb: Mamma Mia! (2008)
    ('22222222-2222-2222-2222-000000000123', 'Gravity', 'Après la destruction de leur navette, deux astronautes dérivent seuls dans l''espace.', 91, 'Science-fiction', '2013-01-01', ''), -- tmdb: Gravity (2013)
    ('22222222-2222-2222-2222-000000000124', 'Le Comte de Monte-Cristo', 'Accusé à tort et emprisonné, Edmond Dantès s''évade et prépare une vengeance implacable.', 178, 'Aventure', '2024-01-01', ''), -- tmdb: Le Comte de Monte-Cristo (2024)
    ('22222222-2222-2222-2222-000000000125', 'Oppenheimer', 'Le physicien Robert Oppenheimer dirige le projet qui conduit à la première bombe atomique.', 180, 'Biopic', '2023-01-01', '') -- tmdb: Oppenheimer (2023)
ON CONFLICT ("Id") DO UPDATE SET "Title" = EXCLUDED."Title", "Synopsis" = EXCLUDED."Synopsis", "Genre" = EXCLUDED."Genre";

-- Programme: each line puts a movie in one room and one time slot, every day from first_day to last_day
-- (days relative to today; negative = past). weekend_only = Saturday and Sunday only. A room and a time slot
-- never hold two movies on the same day. Screening identifiers start with 33333333- (see the reset script).
INSERT INTO "Screenings" ("Id", "MovieId", "RoomId", "StartsAt", "Price")
SELECT
    ('33333333-' || substr(h, 1, 4) || '-' || substr(h, 5, 4) || '-' || substr(h, 9, 4) || '-' || substr(h, 13, 12))::uuid,
    p.movie_id, p.room_id, x.starts_at, s.price
FROM (VALUES
    ('22222222-2222-2222-2222-000000000125'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 5, -58, -43, false),
    ('22222222-2222-2222-2222-000000000116'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 5, -57, -45, false),
    ('22222222-2222-2222-2222-000000000117'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 3, -52, -38, false),
    ('22222222-2222-2222-2222-000000000117'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 5, -52, -38, false),
    ('22222222-2222-2222-2222-000000000122'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 3, -51, -37, false),
    ('22222222-2222-2222-2222-000000000120'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 4, -38, -25, false),
    ('22222222-2222-2222-2222-000000000120'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 3, -38, -25, false),
    ('22222222-2222-2222-2222-000000000119'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 3, -36, -25, false),
    ('22222222-2222-2222-2222-000000000124'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 5, -35, -20, false),
    ('22222222-2222-2222-2222-000000000123'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 2, -32, -22, false),
    ('22222222-2222-2222-2222-000000000118'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 2, -31, -13, false),
    ('22222222-2222-2222-2222-000000000118'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 1, -31, -13, false),
    ('22222222-2222-2222-2222-000000000121'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 5, -30, -15, false),
    ('22222222-2222-2222-2222-000000000121'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 5, -30, -15, false),
    ('22222222-2222-2222-2222-000000000115'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 3, -19, -3, false),
    ('22222222-2222-2222-2222-000000000115'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 4, -19, -3, false),
    ('22222222-2222-2222-2222-000000000010'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 2, -10, 7, false),
    ('22222222-2222-2222-2222-000000000010'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 1, -10, 7, false),
    ('22222222-2222-2222-2222-000000000108'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 4, -10, 8, false),
    ('22222222-2222-2222-2222-000000000108'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 3, -10, 8, false),
    ('22222222-2222-2222-2222-000000000042'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 1, -8, 6, false),
    ('22222222-2222-2222-2222-000000000042'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 1, -8, 6, false),
    ('22222222-2222-2222-2222-000000000042'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 2, -8, 6, false),
    ('22222222-2222-2222-2222-000000000041'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 5, -7, 7, false),
    ('22222222-2222-2222-2222-000000000041'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 5, -7, 7, false),
    ('22222222-2222-2222-2222-000000000041'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 5, -7, 7, false),
    ('22222222-2222-2222-2222-000000000082'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 4, -7, 3, false),
    ('22222222-2222-2222-2222-000000000035'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 1, -6, 12, false),
    ('22222222-2222-2222-2222-000000000035'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 2, -6, 12, false),
    ('22222222-2222-2222-2222-000000000087'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 1, -6, 4, false),
    ('22222222-2222-2222-2222-000000000111'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 2, -6, 13, false),
    ('22222222-2222-2222-2222-000000000111'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 3, -6, 13, false),
    ('22222222-2222-2222-2222-000000000104'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 3, -5, 4, false),
    ('22222222-2222-2222-2222-000000000104'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 5, -5, 4, false),
    ('22222222-2222-2222-2222-000000000009'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 3, -5, 2, false),
    ('22222222-2222-2222-2222-000000000046'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 2, -5, 2, false),
    ('22222222-2222-2222-2222-000000000098'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 2, -4, 11, false),
    ('22222222-2222-2222-2222-000000000098'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 3, -4, 11, false),
    ('22222222-2222-2222-2222-000000000098'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 5, -4, 11, false),
    ('22222222-2222-2222-2222-000000000018'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 5, -4, 11, false),
    ('22222222-2222-2222-2222-000000000018'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 3, -4, 11, false),
    ('22222222-2222-2222-2222-222222222222'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 3, -3, 10, false),
    ('22222222-2222-2222-2222-222222222221'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 4, -3, 15, false),
    ('22222222-2222-2222-2222-222222222222'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 2, -3, 10, false),
    ('22222222-2222-2222-2222-222222222221'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 4, -3, 15, false),
    ('22222222-2222-2222-2222-000000000089'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 1, -2, 10, false),
    ('22222222-2222-2222-2222-000000000089'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 1, -2, 10, false),
    ('22222222-2222-2222-2222-000000000114'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 5, -2, 8, false),
    ('22222222-2222-2222-2222-000000000067'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 4, -2, 9, false),
    ('22222222-2222-2222-2222-000000000097'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 3, -1, 6, false),
    ('22222222-2222-2222-2222-000000000064'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 5, -1, 9, false),
    ('22222222-2222-2222-2222-000000000064'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 5, -1, 9, false),
    ('22222222-2222-2222-2222-000000000050'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 3, -1, 7, false),
    ('22222222-2222-2222-2222-000000000080'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 4, 0, 6, false),
    ('22222222-2222-2222-2222-000000000091'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 4, 1, 8, false),
    ('22222222-2222-2222-2222-000000000069'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 1, 1, 11, false),
    ('22222222-2222-2222-2222-000000000100'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 3, 2, 15, false),
    ('22222222-2222-2222-2222-000000000100'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 4, 2, 15, false),
    ('22222222-2222-2222-2222-000000000100'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 2, 2, 15, false),
    ('22222222-2222-2222-2222-000000000032'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 2, 2, 13, false),
    ('22222222-2222-2222-2222-000000000032'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 4, 2, 13, false),
    ('22222222-2222-2222-2222-000000000086'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 5, 2, 9, false),
    ('22222222-2222-2222-2222-000000000036'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 2, 3, 11, false),
    ('22222222-2222-2222-2222-000000000036'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 2, 3, 11, false),
    ('22222222-2222-2222-2222-000000000063'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 1, 4, 30, false),
    ('22222222-2222-2222-2222-000000000063'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 4, 4, 30, false),
    ('22222222-2222-2222-2222-000000000063'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 1, 4, 30, false),
    ('22222222-2222-2222-2222-000000000056'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 4, 5, 20, false),
    ('22222222-2222-2222-2222-000000000056'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 1, 5, 20, false),
    ('22222222-2222-2222-2222-000000000034'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 2, 7, 13, false),
    ('22222222-2222-2222-2222-000000000081'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 5, 8, 16, false),
    ('22222222-2222-2222-2222-000000000081'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 5, 8, 16, false),
    ('22222222-2222-2222-2222-000000000109'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 3, 8, 26, false),
    ('22222222-2222-2222-2222-000000000109'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 3, 8, 26, false),
    ('22222222-2222-2222-2222-000000000109'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 4, 8, 26, false),
    ('22222222-2222-2222-2222-000000000011'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 5, 9, 24, false),
    ('22222222-2222-2222-2222-000000000011'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 1, 9, 24, false),
    ('22222222-2222-2222-2222-000000000011'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 4, 9, 24, false),
    ('22222222-2222-2222-2222-000000000084'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 4, 9, 24, false),
    ('22222222-2222-2222-2222-000000000084'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 3, 9, 24, false),
    ('22222222-2222-2222-2222-000000000044'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 1, 10, 17, false),
    ('22222222-2222-2222-2222-000000000038'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 2, 10, 35, false),
    ('22222222-2222-2222-2222-000000000038'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 1, 10, 35, false),
    ('22222222-2222-2222-2222-000000000038'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 3, 10, 35, false),
    ('22222222-2222-2222-2222-000000000074'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 3, 10, 19, false),
    ('22222222-2222-2222-2222-000000000074'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 5, 10, 19, false),
    ('22222222-2222-2222-2222-000000000044'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 4, 10, 17, false),
    ('22222222-2222-2222-2222-000000000024'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 5, 10, 18, false),
    ('22222222-2222-2222-2222-000000000055'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 5, 11, 37, false),
    ('22222222-2222-2222-2222-000000000055'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 5, 11, 37, false),
    ('22222222-2222-2222-2222-000000000055'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 5, 11, 37, false),
    ('22222222-2222-2222-2222-000000000060'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 1, 11, 29, false),
    ('22222222-2222-2222-2222-000000000060'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 1, 11, 29, false),
    ('22222222-2222-2222-2222-000000000020'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 2, 12, 20, false),
    ('22222222-2222-2222-2222-000000000061'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 5, 12, 23, false),
    ('22222222-2222-2222-2222-000000000019'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 1, 13, 20, false),
    ('22222222-2222-2222-2222-000000000102'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 3, 14, 39, false),
    ('22222222-2222-2222-2222-000000000102'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 2, 14, 39, false),
    ('22222222-2222-2222-2222-000000000102'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 2, 14, 39, false),
    ('22222222-2222-2222-2222-000000000014'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 2, 15, 21, false),
    ('22222222-2222-2222-2222-000000000048'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 4, 15, 24, false),
    ('22222222-2222-2222-2222-000000000029'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 5, 16, 28, false),
    ('22222222-2222-2222-2222-000000000101'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 3, 16, 27, false),
    ('22222222-2222-2222-2222-000000000021'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 5, 17, 30, false),
    ('22222222-2222-2222-2222-000000000021'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 4, 17, 30, false),
    ('22222222-2222-2222-2222-000000000021'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 5, 17, 30, false),
    ('22222222-2222-2222-2222-000000000051'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 1, 18, 39, false),
    ('22222222-2222-2222-2222-000000000057'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 4, 18, 31, false),
    ('22222222-2222-2222-2222-000000000051'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 3, 18, 39, false),
    ('22222222-2222-2222-2222-000000000051'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 4, 18, 39, false),
    ('22222222-2222-2222-2222-000000000057'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 2, 18, 31, false),
    ('22222222-2222-2222-2222-000000000017'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 4, 18, 27, false),
    ('22222222-2222-2222-2222-000000000006'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 2, 19, 31, false),
    ('22222222-2222-2222-2222-000000000062'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 2, 20, 29, false),
    ('22222222-2222-2222-2222-000000000062'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 3, 20, 29, false),
    ('22222222-2222-2222-2222-000000000094'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 2, 20, 32, false),
    ('22222222-2222-2222-2222-000000000013'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 3, 20, 28, false),
    ('22222222-2222-2222-2222-000000000031'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 5, 20, 26, false),
    ('22222222-2222-2222-2222-000000000039'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 1, 21, 30, false),
    ('22222222-2222-2222-2222-000000000039'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 2, 21, 30, false),
    ('22222222-2222-2222-2222-000000000105'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 1, 21, 32, false),
    ('22222222-2222-2222-2222-000000000076'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 4, 22, 34, false),
    ('22222222-2222-2222-2222-000000000004'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 2, 22, 28, false),
    ('22222222-2222-2222-2222-000000000071'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 2, 23, 35, false),
    ('22222222-2222-2222-2222-000000000088'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 5, 23, 30, false),
    ('22222222-2222-2222-2222-000000000003'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 5, 24, 34, false),
    ('22222222-2222-2222-2222-000000000012'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 3, 25, 36, false),
    ('22222222-2222-2222-2222-000000000078'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 5, 27, 36, false),
    ('22222222-2222-2222-2222-000000000103'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 3, 28, 38, false),
    ('22222222-2222-2222-2222-000000000005'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 4, 28, 40, false),
    ('22222222-2222-2222-2222-000000000065'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 3, 29, 48, false),
    ('22222222-2222-2222-2222-000000000065'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 4, 29, 48, false),
    ('22222222-2222-2222-2222-000000000033'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 5, 29, 47, false),
    ('22222222-2222-2222-2222-000000000033'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 3, 29, 47, false),
    ('22222222-2222-2222-2222-000000000015'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 1, 29, 41, false),
    ('22222222-2222-2222-2222-000000000059'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 3, 30, 43, false),
    ('22222222-2222-2222-2222-000000000022'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 1, 31, 49, false),
    ('22222222-2222-2222-2222-000000000022'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 4, 31, 49, false),
    ('22222222-2222-2222-2222-000000000049'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 5, 31, 38, false),
    ('22222222-2222-2222-2222-000000000070'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 4, 32, 38, false),
    ('22222222-2222-2222-2222-000000000016'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 5, 32, 48, false),
    ('22222222-2222-2222-2222-000000000016'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 1, 32, 48, false),
    ('22222222-2222-2222-2222-000000000113'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 4, 32, 45, false),
    ('22222222-2222-2222-2222-000000000093'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 1, 33, 46, false),
    ('22222222-2222-2222-2222-000000000025'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 1, 33, 44, false),
    ('22222222-2222-2222-2222-000000000047'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 5, 34, 46, false),
    ('22222222-2222-2222-2222-000000000047'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 5, 34, 46, false),
    ('22222222-2222-2222-2222-000000000028'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 4, 34, 44, false),
    ('22222222-2222-2222-2222-000000000027'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 2, 34, 43, false),
    ('22222222-2222-2222-2222-000000000110'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 4, 35, 41, false),
    ('22222222-2222-2222-2222-000000000072'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 3, 35, 43, false),
    ('22222222-2222-2222-2222-000000000023'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 1, 35, 44, false),
    ('22222222-2222-2222-2222-000000000072'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 2, 35, 43, false),
    ('22222222-2222-2222-2222-000000000066'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 3, 35, 47, false),
    ('22222222-2222-2222-2222-000000000040'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 4, 35, 55, true),
    ('22222222-2222-2222-2222-000000000092'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 5, 35, 42, false),
    ('22222222-2222-2222-2222-000000000026'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 2, 35, 45, false),
    ('22222222-2222-2222-2222-000000000068'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 2, 36, 49, false),
    ('22222222-2222-2222-2222-000000000068'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 1, 36, 49, false),
    ('22222222-2222-2222-2222-000000000053'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 3, 37, 45, false),
    ('22222222-2222-2222-2222-000000000053'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 2, 37, 45, false),
    ('22222222-2222-2222-2222-000000000090'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 2, 37, 48, false),
    ('22222222-2222-2222-2222-000000000083'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 4, 38, 48, false),
    ('22222222-2222-2222-2222-000000000083'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 2, 38, 48, false),
    ('22222222-2222-2222-2222-000000000045'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 5, 38, 46, false),
    ('22222222-2222-2222-2222-000000000085'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 1, 39, 52, false),
    ('22222222-2222-2222-2222-000000000077'::uuid, '11111111-1111-1111-1111-000000000004'::uuid, 5, 40, 48, false),
    ('22222222-2222-2222-2222-000000000077'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 5, 40, 48, false),
    ('22222222-2222-2222-2222-000000000030'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 5, 40, 51, false),
    ('22222222-2222-2222-2222-000000000073'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 3, 40, 53, false),
    ('22222222-2222-2222-2222-000000000099'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 1, 40, 51, false),
    ('22222222-2222-2222-2222-000000000112'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 4, 41, 50, false),
    ('22222222-2222-2222-2222-000000000106'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 3, 42, 49, false),
    ('22222222-2222-2222-2222-000000000043'::uuid, '11111111-1111-1111-1111-000000000008'::uuid, 2, 42, 51, false),
    ('22222222-2222-2222-2222-000000000052'::uuid, '11111111-1111-1111-1111-111111111111'::uuid, 4, 43, 49, false),
    ('22222222-2222-2222-2222-000000000008'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 2, 43, 56, false),
    ('22222222-2222-2222-2222-000000000052'::uuid, '11111111-1111-1111-1111-000000000002'::uuid, 4, 43, 49, false),
    ('22222222-2222-2222-2222-000000000037'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 2, 44, 55, false),
    ('22222222-2222-2222-2222-000000000095'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 3, 44, 56, false),
    ('22222222-2222-2222-2222-000000000107'::uuid, '11111111-1111-1111-1111-000000000003'::uuid, 2, 45, 55, false),
    ('22222222-2222-2222-2222-000000000054'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 1, 46, 54, false),
    ('22222222-2222-2222-2222-000000000007'::uuid, '11111111-1111-1111-1111-000000000009'::uuid, 5, 46, 53, false),
    ('22222222-2222-2222-2222-000000000079'::uuid, '11111111-1111-1111-1111-000000000010'::uuid, 4, 47, 54, false),
    ('22222222-2222-2222-2222-000000000075'::uuid, '11111111-1111-1111-1111-000000000006'::uuid, 5, 48, 54, false),
    ('22222222-2222-2222-2222-000000000096'::uuid, '11111111-1111-1111-1111-000000000007'::uuid, 5, 48, 56, false),
    ('22222222-2222-2222-2222-000000000058'::uuid, '11111111-1111-1111-1111-000000000005'::uuid, 4, 49, 56, false)
) AS p (movie_id, room_id, slot_no, first_day, last_day, weekend_only)
JOIN (VALUES
    (1, TIME '10:30', 7.50),
    (2, TIME '13:30', 9.00),
    (3, TIME '16:30', 9.00),
    (4, TIME '19:30', 11.00),
    (5, TIME '22:30', 11.00)
) AS s (slot_no, starts, price) ON s.slot_no = p.slot_no
CROSS JOIN LATERAL generate_series(p.first_day, p.last_day) AS d (day)
CROSS JOIN LATERAL (SELECT (CURRENT_DATE + d.day) + s.starts AS starts_at) AS x
CROSS JOIN LATERAL (SELECT md5(p.movie_id::text || p.room_id::text || x.starts_at::text) AS h) AS k
WHERE NOT p.weekend_only OR EXTRACT(ISODOW FROM x.starts_at) IN (6, 7)
ON CONFLICT ("Id") DO NOTHING;
-- Demo accounts (US 2.1). Passwords, for the demonstration only:
--   manager@grand-cinema.test  /  Manager-Demo-2026!   (role Manager)
--   client@grand-cinema.test   /  Client-Demo-2026!    (role Customer)
-- Hashes produced by ASP.NET Core Identity's PasswordHasher format (v3: PBKDF2, HMAC-SHA512, 100,000 iterations, random salt).
INSERT INTO "Users" ("Id", "Email", "PasswordHash", "FirstName", "LastName", "CreatedAt", "Role")
VALUES
    ('44444444-4444-4444-4444-444444444441', 'manager@grand-cinema.test', 'AQAAAAIAAYagAAAAELa1KX2ovC0OtXLAcj0RNezw1+z35bTvMdcWWzGbf/KDMxe0tQ29/GimwyPWa8hBUg==', 'Paul', 'Durand', NOW(), 'Manager'),
    ('44444444-4444-4444-4444-444444444442', 'client@grand-cinema.test', 'AQAAAAIAAYagAAAAEDpvNboqLNY/zxtSnOafj8FM0tN85JSIlBLpiTysMTkLBR5C2l5qqyxQ9HeESAhqoQ==', 'Léa', 'Martin', NOW(), 'Customer')
ON CONFLICT DO NOTHING;
