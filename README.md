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
└── GrandCinema.Infrastructure.Tests    tests d'intégration sur un vrai PostgreSQL
```

Les dépendances vont toujours vers le centre : `Api → Application → Domain`, et `Infrastructure` implémente les interfaces définies dans `Application`. Le métier se teste donc sans base de données ni serveur web.

La couche Application est rangée par fonctionnalité (`Features/<Fonctionnalité>/Queries`, `Commands`, `DTOs`), et l'Api suit le même découpage (`Endpoints/<Fonctionnalité>/`).

## Avancement

| User story | Contenu | État |
|---|---|---|
| US 1.1 | Consulter les films et les horaires | Fait (API : `GET /api/schedule`) |
| US 2.1 | Créer un compte et se connecter | Fait (API : `POST /api/users`, `POST /api/tokens`, `GET /api/users/me`) |
| US 2.2 | Voir les places restantes en temps réel | À faire |
| US 2.3 | Réserver un nombre de places et passer au paiement | À faire |
| US 5.2 | Gérer la programmation | À faire |
| US 5.3 | Suivre les réservations | À faire |

Les user stories, leurs critères d'acceptation, la Definition of Ready et la Definition of Done sont dans le [dépôt de documentation](https://github.com/Grand-Cinema-Studi/grand-cinema-docs) et sur le [Project GitHub](https://github.com/orgs/Grand-Cinema-Studi/projects/3).

## Endpoints

| Méthode | Route | Rôle |
|---|---|---|
| `GET` | `/api/schedule?date=AAAA-MM-JJ` | Films et séances d'un jour (aujourd'hui si la date est omise), films triés par titre, séances triées par heure |
| `POST` | `/api/users` | Création d'un compte spectateur ; renvoie un jeton d'accès (`201`) |
| `POST` | `/api/tokens` | Connexion : renvoie un jeton d'accès (`200`), ou `401` sans préciser si l'adresse ou le mot de passe est faux |
| `GET` | `/api/users/me` | Profil de l'utilisateur connecté (jeton obligatoire) |
| `GET` | `/health` | État de l'application (`Healthy`) |
| `GET` | `/` | Nom de l'API |

Tout endpoint est réservé aux utilisateurs connectés, sauf ceux déclarés publics (programmation, inscription, connexion, `/health`). Dans Swagger, le bouton **Authorize** reçoit le jeton renvoyé par `POST /api/tokens`.

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

### 4. Créer les tables et charger les données de démonstration

```bash
dotnet ef database update --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
bash scripts/reset-demo-screenings.sh
```

Le script charge `docs/database/seed-dev.sql` : 10 salles (1 700 places), 125 films réels (114 à l'affiche dans les semaines à venir, 11 qui ne sont plus programmés), plusieurs semaines de séances (1 à 3 par jour et par film) et deux comptes de démonstration (`manager@grand-cinema.test` / `Manager-Demo-2026!` et `client@grand-cinema.test` / `Client-Demo-2026!`). Les séances sont calculées à partir de la date du jour : relancer le script les remet à jour.

Les affiches, dates de sortie et durées viennent de [TMDB](https://www.themoviedb.org) : avec `TMDB_TOKEN=<jeton>` dans le `.env` (jeton « API Read Access Token » ou clé d'API d'un compte TMDB), `node --env-file=.env scripts/fetch-tmdb-posters.mjs` (Node 20.6 ou plus) écrit `docs/database/seed-posters.sql`, appliqué ensuite par le script de remise à zéro. Ce produit utilise l'API TMDB mais n'est ni approuvé ni certifié par TMDB.

### 5. Lancer l'API

```bash
dotnet run --project src/GrandCinema.Api
```

- Swagger : http://localhost:5073/swagger
- Programmation du jour : http://localhost:5073/api/schedule

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

La double vente d'une place est empêchée en base par une contrainte d'unicité sur `(ScreeningId, SeatId)` dans `ReservedSeats`.

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
