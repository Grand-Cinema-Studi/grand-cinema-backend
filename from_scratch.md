# From scratch – Backend Grand Cinéma

Journal pas à pas de la construction du backend, pensé comme un tutoriel : une entrée par action, dans l'ordre, avec les commandes exactes à lancer.

Stack : C# / .NET, Minimal API, Clean Architecture, CQRS, EF Core, PostgreSQL.
Périmètre MVP : US 1.1, 2.1, 2.2, 2.3, 5.2, 5.3 (issues du Bloc 2).

Conventions de ce document :
- Légende : [ ] à faire · [x] fait (noter la date et le commit ou la PR).
- Les commandes sont écrites pour **bash** (Git Bash sous Windows, terminal sous Linux et macOS). Les chemins utilisent des `/`. Les commandes `dotnet`, `git` et `docker` sont identiques partout ; les rares variantes PowerShell sont signalées.
- Les valeurs entre chevrons (`<...>`) sont à remplacer.

---

## Phase 1 – Fondations

### Étape 1 – Prérequis [x] (10/10/2026)
- **Quoi :** installer et vérifier le SDK .NET, Git et Docker, puis figer la version du SDK dans le dépôt.
- **Pourquoi :** garantir que le projet se reconstruit à l'identique, sur Windows comme sur Linux.

Installation du SDK si nécessaire :
```bash
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
# global.json : fichier à la racine du dépôt qui impose la version du SDK .NET au projet.
#   --sdk-version     : version du SDK à utiliser (celle relevée à l'étape 1)
#   --roll-forward latestFeature : accepte un SDK plus récent du même numéro majeur et mineur si la version exacte est absente
# Utile avec plusieurs ordinateurs (Windows et Linux) : même version partout, donc même compilation, et version écrite dans le dépôt.
dotnet new globaljson --sdk-version <version relevée> --roll-forward latestFeature
```

- **Vérification :** les quatre outils répondent avec leur version. Versions relevées le 10/10/2026 : .NET SDK 10.0.400 · Git 2.55.0 · Docker 29.7.2 · Docker Compose v5.5.0.

*À quoi l'étape a servi : à disposer d'un poste de développement prêt et identique sur les deux ordinateurs (SDK, Git, Docker). Pourquoi : sans version commune du SDK, le projet peut compiler sur une machine et pas sur l'autre ; figer la version rend le travail reproductible, ce que le jury peut vérifier.*

### Étape 2 – Dépôt public [x]
- **Quoi :** créer sur GitHub (interface web) un dépôt public « grand-cinema-backend » avec un README, un .gitignore adapté à .NET (modèle Visual Studio) et une licence MIT. Relier le dossier local à ce dépôt.
- **Pourquoi :** sans lien Git public, le dossier d'examen est invalide. On initialise le dossier au lieu de le cloner, car il contient déjà ce fichier.

Dans le dossier local `grand-cinema-backend` :
```bash
git init
git add from_scratch.md
git commit -m "docs: add from_scratch journal"
git branch -M main
git remote add origin https://github.com/Grand-Cinema-Studi/grand-cinema-backend.git
git push -u origin main
```

- **Vérification :** le dépôt s'ouvre dans un navigateur sans être connecté à GitHub. Commit : ce17aa4 (docs: add from_scratch journal).

*À quoi l'étape a servi : à créer l'espace public où vit le code et à relier le dossier local à ce dépôt. Pourquoi : l'énoncé impose un lien Git public, sans quoi le dossier n'est pas valide ; GitHub sert aussi de sauvegarde et de synchronisation entre vos deux ordinateurs.*

### Étape 3 – Branches et convention de nommage (D1 Q1) [x]
- **Quoi :** créer dev, protéger main et dev, documenter la convention dans CONTRIBUTING.md et l'intégrer par Pull Request. Une Pull Request est l'équivalent de la « merge request » de l'énoncé.
- **Pourquoi :** attendu explicite du D1 Q1 et preuve d'une gestion itérative et maîtrisée du code.

Créer dev :
```bash
git switch -c dev
git push -u origin dev
```

#### Étape 3b – Protéger `main` et `dev` sur GitHub

Cette étape se fait dans l'interface web de GitHub, juste après la publication de `dev`. Une fois les règles actives, plus aucun push direct n'est possible sur `main` et `dev` : tout passe par une branche et une Pull Request, y compris les modifications de ce journal.

1. Ouvrir le dépôt sur GitHub et cliquer sur l'onglet **Settings** (réservé au propriétaire du dépôt).
2. Dans le menu de gauche, section « Code, planning, and automation », cliquer sur le menu déroulant **Rulests**, puis sur **Rulesets**.
3. Cliquer sur **New ruleset**, puis **New branch ruleset**.
4. Renseigner **Ruleset Name** : `protect-main-and-dev`.
5. Mettre **Enforcement status** sur **Active** (en « Disabled », les règles ne s'appliquent pas).
6. Laisser la **Bypass list** vide : sinon vous pouvez contourner vos propres règles.
7. Dans **Target branches**, cliquer sur **Add target**, choisir **Include by pattern**, saisir `main` et cliquer sur "Add Inclusion pattern". Recommencer avec `dev`.
8. Dans **Branch rules**, cocher :
   - **Restrict deletions** (interdit de supprimer la branche) ;
   - **Block force pushes** (interdit de réécrire l'historique) ;
   - **Require a pull request before merging**, puis régler **Required approvals** sur **0**. En projet solo, on ne peut pas approuver sa propre Pull Request : une approbation requise bloquerait toute fusion.
9. Ne pas cocher « Require status checks to pass » pour l'instant : aucun pipeline d'intégration continue n'existe encore. À ajouter plus tard, quand la CI sera en place.
10. Cliquer sur **Create** en bas de la page.

Variante avec l'ancienne interface : Settings, puis **Branches**, puis **Add branch protection rule**. Saisir `main` comme motif, cocher **Require a pull request before merging** (approbations à 0) et valider ; recommencer pour `dev`.

Vérification : tenter un push direct sur `dev`, qui doit être refusé, puis annuler le commit de test.
```bash
git switch dev
git commit --allow-empty -m "test: direct push"
git push
# attendu : refus, avec un message indiquant que les modifications doivent passer par une Pull Request
git reset --soft HEAD~1
```

Créer la convention sur une branche dédiée :
```bash
git switch -c docs/branch-convention dev
```

Créer le ficher CONTRIBUTING.md et y insérer ce contenu : 
- main = production, alimentée uniquement depuis dev ou une branche hotfix ;
- dev = intégration ;
- préfixes de branches : feature, fix, chore, docs, test, hotfix ;
- nom d'une branche de fonctionnalité = préfixe + numéro de user story + court intitulé (exemple : feature/us-2-3-reserve-places) ;
- messages de commit au format Conventional Commits (feat, fix, test, docs, chore) ;
- une branche = une user story = une Pull Request relue avec une checklist.

pousser la branche dédiée et son contenu :
```bash
git add CONTRIBUTING.md
git commit -m "docs: add branch naming convention"
git push -u origin docs/branch-convention
```

#### Étape 3c – Ouvrir et fusionner la Pull Request sur GitHub

Sur GitHub (interface web) :
1. Ouvrir le dépôt. Un bandeau jaune « docs/branch-convention had recent pushes » propose **Compare & pull request** : cliquer dessus. Sinon, onglet **Pull requests**, puis **New pull request**.
2. Vérifier la ligne de comparaison en haut : **base: dev** ← **compare: docs/branch-convention**. GitHub propose `main` par défaut : si la base affichée est `main`, la changer pour `dev` avec la liste « base ».
3. Renseigner le titre, au même format que le commit : `docs: add branch naming convention`. Dans la description, écrire en deux lignes ce qui change et pourquoi, avec une courte checklist (convention documentée, CONTRIBUTING.md ajouté, aucun autre changement).
4. Cliquer sur **Create pull request**.
5. Ouvrir l'onglet **Files changed** et relire le contenu : en projet solo, cette relecture est la seule « revue ».
6. Revenir à l'onglet **Conversation** et vérifier le message « This branch has no conflicts » ainsi qu'aucune règle bloquante.
7. Selon la situation, avec la flèche du bouton vert **Merge pull request** :
   - **Create a merge commit** : conserve la branche visible dans le graphe Git (recommandé quand le workflow doit être démontré) **<- à sélectionner**
   - **Squash and merge** : un seul commit par Pull Request, historique plus propre mais branches moins visibles.
   - **Rebase and merge** : rejoue les commits de la branche, un par un, à la suite de dev, sans commit de fusion. L’historique est parfaitement linéaire, mais les commits sont recréés avec de nouveaux identifiants et le lien visuel avec la branche disparaît. À éviter quand le workflow par branches doit être démontré.
8. Cliquer sur **Merge pull request** > **Confirm merge**.
9. Noter le numéro de la Pull Request (par exemple `PR #1`) dans la vérification ci-dessous.

*Pour mémoire, les trois modes :*
*Merge commit : garde tous les commits et un commit de fusion, la branche reste visible.*
*Squash : un seul commit pour toute la Pull Request.*
*Rebase : les commits d’origine sont conservés, sans commit de fusion.*

Revenir en local et récupérer `dev` à jour :
```bash
git switch dev
git pull
git push origin --delete docs/branch-convention
git branch -d docs/branch-convention
git fetch --prune
```
(`git fetch --prune` nettoie la référence locale de la branche distante supprimée.)

Variante en ligne de commande, si le CLI GitHub est installé :
```bash
gh pr create --base dev --head docs/branch-convention --title "docs: add branch naming convention" --body "Convention de nommage des branches"
gh pr merge --merge --delete-branch
```
(Avec `--delete-branch`, `gh` supprime la branche puis vous replace sur la branche par défaut, `main` : refaire alors `git switch dev` et `git pull`.)

*À quoi l'étape a servi : à intégrer le travail dans `dev` par une Pull Request, jamais par un push direct, et à vérifier que la protection de branche fonctionne. Pourquoi : c'est le cycle qui sera répété pour chaque user story (branche, Pull Request, relecture, fusion), et il laisse une trace claire de chaque changement.*

- **Vérification :** un push direct sur main est refusé ; la première Pull Request est fusionnée dans dev. PR : Docs/branch convention- #3.

*À quoi l'étape a servi : à poser les règles de collaboration : deux branches stables (main, dev), des branches courtes par user story et une convention de nommage écrite. Pourquoi : c'est un livrable explicite du D1 Q1 ; la protection des branches et les Pull Requests montrent au jury une démarche de qualité, même pour un projet mené seul.*

### Étape 4 – Solution Clean Architecture [x]
- **Quoi :** créer une solution composée de quatre couches et de deux projets de tests :
  - Domain : entités et règles métier, aucune dépendance ;
  - Application : cas d'usage (commandes et requêtes), dépend uniquement de Domain ;
  - Infrastructure : accès aux données et services externes, dépend d'Application ;
  - Api : Minimal API, dépend d'Application et d'Infrastructure ;
  - tests de Domain et tests d'Application.
- **Pourquoi :** le métier ne dépend ni de la base ni du framework web, donc il se teste sans infrastructure et l'on peut changer d'hébergeur ou de base sans toucher au domaine (cohérent avec l'architecture modulaire du Bloc 1).

Branche de travail :
```bash
git switch -c chore/solution-structure dev
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
# Rappel pour la commande ci-dessous
# global.json : fichier à la racine du dépôt qui impose la version du SDK .NET au projet.
# Utile avec plusieurs ordinateurs (Windows et Linux) : même version partout, donc même compilation, et version écrite dans le dépôt.
dotnet new globaljson --sdk-version 10.0.400 --roll-forward latestFeature
dotnet build
```
(Sous PowerShell uniquement : `Remove-Item <fichier>` à la place de `rm`.)

Enregistrer :
```bash
git add .
git commit -m "chore: add clean architecture solution structure"
git push -u origin chore/solution-structure
```

- **Vérification :** `dotnet build` réussit sans erreur ; le projet Domain n'a aucune référence. Commit : 76e1071 (chore: add clean architecture solution structure).

*À quoi l'étape a servi : à construire le squelette du projet en couches, avec des dépendances qui vont toujours vers le métier. Pourquoi : le domaine reste indépendant de la base et du framework web, donc testable seul et facile à faire évoluer ; cela justifie l'architecture modulaire choisie au Bloc 1.*

#### Étape 4b – Ignorer les fichiers générés (.gitignore)

À faire **avant le premier `git add .`** de l'étape 4 : les dossiers `bin/` et `obj/` contiennent les fichiers produits par la compilation. Ils changent à chaque build, ne doivent pas être versionnés, et apparaîtraient sinon en `M` (modifié) dans `git status` sans que vous ayez touché au code.

Créer un `.gitignore` standard .NET à la racine du dépôt :
```bash
dotnet new gitignore
```
Il ignore `bin/`, `obj/`, `.vs/`, les fichiers utilisateur et les résultats de tests. Le versionner avec le reste :
```bash
git add .gitignore
git commit -m "chore: add .gitignore"
```

Au cas où les fichiers générés ont déjà été commités (symptôme : des fichiers de `obj/Debug` apparaissent en `M` après chaque compilation ou après un `git pull`), `.gitignore` seul ne suffit pas : Git continue de suivre les fichiers déjà enregistrés. Il faut les retirer du suivi, sans les supprimer du disque. Sur une branche dédiée, `dev` étant protégée :
```bash
git switch -c chore/gitignore
dotnet new gitignore
git rm -r --cached -q --ignore-unmatch -- "*/bin/*" "*/obj/*"
git status --short
git ls-files -- "*/bin/*" "*/obj/*"
git add .gitignore
git commit -m "chore: add .gitignore and stop tracking build output"
git push -u origin chore/gitignore
```
- `git rm --cached` retire les fichiers de l'index Git uniquement ; ils restent sur le disque.
- Les motifs doivent désigner des **fichiers** (`"*/obj/*"`) et non des dossiers (`"src/*/obj"`) : avec un caractère joker, Git ne prolonge pas un motif de dossier aux fichiers qu'il contient, et `--ignore-unmatch` masque alors l'échec en silence.
- `git status --short` doit montrer beaucoup de lignes `D ` (supprimé de l'index) et `.gitignore` non suivi (`??`).
- Si rien n'est retiré, variante : `git ls-files -z -- "*/bin/*" "*/obj/*" | xargs -0 git rm --cached -q`.
- La commande `git ls-files -- "*/bin/*" "*/obj/*"` ne doit plus rien afficher (variante avec grep : `git ls-files | grep -E "/(bin|obj)/"`).
- Ouvrir ensuite la Pull Request vers `dev`, la fusionner (voir l'étape 3c), puis faire `git switch dev` et `git pull`.

- **Vérification :** après un `dotnet build`, `git status` n'affiche plus de fichiers de `bin/` ni de `obj/`. PR : ______.

*À quoi l'étape a servi : à empêcher que les fichiers produits par la compilation soient versionnés, et à retirer ceux qui l'avaient été par erreur. Pourquoi : ils se régénèrent à chaque build, polluent `git status` et les diffs, alourdissent le dépôt et créent des conflits inutiles ; seul le code source doit être suivi.*

### Étape 5 – CQRS sans bibliothèque [ ]
- **Quoi :** dans Application, définir deux contrats génériques : un gestionnaire de commandes (écriture) et un gestionnaire de requêtes (lecture). Enregistrer les gestionnaires dans l'injection de dépendances et les injecter directement dans les endpoints. Organiser les dossiers par fonctionnalité (par exemple Reservations/Commands, Seances/Queries).
- **Pourquoi :** séparer lecture et écriture clarifie le code et prépare la montée en charge. Aucune bibliothèque tierce n'est utilisée : MediatR est passé sous licence commerciale en 2025 (à vérifier au moment de la soutenance), et le code maison est plus simple à expliquer.

```bash
git switch -c chore/cqrs-contracts dev
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
git switch -c chore/database-setup dev
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
git switch -c chore/database-schema dev
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
(Sous PowerShell uniquement : `mkdir docs/database` suffit.)

Enregistrer :
```bash
git add .
git commit -m "feat(db): add initial schema and migration"
git push -u origin chore/database-schema
```

- **Vérification :** la migration s'applique sur une base vide ; le script SQL et le schéma sont versionnés dans docs/database. Commit : ______.

*À quoi l'étape a servi : à traduire le besoin métier (films, séances, places, réservations, utilisateurs) en tables, avec leur script SQL et leur schéma. Pourquoi : c'est le livrable du D1 Q2 ; la contrainte d'unicité sur séance et place est la première défense contre la double vente, qui sera le bug étudié au Dossier 3.*

### Étape 8 – Livrer la phase [ ]
- **Quoi :** ouvrir une Pull Request vers dev pour chaque branche de la phase, fusionner une fois la compilation passée, puis nettoyer. Relire ce fichier : numéroté, daté.
- **Pourquoi :** montrer au jury une intégration continue d'un incrément fini, tracée de bout en bout.

Pour chaque branche (chore/solution-structure, chore/cqrs-contracts, chore/database-setup, chore/database-schema) :
```bash
gh pr create --base dev --title "<titre>" --body "<description>"
# après fusion :
git switch dev
git pull
git branch -d <nom-de-la-branche>
```

Contrôle final sur dev :
```bash
dotnet restore
dotnet build
dotnet test
```

- **Vérification :** dev contient toute la phase 1, la compilation et les tests passent. PR : ______.

*À quoi l'étape a servi : à intégrer proprement le travail de la phase dans dev et à prouver qu'il compile et se teste. Pourquoi : un incrément fini et relu, tracé par des Pull Requests, montre la gestion itérative attendue par la compétence C3.1 et sert de base saine pour les user stories suivantes.*

---

## Aide-mémoire des commandes récurrentes
```bash
dotnet build                                              # compiler
dotnet test                                               # lancer les tests
dotnet test --collect:"XPlat Code Coverage"               # tests avec couverture (Coverlet)
dotnet run --project src/GrandCinema.Api                  # lancer l'API
docker compose up -d                                      # démarrer PostgreSQL
docker compose down                                       # arrêter PostgreSQL (les données restent dans le volume)
git switch -c feature/us-<n>-<slug> dev               # nouvelle branche de fonctionnalité
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
