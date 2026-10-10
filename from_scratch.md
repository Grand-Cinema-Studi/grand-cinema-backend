# From scratch – Backend Grand Cinéma

Journal pas à pas de la construction du backend, pensé comme un tutoriel : une entrée par action, dans l'ordre, avec les commandes exactes à lancer.

Stack : C# / .NET, Minimal API, Clean Architecture, CQRS, EF Core, PostgreSQL.
Périmètre MVP : US 1.1, 2.1, 2.2, 2.3, 5.2, 5.3 (issues du Bloc 2).

Conventions de ce document :
- Légende : [ ] à faire · [x] fait (noter la date et le commit ou la PR).
- Les commandes sont écrites pour PowerShell (Windows). Sous Linux, utiliser des `/` dans les chemins (déjà le cas ci-dessous) ; les commandes `dotnet`, `git` et `docker` sont identiques.
- Les valeurs entre chevrons (`<...>`) sont à remplacer.

---

## Phase 1 – Fondations

### Étape 1 – Prérequis [x] (10/10/2026)
- **Quoi :** installer et vérifier le SDK .NET, Git et Docker, puis figer la version du SDK dans le dépôt.
- **Pourquoi :** garantir que le projet se reconstruit à l'identique, sur Windows comme sur Linux.

Installation du SDK si nécessaire :
```powershell
# Windows
winget install Microsoft.DotNet.SDK.10
```
```bash
# Ubuntu / Debian (si le paquet est introuvable : installer le SDK 8 LTS, dotnet-sdk-8.0, ou utiliser le script officiel dotnet-install.sh)
sudo apt update
sudo apt install -y dotnet-sdk-10.0

# Fedora
sudo dnf install dotnet-sdk-10.0
```

Vérification des outils :
```bash
dotnet --version
dotnet --list-sdks
git --version
docker --version
docker compose version
```

Figer la version du SDK (à lancer plus tard dans le dépôt, à l'étape 4) :
```bash
dotnet new globaljson --sdk-version <version relevée> --roll-forward latestFeature
```

- **Vérification :** les quatre outils répondent avec leur version. Versions relevées le 10/10/2026 : .NET SDK 10.0.400 · Git 2.55.0 · Docker 29.7.2 · Docker Compose v5.5.0.

*À quoi l'étape a servi : à disposer d'un poste de développement prêt et identique sur les deux ordinateurs (SDK, Git, Docker). Pourquoi : sans version commune du SDK, le projet peut compiler sur une machine et pas sur l'autre ; figer la version rend le travail reproductible, ce que le jury peut vérifier.*

### Étape 2 – Dépôt public [ ]
- **Quoi :** créer sur GitHub (interface web) un dépôt public « grand-cinema-backend » avec un README, un .gitignore adapté à .NET (modèle Visual Studio) et une licence MIT. Relier le dossier local à ce dépôt.
- **Pourquoi :** sans lien Git public, le dossier d'examen est invalide. On initialise le dossier au lieu de le cloner, car il contient déjà ce fichier.

Dans le dossier local `grand-cinema-backend` :
```bash
git init
git branch -M main
git remote add origin https://github.com/Grand-Cinema-Studi/grand-cinema-backend.git
git pull origin main
git add from_scratch.md
git commit -m "docs: add from_scratch journal"
git push -u origin main
```

- **Vérification :** le dépôt s'ouvre dans un navigateur sans être connecté à GitHub. Commit : ______.

*À quoi l'étape a servi : à créer l'espace public où vit le code et à relier le dossier local à ce dépôt. Pourquoi : l'énoncé impose un lien Git public, sans quoi le dossier n'est pas valide ; GitHub sert aussi de sauvegarde et de synchronisation entre vos deux ordinateurs.*

### Étape 3 – Branches et convention de nommage (D1 Q1) [ ]
- **Quoi :** créer develop, protéger main et develop, documenter la convention dans CONTRIBUTING.md et l'intégrer par Pull Request. Une Pull Request est l'équivalent de la « merge request » de l'énoncé.
- **Pourquoi :** attendu explicite du D1 Q1 et preuve d'une gestion itérative et maîtrisée du code.

Créer develop :
```bash
git switch -c develop
git push -u origin develop
```

Sur GitHub (interface web : Settings, Rules ou Branches) : protéger `main` et `develop` (pas de push direct, Pull Request obligatoire).

Créer la convention sur une branche dédiée :
```bash
git switch -c docs/branch-convention develop
# créer CONTRIBUTING.md (contenu ci-dessous)
git add CONTRIBUTING.md
git commit -m "docs: add branch naming convention"
git push -u origin docs/branch-convention
```

Contenu de CONTRIBUTING.md :
- main = production, alimentée uniquement depuis develop ou une branche hotfix ;
- develop = intégration ;
- préfixes de branches : feature, fix, chore, docs, test, hotfix ;
- nom d'une branche de fonctionnalité = préfixe + numéro de user story + court intitulé (exemple : feature/us-2-3-reserve-places) ;
- messages de commit au format Conventional Commits (feat, fix, test, docs, chore) ;
- une branche = une user story = une Pull Request relue avec une checklist.

Ouvrir la Pull Request vers develop (interface web, ou avec le CLI GitHub s'il est installé), la fusionner, puis récupérer develop :
```bash
gh pr create --base develop --title "docs: add branch naming convention" --body "Convention de nommage des branches"
git switch develop
git pull
git branch -d docs/branch-convention
```

- **Vérification :** un push direct sur main est refusé ; la première Pull Request est fusionnée dans develop. PR : ______.

*À quoi l'étape a servi : à poser les règles de collaboration : deux branches stables (main, develop), des branches courtes par user story et une convention de nommage écrite. Pourquoi : c'est un livrable explicite du D1 Q1 ; la protection des branches et les Pull Requests montrent au jury une démarche de qualité, même pour un projet mené seul.*

### Étape 4 – Solution Clean Architecture [ ]
- **Quoi :** créer une solution composée de quatre couches et de deux projets de tests :
  - Domain : entités et règles métier, aucune dépendance ;
  - Application : cas d'usage (commandes et requêtes), dépend uniquement de Domain ;
  - Infrastructure : accès aux données et services externes, dépend d'Application ;
  - Api : Minimal API, dépend d'Application et d'Infrastructure ;
  - tests de Domain et tests d'Application.
- **Pourquoi :** le métier ne dépend ni de la base ni du framework web, donc il se teste sans infrastructure et l'on peut changer d'hébergeur ou de base sans toucher au domaine (cohérent avec l'architecture modulaire du Bloc 1).

Branche de travail :
```bash
git switch -c chore/solution-structure develop
```

Créer la solution et les projets (avec le SDK 10, le fichier de solution a l'extension slnx : c'est normal) :
```bash
dotnet new sln -n GrandCinema
dotnet new classlib -n GrandCinema.Domain -o src/GrandCinema.Domain
dotnet new classlib -n GrandCinema.Application -o src/GrandCinema.Application
dotnet new classlib -n GrandCinema.Infrastructure -o src/GrandCinema.Infrastructure
dotnet new web -n GrandCinema.Api -o src/GrandCinema.Api
dotnet new xunit -n GrandCinema.Domain.Tests -o tests/GrandCinema.Domain.Tests
dotnet new xunit -n GrandCinema.Application.Tests -o tests/GrandCinema.Application.Tests
```

Ajouter les projets à la solution :
```bash
dotnet sln add src/GrandCinema.Domain/GrandCinema.Domain.csproj
dotnet sln add src/GrandCinema.Application/GrandCinema.Application.csproj
dotnet sln add src/GrandCinema.Infrastructure/GrandCinema.Infrastructure.csproj
dotnet sln add src/GrandCinema.Api/GrandCinema.Api.csproj
dotnet sln add tests/GrandCinema.Domain.Tests/GrandCinema.Domain.Tests.csproj
dotnet sln add tests/GrandCinema.Application.Tests/GrandCinema.Application.Tests.csproj
```

Déclarer les références (toujours vers l'intérieur) :
```bash
dotnet add src/GrandCinema.Application reference src/GrandCinema.Domain
dotnet add src/GrandCinema.Infrastructure reference src/GrandCinema.Application
dotnet add src/GrandCinema.Api reference src/GrandCinema.Application src/GrandCinema.Infrastructure
dotnet add tests/GrandCinema.Domain.Tests reference src/GrandCinema.Domain
dotnet add tests/GrandCinema.Application.Tests reference src/GrandCinema.Application
```

Supprimer les fichiers d'exemple générés, figer la version du SDK (voir l'étape 1) et compiler :
```bash
rm src/GrandCinema.Domain/Class1.cs src/GrandCinema.Application/Class1.cs src/GrandCinema.Infrastructure/Class1.cs
rm tests/GrandCinema.Domain.Tests/UnitTest1.cs tests/GrandCinema.Application.Tests/UnitTest1.cs
dotnet new globaljson --sdk-version <version relevée> --roll-forward latestFeature
dotnet build
```
(Sous PowerShell : `Remove-Item <fichier>` à la place de `rm`.)

Enregistrer :
```bash
git add .
git commit -m "chore: add clean architecture solution structure"
git push -u origin chore/solution-structure
```

- **Vérification :** `dotnet build` réussit sans erreur ; le projet Domain n'a aucune référence. Commit : ______.

*À quoi l'étape a servi : à construire le squelette du projet en couches, avec des dépendances qui vont toujours vers le métier. Pourquoi : le domaine reste indépendant de la base et du framework web, donc testable seul et facile à faire évoluer ; cela justifie l'architecture modulaire choisie au Bloc 1.*

### Étape 5 – CQRS sans bibliothèque [ ]
- **Quoi :** dans Application, définir deux contrats génériques : un gestionnaire de commandes (écriture) et un gestionnaire de requêtes (lecture). Enregistrer les gestionnaires dans l'injection de dépendances et les injecter directement dans les endpoints. Organiser les dossiers par fonctionnalité (par exemple Reservations/Commands, Seances/Queries).
- **Pourquoi :** séparer lecture et écriture clarifie le code et prépare la montée en charge. Aucune bibliothèque tierce n'est utilisée : MediatR est passé sous licence commerciale en 2025 (à vérifier au moment de la soutenance), et le code maison est plus simple à expliquer.

```bash
git switch -c chore/cqrs-contracts develop
# (ou continuer sur la branche de l'étape 4 si elle n'est pas encore fusionnée)
# créer les interfaces et un gestionnaire fictif, l'appeler depuis un endpoint de test
dotnet build
dotnet run --project src/GrandCinema.Api
# dans un autre terminal : appeler l'endpoint de test
curl http://localhost:<port>/<endpoint-de-test>
# supprimer le gestionnaire fictif et son endpoint, puis :
git add .
git commit -m "feat: add CQRS handler contracts"
git push -u origin chore/cqrs-contracts
```

- **Vérification :** le gestionnaire fictif répond via l'endpoint de test, puis est supprimé. Commit : ______.

*À quoi l'étape a servi : à fixer la façon d'écrire les cas d'usage : une commande modifie les données, une requête les lit. Pourquoi : cette séparation rend le code lisible et prépare la montée en charge des lectures pendant le festival ; le faire sans bibliothèque évite une dépendance payante et simplifie l'explication à l'oral.*

### Étape 6 – Packages et base de données locale [ ]
- **Quoi :** ajouter EF Core et PostgreSQL, installer l'outil dotnet-ef, décrire PostgreSQL dans un fichier docker-compose, garder le mot de passe hors de Git.
- **Pourquoi :** PostgreSQL est le choix du Bloc 1 (RDS PostgreSQL). Le conteneur reproduit l'environnement de production en local, et les secrets restent hors du dépôt public.

```bash
git switch -c chore/database-setup develop
```

Packages :
```bash
dotnet add src/GrandCinema.Infrastructure package Microsoft.EntityFrameworkCore
dotnet add src/GrandCinema.Infrastructure package Npgsql.EntityFrameworkCore.PostgreSQL
dotnet add src/GrandCinema.Api package Microsoft.EntityFrameworkCore.Design
dotnet add tests/GrandCinema.Application.Tests package Shouldly
```
(Éviter FluentAssertions : sa version 8 est commerciale.)

Outil en ligne de commande dotnet-ef, installé localement au dépôt via un manifeste :
```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef
```
Sur une autre machine après un clone ou un pull :
```bash
dotnet tool restore
```

Fichier `docker-compose.yml` à la racine (le mot de passe vient d'un fichier `.env` non versionné) :
```yaml
services:
  db:
    image: postgres:16
    environment:
      POSTGRES_DB: grandcinema
      POSTGRES_USER: grandcinema
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD}
    ports:
      - "5432:5432"
    volumes:
      - grandcinema-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U grandcinema -d grandcinema"]
      interval: 5s
      timeout: 3s
      retries: 10
volumes:
  grandcinema-data:
```

Ajouter `.env` au `.gitignore`, créer `.env` (mot de passe) et un `.env.example` versionné sans valeur secrète, puis démarrer :
```bash
echo ".env" >> .gitignore
docker compose up -d
docker compose ps
```

Chaîne de connexion stockée dans les secrets utilisateur .NET (propres à chaque machine, à recréer sur l'autre ordinateur) :
```bash
dotnet user-secrets init --project src/GrandCinema.Api
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=grandcinema;Username=grandcinema;Password=<mot-de-passe>" --project src/GrandCinema.Api
```

Enregistrer :
```bash
git add .
git commit -m "chore: add EF Core, PostgreSQL and docker compose"
git push -u origin chore/database-setup
```

- **Vérification :** `docker compose ps` indique le conteneur « healthy » ; une connexion de test réussit. Commit : ______.

*À quoi l'étape a servi : à brancher l'application sur une vraie base PostgreSQL locale, avec les outils de migration, sans exposer de mot de passe. Pourquoi : travailler sur la même technologie que la production (RDS PostgreSQL au Bloc 1) évite les surprises au déploiement ; garder les secrets hors du dépôt public est une exigence de sécurité.*

### Étape 7 – Modèle de données (D1 Q2) [ ]
- **Quoi :** modéliser sept tables à partir des profils et EPICs du Bloc 1 (identifiants GUID) :
  - Users : adresse e-mail unique, mot de passe haché, nom, prénom, rôle (Spectateur, Caissier, Contrôleur, Responsable), date de création ;
  - Films : titre, synopsis, durée en minutes, genre, URL de l'affiche ;
  - Salles : nom unique, capacité ;
  - Places : salle, rangée, numéro ; combinaison salle-rangée-numéro unique ;
  - Séances : film, salle, début, prix ; index sur salle et début ;
  - Réservations : utilisateur, séance, statut (En attente de paiement, Confirmée, Expirée, Annulée), date de création, date d'expiration, montant total ;
  - Places réservées : réservation, séance, place ; clé primaire sur réservation et place ; **contrainte d'unicité sur séance et place**.
- **Pourquoi :** l'énoncé cite cinq tables ; les deux ajoutées (Salles et Places réservées) respectent la troisième forme normale et portent la capacité par salle. La contrainte d'unicité sur séance et place garantit la non-double vente au niveau de la base, même en cas de bug applicatif. À l'expiration ou à l'annulation, les lignes de places réservées sont supprimées pour libérer les places ; le statut conserve l'historique.
- **Point de vigilance :** une place doit appartenir à la salle de la séance. Ce contrôle ne se fait pas par clé étrangère : il est assuré dans le gestionnaire de réservation et couvert par un test.

Règles de mise en œuvre : écrire les entités dans Domain sans attribut lié à EF ; configurer en Fluent API et placer le DbContext dans Infrastructure.

```bash
git switch -c chore/database-schema develop
# écrire les entités (Domain), le DbContext et les configurations (Infrastructure)
```

Créer la migration initiale et l'appliquer à la base :
```bash
dotnet ef migrations add InitialSchema --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
```

Exporter le script SQL (livrable D1 Q2) et ajouter le schéma entité-relation (par exemple en Mermaid) :
```bash
mkdir -p docs/database
dotnet ef migrations script --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api -o docs/database/schema.sql
# créer docs/database/erd.md avec le schéma entité-relation
```
(Sous PowerShell : `mkdir docs/database` suffit.)

Enregistrer :
```bash
git add .
git commit -m "feat(db): add initial schema and migration"
git push -u origin chore/database-schema
```

- **Vérification :** la migration s'applique sur une base vide ; le script SQL et le schéma sont versionnés dans docs/database. Commit : ______.

*À quoi l'étape a servi : à traduire le besoin métier (films, séances, places, réservations, utilisateurs) en tables, avec leur script SQL et leur schéma. Pourquoi : c'est le livrable du D1 Q2 ; la contrainte d'unicité sur séance et place est la première défense contre la double vente, qui sera le bug étudié au Dossier 3.*

### Étape 8 – Livrer la phase [ ]
- **Quoi :** ouvrir une Pull Request vers develop pour chaque branche de la phase, fusionner une fois la compilation passée, puis nettoyer. Relire ce fichier : numéroté, daté.
- **Pourquoi :** montrer au jury une intégration continue d'un incrément fini, tracée de bout en bout.

Pour chaque branche (chore/solution-structure, chore/cqrs-contracts, chore/database-setup, chore/database-schema) :
```bash
gh pr create --base develop --title "<titre>" --body "<description>"
# après fusion :
git switch develop
git pull
git branch -d <nom-de-la-branche>
```

Contrôle final sur develop :
```bash
dotnet restore
dotnet build
dotnet test
```

- **Vérification :** develop contient toute la phase 1, la compilation et les tests passent. PR : ______.

*À quoi l'étape a servi : à intégrer proprement le travail de la phase dans develop et à prouver qu'il compile et se teste. Pourquoi : un incrément fini et relu, tracé par des Pull Requests, montre la gestion itérative attendue par la compétence C3.1 et sert de base saine pour les user stories suivantes.*

---

## Aide-mémoire des commandes récurrentes
```bash
dotnet build                                              # compiler
dotnet test                                               # lancer les tests
dotnet test --collect:"XPlat Code Coverage"               # tests avec couverture (Coverlet)
dotnet run --project src/GrandCinema.Api                  # lancer l'API
docker compose up -d                                      # démarrer PostgreSQL
docker compose down                                       # arrêter PostgreSQL (les données restent dans le volume)
git switch -c feature/us-<n>-<slug> develop               # nouvelle branche de fonctionnalité
git push -u origin <branche>                              # publier une branche
```

## Questions probables du jury (phase 1)
- **Pourquoi une contrainte d'unicité et une transaction ?** La contrainte garantit l'intégrité même en cas d'erreur de code ; la transaction permet une gestion d'erreur propre et un message clair à l'utilisateur.
- **Pourquoi sept tables et non cinq ?** Normalisation (troisième forme normale) et capacité décrite par salle.
- **Pourquoi pas MediatR ?** Licence commerciale depuis 2025, et le besoin est couvert par deux interfaces simples.
- **Pourquoi Clean Architecture ?** Domaine indépendant du framework et de la base, testabilité, cohérence avec l'architecture modulaire et évolutive du Bloc 1.

## Phases suivantes (à compléter)
- Phase 2 : authentification (US 2.1), Swagger, première user story complète avec feature branch et Pull Request.
- Phase 3 : consultation des séances et places restantes (US 1.1, 2.2).
- Phase 4 : réservation avec verrouillage et expiration (US 2.3), bug de double réservation, test de reproduction et correctif.
- Phase 5 : programmation et suivi des réservations (US 5.2, 5.3).
- Phase 6 : tests unitaires, couverture, SonarCloud, déploiement.
