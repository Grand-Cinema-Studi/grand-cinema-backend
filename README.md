# Grand Cinéma – Backend

API de la plateforme de réservation en ligne du **Grand Cinéma** : un cinéma de 10 salles et 1 700 places qui veut proposer la consultation des séances et la réservation à distance, notamment pendant le Festival de Jazz.

Projet réalisé dans le cadre du Bachelor Développeur d'Application (Studi), Bloc 3 « Développer une solution digitale ».

| Ressource | Lien |
|---|---|
| Dépôt backend (ce dépôt) | https://github.com/Grand-Cinema-Studi/grand-cinema-backend |
| Dépôt frontend | https://github.com/Grand-Cinema-Studi/grand-cinema-frontend |
| Dépôt de documentation (architecture, décisions techniques, règles communes, déploiement) | https://github.com/Grand-Cinema-Studi/grand-cinema-docs |
| Backlog (EPICs, user stories) | https://github.com/orgs/Grand-Cinema-Studi/projects/3 |

## Stack

| Domaine | Choix |
|---|---|
| Langage et framework | C# / .NET 10, ASP.NET Core Minimal API |
| Architecture | Clean Architecture, CQRS sans bibliothèque |
| Base de données | PostgreSQL 16 (Docker en local), EF Core 10 et migrations |
| Authentification | Jeton JWT (`Authorization: Bearer`), mots de passe hachés par PBKDF2 (`PasswordHasher` d'ASP.NET Core), voir l'[ADR 0002](https://github.com/Grand-Cinema-Studi/grand-cinema-docs/blob/main/adr/0002-authentication-jwt.md) |
| Documentation de l'API | OpenAPI (généré par ASP.NET Core) et Swagger UI |
| Paiement | Stripe Checkout en mode test (`Stripe.net`) : la carte est saisie sur la page de Stripe, jamais sur la nôtre ; la réservation n'est confirmée que par la notification signée de Stripe (webhook) |
| Tests | xUnit, Moq, Shouldly, `FakeTimeProvider`, Testcontainers (PostgreSQL), Coverlet |

## Architecture

```text
src/
├── GrandCinema.Domain           entités et énumérations, sans aucune dépendance
├── GrandCinema.Application      cas d'usage (handlers CQRS), DTO, interfaces des repositories
├── GrandCinema.Infrastructure   EF Core, PostgreSQL, migrations, implémentation des repositories
└── GrandCinema.Api              endpoints HTTP, injection de dépendances, Swagger
tests/
├── GrandCinema.Domain.Tests
├── GrandCinema.Application.Tests       tests unitaires des handlers (repository simulé)
├── GrandCinema.Infrastructure.Tests    tests d'intégration sur un vrai PostgreSQL
└── GrandCinema.Api.Tests               tests d'API de bout en bout (requête HTTP réelle, PostgreSQL jetable)
```

Les dépendances vont toujours vers le centre : `Api → Application → Domain`, et `Infrastructure` implémente les interfaces définies dans `Application`. Le métier se teste donc sans base de données ni serveur web.

La couche Application est rangée par fonctionnalité (`Features/<Fonctionnalité>/Queries`, `Commands`, `DTOs`), et l'Api suit le même découpage (`Endpoints/<Fonctionnalité>/`).

## Avancement

| User story | Contenu | État |
|---|---|---|
| US 1.1 | Consulter les films et les horaires | Fait côté API : `GET /api/schedule`, `GET /api/movies`, `GET /api/movies/featured` (films à la une), `GET /api/movies/{id}` (fiche d'un film avec toutes ses séances à venir, réservables jusqu'à un an à l'avance) ; écrans dans le front |
| US 2.1 | Créer un compte et se connecter | Fait (API et front) : `POST /api/users` (avec l'acceptation de la politique de confidentialité), `POST /api/tokens`, `GET /api/users/me`, `DELETE /api/users/me` |
| US 2.2 | Voir les places restantes en temps réel | Fait (API et front) : `remainingSeats` sur chaque séance de `GET /api/schedule` et de `GET /api/movies/{id}` |
| US 2.3 | Réserver un nombre de places et passer au paiement | Fait (API et front) : `POST /api/reservations` (blocage de 10 minutes, sans double vente, au plus 10 places par compte et par séance ; réservé au spectateur depuis le 07/11/2026, `403` pour le personnel), `GET /api/reservations/{id}`, `DELETE /api/reservations/{id}` (annulation avant paiement, places remises en vente aussitôt) ; paiement avec l'US 3.1 |
| US 3.1 | Payer en ligne (Stripe, mode test) | Fait côté API : `POST /api/reservations/{id}/payment` (conditions générales de vente acceptées, page de paiement Stripe), `POST /api/payments/stripe-webhook` (confirmation par notification signée, remboursement d'un paiement arrivé après la libération des places) ; écran dans le front |
| US 3.2 | Recevoir un billet dématérialisé | Fait côté API : code de billet unique (128 bits aléatoires) tiré au paiement confirmé, dans la même mise à jour SQL : aucun billet sans paiement ; `GET /api/reservations/{id}/ticket` (film, séance, salle, places, code) et `GET /api/users/me/tickets` ; le QR code est dessiné par le front, rien n'est stocké |
| US 4.1 | Scanner un billet | Fait côté API : le contrôleur choisit une séance du jour (`GET /api/ticket-checks/screenings`) ; `POST /api/ticket-checks` vérifie le billet et le marque utilisé en une seule instruction SQL : un billet ne laisse entrer qu'une fois, même scanné à deux portes au même instant ; refus explicites (déjà utilisé, autre séance, code inconnu ou illisible) ; compte contrôleur de démonstration |
| US 5.1 | Consulter les réservations au guichet | Fait côté API : `GET /api/reservation-tracking` ouvert au caissier (`Cashier`), en lecture seule ; aucun endpoint d'écriture ni la programmation (`403`) ; compte caissier de démonstration |
| US 5.2 | Gérer la programmation | Fait (API et front) : `GET /api/programming`, `POST /api/programming/screenings`, `PUT` et `DELETE /api/programming/screenings/{id}`, réservés au responsable (`Manager`) ; avertissement quand des places sont prises, salle libre nettoyage compris, séance annulée cachée partout mais gardée |
| US 5.3 | Suivre les réservations | Fait (API et front) : `GET /api/reservation-tracking`, réservé au personnel (responsable, et caissier depuis l'US 5.1) : par séance d'un jour, places vendues en ligne et au guichet, bloquées, capacité et places restantes ; lecture par projection SQL (ADR 0004) |

Les user stories, leurs critères d'acceptation, la Definition of Ready et la Definition of Done sont dans le [dépôt de documentation](https://github.com/Grand-Cinema-Studi/grand-cinema-docs) et sur le [Project GitHub](https://github.com/orgs/Grand-Cinema-Studi/projects/3).

## Endpoints

| Méthode | Route | Rôle |
|---|---|---|
| `GET` | `/api/schedule?date=AAAA-MM-JJ` | Films et séances d'un jour (aujourd'hui si la date est omise), séances pas encore commencées seulement, films triés par titre, séances triées par heure, avec les places restantes de chaque séance (vendues et paiements en cours déduits) |
| `GET` | `/api/movies` | Films à l'affiche (au moins une séance à venir), triés par titre |
| `GET` | `/api/movies/featured` | Films à la une de l'accueil : au plus 4, ceux dont la prochaine séance est la plus proche (aujourd'hui, puis les jours et mois suivants) ; une séance commencée ou annulée ne compte pas |
| `GET` | `/api/movies/{id}` | Un film et toutes ses séances à venir (sans limite de date), regroupées par jour, avec les places restantes ; `404` si le film n'existe pas |
| `POST` | `/api/users` | Création d'un compte spectateur ; `acceptPrivacyPolicy: true` obligatoire (date et version de la politique enregistrées) ; renvoie un jeton d'accès (`201`) |
| `POST` | `/api/tokens` | Connexion : renvoie un jeton d'accès (`200`), ou `401` sans préciser si l'adresse ou le mot de passe est faux |
| `GET` | `/api/users/me` | Profil de l'utilisateur connecté (jeton obligatoire) |
| `DELETE` | `/api/users/me` | Suppression du compte de l'utilisateur connecté (`204`, droit à l'effacement) ; ses réservations sont gardées pour la comptabilité, sans lien avec le compte |
| `POST` | `/api/reservations` | Spectateur seulement (`403` pour le personnel : caissier, contrôleur, responsable) : blocage de 1 à 10 places d'une séance pendant 10 minutes, pour l'utilisateur connecté (au plus 10 par compte et par séance, bloquées ou payées) ; places attribuées par le cinéma ; `409` s'il n'en reste pas assez (y compris pour le second de deux clients sur la dernière place), si la séance a commencé, ou si le compte dépasserait 10 places sur la séance |
| `DELETE` | `/api/reservations/{id}` | Annulation d'un blocage par son propriétaire, avant paiement (`204`) : places remises en vente aussitôt ; `404` pour celui d'un autre, `409` pour une réservation payée |
| `GET` | `/api/reservations/{id}` | Un blocage ou une réservation de l'utilisateur connecté, avec son film, ses places et son statut (`PendingPayment`, `Expired`, `Cancelled`, `Confirmed`) ; `404` pour celle d'un autre |
| `GET` | `/api/reservations/{id}/ticket` | Le billet d'une réservation payée de l'utilisateur connecté (US 3.2) : film, séance, salle, places et code du billet (32 caractères hexadécimaux), que le front dessine en QR code ; `404` `ticket-not-found` pour une réservation non payée, inconnue ou d'un autre |
| `GET` | `/api/users/me/tickets` | Les billets de l'utilisateur connecté (US 3.2) : ses réservations payées, la séance la plus tardive d'abord ; une séance annulée après le paiement est signalée (`screeningCancelled`) |
| `POST` | `/api/reservations/{id}/payment` | Page de paiement Stripe d'un blocage de l'utilisateur connecté (`200`, `checkoutUrl`) ; `acceptTermsOfSale: true` obligatoire (sinon `400`), date et version des conditions générales de vente enregistrées ; `409 hold-expired` après les 10 minutes ou une annulation, `409 reservation-paid` si elle est déjà payée |
| `POST` | `/api/payments/stripe-webhook` | Notification de Stripe, sans jeton : signature (`Stripe-Signature`) vérifiée avant tout, `400` sinon ; un paiement abouti confirme la réservation une seule fois ; un paiement arrivé après la libération des places, ou un second paiement, est remboursé |
| `GET` | `/api/programming?date=AAAA-MM-JJ` | Responsable seulement : toutes les séances du jour (commencées comprises) avec places prises et capacité, le catalogue des films et les salles |
| `POST` | `/api/programming/screenings` | Responsable seulement : création d'une séance (film, salle, heure locale du cinéma sans fuseau, prix) (`201`) ; `400` pour une heure passée, avec fuseau, ou un prix nul ; `409 room-not-available` si la salle est occupée à ce moment, nettoyage de 15 minutes compris |
| `PUT` | `/api/programming/screenings/{id}` | Responsable seulement : nouvelle date, heure ou prix (`204`) ; ni la salle ni le film ne changent ; `409 screening-started`, `409 room-not-available` ; `409 screening-has-reservations` tant que des places sont prises et que la demande n'est pas renvoyée avec `?confirmed=true` |
| `DELETE` | `/api/programming/screenings/{id}` | Responsable seulement : annulation (`204`) ; la séance disparaît de toutes les lectures, ses blocages sont annulés, ses ventes gardées ; même avertissement que la modification |
| `GET` | `/api/reservation-tracking?date=AAAA-MM-JJ` | Responsable ou caissier, en lecture seule : chaque séance du jour (commencées comprises, annulées exclues) avec places vendues en ligne (payées sur Stripe) et au guichet, bloquées par un paiement en cours, capacité et places restantes |
| `GET` | `/api/ticket-checks/screenings?date=AAAA-MM-JJ` | Contrôleur seulement (US 4.1) : les séances d'un jour (aujourd'hui, à l'heure du cinéma, sans `date`), commencées comprises, annulées exclues, par heure puis salle |
| `POST` | `/api/ticket-checks` | Contrôleur seulement (US 4.1) : `screeningId` et `ticketCode` lu dans le QR code ; `200` (film, heure, salle, places) et billet marqué utilisé ; `409` `ticket-already-used`, `409` `ticket-for-another-screening`, `404` `ticket-not-found` (code inconnu, ou QR code d'autre chose) |
| `GET` | `/health` | État de l'application (`Healthy`) |
| `GET` | `/` | Nom de l'API |

Tout endpoint est réservé aux utilisateurs connectés, sauf ceux déclarés publics (programmation, films à l'affiche, films à la une, fiche d'un film, inscription, connexion, notification de Stripe, protégée par sa signature, `/health`). Une route inconnue répond donc `401` à un visiteur sans jeton, et non `404`. La programmation (`/api/programming…`) est en plus réservée au rôle `Manager` : `403` pour tout autre rôle. Dans Swagger, le bouton **Authorize** reçoit le jeton renvoyé par `POST /api/tokens`.

En développement, la documentation interactive est sur `/swagger` et le document OpenAPI sur `/openapi/v1.json`. Swagger est désactivé par défaut et s'active par configuration (`Swagger:Enabled`, ou la variable d'environnement `Swagger__Enabled=true`).

## Lancer le projet en local

### Prérequis

- SDK .NET 10 (la version exacte est fixée par `global.json`) ;
- Docker (Docker Desktop sous Windows et macOS) ;
- Git Bash sous Windows pour les scripts `.sh`.

### 1. Récupérer le dépôt et les outils

```bash
git clone https://github.com/Grand-Cinema-Studi/grand-cinema-backend.git
cd grand-cinema-backend
dotnet tool restore        # installe dotnet-ef, à la version du manifeste dotnet-tools.json
```

### 2. Démarrer PostgreSQL

Créer un fichier `.env` à la racine (il est ignoré par Git) :

```dotenv
POSTGRES_DB=grandcinema
POSTGRES_USER=grandcinema
POSTGRES_PASSWORD=<mot-de-passe>
```

```bash
docker compose up -d
docker compose ps          # le service db doit être « healthy »
```

### 3. Configurer la chaîne de connexion

Elle est stockée dans les secrets utilisateur .NET, hors du dépôt (mêmes valeurs que le `.env`) :

```bash
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=grandcinema;Username=grandcinema;Password=<mot-de-passe>" --project src/GrandCinema.Api
```

Clé de signature des jetons JWT (32 octets au moins), également hors du dépôt :

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/GrandCinema.Api
```

Clés Stripe de **test** (US 3.1), hors du dépôt : la clé secrète `sk_test_…` du tableau de bord Stripe (mode test, Developers → API keys), et le secret des notifications `whsec_…`, affiché par la [Stripe CLI](https://docs.stripe.com/stripe-cli) (`stripe login`, puis la commande `stripe listen` de l'étape 5). L'adresse du front local (`Stripe:FrontBaseUrl`) est dans `appsettings.Development.json`.

```bash
dotnet user-secrets set "Stripe:SecretKey" "<sk_test_…>" --project src/GrandCinema.Api
dotnet user-secrets set "Stripe:WebhookSecret" "<whsec_…>" --project src/GrandCinema.Api
```

### 4. Créer les tables et charger les données de démonstration

```bash
dotnet ef database update --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
bash scripts/reset-demo-screenings.sh
```

Le script charge `docs/database/seed-dev.sql` : 10 salles (1 700 places), 125 films réels (114 à l'affiche dans les semaines à venir, 11 qui ne sont plus programmés), plusieurs semaines de séances (1 à 3 par jour et par film), les places de chaque salle, des ventes au guichet sur les séances de 19 h 30 des 7 prochains jours (une séance complète par jour), quatre séances de films de jazz dans 6 mois et dans un an (réservation longtemps à l'avance) et quatre comptes de démonstration (`manager@grand-cinema.test` / `Manager-Demo-2026!`, `cashier@grand-cinema.test` / `Cashier-Demo-2026!`, `inspector@grand-cinema.test` / `Inspector-Demo-2026!` et `client@grand-cinema.test` / `Client-Demo-2026!`). Les séances sont calculées à partir de la date du jour : relancer le script les remet à jour.

Les affiches, dates de sortie et durées viennent de [TMDB](https://www.themoviedb.org) : avec `TMDB_TOKEN=<jeton>` dans le `.env` (jeton « API Read Access Token » ou clé d'API d'un compte TMDB), `node --env-file=.env scripts/fetch-tmdb-posters.mjs` (Node 20.6 ou plus) écrit `docs/database/seed-posters.sql`, appliqué ensuite par le script de remise à zéro. Ce produit utilise l'API TMDB mais n'est ni approuvé ni certifié par TMDB.

### 5. Lancer l'API

```bash
dotnet run --project src/GrandCinema.Api
```

- Swagger : http://localhost:5073/swagger
- Programmation du jour : http://localhost:5073/api/schedule
- Fiche d'un film (Bird, avec sa séance dans un an) : http://localhost:5073/api/movies/22222222-2222-2222-2222-000000000005

Pour payer en local, dans un autre terminal, la Stripe CLI relaie à l'API les notifications que Stripe ne peut pas envoyer à `localhost` :

```bash
stripe listen --events checkout.session.completed --forward-to http://localhost:5073/api/payments/stripe-webhook
```

Cartes de test : `4242 4242 4242 4242` (paiement accepté), `4000 0000 0000 0002` (carte refusée), avec une date future et un code quelconque. Aucun paiement réel n'est possible avec des clés de test.

## Tests

```bash
dotnet test                                       # tous les tests (Docker doit tourner)
dotnet test --filter "Category!=Integration"      # tests unitaires seuls, sans Docker
dotnet test --filter "US=1.1"                     # tests d'une user story
dotnet test --collect:"XPlat Code Coverage"       # avec mesure de couverture (Coverlet)
```

- Les tests sont écrits en TDD, un test par critère d'acceptation, nommés `When_<situation>_Then_<résultat attendu>` et étiquetés avec le numéro de la story (`[Trait("US", "1.1")]`).
- Les tests d'intégration démarrent un PostgreSQL jetable avec Testcontainers et y appliquent les migrations : ils ne touchent pas à la base de développement.

## Base de données

- Schéma entité-relation, rôle des tables, contraintes : [docs/database/erd.md](docs/database/erd.md)
- Script SQL de création, généré depuis les migrations : [docs/database/schema.sql](docs/database/schema.sql)
- Données de démonstration : [docs/database/seed-dev.sql](docs/database/seed-dev.sql)

La double vente d'une place est empêchée de deux façons : le blocage se fait dans une transaction qui verrouille la séance (`SELECT … FOR UPDATE`), et une contrainte d'unicité sur `(ScreeningId, SeatId)` dans `ReservedSeats` refuse en base toute seconde vente. La confirmation d'un paiement prend le même verrou : elle ne peut pas croiser la libération des places d'un blocage expiré.

Une séance annulée n'est jamais supprimée : elle garde la date de son annulation (`CancelledAt`), et un filtre global d'EF Core la retire de toutes les lectures. Ses ventes restent, pour le suivi, l'analyse et les remboursements ; la réservation d'un spectateur reste lisible (`IgnoreQueryFilters`), et son billet aussi.

Le code d'un billet (`TicketCode`, US 3.2) est écrit par la même instruction `UPDATE` que le passage d'une réservation à `Confirmed`, et seulement si elle attendait son paiement : il n'existe pas de billet sans paiement confirmé. Il est tiré par le générateur cryptographique de .NET (128 bits) et protégé par un index unique ; le QR code ne contient que lui.

Le passage d'un billet à l'entrée (`TicketUsedAt`, US 4.1) est vérifié et enregistré par une seule instruction : `UPDATE … SET "TicketUsedAt" = … WHERE "TicketCode" = … AND "ScreeningId" = … AND "Status" = 'Confirmed' AND "TicketUsedAt" IS NULL`. PostgreSQL verrouille la ligne : de deux scans simultanés du même billet, le second ne trouve plus la ligne « non utilisée » et ne modifie rien. Un test d'intégration lance les deux scans en parallèle.

## Branches et contributions

| Branche | Rôle |
|---|---|
| `main` | production ; alimentée uniquement depuis `dev` ou une branche `hotfix/` |
| `dev` | intégration : c'est la branche `develop` du Git Flow, au nom abrégé |
| `feature/us-<n>-<intitulé>` | une user story (exemple : `feature/us-1-1-view-schedule`) |
| `fix/`, `chore/`, `docs/`, `test/`, `hotfix/` | correctifs, outillage, documentation, tests, correctifs urgents |

`main` et `dev` sont protégées : toute modification passe par une Pull Request. Les messages de commit suivent [Conventional Commits](https://www.conventionalcommits.org/fr/). Détail dans [CONTRIBUTING.md](CONTRIBUTING.md).

## Auteur

Slimane Smahi — Bachelor Développeur d'Application, Studi (2026).
