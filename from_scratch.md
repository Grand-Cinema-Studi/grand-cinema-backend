# From scratch – Backend Grand Cinéma

Journal pas à pas de la construction du backend, pensé comme un tutoriel : une entrée par action, dans l'ordre, avec les commandes exactes à lancer.

Stack : C# / .NET, Minimal API, Clean Architecture, CQRS, EF Core, PostgreSQL.
Périmètre MVP (Bloc 2) : 11 user stories, 1.1, 1.2, 2.1, 2.2, 2.3, 3.1, 3.2, 4.1, 5.1, 5.2 et 5.3 ; les US 2.4, 5.4 et 5.5 sont en V2.

Architecture et hébergement : ceux du Bloc 1 (Front-end Angular sur S3 + CloudFront, API Back-end sur EC2, PostgreSQL, Stripe, CloudWatch), montés pour le projet dans une version réduite qui sert d'environnement de **recette** au sens du Bloc 2 (voir la phase 1 bis).

Conventions de ce document :

- Légende : [ ] à faire · [x] fait (noter la date et le commit ou la PR).
- Les commandes sont écrites pour **bash** (Git Bash sous Windows, terminal sous Linux et macOS). Les chemins utilisent des `/`. Les commandes `dotnet`, `git` et `docker` sont identiques partout ; les rares variantes PowerShell sont signalées.
- Les valeurs entre chevrons (`<...>`) sont à remplacer.
- Vocabulaire : « dépôt » désigne toujours le dépôt Git ; **repository** désigne le pattern d'accès aux données (`IScreeningReadRepository`, ...).

## Convention de nommage du code

Tout ce qui est nommé dans le code est en **anglais** : projets, espaces de noms, dossiers, classes, propriétés, méthodes, noms des tests, tables et colonnes de la base, branches et messages de commit. Le français reste réservé à ce journal, aux textes affichés à l'utilisateur (dans le front) et aux données d'exemple.

Correspondance avec le vocabulaire du Bloc 1 (MCD, user stories) :

| Bloc 1 (français)                                            | Code (anglais)                                                    |
| ------------------------------------------------------------ | ----------------------------------------------------------------- |
| Programmation (films et horaires d'un jour)                  | Schedule                                                          |
| Films à l'affiche (au moins une séance à venir)              | MoviesShowing                                                     |
| Film                                                         | Movie                                                             |
| Salle                                                        | Room                                                              |
| Place                                                        | Seat                                                              |
| Séance                                                       | Screening                                                         |
| Réservation                                                  | Reservation                                                       |
| Place réservée                                               | ReservedSeat                                                      |
| Utilisateur                                                  | User                                                              |
| Rôle : Spectateur, Caissier, Contrôleur, Responsable         | UserRole : Customer, Cashier, TicketInspector, Manager            |
| Inscription / Connexion / Jeton d'accès                      | RegisterUser / AuthenticateUser / AccessToken                     |
| Statut : En attente de paiement, Confirmée, Expirée, Annulée | ReservationStatus : PendingPayment, Confirmed, Expired, Cancelled |

Règles complémentaires :

- noms des tests : `When_<situation>_Then_<résultat attendu>`, qui reprend le « Quand / Alors » des critères d'acceptation (exemple : `When_no_screening_exists_Then_the_movie_list_is_empty`) ;
- couche Application rangée par fonctionnalité : `Features/<Feature>/Commands`, `Queries` et `DTOs` (voir l'étape 5) ;
- couche Api rangée par la même fonctionnalité : `Endpoints/<Feature>/`, un fichier par cas d'usage, nommé d'après lui (exemple : `Endpoints/Screenings/ScheduleEndpoints.cs`, voir l'étape 7 ter) ;
- couche Domain rangée par type d'élément : `Entities`, puis `Enums` quand le besoin apparaît.

_Pourquoi : l'anglais est la langue de l'écosystème .NET (bibliothèques, documentation, messages d'erreur) ; une seule langue évite les noms hybrides comme `GetSeancesByDate`, et ce tableau garde le lien avec le MCD du Bloc 1, que le jury compare au code._

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

_À quoi l'étape a servi : à disposer d'un poste de développement prêt et identique sur les deux ordinateurs (SDK, Git, Docker). Pourquoi : sans version commune du SDK, le projet peut compiler sur une machine et pas sur l'autre ; figer la version rend le travail reproductible, ce que le jury peut vérifier._

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

_À quoi l'étape a servi : à créer l'espace public où vit le code et à relier le dossier local à ce dépôt. Pourquoi : l'énoncé impose un lien Git public, sans quoi le dossier n'est pas valide ; GitHub sert aussi de sauvegarde et de synchronisation entre vos deux ordinateurs._

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
9. Ne pas cocher « Require status checks to pass » pour l'instant : aucun pipeline d'intégration continue n'existe encore. À ajouter quand la CI sera en place (étape 11e).
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

_À quoi la sous-étape a servi : à empêcher toute modification directe de `main` et `dev` : suppression, réécriture de l'historique et push direct sont refusés, tout passe par une Pull Request. Pourquoi : les deux branches stables restent toujours dans un état relu et cohérent ; le test du push refusé prouve que la règle est réellement active, et pas seulement configurée._

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

_Pour mémoire, les trois modes :_
_Merge commit : garde tous les commits et un commit de fusion, la branche reste visible._
_Squash : un seul commit pour toute la Pull Request._
_Rebase : les commits d’origine sont conservés, sans commit de fusion._

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

_À quoi l'étape a servi : à intégrer le travail dans `dev` par une Pull Request, jamais par un push direct, et à vérifier que la protection de branche fonctionne. Pourquoi : c'est le cycle qui sera répété pour chaque user story (branche, Pull Request, relecture, fusion), et il laisse une trace claire de chaque changement._

- **Vérification :** un push direct sur main est refusé ; la première Pull Request est fusionnée dans dev. PR : Docs/branch convention- #3.

_À quoi l'étape a servi : à poser les règles de collaboration : deux branches stables (main, dev), des branches courtes par user story et une convention de nommage écrite. Pourquoi : c'est un livrable explicite du D1 Q1 ; la protection des branches et les Pull Requests montrent au jury une démarche de qualité, même pour un projet mené seul._

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

_À quoi l'étape a servi : à construire le squelette du projet en couches, avec des dépendances qui vont toujours vers le métier. Pourquoi : le domaine reste indépendant de la base et du framework web, donc testable seul et facile à faire évoluer ; cela justifie l'architecture modulaire choisie au Bloc 1._

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

- **Vérification :** après un `dotnet build`, `git status` n'affiche plus de fichiers de `bin/` ni de `obj/`. PR : **\_\_**.

_À quoi l'étape a servi : à empêcher que les fichiers produits par la compilation soient versionnés, et à retirer ceux qui l'avaient été par erreur. Pourquoi : ils se régénèrent à chaque build, polluent `git status` et les diffs, alourdissent le dépôt et créent des conflits inutiles ; seul le code source doit être suivi._

### Étape 5 – CQRS et premier cas d'usage en TDD (US 1.1) [x]

- **Quoi :** poser les contrats CQRS, puis développer en TDD (cycle rouge, vert, refactor) le premier vrai cas d'usage du Grand Cinéma : l'**US 1.1, consulter les films et les horaires** (la programmation, `Schedule` dans le code). Le gestionnaire fictif prévu au départ est abandonné.
- **Pourquoi :** l'US 1.1 est la plus petite tranche complète (aucune dépendance à l'authentification ni au paiement) : elle valide l'architecture et l'outillage de tests avant les stories à risque (US 2.3). Le TDD est la méthode annoncée au Bloc 2 : chaque critère d'acceptation (Étant donné / Quand / Alors) devient un test. Aucune bibliothèque CQRS n'est utilisée : MediatR est passé sous licence commerciale en 2025 (à vérifier au moment de la soutenance), et deux interfaces suffisent.

Organisation retenue (tous les noms en anglais, voir la convention de nommage en tête de ce journal). La couche Application est rangée **par fonctionnalité** ; dans chaque fonctionnalité, les commandes, les requêtes et les DTO :

```text
src/GrandCinema.Domain/
└── Entities/                 Movie, Room, Screening (puis Seat, Reservation, ReservedSeat, User)
src/GrandCinema.Application/
├── Abstractions/
│   ├── Cqrs/                 IQueryHandler, ICommandHandler
│   └── Persistence/          interfaces des repositories (IScreeningReadRepository, ...)
└── Features/
    ├── Movies/               US 1.1 (films à l'affiche, étape 7 quater)
    ├── Screenings/           US 1.1, 2.2, 5.2
    │   ├── Commands/         (créé avec la première commande, US 5.2)
    │   ├── DTOs/             ScheduleDto, MovieScheduleDto, ScreeningDto
    │   └── Queries/          GetScheduleQuery, GetScheduleHandler
    ├── Reservations/         US 2.3, 5.3
    └── Auth/                 US 2.1
tests/GrandCinema.Application.Tests/
└── Features/Screenings/Queries/GetScheduleHandlerTests.cs
```

- `Abstractions/Cqrs` contient les contrats des handlers de requêtes et de commandes. Requêtes et commandes sont des « messages » au sens CQRS, mais elles sont traitées dans le même processus, par un simple appel de méthode, sans broker de messages (ni RabbitMQ ni Kafka) : d'où le nom `Cqrs` plutôt que `Messaging`.
- Les dossiers sont créés au moment où le premier fichier y arrive : Git ne versionne pas les dossiers vides, et un dossier vide n'apporte rien.
- Les interfaces des repositories sont dans `Abstractions/Persistence` et non dans une fonctionnalité : la réservation lira aussi les séances, et une fonctionnalité ne doit pas dépendre d'une autre.
- Les tests reprennent exactement l'arborescence de `src` : à toute classe correspond un test au même chemin.

#### Étape 5a – Contrats CQRS (branche chore)

```bash
git switch -c chore/cqrs-contracts dev
mkdir -p src/GrandCinema.Application/Abstractions/Cqrs
```

Créer `src/GrandCinema.Application/Abstractions/Cqrs/IQueryHandler.cs` (lecture) et `ICommandHandler.cs` (écriture) :

```csharp
namespace GrandCinema.Application.Abstractions.Cqrs;

public interface IQueryHandler<in TQuery, TResult>
{
    Task<TResult> Handle(TQuery query, CancellationToken cancellationToken = default);
}

public interface ICommandHandler<in TCommand, TResult>
{
    Task<TResult> Handle(TCommand command, CancellationToken cancellationToken = default);
}
```

(Mettre chaque interface dans son propre fichier.) Aucun test à ce stade : ce sont de simples contrats, ils seront exercés par le premier gestionnaire réel.

```bash
dotnet build
git add .
git commit -m "chore: add CQRS handler contracts"
git push -u origin chore/cqrs-contracts
```

Ouvrir la Pull Request vers `dev` et la fusionner (voir l'étape 3c), puis :

```bash
git switch dev
git pull
```

_À quoi la sous-étape a servi : à créer les deux interfaces communes à tous les cas d'usage, `IQueryHandler` pour lire et `ICommandHandler` pour modifier, dans `Abstractions/Cqrs`, et à les intégrer dans `dev` par une Pull Request séparée. Pourquoi : chaque handler suit le même contrat (une méthode `Handle` qui reçoit la requête ou la commande et renvoie un résultat), ce qui rend le code prévisible et l'appel depuis les endpoints uniforme ; une branche chore séparée garde la Pull Request de l'US 1.1 centrée sur la fonctionnalité._

#### Étape 5b – Branche de la fonctionnalité et outils de test

Une branche par user story, au nom conforme à la convention :

```bash
git switch -c feature/us-1-1-view-schedule dev
```

Ajouter les outils de test au projet de tests de l'application (Moq pour les doublures, comme annoncé au Bloc 2 ; Shouldly pour des assertions lisibles ; une horloge simulée pour maîtriser « aujourd'hui ») :

```bash
dotnet add tests/GrandCinema.Application.Tests package Moq
dotnet add tests/GrandCinema.Application.Tests package Shouldly
dotnet add tests/GrandCinema.Application.Tests package Microsoft.Extensions.TimeProvider.Testing
```

(Éviter FluentAssertions : sa version 8 est commerciale.)

Règles du TDD dans ce projet :

- on n'écrit **aucun code de production sans test rouge** qui l'exige ;
- chaque test porte l'étiquette de la story (`[Trait("US", "1.1")]`) et un nom construit sur le critère d'acceptation : `When_<situation>_Then_<résultat attendu>` ;
- structure Arrange / Act / Assert, un seul comportement par test ;
- les tests de l'application n'utilisent ni base de données ni réseau : le repository est une doublure (Moq) ;
- si un nouveau test passe dès son écriture, casser volontairement le code (sans commiter) pour vérifier qu'il échoue, puis le rétablir, et le mentionner dans le message de commit : un test jamais vu rouge peut ne rien tester.

Lancer uniquement les tests de la story :

```bash
dotnet test --filter "US=1.1"
```

_À quoi la sous-étape a servi : à ouvrir la branche dédiée à l'US 1.1, à installer les outils de test (Moq pour simuler le repository, Shouldly pour les assertions, FakeTimeProvider pour fixer la date) et à poser les règles TDD du projet. Pourquoi : une branche par story trace le travail jusqu'à la Pull Request ; les doublures et l'horloge simulée rendent les tests rapides et reproductibles, sans base de données ni dépendance à la date réelle ; l'étiquette `US` permet de lancer les tests d'une seule story._

#### Étape 5c – De l'US 1.1 à la liste de tests

Critères d'acceptation du Bloc 2, traduits en tests (on écrit la liste avant le premier test) :

| Critère d'acceptation                                                        | Test à écrire                                                                                              |
| ---------------------------------------------------------------------------- | ---------------------------------------------------------------------------------------------------------- |
| Je vois les films avec leurs séances (jour, heure, salle)                    | Cycle 1 : `When_screenings_exist_Then_they_are_grouped_by_movie`                                           |
| Quand je choisis une date, seules les séances de ce jour s'affichent         | Cycle 2 : `When_a_date_is_chosen_Then_the_repository_is_queried_for_that_day`                              |
| Quand aucune séance n'existe pour cette date, un message explicite s'affiche | Cycle 3 : `When_no_screening_exists_Then_the_movie_list_is_empty` (l'affichage du message relève du front) |
| J'ouvre la programmation (sans choisir de date)                              | Cycle 4 : `When_no_date_is_chosen_Then_today_is_used`                                                      |
| Séances lisibles dans l'ordre                                                | Cycle 5 : `When_a_movie_has_several_screenings_Then_they_are_sorted_by_time`                               |
| Les films sont affichés par ordre alphabétique de titre (critère ajouté)     | Cycle 6 : `When_several_movies_exist_Then_they_are_sorted_by_title` (voir l'étape 5e bis)                  |

Remarque : le filtre par jour est réellement exécuté par le repository (requête SQL). Le test du cycle 2 vérifie la bonne transmission de la date ; le filtre lui-même sera vérifié par un **test d'intégration** avec PostgreSQL à la suite de l'étape 7.

_À quoi la sous-étape a servi : à transformer chaque critère d'acceptation de l'US 1.1 en un test nommé, avant d'écrire la moindre ligne de code. Pourquoi : la liste fixe le périmètre (on code ce que les critères demandent, rien de plus) et crée la traçabilité du Bloc 2 au Bloc 3 : pour chaque critère, le jury retrouve le test qui le prouve._

#### Étape 5d – Cycle 1 (rouge, vert, refactor)

**1. Rouge : écrire le test.**

```bash
mkdir -p tests/GrandCinema.Application.Tests/Features/Screenings/Queries
```

Créer `tests/GrandCinema.Application.Tests/Features/Screenings/Queries/GetScheduleHandlerTests.cs` :

```csharp
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Screenings.Queries;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Screenings.Queries;

public class GetScheduleHandlerTests
{
    private readonly Mock<IScreeningReadRepository> _screenings = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_screenings_exist_Then_they_are_grouped_by_movie()
    {
        // Arrange
        var day = new DateOnly(2026, 7, 18);
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", DurationMinutes = 110, Genre = "Documentary" };
        var startsAt = day.ToDateTime(new TimeOnly(18, 30));
        _screenings
            .Setup(r => r.GetByDateAsync(day, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Screening> { new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9m } });
        var handler = new GetScheduleHandler(_screenings.Object, _time);

        // Act
        var result = await handler.Handle(new GetScheduleQuery(day));

        // Assert
        var movieSchedule = result.Movies.ShouldHaveSingleItem();
        movieSchedule.Title.ShouldBe("Blue Note");
        var screening = movieSchedule.Screenings.ShouldHaveSingleItem();
        screening.RoomName.ShouldBe("Salle 1");
        screening.StartsAt.ShouldBe(startsAt);
    }
}
```

Les valeurs entre guillemets (« Salle 1 », « Blue Note ») sont des données d'exemple, pas des noms de code : elles peuvent rester en français.

Lancer `dotnet build` : le test ne compile pas, car les types n'existent pas encore. C'est un premier « rouge », mais il faut un rouge d'assertion.

**2. Squelettes minimaux pour compiler**, sans logique :

```bash
mkdir -p src/GrandCinema.Domain/Entities
mkdir -p src/GrandCinema.Application/Abstractions/Persistence
mkdir -p src/GrandCinema.Application/Features/Screenings/DTOs src/GrandCinema.Application/Features/Screenings/Queries
```

- dans `src/GrandCinema.Domain/Entities/` (espace de noms `GrandCinema.Domain.Entities`) : les classes `Movie` (Id, Title, Synopsis facultatif, DurationMinutes, Genre, PosterUrl facultatif), `Room` (Id, Name, Capacity) et `Screening` (Id, Movie, Room, StartsAt de type DateTime en heure locale du cinéma, Price). Les propriétés obligatoires utilisent `required` ;
- dans `src/GrandCinema.Application/Abstractions/Persistence/` : l'interface `IScreeningReadRepository` avec une seule méthode `Task<IReadOnlyList<Screening>> GetByDateAsync(DateOnly date, CancellationToken cancellationToken = default)` ;
- dans `src/GrandCinema.Application/Features/Screenings/DTOs/` (un record par fichier) : `ScheduleDto(DateOnly Date, IReadOnlyList<MovieScheduleDto> Movies)`, `MovieScheduleDto(Guid MovieId, string Title, int DurationMinutes, string Genre, string? PosterUrl, IReadOnlyList<ScreeningDto> Screenings)`, `ScreeningDto(Guid ScreeningId, DateTime StartsAt, string RoomName)` ;
- dans `src/GrandCinema.Application/Features/Screenings/Queries/` : le record `GetScheduleQuery(DateOnly? Date)` et la classe `GetScheduleHandler`, qui implémente `IQueryHandler<GetScheduleQuery, ScheduleDto>`, reçoit `IScreeningReadRepository` et `TimeProvider` par son constructeur, et dont la méthode `Handle` lève une `NotImplementedException`.

Pas de dossier `Commands/` pour l'instant : l'US 1.1 ne fait que lire.

**3. Vérifier le vrai rouge.**

```bash
dotnet test --filter "US=1.1"
```

Résultat attendu : 1 test échoué (`NotImplementedException`). Enregistrer :

```bash
git add .
git commit -m "test(us-1.1): red - group screenings by movie"
```

**4. Vert : le code minimal.** Dans `Handle` : utiliser la date de la requête, appeler le repository, regrouper les séances par film (`GroupBy` sur l'identifiant du film) et construire le `ScheduleDto`. Ne rien ajouter d'autre (ni tri, ni date du jour) : ces comportements viendront de tests suivants.

```bash
dotnet test --filter "US=1.1"
```

Résultat attendu : 1 test réussi.

```bash
git add .
git commit -m "feat(us-1.1): green - group screenings by movie"
```

**5. Refactor.** Sans changer le comportement : extraire la construction de `MovieScheduleDto` dans une méthode privée (par exemple `ToMovieSchedule`), clarifier les noms. Relancer les tests (toujours verts).

```bash
dotnet test --filter "US=1.1"
git add .
git commit -m "refactor(us-1.1): extract movie schedule mapping"
```

_À quoi la sous-étape a servi : à dérouler un premier cycle TDD complet. Rouge : le test décrit le comportement attendu et échoue, ce qui prouve qu'il teste vraiment quelque chose. Vert : le code minimal le fait passer. Refactor : le code est amélioré sans changer de comportement, sous la protection du test. Pourquoi : chaque ligne de production est justifiée par un test ; les trois commits montrent dans l'historique Git que la méthode a réellement été appliquée._

#### Étape 5e – Cycles 2 à 5

Pour chacun, dans la même classe de tests, répéter exactement : écrire le test (rouge), vérifier l'échec avec `dotnet test --filter "US=1.1"`, écrire le code minimal (vert), refactorer, avec un commit par phase (`test(us-1.1): red - ...`, `feat(us-1.1): green - ...`, `refactor(us-1.1): ...`).

- **Cycle 2 – date choisie.** Test `When_a_date_is_chosen_Then_the_repository_is_queried_for_that_day` : requête avec le 19 juillet ; vérifier avec `_screenings.Verify(...)` que le repository est appelé une seule fois pour cette date, et que `result.Date` vaut le 19 juillet.
- **Cycle 3 – aucune séance.** Test `When_no_screening_exists_Then_the_movie_list_is_empty` : le repository renvoie une liste vide ; attendre `result.Movies` vide (et non une erreur). Le message explicite est affiché par le front à partir de cette liste vide.
- **Cycle 4 – sans date.** Test `When_no_date_is_chosen_Then_today_is_used` : requête avec `Date` nul ; l'horloge simulée est réglée sur le 18 juillet 2026 (fuseau local fixé avec `SetLocalTimeZone`) ; vérifier que le repository est appelé pour le 18 juillet. Code : `query.Date ?? DateOnly.FromDateTime(timeProvider.GetLocalNow().DateTime)`.
- **Cycle 5 – ordre chronologique.** Test `When_a_movie_has_several_screenings_Then_they_are_sorted_by_time` : le repository renvoie une séance de 21 h avant une séance de 18 h 30 ; attendre 18 h 30 puis 21 h. Code : tri sur `StartsAt`.

Point à retenir pour la production : « aujourd'hui » dépend du fuseau horaire. Un serveur en UTC verrait un autre jour que Paris en soirée. Le fuseau du cinéma est fixé au déploiement par la variable d'environnement `TZ=Europe/Paris` (étapes 9 et 10).

_À quoi la sous-étape a servi : à compléter le comportement de l'US 1.1 critère par critère (date choisie, aucune séance, date du jour par défaut, ordre chronologique), un cycle rouge, vert, refactor à la fois. Pourquoi : avancer par petits pas garde les tests verts entre deux cycles et limite le débogage ; à la fin, chaque critère d'acceptation est couvert par un test qui sert de non-régression._

#### Étape 5e bis – Cycle 6 : tri des films par titre (correctif)

- **Quoi :** couvrir par un test le tri des films par titre (`OrderBy(m => m.Title)` dans `GetScheduleHandler`), présent dans le code sans qu'aucun test ni critère d'acceptation ne l'exige.
- **Pourquoi :** c'est du code de production sans test, contraire à la règle TDD du projet. Le tri est pourtant utile : sans lui, l'ordre des films dépend de ce que renvoie PostgreSQL, sans garantie, et peut changer d'un appel à l'autre. On le garde, mais on le rend officiel : une règle écrite, puis un test.

1. **Décider de la règle et l'écrire dans le backlog.** C'est une décision du Product Owner : ordre alphabétique de titre (choix actuel) ou ordre de la première séance. Ajouter le critère à l'issue de l'US 1.1 sur GitHub, par exemple : « Les films sont affichés par ordre alphabétique de titre ». Sans critère, le test vérifierait une règle absente de toute spécification.

2. **Créer la branche :**

```bash
git switch dev
git pull
git switch -c test/us-1-1-movies-sorted-by-title dev
```

3. **Écrire le test** dans `tests/GrandCinema.Application.Tests/Features/Screenings/Queries/GetScheduleHandlerTests.cs` :

```csharp
[Fact]
[Trait("US", "1.1")]
public async Task When_several_movies_exist_Then_they_are_sorted_by_title()
{
    // Arrange
    var day = new DateOnly(2026, 7, 18);
    var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
    Movie NewMovie(string title) => new()
    {
        Id = Guid.NewGuid(), Title = title, Synopsis = "Synopsis", DurationMinutes = 100,
        Genre = "Drama", ReleaseDate = new DateTime(2025, 1, 1), PosterUrl = "https://example.com/p.jpg"
    };
    var startsAt = day.ToDateTime(new TimeOnly(20, 0));
    _screenings
        .Setup(r => r.GetScreeningsAsync(day, It.IsAny<CancellationToken>()))
        .ReturnsAsync(new List<Screening>
        {
            new() { Id = Guid.NewGuid(), Room = room, Movie = NewMovie("Zorro"), StartsAt = startsAt, Price = 9m },
            new() { Id = Guid.NewGuid(), Room = room, Movie = NewMovie("Blue Note"), StartsAt = startsAt, Price = 9m },
            new() { Id = Guid.NewGuid(), Room = room, Movie = NewMovie("Metropolis"), StartsAt = startsAt, Price = 9m }
        });
    var handler = new GetScheduleHandler(_screenings.Object, _time);

    // Act
    var result = await handler.Handle(new GetScheduleQuery(day));

    // Assert
    result.Movies.Select(m => m.Title).ShouldBe(new[] { "Blue Note", "Metropolis", "Zorro" });
}
```

4. **Vérifier le rouge en cassant volontairement le code.** Le comportement existe déjà, donc le test passe tout de suite : cela ne prouve pas qu'il teste quelque chose. Sans rien commiter, retirer temporairement `.OrderBy(m => m.Title)` du handler, puis lancer :

```bash
dotnet test --filter "US=1.1"
```

Résultat attendu : le nouveau test échoue (ordre Zorro, Blue Note, Metropolis). Remettre la ligne, relancer : 6 tests réussis.

5. **Enregistrer, en gardant la trace de la vérification dans le message :**

```bash
git add tests
git commit -m "test(us-1.1): movies sorted by title" -m "Verified red by temporarily removing the OrderBy."
git push -u origin test/us-1-1-movies-sorted-by-title
```

Ouvrir la Pull Request vers `dev` et la fusionner (voir l'étape 3c).

6. **Pour aller plus loin (facultatif).** `OrderBy(m => m.Title)` compare les titres selon la langue du serveur : sur un serveur en anglais ou en mode « invariant », « Élise » peut passer après « Zorro ». C'est le même piège que le fuseau horaire. Correctif : fixer la comparaison en français avec `StringComparer.Create(CultureInfo.GetCultureInfo("fr-FR"), ignoreCase: true)`, couvert par un test qui ajoute un titre accentué.

- **Vérification :** `dotnet test --filter "US=1.1"` affiche 6 tests réussis ; le critère de tri figure dans l'issue de l'US 1.1 ; le message du commit mentionne la vérification du rouge. PR : **\_\_**.

_À quoi la sous-étape a servi : à corriger un écart à la méthode (du code de production sans test) en le transformant en règle explicite : un critère d'acceptation validé, puis un test dont on a prouvé qu'il échoue sans le code. Pourquoi : un test qui n'a jamais été vu rouge peut ne rien tester ; casser volontairement le code est le moyen simple de s'en assurer quand le comportement existe déjà, et la traçabilité critère → test reste complète pour le jury._

#### Étape 5f – Clôture de l'étape

Contrôle de la suite complète et de la couverture du projet de tests :

```bash
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

_À quoi sert la seconde commande : elle lance les tests et mesure la couverture de code, c'est-à-dire la part du code de production réellement exécutée par les tests. L'option `--collect:"XPlat Code Coverage"` active le collecteur Coverlet (« XPlat » : multiplateforme, il fonctionne aussi sous Linux), fourni par le paquet `coverlet.collector` déjà référencé dans les projets de tests. Elle produit, pour chaque projet de tests, un fichier `TestResults/<guid>/coverage.cobertura.xml` qui indique ligne par ligne et branche par branche (`if`, `??`, etc.) ce qui a été exécuté ; le dossier `TestResults/` est ignoré par Git. Pourquoi : la couverture prouve au jury que les tests exercent bien le code écrit (le `GetScheduleHandler` doit être couvert à 100 %) et repère le code jamais testé. Pour un rapport lisible, générer une page HTML avec ReportGenerator : `dotnet tool install -g dotnet-reportgenerator-globaltool`, puis `reportgenerator -reports:"tests/**/TestResults/**/coverage.cobertura.xml" -targetdir:coverage-report -reporttypes:Html`, et ouvrir `coverage-report/index.html` (ajouter `coverage-report/` au `.gitignore`)._

_Alternative dans VS Code : avec l'extension C# Dev Kit, l'action « Run Tests with Coverage » de l'explorateur de tests colore directement dans l'éditeur les lignes couvertes et non couvertes, avec un pourcentage par fichier ; c'est pratique pendant les cycles TDD, tandis que le rapport ReportGenerator reste préférable pour une vue d'ensemble à montrer au jury. Limite : un pourcentage élevé montre que le code a été exécuté, pas que les assertions vérifient les bons résultats._

Publier la branche et ouvrir la Pull Request vers `dev` :

```bash
git push -u origin feature/us-1-1-view-schedule
```

La Pull Request n'est fusionnée que lorsque tous les tests sont verts. L'US 1.1 sera terminée après la suite décrite ci-dessous.

**Suite de l'US 1.1, après l'étape 7 (base de données)** : implémentation de `IScreeningReadRepository` dans Infrastructure avec un test d'intégration sur PostgreSQL et l'index sur `StartsAt` (étape 7 bis), puis enregistrement des gestionnaires dans l'injection de dépendances, endpoint Minimal API de consultation de la programmation et documentation Swagger (étape 7 ter), puis fusion de la Pull Request.

_À quoi la sous-étape a servi : à vérifier que toute la suite de tests passe, à mesurer la couverture et à publier la branche pour la Pull Request, en notant ce qui reste à faire pour terminer l'US 1.1 (repository réel, endpoint, Swagger). Pourquoi : seul du code testé entre dans `dev` ; la couverture est un livrable du Dossier 3 ; découper l'US en deux temps permet de développer la logique métier avant que la base de données existe._

- **Vérification :** `dotnet test --filter "US=1.1"` affiche 6 tests réussis (5, puis 6 après l'étape 5e bis) ; l'historique de la branche montre pour chaque cycle un commit rouge, un commit vert et un commit de refactor ; aucun nom en français dans le code. Commits : **\_\_**. PR : **\_\_**.

_À quoi l'étape a servi : à poser la façon d'écrire les cas d'usage (commande qui modifie, requête qui lit), rangés par fonctionnalité, et à dérouler pour la première fois le cycle TDD sur une vraie règle du Grand Cinéma, en partant des critères d'acceptation. Pourquoi : écrire le test avant le code force à penser le comportement attendu, laisse une documentation vivante de la story et garantit la non-régression ; ranger par fonctionnalité regroupe tout ce qui concerne un cas d'usage au même endroit ; la séparation en couches permet de tester sans base de données, et le faire sans bibliothèque CQRS évite une dépendance payante tout en restant simple à expliquer à l'oral._

### Étape 6 – Packages et base de données locale [x]

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
```

Outil en ligne de commande dotnet-ef, installé localement au dépôt via un manifeste :

```bash
dotnet new tool-manifest
dotnet tool install dotnet-ef
```

_À quoi servent ces commandes : `dotnet new tool-manifest` crée le fichier `dotnet-tools.json` à la racine du dépôt (depuis le SDK .NET 8 ; les SDK plus anciens le plaçaient dans un dossier `.config/`, encore reconnu), un manifeste qui liste les outils .NET du projet avec leur version exacte (vide au départ). `dotnet tool install dotnet-ef`, sans l'option `-g`, installe l'outil localement au dépôt et l'inscrit dans ce manifeste ; il s'appelle ensuite avec `dotnet ef ...`. `dotnet-ef` est l'outil en ligne de commande d'Entity Framework Core : il gère le schéma de la base PostgreSQL à partir des entités (`dotnet ef migrations add InitialCreate` génère une migration, c'est-à-dire le code C# qui crée ou modifie les tables ; `dotnet ef database update` l'applique à la base). Pourquoi local plutôt que global : le manifeste est commité, donc tout le monde (autre développeur, CI, autre machine) utilise la même version de l'outil, réinstallée par un simple `dotnet tool restore` ; et la version de `dotnet-ef` doit correspondre à celle d'EF Core du projet, ce qu'une installation globale ne garantit pas. Penser à commiter `dotnet-tools.json`._

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

_Pourquoi les user-secrets plutôt que le seul `.env` : le `.env` est lu par Docker Compose pour configurer le conteneur PostgreSQL, mais ASP.NET Core ne le lit pas nativement (il faudrait ajouter un paquet comme DotNetEnv, exporter les variables à la main avant `dotnet run`, ou lancer l'API dans Docker). ASP.NET Core charge sa configuration depuis `appsettings.json`, les user-secrets (en environnement `Development` uniquement), les variables d'environnement (`ConnectionStrings__Default`) et la ligne de commande. Avantages des user-secrets : intégrés au framework, sans paquet ni code ; stockés hors du dépôt, dans le dossier personnel (`~/.microsoft/usersecrets/<id>/secrets.json`), donc impossibles à commiter par erreur ; lus aussi par `dotnet ef` pour les migrations ; pratique recommandée par Microsoft pour les secrets de développement ; jamais utilisés en production, où l'on passe par des variables d'environnement ou un gestionnaire de secrets AWS. Inconvénient accepté : le mot de passe est écrit à deux endroits (`.env` pour Docker, user-secrets pour .NET) et doit être recréé sur chaque machine. Chaque outil garde ainsi son rôle : `.env` pour Docker, user-secrets pour .NET en développement, variables d'environnement en production._

Enregistrer :

```bash
git add .
git commit -m "chore: add EF Core, PostgreSQL and docker compose"
git push -u origin chore/database-setup
```

- **Vérification :** `docker compose ps` indique le conteneur « healthy » ; une connexion de test réussit. Commit : **\_\_**.

_À quoi l'étape a servi : à brancher l'application sur une vraie base PostgreSQL locale, avec les outils de migration, sans exposer de mot de passe. Pourquoi : travailler sur la même technologie que la production (RDS PostgreSQL au Bloc 1) évite les surprises au déploiement ; garder les secrets hors du dépôt public est une exigence de sécurité._

### Étape 7 – Modèle de données (D1 Q2) [x] (13/10/2026)

- **Quoi :** modéliser sept tables à partir des profils et EPICs du Bloc 1 (identifiants GUID ; noms anglais, correspondance française entre parenthèses) :
  - `Users` (utilisateurs) : Email unique, PasswordHash, LastName, FirstName, Role (`UserRole` : Customer, Cashier, TicketInspector, Manager), CreatedAt ;
  - `Movies` (films) : Title, Synopsis, DurationMinutes, Genre, ReleaseDate, PosterUrl ;
  - `Rooms` (salles) : Name unique, Capacity (nombre maximal de places, strictement positif) ;
  - `Seats` (places) : RoomId, Row (lettre du rang), Number ; combinaison RoomId-Row-Number unique ;
  - `Screenings` (séances) : MovieId, RoomId, StartsAt, Price ; index sur RoomId et StartsAt ;
  - `Reservations` (réservations) : UserId, ScreeningId, Status (`ReservationStatus` : PendingPayment, Confirmed, Expired, Cancelled), CreatedAt, ExpiresAt (facultatif), TotalAmount ;
  - `ReservedSeats` (places réservées) : ReservationId, ScreeningId, SeatId ; clé primaire sur ReservationId et SeatId ; **contrainte d'unicité sur ScreeningId et SeatId**.
- **Pourquoi :** l'énoncé cite cinq tables ; les deux ajoutées (Rooms et ReservedSeats) respectent la troisième forme normale et portent la capacité par salle. La contrainte d'unicité sur ScreeningId et SeatId garantit la non-double vente au niveau de la base, même en cas de bug applicatif. À l'expiration ou à l'annulation, les lignes de ReservedSeats sont supprimées pour libérer les places ; le statut conserve l'historique.
- **Points de vigilance :** certaines règles ne s'expriment pas par une contrainte de base et sont assurées dans les gestionnaires, couvertes par des tests : une place doit appartenir à la salle de la séance ; le nombre de places d'une salle ne dépasse pas sa `Capacity` (une contrainte `CHECK` ne compte pas les lignes d'une autre table) ; l'adresse e-mail est enregistrée en minuscules, pour que l'unicité ne dépende pas de la casse.

Écarts assumés par rapport au premier modèle :

- `Movies.ReleaseDate` ajouté, en `DateOnly` (type `date` en base) ;
- `Movies.Synopsis` et `Movies.PosterUrl` obligatoires : un film est publié complet ;
- `Rooms.Capacity` conservé comme nombre maximal de places, avec une contrainte `CHECK (Capacity > 0)` ;
- `Reservations.ExpiresAt` facultatif : une réservation confirmée n'expire plus.

Règles de mise en œuvre : compléter les entités de `Domain/Entities` (Movie, Room et Screening existent depuis l'étape 5) et ajouter les autres, sans attribut lié à EF ; placer les énumérations dans `Domain/Enums` ; configurer en Fluent API ; placer le DbContext, les configurations et les migrations dans `Infrastructure/Persistence`.

```bash
git switch -c chore/database-schema dev
```

#### Étape 7a – Entités et énumérations (Domain)

Dans `src/GrandCinema.Domain/Enums/` (espace de noms `GrandCinema.Domain.Enums`), un fichier par énumération : `UserRole` et `ReservationStatus`.

Dans `src/GrandCinema.Domain/Entities/`, une classe par entité, en C# pur :

- les montants (`Price`, `TotalAmount`) en `decimal`, jamais en `float` (type binaire approximatif, erreurs d'arrondi sur les totaux) ;
- le mot de passe sous le nom `PasswordHash` : la propriété ne contient jamais le mot de passe en clair ;
- chaque relation porte sa clé étrangère (`MovieId`, `RoomId`, `UserId`, `ScreeningId`, `SeatId`, `ReservationId`), sans `required` (un `Guid` vide passerait quand même) ;
- les propriétés de navigation sont initialisées à `null!` (`public Room Room { get; set; } = null!;`), et non `required`, pour pouvoir créer une entité à partir des seuls identifiants ;
- les collections sont initialisées (`= new List<Seat>();`) et ne sont donc jamais nulles : `Room.Seats`, `Reservation.ReservedSeats` ;
- `ReservedSeat` n'a pas d'`Id` propre (clé composite) ; il a les navigations `Reservation` et `Seat`, et seulement la clé étrangère `ScreeningId`, qui sert à la contrainte anti-double-vente.

Le passage de `ReleaseDate` en `DateOnly` se répercute sur `MovieScheduleDto` et sur les données des tests de l'US 1.1. Contrôle :

```bash
dotnet build
dotnet test
```

#### Étape 7b – DbContext et configurations (Infrastructure)

Arborescence :

```text
src/GrandCinema.Infrastructure/
├── DependencyInjection.cs        ← étape 7c
└── Persistence/
    ├── GrandCinemaDbContext.cs
    ├── Configurations/           ← une classe IEntityTypeConfiguration<T> par entité
    └── Migrations/               ← généré par dotnet ef (étape 7d), ne pas créer
```

`src/GrandCinema.Infrastructure/Persistence/GrandCinemaDbContext.cs` :

```csharp
using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrandCinema.Infrastructure.Persistence;

public class GrandCinemaDbContext : DbContext
{
    public GrandCinemaDbContext(DbContextOptions<GrandCinemaDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Movie> Movies => Set<Movie>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Screening> Screenings => Set<Screening>();
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<ReservedSeat> ReservedSeats => Set<ReservedSeat>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(GrandCinemaDbContext).Assembly);
    }
}
```

Le constructeur qui reçoit `DbContextOptions` permet l'enregistrement par injection de dépendances (étape 7c) ; le nom de chaque `DbSet` devient le nom de la table ; `ApplyConfigurationsFromAssembly` charge automatiquement toutes les classes de `Configurations/`.

Modèle d'une configuration, `Configurations/ScreeningConfiguration.cs` :

```csharp
using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GrandCinema.Infrastructure.Persistence.Configurations;

public sealed class ScreeningConfiguration : IEntityTypeConfiguration<Screening>
{
    public void Configure(EntityTypeBuilder<Screening> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StartsAt).HasColumnType("timestamp without time zone");
        builder.Property(s => s.Price).HasPrecision(10, 2);

        builder.HasOne(s => s.Movie).WithMany().HasForeignKey(s => s.MovieId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Room).WithMany().HasForeignKey(s => s.RoomId).OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.RoomId, s.StartsAt });
    }
}
```

Les autres configurations, sur le même modèle :

| Configuration | Règles |
|---|---|
| `MovieConfiguration` | `Title` 200, `Genre` 50, `PosterUrl` 500 caractères ; `Synopsis` sans limite (`text`) |
| `RoomConfiguration` | `Name` 50 caractères, unique ; `ToTable(t => t.HasCheckConstraint("CK_Rooms_Capacity", "\"Capacity\" > 0"))` |
| `SeatConfiguration` | `Row` 5 caractères ; relation `HasOne(s => s.Room).WithMany(r => r.Seats)` ; index unique `(RoomId, Row, Number)` |
| `UserConfiguration` | `Email` 256, unique ; `PasswordHash` 256 ; `FirstName` et `LastName` 100 ; `Role` en texte : `HasConversion<string>().HasMaxLength(20)` |
| `ReservationConfiguration` | `Status` en texte (20) ; `TotalAmount` `HasPrecision(10, 2)` ; clés étrangères vers `User` et `Screening` |
| `ReservedSeatConfiguration` | clé primaire `HasKey(rs => new { rs.ReservationId, rs.SeatId })` ; clé étrangère sans navigation `HasOne<Screening>().WithMany().HasForeignKey(rs => rs.ScreeningId)` ; **index unique `(ScreeningId, SeatId)`** |

Choix communs :

- pas de `IsRequired()` : avec la nullabilité activée, une propriété non nullable devient déjà une colonne `NOT NULL` ;
- suppressions en `DeleteBehavior.Restrict` partout, sauf `ReservedSeats → Reservations` en `Cascade` (supprimer une réservation libère ses places) : on ne supprime pas par accident un film ou une salle encore référencés ;
- énumérations stockées en texte : lisibles en SQL, et insensibles à un changement d'ordre des valeurs dans le code ;
- dates : Npgsql mappe un `DateTime` en `timestamp with time zone` et refuse alors d'écrire une date qui n'est pas en UTC. `StartsAt` est l'heure locale du cinéma, d'où `timestamp without time zone` ; `CreatedAt` et `ExpiresAt` sont des instants techniques, gardés en `timestamp with time zone` et toujours écrits en UTC (`DateTime.UtcNow`) ; `ReleaseDate` (`DateOnly`) devient `date`.

#### Étape 7c – Enregistrer le DbContext dans l'API

`dotnet ef`, lancé avec `--startup-project src/GrandCinema.Api`, construit le DbContext à partir du `Program.cs` de l'API. Sans enregistrement, la migration échoue (« Unable to create a DbContext »).

`src/GrandCinema.Infrastructure/DependencyInjection.cs` :

```csharp
using GrandCinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GrandCinema.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is missing.");

        services.AddDbContext<GrandCinemaDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
```

Dans `src/GrandCinema.Api/Program.cs`, avant `builder.Build()` (après, la liste des services est figée) :

```csharp
using GrandCinema.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();
```

Aucun paquet à ajouter : les abstractions de configuration et d'injection de dépendances arrivent avec EF Core.

Contrôle :

```bash
dotnet build
dotnet ef dbcontext info --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
```

Résultat attendu : le type `GrandCinema.Infrastructure.Persistence.GrandCinemaDbContext`, le fournisseur `Npgsql.EntityFrameworkCore.PostgreSQL` et le nom de la base.

_À quoi la sous-étape a servi : à rendre le DbContext disponible pour l'API et pour l'outil de migration, avec la chaîne de connexion des user-secrets. Pourquoi : en Clean Architecture, l'API ne connaît pas les détails de la base ; elle appelle `AddInfrastructure`, et Infrastructure choisit PostgreSQL et EF Core. Cette méthode enregistrera aussi les repositories (étape 7 bis). Le `throw` donne une erreur claire au démarrage si la chaîne manque, par exemple sur une machine où les user-secrets n'ont pas été recréés. `dotnet ef` utilise l'environnement `Development` par défaut, d'où l'accès aux user-secrets._

#### Étape 7d – Migration initiale

PostgreSQL doit tourner :

```bash
docker compose up -d
docker compose ps
```

Générer la migration (cette commande ne se connecte pas à la base : elle compare le modèle au dernier état connu) :

```bash
dotnet ef migrations add InitialSchema --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api --output-dir Persistence/Migrations
```

Trois fichiers sont créés dans `Persistence/Migrations/` : la migration (`Up()` crée, `Down()` annule), ses métadonnées (`.Designer.cs`) et le `GrandCinemaDbContextModelSnapshot.cs`, état du modèle qui sert de point de comparaison à la migration suivante.

**Relire la migration avant de l'appliquer** : les 7 tables, les index uniques (`Email`, `Rooms.Name`, `(RoomId, Row, Number)`, `(ScreeningId, SeatId)`), la contrainte `CHECK`, les types `numeric(10,2)`, `date`, `timestamp without time zone` pour `StartsAt`, les `Restrict` et l'unique `Cascade`. En cas d'erreur, corriger la configuration puis :

```bash
dotnet ef migrations remove --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
```

Appliquer la migration (cette commande se connecte à la base) :

```bash
dotnet ef database update --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
```

Vérifier (utilisateur et base : les valeurs `POSTGRES_USER` et `POSTGRES_DB` du `.env`) :

```bash
dotnet ef migrations list --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
dotnet ef migrations has-pending-model-changes --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
docker compose exec db psql -U <utilisateur> -d <base> -c "\dt"
```

Résultats attendus : `InitialSchema` sans la mention « (Pending) » ; « No changes have been made to the model since the last migration » ; les 7 tables et `__EFMigrationsHistory`.

_À quoi la sous-étape a servi : à créer réellement les tables dans PostgreSQL à partir du modèle C#. Pourquoi : la migration est versionnée avec le code, donc le schéma évolue avec lui et se reconstruit à l'identique sur toute machine. Elle n'est pas appliquée au démarrage de l'API (pas de `Database.Migrate()` dans `Program.cs`) : une API qui modifie son schéma en démarrant est risquée en production, par exemple si plusieurs instances démarrent ensemble. En local, on l'applique à la main ; en production, on appliquera le script SQL ou un « migration bundle » lors du déploiement._

#### Étape 7e – Livrables D1 Q2 : script SQL et schéma entité-relation

```bash
mkdir -p docs/database
dotnet ef migrations script --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api -o docs/database/schema.sql
```

(Sous PowerShell uniquement : `mkdir docs/database` suffit.)

L'option `--idempotent` produit un script qui vérifie `__EFMigrationsHistory` et peut être rejoué sans erreur : utile au déploiement, pas nécessaire pour le livrable.

Créer `docs/database/erd.md` : schéma entité-relation en Mermaid (`erDiagram`, affiché directement par GitHub ; dans VS Code, aperçu avec Ctrl+Shift+V et l'extension *Markdown Preview Mermaid Support*), puis le rôle des tables, les contraintes clés, les règles de suppression, les règles vérifiées dans le code et le choix des types de dates.

#### Étape 7f – Enregistrer

```bash
dotnet build
dotnet test
git status        # ni .env, ni TestResults/, ni bin/, ni obj/
git add .
git commit -m "feat(db): add initial schema and migration"
git push -u origin chore/database-schema
```

Ouvrir la Pull Request vers `dev` avec la checklist : la migration s'applique sur une base vide ; `schema.sql` et `erd.md` sont versionnés ; les tests sont verts ; aucun secret n'est commité.

- **Vérification :** la migration s'applique sur une base vide ; le script SQL et le schéma sont versionnés dans docs/database. Commit : **\_\_**.

_À quoi l'étape a servi : à traduire le besoin métier (films, séances, places, réservations, utilisateurs) en tables, avec leur script SQL et leur schéma. Pourquoi : c'est le livrable du D1 Q2 ; la contrainte d'unicité sur séance et place est la première défense contre la double vente, qui sera le bug étudié au Dossier 3._

### Étape 7 bis – Suite de l'US 1.1 : ScreeningReadRepository [x] (date et PR à noter)

- **Quoi :** implémenter `IScreeningReadRepository` dans Infrastructure avec EF Core, le vérifier par un test d'intégration sur un vrai PostgreSQL, ajouter l'index sur `StartsAt` qu'exige sa requête, puis l'enregistrer dans l'injection de dépendances.
- **Pourquoi :** le gestionnaire `GetScheduleHandler` est testé avec une doublure (Moq) ; le filtre par jour, lui, s'exécute en SQL et ne peut être prouvé que contre PostgreSQL (remarque de l'étape 5c). L'index existant `(RoomId, StartsAt)` commence par `RoomId` : PostgreSQL ne peut pas s'en servir pour une recherche sur `StartsAt` seul, toutes salles confondues.

```bash
git switch dev
git pull
git switch -c feature/us-1-1-screening-repository dev
```

#### Étape 7 bis a – Projet de tests d'intégration

```bash
dotnet new xunit -n GrandCinema.Infrastructure.Tests -o tests/GrandCinema.Infrastructure.Tests
dotnet sln GrandCinema.slnx add tests/GrandCinema.Infrastructure.Tests
dotnet add tests/GrandCinema.Infrastructure.Tests reference src/GrandCinema.Infrastructure
dotnet add tests/GrandCinema.Infrastructure.Tests package Testcontainers.PostgreSql
dotnet add tests/GrandCinema.Infrastructure.Tests package Shouldly
rm tests/GrandCinema.Infrastructure.Tests/UnitTest1.cs
```

Testcontainers démarre un conteneur PostgreSQL jetable le temps des tests (Docker doit tourner) : même moteur qu'en production, sans toucher à la base de développement.

Créer `tests/GrandCinema.Infrastructure.Tests/PostgreSqlFixture.cs`, qui démarre le conteneur une fois pour la classe de tests et y applique les migrations :

```csharp
using GrandCinema.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace GrandCinema.Infrastructure.Tests;

public sealed class PostgreSqlFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public GrandCinemaDbContext CreateDbContext() =>
        new(new DbContextOptionsBuilder<GrandCinemaDbContext>().UseNpgsql(_container.GetConnectionString()).Options);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        await using var db = CreateDbContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _container.DisposeAsync().AsTask();
}
```

`MigrateAsync` applique les vraies migrations : chaque lancement vérifie aussi qu'elles fonctionnent sur une base vide. La signature d'`IAsyncLifetime` ci-dessus est celle de xUnit 2 (version utilisée par les autres projets de tests) ; xUnit 3 utilise `ValueTask`. L'image est passée au constructeur de `PostgreSqlBuilder` : le constructeur sans paramètre est obsolète dans Testcontainers 4.

#### Étape 7 bis b – Rouge : le filtre par jour

Créer `tests/GrandCinema.Infrastructure.Tests/Persistence/Repositories/ScreeningReadRepositoryTests.cs` :

```csharp
using GrandCinema.Domain.Entities;
using GrandCinema.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Repositories;

public class ScreeningReadRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;

    public ScreeningReadRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_screenings_exist_on_several_days_Then_only_those_of_the_chosen_day_are_returned()
    {
        // Arrange
        var movie = new Movie { Id = Guid.NewGuid(), Title = "Blue Note", Synopsis = "A documentary about the jazz label", DurationMinutes = 136, Genre = "Documentary", ReleaseDate = new DateOnly(2023, 10, 1), PosterUrl = "https://example.com/poster.jpg" };
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        Screening At(DateTime startsAt) => new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9m };

        await using (var seed = _database.CreateDbContext())
        {
            seed.Screenings.AddRange(
                At(new DateTime(2026, 7, 18, 23, 30, 0)),
                At(new DateTime(2026, 7, 19, 0, 0, 0)),
                At(new DateTime(2026, 7, 19, 21, 0, 0)),
                At(new DateTime(2026, 7, 20, 0, 0, 0)));
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new ScreeningReadRepository(db);

        // Act
        var result = await repository.GetScreeningsAsync(new DateOnly(2026, 7, 19));

        // Assert
        result.Select(s => s.StartsAt).ShouldBe(new[] { new DateTime(2026, 7, 19, 0, 0, 0), new DateTime(2026, 7, 19, 21, 0, 0) }, ignoreOrder: true);
        result.ShouldAllBe(s => s.Movie.Title == "Blue Note" && s.Room.Name == "Salle 1");
    }
}
```

Points clés du test :

- les séances de 23 h 30 la veille et de minuit le lendemain encadrent la journée : elles vérifient les deux bornes du filtre ;
- la lecture se fait avec un **nouveau** DbContext : celui qui a inséré les données garde les entités en mémoire et remplirait `Movie` et `Room` même sans `Include`, ce qui masquerait un oubli ;
- la dernière assertion vérifie que le film et la salle sont chargés, car le gestionnaire s'en sert (titre, nom de salle) ;
- le trait `Category=Integration` permet de lancer les tests unitaires seuls, sans Docker : `dotnet test --filter "Category!=Integration"`.

Squelette minimal pour compiler, `src/GrandCinema.Infrastructure/Persistence/Repositories/ScreeningReadRepository.cs` :

```csharp
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class ScreeningReadRepository : IScreeningReadRepository
{
    private readonly GrandCinemaDbContext _db;

    public ScreeningReadRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Screening>> GetScreeningsAsync(DateOnly date, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
```

```bash
dotnet test --filter "US=1.1"
```

Résultat attendu : le test d'intégration échoue (`NotImplementedException`), les 6 tests unitaires restent verts.

```bash
git add .
git commit -m "test(us-1.1): red - screenings filtered by day in PostgreSQL"
```

#### Étape 7 bis c – Vert : la requête

```csharp
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class ScreeningReadRepository : IScreeningReadRepository
{
    private readonly GrandCinemaDbContext _db;

    public ScreeningReadRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<Screening>> GetScreeningsAsync(DateOnly date, CancellationToken cancellationToken = default)
    {
        var start = date.ToDateTime(TimeOnly.MinValue);
        var end = start.AddDays(1);

        return await _db.Screenings
            .AsNoTracking()
            .Include(s => s.Movie)
            .Include(s => s.Room)
            .Where(s => s.StartsAt >= start && s.StartsAt < end)
            .ToListAsync(cancellationToken);
    }
}
```

- **Intervalle `[début du jour ; lendemain[`** plutôt que `s.StartsAt.Date == ...` : une fonction appliquée à la colonne empêche PostgreSQL d'utiliser un index ; une comparaison directe sur la colonne le permet. La borne de fin est exclue, d'où `<` : minuit le lendemain appartient au jour suivant.
- **`AsNoTracking`** : lecture seule (côté query du CQRS), EF ne suit pas les entités, ce qui est plus rapide.
- **`Include`** : charge le film et la salle dans la même requête SQL (jointure), sans requête supplémentaire par séance.
- Pas de tri ici : il est fait par le gestionnaire et déjà couvert par les tests unitaires (cycles 5 et 6).

```bash
dotnet test --filter "US=1.1"
```

Résultat attendu : 7 tests réussis.

```bash
git add .
git commit -m "feat(us-1.1): green - screenings filtered by day in PostgreSQL"
```

#### Étape 7 bis d – Index sur StartsAt

_À quoi sert cette sous-étape : à rendre la requête de la programmation plus rapide, sans rien changer à son résultat (le test d'intégration passe déjà sans index). Un index fonctionne comme l'index d'un livre : sans lui, PostgreSQL lit toutes les séances de la table, y compris celles des années passées, pour garder celles du jour demandé ; avec lui, il dispose d'une liste triée par date et heure, va directement au début du jour et s'arrête au lendemain. Pourquoi ici : la programmation est la page la plus consultée du site, et la table `Screenings` ne fait que grandir (les anciennes séances restent pour l'historique des réservations). Pourquoi un nouvel index alors que `(RoomId, StartsAt)` existe : un index composite est trié d'abord par sa première colonne, comme un annuaire trié par nom puis par prénom ; il sert pour une recherche sur `RoomId`, ou sur `RoomId` puis `StartsAt`, mais pas sur `StartsAt` seul (règle du préfixe gauche), or la programmation cherche toutes salles confondues. Chaque index correspond donc à une requête réelle : `(RoomId, StartsAt)` pour vérifier qu'une salle n'a pas deux séances qui se chevauchent, `(StartsAt)` pour la programmation du jour. Avec quelques centaines de séances, la différence est imperceptible : l'intérêt est de montrer au jury une démarche (un index justifié par une requête, et prouvé par `EXPLAIN`), plutôt que des index posés au hasard._

La requête filtre sur `StartsAt` seul. Dans `ScreeningConfiguration`, ajouter :

```csharp
builder.HasIndex(s => s.StartsAt);
```

L'index `(RoomId, StartsAt)` est conservé : il servira à vérifier qu'une salle n'a pas deux séances qui se chevauchent.

```bash
dotnet ef migrations add AddScreeningStartsAtIndex --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api
dotnet ef migrations script --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api -o docs/database/schema.sql
dotnet test --filter "US=1.1"
```

La migration doit contenir un seul `CreateIndex` (`IX_Screenings_StartsAt`). Le test d'intégration rejoue toutes les migrations et reste vert. Mettre à jour la section « Contraintes clés » de `docs/database/erd.md` (index non uniques).

Pour montrer l'index au jury, demander le plan d'exécution. Sur une table presque vide, PostgreSQL préfère lire toute la table : désactiver ce choix le temps de la démonstration.

```bash
docker compose exec db psql -U <utilisateur> -d <base> -c "SET enable_seqscan = off; EXPLAIN SELECT * FROM \"Screenings\" WHERE \"StartsAt\" >= '2026-07-19' AND \"StartsAt\" < '2026-07-20';"
```

Résultat attendu : `Index Scan using "IX_Screenings_StartsAt"`.

```bash
git add .
git commit -m "feat(db): add index on screening start time"
```

#### Étape 7 bis e – Enregistrement dans l'injection de dépendances

Dans `AddInfrastructure`, après `AddDbContext` :

```csharp
services.AddScoped<IScreeningReadRepository, ScreeningReadRepository>();
```

`Scoped` : une instance par requête HTTP, comme le DbContext qu'elle utilise.

```bash
dotnet build
dotnet test --filter "US=1.1"
git add .
git commit -m "feat(us-1.1): register screening repository"
git push -u origin feature/us-1-1-screening-repository
```

Ouvrir la Pull Request vers `dev`.

- **Vérification :** 7 tests US 1.1 verts, dont le test d'intégration ; la migration `AddScreeningStartsAtIndex` est appliquée ; le plan d'exécution utilise `IX_Screenings_StartsAt`. PR : **\_\_**.

_À quoi l'étape a servi : à brancher le cas d'usage de l'US 1.1 sur la vraie base, et à prouver contre PostgreSQL ce que le test unitaire ne pouvait pas prouver : seules les séances du jour demandé sont renvoyées, bornes comprises, avec leur film et leur salle. Pourquoi : un test d'intégration sur le même moteur qu'en production détecte les erreurs de traduction SQL et de mapping ; l'index est ajouté parce qu'une requête réelle le demande, et le plan d'exécution le prouve._

Reste de l'US 1.1 : l'endpoint `GET /api/schedule` et Swagger, à l'étape 7 ter.

### Étape 7 ter – Fin de l'US 1.1 : endpoint et Swagger [x] (date et PR à noter)

- **Quoi :** enregistrer le gestionnaire de l'US 1.1 dans l'injection de dépendances, exposer l'endpoint `GET /api/schedule`, le documenter avec Swagger, puis l'essayer avec des données de démonstration.
- **Pourquoi :** jusqu'ici, la logique (gestionnaire) et l'accès aux données (repository) existent et sont testés, mais rien n'est appelable en HTTP. L'endpoint rend l'US 1.1 utilisable par le front Angular ; Swagger donne une documentation interactive de l'API, utile au front, au jury et aux essais manuels.

```bash
git switch dev
git pull
git switch -c feature/us-1-1-schedule-endpoint dev
```

#### Étape 7 ter a – Enregistrer les services de la couche Application

La couche Application a besoin des abstractions d'injection de dépendances pour déclarer ses propres services :

```bash
dotnet add src/GrandCinema.Application package Microsoft.Extensions.DependencyInjection.Abstractions
```

Créer `src/GrandCinema.Application/DependencyInjection.cs`, sur le modèle de celui d'Infrastructure :

```csharp
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Screenings.DTOs;
using GrandCinema.Application.Features.Screenings.Queries;
using Microsoft.Extensions.DependencyInjection;

namespace GrandCinema.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<IQueryHandler<GetScheduleQuery, ScheduleDto>, GetScheduleHandler>();

        return services;
    }
}
```

- `TimeProvider.System` est l'horloge réelle, remplacée par `FakeTimeProvider` dans les tests : c'est grâce à cette abstraction que le cycle 4 a pu fixer la date.
- Chaque nouveau gestionnaire (US suivantes) s'ajoute ici, sur une ligne.

#### Étape 7 ter b – L'endpoint

Créer `src/GrandCinema.Api/Endpoints/Screenings/ScheduleEndpoints.cs` :

```csharp
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Screenings.DTOs;
using GrandCinema.Application.Features.Screenings.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Screenings;

public static class ScheduleEndpoints
{
    public static IEndpointRouteBuilder MapScheduleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/schedule", GetSchedule)
            .WithName("GetSchedule")
            .WithTags("Schedule")
            .WithSummary("Movies and screenings of a day")
            .WithDescription("Returns the movies showing on the given date (today if omitted), with their screenings sorted by time.");

        return app;
    }

    private static async Task<Ok<ScheduleDto>> GetSchedule(
        DateOnly? date,
        IQueryHandler<GetScheduleQuery, ScheduleDto> handler,
        CancellationToken cancellationToken)
    {
        var schedule = await handler.Handle(new GetScheduleQuery(date), cancellationToken);
        return TypedResults.Ok(schedule);
    }
}
```

- `DateOnly? date` est lu dans l'URL (`?date=2026-07-18`) ; absent, il vaut `null` et le gestionnaire prend la date du jour. Une date invalide (`?date=abc`) est refusée automatiquement avec un code 400.
- Le gestionnaire est demandé par son interface : l'injection de dépendances fournit `GetScheduleHandler`, qui reçoit lui-même le repository et l'horloge.
- `CancellationToken` : si le navigateur abandonne la requête, la requête SQL en cours est annulée.
- `TypedResults.Ok` (plutôt que `Results.Ok`) donne un type de retour précis, `Ok<ScheduleDto>` : Swagger en déduit seul la forme de la réponse 200.
- `WithName`, `WithTags`, `WithSummary` et `WithDescription` alimentent la documentation Swagger.
- Rangement : un dossier par fonctionnalité, le même que dans Application (`Endpoints/Screenings/` répond à `Features/Screenings/`), et un fichier par cas d'usage, comme un gestionnaire par cas d'usage dans Application. Les places restantes d'une séance (US 2.2) iront dans `Endpoints/Screenings/RemainingSeatsEndpoints.cs`, une réservation dans `Endpoints/Reservations/`. Tout ce qui concerne une fonctionnalité porte le même nom dans les deux couches, et `Program.cs` reste court : une ligne `Map...Endpoints()` par cas d'usage.

#### Étape 7 ter c – Swagger

Depuis .NET 9, ASP.NET Core génère lui-même le document OpenAPI (la description de l'API au format JSON) ; l'interface web Swagger UI s'ajoute à part, pour l'afficher et essayer les endpoints.

```bash
dotnet add src/GrandCinema.Api package Microsoft.AspNetCore.OpenApi
dotnet add src/GrandCinema.Api package Swashbuckle.AspNetCore.SwaggerUI
```

Remplacer le contenu de `src/GrandCinema.Api/Program.cs` (l'endpoint « Hello World » de départ disparaît) :

```csharp
using GrandCinema.Api.Endpoints.Screenings;
using GrandCinema.Application;
using GrandCinema.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

var app = builder.Build();

if (app.Configuration.GetValue<bool>("Swagger:Enabled"))
{
    app.MapOpenApi();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "Grand Cinema API v1"));
}

app.MapHealthChecks("/health");
app.MapScheduleEndpoints();

app.Run();
```

Activer Swagger par configuration : désactivé par défaut, activé en développement.

Dans `src/GrandCinema.Api/appsettings.json`, ajouter :

```json
"Swagger": {
  "Enabled": false
}
```

Dans `src/GrandCinema.Api/appsettings.Development.json`, ajouter :

```json
"Swagger": {
  "Enabled": true
}
```

Pour que `dotnet run` ouvre directement Swagger, ajouter `"launchUrl": "swagger",` dans les profils `http` et `https` de `src/GrandCinema.Api/Properties/launchSettings.json`.

- `/openapi/v1.json` : le document OpenAPI ; `/swagger` : l'interface web.
- `/health` répond `Healthy` tant que l'application tourne. Il servira au déploiement (étapes 10 et 11) pour vérifier qu'une nouvelle version a bien démarré.
- En production, Swagger s'active le temps d'une démonstration avec la variable d'environnement `Swagger__Enabled=true` (le double tiret bas remplace le `:` des sections de configuration). Désactivé, il ne dévoile pas la liste des endpoints à n'importe quel visiteur.

```bash
dotnet build
dotnet test
```

#### Étape 7 ter d – Données de démonstration et essai

La base de développement est vide : la programmation renverrait une liste vide. Créer `docs/database/seed-dev.sql` :

```sql
-- Demo data for local development: one room, two movies, screenings today and tomorrow.
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
```

Le charger dans la base de développement (utilisateur et base : valeurs du `.env`) :

```bash
docker compose exec -T db psql -U <utilisateur> -d <base> < docs/database/seed-dev.sql
```

_Pourquoi un script SQL plutôt qu'un seed en C# : d'abord pour montrer au jury la maîtrise du SQL lui-même, et pas seulement d'EF Core : insertion de plusieurs lignes, identifiants explicites pour relier films, salles et séances, `ON CONFLICT DO NOTHING` pour pouvoir rejouer le script sans erreur, dates calculées par PostgreSQL (`CURRENT_DATE + TIME '18:30'`) pour que la programmation du jour soit toujours remplie. Ensuite parce que le même fichier se rejoue tel quel sur la base de production, pour une démonstration (étapes 10i et 10 G g). Alternatives écartées : `HasData` dans les configurations EF écrit les lignes dans la migration, donc les enverrait aussi en production, avec des dates figées le jour de la génération, et chaque retouche des données créerait une nouvelle migration (il est réservé aux données réelles et stables, comme les salles et les places du cinéma) ; `UseSeeding` (EF Core 9), activable en développement seulement, aurait convenu, mais aurait caché le SQL derrière du C#._

Les séances de démonstration sont datées du jour où le script est chargé : le lendemain, elles sont passées. Pour les remettre à aujourd'hui et demain, créer `scripts/reset-demo-screenings.sh` :

```bash
#!/usr/bin/env bash
# Resets the demo screenings of the development database: deletes them, then reloads docs/database/seed-dev.sql,
# so that the demo schedule is always dated today and tomorrow.
# Demo screenings are recognised by their identifier, which starts with 33333333- (see seed-dev.sql).
# Usage, from anywhere in the repository: scripts/reset-demo-screenings.sh
set -euo pipefail

cd "$(dirname "$0")/.."

env_value() {
    grep -E "^$1=" .env | head -n 1 | cut -d '=' -f 2- | tr -d '\r'
}

DB_USER=$(env_value POSTGRES_USER)
DB_NAME=$(env_value POSTGRES_DB)

if [ -z "$(docker compose ps --quiet --status running db)" ]; then
    echo "PostgreSQL is not running: start it with 'docker compose up -d'." >&2
    exit 1
fi

{
    cat <<'SQL'
DELETE FROM "Reservations" WHERE "ScreeningId"::text LIKE '33333333-%';
DELETE FROM "Screenings" WHERE "Id"::text LIKE '33333333-%';
SQL
    cat docs/database/seed-dev.sql
} | docker compose exec -T db psql --quiet --single-transaction -v ON_ERROR_STOP=1 -U "$DB_USER" -d "$DB_NAME"

echo "Demo screenings reset for today ($(date +%F)) and tomorrow."
```

```bash
chmod +x scripts/reset-demo-screenings.sh
scripts/reset-demo-screenings.sh
```

Sous Windows (Git Bash), lancer `bash scripts/reset-demo-screenings.sh`.

Ce que fait le script :

- se place à la racine du dépôt, d'où qu'il soit lancé, et lit l'utilisateur et la base dans `.env` sans exécuter ce fichier (ni lire le mot de passe : la connexion se fait de l'intérieur du conteneur) ; `tr -d '\r'` tolère un `.env` enregistré avec des fins de ligne Windows ;
- s'arrête avec un message clair si PostgreSQL n'est pas démarré ;
- supprime **uniquement** les séances de démonstration, reconnues à leur identifiant qui commence par `33333333-` : les séances créées à la main pendant les essais sont conservées. Les réservations faites sur ces séances sont supprimées d'abord (leurs places réservées suivent, par la suppression en cascade), sinon la clé étrangère en `Restrict` bloquerait la suppression ;
- recharge `seed-dev.sql` : les films et la salle existent déjà et sont ignorés (`ON CONFLICT DO NOTHING`), les séances sont recréées aux dates du jour ;
- exécute le tout en **une seule transaction** (`--single-transaction`, avec `ON_ERROR_STOP` qui arrête à la première erreur) : en cas d'erreur, rien n'est supprimé.

Lancer l'API :

```bash
dotnet run --project src/GrandCinema.Api
```

Essais :

- `http://localhost:5073/swagger` : l'endpoint `GET /api/schedule` apparaît sous « Schedule », avec son résumé ; **Try it out**, puis **Execute** sans date : deux films du jour, Blue Note (18 h 30, 21 h) puis Metropolis (14 h) ;
- même essai avec la date de demain : Metropolis à 20 h seulement ;
- `?date=abc` : code 400 ;
- `http://localhost:5073/health` : `Healthy`.

Ou en ligne de commande :

```bash
curl http://localhost:5073/api/schedule
curl "http://localhost:5073/api/schedule?date=2026-07-19"
```

Pas de test automatisé pour l'endpoint à ce stade : il ne contient aucune logique (il traduit la requête HTTP en `GetScheduleQuery`), et ce qu'il appelle est couvert par les tests unitaires et le test d'intégration. Des tests de l'API complète (`WebApplicationFactory`) sont prévus en phase 6.

#### Étape 7 ter e – Enregistrer

```bash
git add .
git commit -m "feat(us-1.1): add schedule endpoint and Swagger"
git push -u origin feature/us-1-1-schedule-endpoint
```

Ouvrir la Pull Request vers `dev`. La programmation par jour est alors en place ; l'US 1.1 sera terminée avec la liste des films à l'affiche (étape 7 quater).

- **Vérification :** Swagger affiche `GET /api/schedule` ; la programmation du jour s'affiche avec les données de démonstration ; `/health` répond `Healthy` ; tous les tests sont verts. PR : **\_\_**.

_À quoi l'étape a servi : à rendre l'US 1.1 appelable en HTTP et documentée. Pourquoi : l'endpoint ne fait que traduire le HTTP en requête CQRS, toute la logique reste testée dans les couches internes ; l'enregistrement par `AddApplication` et `AddInfrastructure` garde `Program.cs` court, chaque couche déclarant ses propres services. Swagger est piloté par la configuration pour être visible en développement et activable en production le temps d'une démonstration, sans exposer en permanence la carte de l'API._

### Étape 7 quater – Films à l'affiche : `GET /api/movies` [ ]

- **Quoi :** exposer la liste des films actuellement programmés, c'est-à-dire ayant au moins une séance à venir, triés par titre, pour la page « Films à l'affiche » du front.
- **Pourquoi :** `/api/schedule` répond à une autre question (les séances d'un jour donné) ; la liste des films proposés est une autre ressource, nommée d'après ce qu'elle renvoie : des films. Une fois la règle écrite dans le backlog, elle se développe en TDD comme l'US 1.1.

**1. Écrire la règle dans le backlog.** Décision du Product Owner, ajoutée comme critère à l'issue de l'US 1.1 sur GitHub : « Je vois la liste des films actuellement programmés (au moins une séance à venir), triés par titre ». « À venir » : la séance commence maintenant ou plus tard, à l'heure du cinéma ; une séance commencée ne compte plus. Un film dont la dernière séance du jour est passée disparaît donc de la liste.

**2. Liste des tests.**

| Comportement | Test |
|---|---|
| Le repository est interrogé à partir de l'heure actuelle du cinéma | Cycle 1 : `When_movies_are_requested_Then_the_repository_is_queried_from_now` |
| Les films programmés sont renvoyés | Cycle 2 : `When_a_movie_is_scheduled_Then_it_is_returned` |
| Ils sont triés par titre | Cycle 3 : `When_several_movies_are_scheduled_Then_they_are_sorted_by_title` |
| Seuls les films ayant une séance à venir sont retenus, chacun une seule fois | Test d'intégration : `When_movies_have_past_and_upcoming_screenings_Then_only_those_with_an_upcoming_screening_are_returned` |

Comme pour l'US 1.1, le gestionnaire fixe l'heure et met en forme ; le filtre « au moins une séance à venir » s'exécute en SQL, et ne se prouve que contre PostgreSQL.

```bash
git switch dev
git pull
git switch -c feature/us-1-1-movies-showing dev
mkdir -p src/GrandCinema.Application/Features/Movies/DTOs/Mappings src/GrandCinema.Application/Features/Movies/Queries
mkdir -p tests/GrandCinema.Application.Tests/Features/Movies/Queries
```

Nouvelle fonctionnalité `Movies` dans Application (et plus loin `Endpoints/Movies` dans l'Api), selon la convention de rangement par fonctionnalité.

#### Étape 7 quater a – Cycle 1 : le repository est interrogé à partir de maintenant

**Rouge.** Créer `tests/GrandCinema.Application.Tests/Features/Movies/Queries/GetMoviesShowingHandlerTests.cs` :

```csharp
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Movies.Queries;
using GrandCinema.Domain.Entities;
using Microsoft.Extensions.Time.Testing;
using Moq;
using Shouldly;

namespace GrandCinema.Application.Tests.Features.Movies.Queries;

public class GetMoviesShowingHandlerTests
{
    private readonly Mock<IMovieReadRepository> _movies = new();
    private readonly FakeTimeProvider _time = new(new DateTimeOffset(2026, 7, 18, 10, 0, 0, TimeSpan.Zero));

    private static Movie NewMovie(string title) => new()
    {
        Id = Guid.NewGuid(), Title = title, Synopsis = "Synopsis", DurationMinutes = 100,
        Genre = "Drama", ReleaseDate = new DateOnly(2025, 1, 1), PosterUrl = "https://example.com/p.jpg"
    };

    [Fact]
    [Trait("US", "1.1")]
    public async Task When_movies_are_requested_Then_the_repository_is_queried_from_now()
    {
        // Arrange
        _time.SetLocalTimeZone(TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris"));
        _movies
            .Setup(r => r.GetMoviesScheduledFromAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie>());
        var handler = new GetMoviesShowingHandler(_movies.Object, _time);

        // Act
        await handler.Handle(new GetMoviesShowingQuery());

        // Assert
        _movies.Verify(r => r.GetMoviesScheduledFromAsync(new DateTime(2026, 7, 18, 12, 0, 0), It.IsAny<CancellationToken>()), Times.Once);
    }
}
```

L'horloge simulée indique 10 h UTC le 18 juillet, soit 12 h à Paris (heure d'été) : le repository doit être interrogé avec l'heure du cinéma, pas l'heure UTC.

Squelettes minimaux pour compiler :

`src/GrandCinema.Application/Abstractions/Persistence/IMovieReadRepository.cs` :

```csharp
using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Abstractions.Persistence;

public interface IMovieReadRepository
{
    Task<IReadOnlyList<Movie>> GetMoviesScheduledFromAsync(DateTime from, CancellationToken cancellationToken = default);
}
```

`src/GrandCinema.Application/Features/Movies/DTOs/MovieDto.cs` :

```csharp
namespace GrandCinema.Application.Features.Movies.DTOs;

public record class MovieDto
{
    public required Guid MovieId { get; init; }
    public required string Title { get; init; }
    public required string Synopsis { get; init; }
    public required int DurationMinutes { get; init; }
    public required string Genre { get; init; }
    public required DateOnly ReleaseDate { get; init; }
    public required string PosterUrl { get; init; }
}
```

`src/GrandCinema.Application/Features/Movies/Queries/GetMoviesShowingQuery.cs` :

```csharp
namespace GrandCinema.Application.Features.Movies.Queries;

public sealed record class GetMoviesShowingQuery;
```

`src/GrandCinema.Application/Features/Movies/Queries/GetMoviesShowingHandler.cs` :

```csharp
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Application.Features.Movies.DTOs;

namespace GrandCinema.Application.Features.Movies.Queries;

public sealed class GetMoviesShowingHandler : IQueryHandler<GetMoviesShowingQuery, IReadOnlyList<MovieDto>>
{
    private readonly IMovieReadRepository _movieReadRepository;
    private readonly TimeProvider _timeProvider;

    public GetMoviesShowingHandler(IMovieReadRepository movieReadRepository, TimeProvider timeProvider)
    {
        _movieReadRepository = movieReadRepository;
        _timeProvider = timeProvider;
    }

    public Task<IReadOnlyList<MovieDto>> Handle(GetMoviesShowingQuery query, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
```

- `MovieDto` reprend les champs de `MovieScheduleDto`, sans les séances : un DTO par cas d'usage, taillé pour son écran. Partager le même DTO lierait deux fonctionnalités qui évolueront séparément.
- `GetMoviesShowingQuery` n'a aucun paramètre : la requête est toujours « les films à l'affiche maintenant ».

```bash
dotnet test --filter "US=1.1"
git add .
git commit -m "test(us-1.1): red - movies showing queried from now"
```

Résultat attendu : le nouveau test échoue (`NotImplementedException`).

**Vert.** Le minimum : lire l'heure du cinéma et interroger le repository.

```csharp
    public async Task<IReadOnlyList<MovieDto>> Handle(GetMoviesShowingQuery query, CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetLocalNow().DateTime;
        await _movieReadRepository.GetMoviesScheduledFromAsync(now, cancellationToken);

        return [];
    }
```

Renvoyer une liste vide est volontaire : aucun test n'exige encore de renvoyer les films, le cycle 2 s'en chargera.

```bash
dotnet test --filter "US=1.1"
git add .
git commit -m "feat(us-1.1): green - movies showing queried from now"
```

#### Étape 7 quater b – Cycle 2 : les films programmés sont renvoyés

**Rouge.** Ajouter dans la classe de tests :

```csharp
    [Fact]
    [Trait("US", "1.1")]
    public async Task When_a_movie_is_scheduled_Then_it_is_returned()
    {
        // Arrange
        var movie = NewMovie("Blue Note");
        _movies
            .Setup(r => r.GetMoviesScheduledFromAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { movie });
        var handler = new GetMoviesShowingHandler(_movies.Object, _time);

        // Act
        var result = await handler.Handle(new GetMoviesShowingQuery());

        // Assert
        var dto = result.ShouldHaveSingleItem();
        dto.MovieId.ShouldBe(movie.Id);
        dto.Title.ShouldBe("Blue Note");
    }
```

Résultat attendu : échec, la liste renvoyée est vide.

```bash
git add .
git commit -m "test(us-1.1): red - scheduled movie returned"
```

**Vert.** Créer la conversion `src/GrandCinema.Application/Features/Movies/DTOs/Mappings/MovieMappings.cs`, sur le modèle de `ScreenMappings` :

```csharp
using GrandCinema.Domain.Entities;

namespace GrandCinema.Application.Features.Movies.DTOs.Mappings;

internal static class MovieMappings
{
    public static MovieDto ToMovieDto(this Movie movie) => new()
    {
        MovieId = movie.Id,
        Title = movie.Title,
        Synopsis = movie.Synopsis,
        DurationMinutes = movie.DurationMinutes,
        Genre = movie.Genre,
        ReleaseDate = movie.ReleaseDate,
        PosterUrl = movie.PosterUrl
    };
}
```

Et dans le gestionnaire (ajouter `using GrandCinema.Application.Features.Movies.DTOs.Mappings;`) :

```csharp
        var now = _timeProvider.GetLocalNow().DateTime;
        var movies = await _movieReadRepository.GetMoviesScheduledFromAsync(now, cancellationToken);

        return movies
            .Select(m => m.ToMovieDto())
            .ToList();
```

```bash
dotnet test --filter "US=1.1"
git add .
git commit -m "feat(us-1.1): green - scheduled movie returned"
```

#### Étape 7 quater c – Cycle 3 : tri par titre

**Rouge.** Ajouter :

```csharp
    [Fact]
    [Trait("US", "1.1")]
    public async Task When_several_movies_are_scheduled_Then_they_are_sorted_by_title()
    {
        // Arrange
        _movies
            .Setup(r => r.GetMoviesScheduledFromAsync(It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Movie> { NewMovie("Zorro"), NewMovie("Blue Note"), NewMovie("Metropolis") });
        var handler = new GetMoviesShowingHandler(_movies.Object, _time);

        // Act
        var result = await handler.Handle(new GetMoviesShowingQuery());

        // Assert
        result.Select(m => m.Title).ShouldBe(new[] { "Blue Note", "Metropolis", "Zorro" });
    }
```

Résultat attendu : échec, les films sont dans l'ordre renvoyé par le repository.

```bash
git add .
git commit -m "test(us-1.1): red - movies showing sorted by title"
```

**Vert.** Ajouter le tri avant la conversion :

```csharp
        return movies
            .OrderBy(m => m.Title)
            .Select(m => m.ToMovieDto())
            .ToList();
```

```bash
dotnet test --filter "US=1.1"
git add .
git commit -m "feat(us-1.1): green - movies showing sorted by title"
```

Cette fois, chaque cycle a un vrai rouge : la phase verte n'a écrit que ce que le test en cours exigeait (comparer avec l'étape 5e bis).

#### Étape 7 quater d – Le repository, contre PostgreSQL

**Rouge.** Créer `tests/GrandCinema.Infrastructure.Tests/Persistence/Repositories/MovieReadRepositoryTests.cs` :

```csharp
using GrandCinema.Domain.Entities;
using GrandCinema.Infrastructure.Persistence.Repositories;
using Shouldly;

namespace GrandCinema.Infrastructure.Tests.Persistence.Repositories;

public class MovieReadRepositoryTests : IClassFixture<PostgreSqlFixture>
{
    private readonly PostgreSqlFixture _database;

    public MovieReadRepositoryTests(PostgreSqlFixture database)
    {
        _database = database;
    }

    [Fact]
    [Trait("US", "1.1")]
    [Trait("Category", "Integration")]
    public async Task When_movies_have_past_and_upcoming_screenings_Then_only_those_with_an_upcoming_screening_are_returned()
    {
        // Arrange
        var now = new DateTime(2026, 7, 18, 12, 0, 0);
        var room = new Room { Id = Guid.NewGuid(), Name = "Salle 1", Capacity = 170 };
        Movie NewMovie(string title) => new()
        {
            Id = Guid.NewGuid(), Title = title, Synopsis = "Synopsis", DurationMinutes = 100,
            Genre = "Drama", ReleaseDate = new DateOnly(2025, 1, 1), PosterUrl = "https://example.com/p.jpg"
        };
        var endedThisMorning = NewMovie("Ended this morning");
        var startsNow = NewMovie("Starts now");
        var twiceThisWeek = NewMovie("Twice this week");
        var neverScheduled = NewMovie("Never scheduled");
        Screening At(Movie movie, DateTime startsAt) => new() { Id = Guid.NewGuid(), Movie = movie, Room = room, StartsAt = startsAt, Price = 9m };

        await using (var seed = _database.CreateDbContext())
        {
            seed.Movies.Add(neverScheduled);
            seed.Screenings.AddRange(
                At(endedThisMorning, new DateTime(2026, 7, 18, 11, 0, 0)),
                At(startsNow, new DateTime(2026, 7, 18, 12, 0, 0)),
                At(twiceThisWeek, new DateTime(2026, 7, 19, 18, 0, 0)),
                At(twiceThisWeek, new DateTime(2026, 7, 20, 18, 0, 0)));
            await seed.SaveChangesAsync();
        }

        await using var db = _database.CreateDbContext();
        var repository = new MovieReadRepository(db);

        // Act
        var result = await repository.GetMoviesScheduledFromAsync(now);

        // Assert
        result.Select(m => m.Title).ShouldBe(new[] { "Starts now", "Twice this week" }, ignoreOrder: true);
    }
}
```

Chaque film du jeu de données vérifie un cas précis :

- « Ended this morning » : séance le même jour, mais une heure plus tôt. Exclu : la comparaison porte sur l'heure, pas seulement sur le jour ;
- « Starts now » : séance exactement à l'heure de la requête. Inclus : la borne est `>=` ;
- « Twice this week » : deux séances à venir. Présent **une seule fois** ;
- « Never scheduled » : aucune séance. Exclu.

Squelette `src/GrandCinema.Infrastructure/Persistence/Repositories/MovieReadRepository.cs` :

```csharp
using GrandCinema.Application.Abstractions.Persistence;
using GrandCinema.Domain.Entities;

namespace GrandCinema.Infrastructure.Persistence.Repositories;

public sealed class MovieReadRepository : IMovieReadRepository
{
    private readonly GrandCinemaDbContext _db;

    public MovieReadRepository(GrandCinemaDbContext db)
    {
        _db = db;
    }

    public Task<IReadOnlyList<Movie>> GetMoviesScheduledFromAsync(DateTime from, CancellationToken cancellationToken = default)
        => throw new NotImplementedException();
}
```

```bash
dotnet test --filter "US=1.1"
git add .
git commit -m "test(us-1.1): red - movies with an upcoming screening in PostgreSQL"
```

**Vert.** Ajouter `using Microsoft.EntityFrameworkCore;` et remplacer la méthode :

```csharp
    public async Task<IReadOnlyList<Movie>> GetMoviesScheduledFromAsync(DateTime from, CancellationToken cancellationToken = default)
    {
        return await _db.Movies
            .AsNoTracking()
            .Where(m => _db.Screenings.Any(s => s.MovieId == m.Id && s.StartsAt >= from))
            .ToListAsync(cancellationToken);
    }
```

- **`Any` devient `EXISTS` en SQL** : pour chaque film, PostgreSQL cherche une séance à venir et s'arrête à la première trouvée. Chaque film sort une seule fois, sans `Distinct` ; une jointure entre films et séances aurait renvoyé « Twice this week » deux fois.
- La recherche s'appuie sur l'index existant `IX_Screenings_MovieId`.
- L'heure `from` vient du gestionnaire, et non de `DateTime.Now` dans le repository : le fuseau du cinéma est géré à un seul endroit (`TimeProvider`, réglé par `TZ=Europe/Paris` en production), et le repository se teste avec une heure fixe.

```bash
dotnet test --filter "US=1.1"
git add .
git commit -m "feat(us-1.1): green - movies with an upcoming screening in PostgreSQL"
```

Résultat attendu : 9 tests unitaires et 2 tests d'intégration réussis. Pour vérifier que le test protège bien la règle, remplacer temporairement `>=` par `>`, puis `s.StartsAt >= from` par `s.StartsAt.Date >= from.Date` : le test d'intégration échoue chaque fois.

#### Étape 7 quater e – Enregistrement et endpoint

Dans `AddApplication` (avec les `using` de `Features.Movies.DTOs` et `Features.Movies.Queries`) :

```csharp
services.AddScoped<IQueryHandler<GetMoviesShowingQuery, IReadOnlyList<MovieDto>>, GetMoviesShowingHandler>();
```

Dans `AddInfrastructure` :

```csharp
services.AddScoped<IMovieReadRepository, MovieReadRepository>();
```

Créer `src/GrandCinema.Api/Endpoints/Movies/MoviesShowingEndpoints.cs` :

```csharp
using GrandCinema.Application.Abstractions.Cqrs;
using GrandCinema.Application.Features.Movies.DTOs;
using GrandCinema.Application.Features.Movies.Queries;
using Microsoft.AspNetCore.Http.HttpResults;

namespace GrandCinema.Api.Endpoints.Movies;

public static class MoviesShowingEndpoints
{
    public static IEndpointRouteBuilder MapMoviesShowingEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/movies", GetMoviesShowing)
            .WithName("GetMoviesShowing")
            .WithTags("Movies")
            .WithSummary("Movies currently showing")
            .WithDescription("Returns the movies with at least one upcoming screening, sorted by title.");

        return app;
    }

    private static async Task<Ok<IReadOnlyList<MovieDto>>> GetMoviesShowing(
        IQueryHandler<GetMoviesShowingQuery, IReadOnlyList<MovieDto>> handler,
        CancellationToken cancellationToken)
    {
        var movies = await handler.Handle(new GetMoviesShowingQuery(), cancellationToken);
        return TypedResults.Ok(movies);
    }
}
```

Dans `Program.cs` : ajouter `using GrandCinema.Api.Endpoints.Movies;`, puis `app.MapMoviesShowingEndpoints();` sous `app.MapScheduleEndpoints();`.

**Essai** avec les données de démonstration (`scripts/reset-demo-screenings.sh` pour les remettre à aujourd'hui et demain) : dans Swagger, `GET /api/movies` sous « Movies » renvoie Blue Note et Metropolis. Après 21 h, Blue Note n'a plus de séance à venir et disparaît de la liste : c'est la règle attendue.

```bash
dotnet build
dotnet test
git add .
git commit -m "feat(us-1.1): add movies showing endpoint"
git push -u origin feature/us-1-1-movies-showing
```

Ouvrir la Pull Request vers `dev`.

- **Vérification :** le critère figure dans l'issue de l'US 1.1 ; trois cycles rouge, vert dans l'historique ; 9 tests unitaires et 2 tests d'intégration verts ; `GET /api/movies` visible dans Swagger. PR : **\_\_**.

_À quoi l'étape a servi : à ajouter la liste des films à l'affiche en suivant toute la démarche du projet : une règle d'abord écrite dans le backlog, puis un cycle TDD par comportement, un test d'intégration pour la partie SQL, et un endpoint sans logique. Pourquoi : la ressource porte le nom de ce qu'elle renvoie (`/api/movies`), la règle « au moins une séance à venir » est traduite en `EXISTS` par EF Core, ce qui renvoie chaque film une seule fois et laisse PostgreSQL s'arrêter à la première séance trouvée ; l'heure de référence vient du gestionnaire, pour que le fuseau du cinéma ne soit géré qu'à un seul endroit._

### Étape 8 – Livrer la phase [x] (date et PR à noter)

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

- **Vérification :** dev contient toute la phase 1, la compilation et les tests passent. PR : **\_\_**.

_À quoi l'étape a servi : à intégrer proprement le travail de la phase dans dev et à prouver qu'il compile et se teste. Pourquoi : un incrément fini et relu, tracé par des Pull Requests, montre la gestion itérative attendue par la compétence C3.1 et sert de base saine pour les user stories suivantes._

---

## Phase 1 bis – Mise en ligne sur AWS

Architecture cible (Bloc 1) :

```text
Navigateur ── HTTPS ──► CloudFront (CDN)
                          ├─ fichiers du front ──► S3 (hébergement statique) : Front-end Angular (dépôt grand-cinema-frontend)
                          └─ /api/* ──► EC2 + Auto Scaling : API Back-end (conteneur grandcinema-api, port 8080)
                                          ├─► RDS PostgreSQL 16 (privée, sans accès Internet)
                                          ├─► S3 (billets et QR codes, US 3.2)
                                          └─► Stripe (paiement, US 3.1)

CloudWatch             supervision : métriques, journaux, alertes
SSM Parameter Store    secrets chiffrés, lus par le serveur au déploiement
SSM Session Manager    accès au serveur depuis la console AWS, sans SSH ni port ouvert
GitHub Actions         CI à chaque Pull Request ; recette déployée à chaque fusion dans dev ;
                       mise en production manuelle, après validation du Product Owner (Bloc 2)
```

Ce journal construit la partie back-end : l'image de l'API, le serveur, la base et la distribution CloudFront placée devant l'API. Le front (compilation Angular, bucket S3, comportement par défaut de CloudFront) se construit dans le journal du dépôt `grand-cinema-frontend` : le front et le back se livrent séparément, chacun avec son pipeline (Bloc 2).

Choix : l'API tourne dans un conteneur Docker sur une machine EC2, la base dans RDS (service PostgreSQL géré : sauvegardes, mises à jour mineures). Docker garantit que l'image essayée en local est exactement celle qui tourne sur AWS ; EC2 laisse voir et comprendre chaque brique (serveur, réseau, droits), ce que masquerait un service plus intégré comme Elastic Beanstalk ou ECS. Le déploiement est d'abord fait à la main (étape 10) pour comprendre chaque geste, puis automatisé (étape 11).

Région : **eu-west-3 (Paris)**, proche des utilisateurs et dans l'Union européenne (RGPD).

**Deux chemins après l'étape 9, au choix :**

| | Chemin A : étapes 10 et 11 | Chemin B, gratuit : étapes 10 G et 11 G (**retenu**) |
|---|---|---|
| Compte AWS | offre gratuite ou payant | **Free plan** : jamais facturé, crédits pendant 6 mois |
| Base | RDS PostgreSQL (gérée par AWS) | PostgreSQL en conteneur sur le serveur |
| Front | S3 + CloudFront (journal du front) | S3 + CloudFront (journal du front), comme la cible |
| HTTPS | répartiteur de charge et certificat | CloudFront (adresse `*.cloudfront.net`) ; HTTP entre CloudFront et le serveur |
| Environnement (Bloc 2) | production : déploiement manuel après validation du Product Owner | **recette** : déployée automatiquement à chaque fusion dans `dev` |
| Images | ECR (500 Mo gratuits au plus) | GitHub Container Registry (gratuit, images publiques) |
| Coût | payant dès la fin de l'offre gratuite | nul tant que dure le Free plan |
| Sauvegardes | automatiques (RDS) | `pg_dump` planifié, à organiser |

Le chemin B est retenu pour respecter un budget nul. Le chemin A reste décrit : c'est l'architecture cible du Bloc 1, et l'image Docker permet d'y passer sans changer le code.

Le chemin B monte l'environnement de **recette** du Bloc 2 : c'est l'adresse donnée au jury. C'est une version réduite de la cible, et les écarts sont assumés par écrit : un seul serveur sans Auto Scaling, PostgreSQL en conteneur au lieu de RDS, HTTP entre CloudFront et le serveur. Le front, lui, est déployé comme dans la cible (S3 + CloudFront).

### Étape 9 – Image Docker de l'API [ ]

- **Quoi :** décrire dans un `Dockerfile` la construction d'une image qui contient l'API publiée et l'outil de migration, puis l'essayer en local contre le PostgreSQL du docker compose.
- **Pourquoi :** l'image est l'unité de déploiement : construite une fois, elle tourne à l'identique sur le poste, dans GitHub Actions et sur EC2. Le serveur n'a besoin que de Docker, ni SDK .NET ni code source.

```bash
git switch dev
git pull
git switch -c chore/docker-image dev
```

`Dockerfile` à la racine du dépôt :

```dockerfile
# Build stage: restores, publishes the API and builds the migration bundle
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY global.json dotnet-tools.json ./
COPY src/GrandCinema.Domain/GrandCinema.Domain.csproj src/GrandCinema.Domain/
COPY src/GrandCinema.Application/GrandCinema.Application.csproj src/GrandCinema.Application/
COPY src/GrandCinema.Infrastructure/GrandCinema.Infrastructure.csproj src/GrandCinema.Infrastructure/
COPY src/GrandCinema.Api/GrandCinema.Api.csproj src/GrandCinema.Api/
RUN dotnet tool restore && dotnet restore src/GrandCinema.Api/GrandCinema.Api.csproj

COPY src/ src/
RUN dotnet publish src/GrandCinema.Api/GrandCinema.Api.csproj -c Release -o /app/publish --no-restore

# The design-time tools build the host, which requires a connection string: a placeholder is enough to build the bundle
RUN ConnectionStrings__Default="Host=placeholder" dotnet ef migrations bundle \
    --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api \
    --configuration Release --output /app/publish/efbundle

# Runtime stage: only the ASP.NET Core runtime and the published files
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "GrandCinema.Api.dll"]
```

`.dockerignore` à la racine (ce qui n'est pas envoyé à Docker pendant la construction) :

```text
**/bin/
**/obj/
**/TestResults/
.git/
.github/
.vscode/
.env
docs/
tests/
Dockerfile
docker-compose.yml
```

Points clés :

- **Deux étapes (multi-stage).** L'étape `build` utilise le SDK complet ; l'image finale ne garde que le runtime ASP.NET Core et les fichiers publiés : plus légère, et sans compilateur ni code source à exploiter en cas d'intrusion.
- **Les `.csproj` copiés avant le code.** Docker met chaque instruction en cache : tant que les projets et leurs paquets ne changent pas, la restauration n'est pas refaite, et une modification du code ne relance que la compilation.
- **Le migration bundle (`efbundle`).** `dotnet ef migrations bundle` produit un exécutable autonome qui applique les migrations à une base, sans SDK ni `dotnet ef`. Il est rangé dans l'image : la version de l'API et celle du schéma voyagent ensemble. Les outils EF construisent l'hôte de l'API, et `AddInfrastructure` refuse de démarrer sans chaîne de connexion : une valeur factice suffit pour construire le bundle, qui n'a pas besoin de la base à ce moment-là.
- **`USER $APP_UID`.** L'image officielle fournit un utilisateur sans droits d'administration : si l'application était compromise, l'attaquant ne serait pas `root` dans le conteneur.
- **Port 8080.** Port d'écoute par défaut des images ASP.NET Core depuis .NET 8 (un utilisateur non `root` ne peut pas écouter sur le port 80).
- **`.dockerignore`.** Exclut notamment `.env` : aucun secret ne doit entrer dans une image.

Essai en local, contre le PostgreSQL du docker compose (qui doit tourner) :

```bash
docker build -t grandcinema-api:local .
docker network ls
```

Repérer le réseau créé par docker compose (nom du dossier suivi de `_default`, par exemple `grand-cinema-backend_default`). Dans ce réseau, la base s'appelle `db` (nom du service), pas `localhost`.

Appliquer les migrations avec le bundle de l'image (sans effet si la base est déjà à jour) :

```bash
docker run --rm --network <reseau> -e ConnectionStrings__Default="Host=db;Port=5432;Database=<base>;Username=<utilisateur>;Password=<mot-de-passe>" --entrypoint /app/efbundle grandcinema-api:local
```

Résultat attendu : `No migrations were applied. The database is already up to date.`

Lancer l'API en conteneur :

```bash
docker run --rm -p 8080:8080 --network <reseau> -e ConnectionStrings__Default="Host=db;Port=5432;Database=<base>;Username=<utilisateur>;Password=<mot-de-passe>" -e TZ=Europe/Paris -e Swagger__Enabled=true grandcinema-api:local
```

Essais : `http://localhost:8080/health`, `http://localhost:8080/api/schedule`, `http://localhost:8080/swagger`. Relancer sans `-e Swagger__Enabled=true` : `/swagger` répond 404. Arrêter avec Ctrl+C.

La chaîne de connexion tapée en clair reste dans l'historique du terminal : acceptable pour la base locale, jamais pour celle de production (étape 10).

- **`TZ=Europe/Paris` règle le point laissé ouvert à l'étape 5e.** Le gestionnaire prend « aujourd'hui » dans le fuseau local du système ; un serveur est presque toujours en UTC, et verrait le lendemain dès 22 h ou 23 h à Paris. La variable `TZ` fixe le fuseau du conteneur sur celui du cinéma, quel que soit le serveur.

```bash
git add Dockerfile .dockerignore
git commit -m "chore(deploy): add API Docker image"
git push -u origin chore/docker-image
```

Ouvrir la Pull Request vers `dev`.

- **Vérification :** l'image se construit ; le bundle s'exécute ; l'API en conteneur répond sur `/health` et `/api/schedule` ; Swagger n'est visible qu'avec `Swagger__Enabled=true`. PR : **\_\_**.

_À quoi l'étape a servi : à empaqueter l'API et ses migrations dans une image unique, reproductible et sans secret. Pourquoi : ce qui a été essayé en local est exactement ce qui sera déployé ; le serveur n'a plus besoin que de Docker ; les migrations ne s'appliquent pas au démarrage de l'API (voir l'étape 7d) mais par un geste explicite, avant de lancer la nouvelle version._

### Étape 10 – Premier déploiement sur AWS, à la main [ ]

- **Quoi :** créer les ressources AWS (registre d'images, réseau, base RDS, secret, serveur EC2), puis déployer une première version de l'image à la main.
- **Pourquoi :** comprendre chaque brique avant de l'automatiser. L'étape 11 rejouera exactement les mêmes gestes depuis GitHub Actions.

```bash
git switch dev
git pull
git switch -c chore/aws-deployment dev
```

#### Étape 10a – Préparer le compte AWS

1. **Ne pas travailler avec le compte racine.** Activer l'authentification à deux facteurs (MFA) sur le compte racine, puis créer un accès personnel d'administration (IAM Identity Center, ou à défaut un utilisateur IAM avec MFA) et l'utiliser pour la suite.
2. **Créer une alerte de coût** avant toute ressource : Billing and Cost Management → Budgets → Create budget → modèle **Zero spend budget** (alerte au premier centime dépensé) ou **Monthly cost budget** avec un petit montant, et une adresse e-mail. Les offres gratuites dépendent de la date de création du compte : vérifier dans la console celles dont le compte bénéficie (types d'instance EC2 et RDS éligibles, crédits).
3. **Choisir la région eu-west-3 (Paris)** en haut à droite de la console, pour toutes les ressources.
4. **Installer l'AWS CLI v2** sur le poste ([guide d'installation officiel](https://docs.aws.amazon.com/cli/latest/userguide/getting-started-install.html)), puis la connecter au compte :

```bash
aws --version
aws configure sso            # avec IAM Identity Center ; sinon : aws configure
aws sts get-caller-identity  # affiche le numéro de compte : la connexion fonctionne
```

#### Étape 10b – Registre d'images (ECR)

```bash
aws ecr create-repository --repository-name grandcinema-api --region eu-west-3
aws ecr put-lifecycle-policy --repository-name grandcinema-api --region eu-west-3 --lifecycle-policy-text '{"rules":[{"rulePriority":1,"description":"Keep the last 10 images","selection":{"tagStatus":"any","countType":"imageCountMoreThan","countNumber":10},"action":{"type":"expire"}}]}'
```

L'adresse du registre est `<compte>.dkr.ecr.eu-west-3.amazonaws.com/grandcinema-api` (`<compte>` : numéro à douze chiffres affiché par `aws sts get-caller-identity`). La règle de cycle de vie ne garde que les dix dernières images : le stockage reste gratuit ou presque, et les versions récentes restent disponibles pour revenir en arrière.

#### Étape 10c – Groupes de sécurité (pare-feu)

Console EC2 → Security Groups → Create security group, dans le VPC par défaut :

| Nom | Règle entrante | Rôle |
|---|---|---|
| `grandcinema-api-sg` | HTTP, port 80, source `0.0.0.0/0` | l'API est publique |
| `grandcinema-db-sg` | PostgreSQL, port 5432, source `grandcinema-api-sg` | seule l'API peut joindre la base |

Aucune règle SSH (port 22) : l'accès au serveur passe par Session Manager (étape 10f), sans port ouvert. Pour `grandcinema-db-sg`, la source est le **groupe de sécurité** de l'API, pas une adresse IP : la règle reste valable même si le serveur change d'adresse.

#### Étape 10d – Base de données (RDS PostgreSQL)

Console RDS → Create database :

- **Standard create**, moteur **PostgreSQL**, version **16** (la même que le docker compose et que les tests d'intégration) ;
- modèle **Free tier** ou **Dev/Test**, déploiement **Single-AZ** ;
- identifiant : `grandcinema-db` ; nom d'utilisateur principal : par exemple `grandcinema_admin` ; mot de passe : **Self managed**, long et généré (le noter dans un gestionnaire de mots de passe) ;
- classe d'instance : la plus petite éligible à l'offre gratuite du compte (par exemple `db.t4g.micro`) ; stockage gp3, 20 Gio, **sans** mise à l'échelle automatique ;
- connectivité : **Don't connect to an EC2 compute resource**, VPC par défaut, **Public access : No**, groupe de sécurité `grandcinema-db-sg` (retirer `default`) ;
- **Additional configuration → Initial database name : `grandcinema`** (sans ce champ, aucune base applicative n'est créée) ;
- sauvegardes automatiques : 1 jour ; Performance Insights et Enhanced monitoring désactivés (coûts).

Une fois la base disponible (quelques minutes), relever son **Endpoint** (onglet Connectivity & security), de la forme `grandcinema-db.xxxxxxxx.eu-west-3.rds.amazonaws.com`.

Chaîne de connexion de production :

```text
Host=<endpoint>;Port=5432;Database=grandcinema;Username=grandcinema_admin;Password=<mot-de-passe>;SSL Mode=Require
```

`SSL Mode=Require` : RDS PostgreSQL impose le chiffrement des connexions ; les échanges entre l'API et la base sont chiffrés.

#### Étape 10e – Secrets dans Parameter Store

Console Systems Manager → Parameter Store → Create parameter :

| Nom | Type | Valeur |
|---|---|---|
| `/grandcinema/prod/connection-string` | **SecureString** | la chaîne de connexion de production |
| `/grandcinema/prod/swagger-enabled` | String | `false` |

Passer par la console évite que le mot de passe reste dans l'historique du terminal. Le type SecureString chiffre la valeur (clé KMS gérée par AWS) ; le niveau Standard de Parameter Store est gratuit. Le second paramètre pilote Swagger en production sans reconstruire l'image.

#### Étape 10f – Serveur EC2

**Rôle IAM du serveur.** Console IAM → Roles → Create role → AWS service, cas d'usage **EC2** :

- politiques gérées : `AmazonSSMManagedInstanceCore` (Session Manager et commandes à distance) et `AmazonEC2ContainerRegistryReadOnly` (télécharger les images) ;
- nom : `grandcinema-ec2-role` ;
- après création, ajouter une politique en ligne (Add permissions → Create inline policy → JSON), nommée `read-grandcinema-parameters` :

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "ssm:GetParameter",
      "Resource": "arn:aws:ssm:eu-west-3:<compte>:parameter/grandcinema/prod/*"
    }
  ]
}
```

Le serveur ne reçoit que les droits dont il a besoin (principe du moindre privilège), et aucune clé d'accès n'est stockée dessus : AWS lui fournit des identifiants temporaires par ce rôle.

**Instance.** Console EC2 → Launch instance :

- nom : `grandcinema-api` ; image : **Amazon Linux 2023** (architecture x86_64) ; type : `t3.micro` (ou le type éligible à l'offre gratuite du compte) ;
- paire de clés : **Proceed without a key pair** (pas de SSH) ;
- réseau : VPC par défaut, **Auto-assign public IP : Enable**, groupe de sécurité `grandcinema-api-sg` ;
- stockage : 16 Gio gp3 (les images Docker occupent de la place) ;
- Advanced details → **IAM instance profile : `grandcinema-ec2-role`** ; **User data** (script exécuté au premier démarrage) :

```bash
#!/bin/bash
dnf install -y docker
systemctl enable --now docker
```

**Adresse IP fixe.** EC2 → Elastic IPs → Allocate Elastic IP address, puis Actions → Associate → instance `grandcinema-api`. Sans elle, l'adresse publique change à chaque arrêt et redémarrage du serveur. Toute adresse IPv4 publique est facturée par AWS (quelques euros par mois) : à prévoir dans le budget, et à libérer à la fin du projet.

**Connexion au serveur.** EC2 → instance → Connect → **Session Manager** → Connect. Un terminal s'ouvre dans le navigateur, sans clé ni port 22. Vérifier :

```bash
docker --version
aws --version
```

Si Session Manager n'est pas proposé, attendre quelques minutes après le démarrage, et vérifier que le rôle `grandcinema-ec2-role` est bien attaché à l'instance.

#### Étape 10g – Script de déploiement

Créer `deploy/run-api.sh` dans le dépôt :

```bash
#!/usr/bin/env bash
# Deploys a version of the API on the EC2 instance: applies the migrations, then replaces the running container.
# Usage: run-api.sh <account>.dkr.ecr.eu-west-3.amazonaws.com/grandcinema-api:<tag>
set -euo pipefail

IMAGE="$1"
REGION="eu-west-3"
REGISTRY="${IMAGE%%/*}"
ENV_FILE="/opt/grandcinema/api.env"

aws ecr get-login-password --region "$REGION" | docker login --username AWS --password-stdin "$REGISTRY"
docker pull "$IMAGE"

CONNECTION_STRING=$(aws ssm get-parameter --region "$REGION" --name /grandcinema/prod/connection-string --with-decryption --query Parameter.Value --output text)
SWAGGER_ENABLED=$(aws ssm get-parameter --region "$REGION" --name /grandcinema/prod/swagger-enabled --query Parameter.Value --output text)

umask 077
cat > "$ENV_FILE" <<EOF
ConnectionStrings__Default=$CONNECTION_STRING
Swagger__Enabled=$SWAGGER_ENABLED
TZ=Europe/Paris
EOF

docker run --rm --env-file "$ENV_FILE" --entrypoint /app/efbundle "$IMAGE"

docker rm -f grandcinema-api 2>/dev/null || true
docker run -d --name grandcinema-api --restart unless-stopped -p 80:8080 --env-file "$ENV_FILE" "$IMAGE"
docker image prune -af

for attempt in $(seq 1 30); do
    if curl -fs http://localhost/health > /dev/null; then
        echo "Deployed $IMAGE"
        exit 0
    fi
    sleep 2
done

echo "The API did not become healthy" >&2
docker logs --tail 50 grandcinema-api >&2
exit 1
```

```bash
chmod +x deploy/run-api.sh
```

Ce que fait le script, dans l'ordre :

1. se connecte au registre ECR et télécharge l'image demandée ;
2. lit la chaîne de connexion et le réglage de Swagger dans Parameter Store, et les écrit dans un fichier lisible par `root` seul (`umask 077`) : le secret n'apparaît ni dans le dépôt, ni dans l'image, ni dans la liste des processus ;
3. applique les migrations avec le bundle de la **nouvelle** image, avant de démarrer la nouvelle version ; si elles échouent, le script s'arrête (`set -e`) et l'ancienne version continue de tourner ;
4. remplace le conteneur (quelques secondes d'interruption, acceptables ici) ; `--restart unless-stopped` le relance après un redémarrage du serveur ou un plantage ;
5. supprime les anciennes images pour ne pas remplir le disque ;
6. attend que `/health` réponde, sinon affiche les derniers journaux et échoue.

Installer le script sur le serveur (terminal Session Manager) : créer le dossier, puis coller le contenu du fichier entre les deux lignes `SCRIPT` :

```bash
sudo mkdir -p /opt/grandcinema
sudo tee /opt/grandcinema/run-api.sh > /dev/null <<'SCRIPT'
<contenu de deploy/run-api.sh>
SCRIPT
sudo chmod 700 /opt/grandcinema/run-api.sh
```

Le script versionné dans `deploy/` est la référence : après toute modification, le recopier sur le serveur de la même façon.

#### Étape 10h – Construire, pousser et déployer

Sur le poste, depuis la racine du dépôt :

```bash
ACCOUNT_ID=$(aws sts get-caller-identity --query Account --output text)
REGISTRY=$ACCOUNT_ID.dkr.ecr.eu-west-3.amazonaws.com
IMAGE=$REGISTRY/grandcinema-api:$(git rev-parse --short HEAD)

aws ecr get-login-password --region eu-west-3 | docker login --username AWS --password-stdin $REGISTRY
docker build -t $IMAGE .
docker push $IMAGE
echo $IMAGE
```

Sur un Mac à puce Apple, construire pour l'architecture du serveur : `docker build --platform linux/amd64 -t $IMAGE .`

L'image est étiquetée avec le commit Git : on sait toujours quelle version du code tourne en production.

Dans le terminal Session Manager, avec l'adresse affichée par `echo $IMAGE` :

```bash
sudo /opt/grandcinema/run-api.sh <image>
```

Résultat attendu : les migrations sont appliquées sur la base RDS vide, puis `Deployed <image>`.

#### Étape 10i – Vérifier

Depuis le poste, avec l'Elastic IP :

```bash
curl http://<elastic-ip>/health
curl http://<elastic-ip>/api/schedule
```

Résultats attendus : `Healthy`, puis `{"date":"<date du jour à Paris>","movies":[]}`. La liste est vide : la base de production ne contient encore aucune séance.

**Démonstration de Swagger au jury.** Passer le paramètre `/grandcinema/prod/swagger-enabled` à `true` dans Parameter Store, relancer `sudo /opt/grandcinema/run-api.sh <image>` (même image), ouvrir `http://<elastic-ip>/swagger`. Après la démonstration, remettre `false` et relancer.

**Données de démonstration (facultatif).** Depuis le terminal Session Manager, charger le script de l'étape 7 ter dans la base RDS avec un client `psql` lancé en conteneur (le dépôt étant public, le script est lu directement sur GitHub) :

```bash
read -rs -p "Mot de passe RDS : " PGPASSWORD; echo
curl -fsSL https://raw.githubusercontent.com/Grand-Cinema-Studi/grand-cinema-backend/main/docs/database/seed-dev.sql \
  | sudo docker run --rm -i -e PGPASSWORD="$PGPASSWORD" postgres:16 psql -h <endpoint> -U grandcinema_admin -d grandcinema
```

**Revenir à une version précédente.** Relancer le script avec l'étiquette d'une image antérieure (liste dans la console ECR) : `sudo /opt/grandcinema/run-api.sh <registre>/grandcinema-api:<ancien-commit>`. Attention : les migrations ne sont jamais annulées automatiquement ; une migration qui supprime une colonne doit être pensée pour rester compatible avec la version précédente.

#### Étape 10j – Coûts et limites

- **Coûts.** Ressources facturées hors offre gratuite : l'instance EC2, l'instance et le stockage RDS, l'adresse IPv4 publique. Hors périodes de travail, arrêter l'instance EC2 et la base RDS (une base RDS arrêtée redémarre seule au bout de sept jours). À la fin du projet : supprimer l'instance, la base, l'Elastic IP, le registre ECR et les paramètres.
- **HTTP seulement** (sans CloudFront devant ; la distribution de l'étape 10 G i s'applique aussi à ce chemin). L'API répond en HTTP, sans chiffrement entre le navigateur et le serveur : suffisant pour une démonstration, insuffisant pour une vraie mise en production avec authentification. Évolution : un nom de domaine et un certificat (équilibreur de charge AWS avec certificat ACM, ou proxy Caddy avec Let's Encrypt sur le serveur), à prévoir avant l'authentification (US 2.1) et avant de brancher le front Angular.
- **Un seul serveur.** Pas de haute disponibilité : si l'instance tombe, l'API est indisponible le temps de la relancer. Cohérent avec le volume d'un cinéma indépendant ; l'image Docker permettrait de passer plus tard sur plusieurs instances ou sur ECS sans changer le code.

```bash
git add deploy/run-api.sh
git commit -m "chore(deploy): add EC2 deployment script"
git push -u origin chore/aws-deployment
```

Ouvrir la Pull Request vers `dev`.

- **Vérification :** `/health` répond `Healthy` sur l'Elastic IP ; `/api/schedule` renvoie la date du jour à Paris ; la base RDS n'est pas joignable depuis Internet ; aucun secret dans le dépôt ni dans l'image. PR : **\_\_**.

_À quoi l'étape a servi : à mettre l'API en ligne sur AWS en comprenant chaque brique : registre d'images, pare-feu, base gérée, secret chiffré, serveur et script de déploiement. Pourquoi : la base n'est accessible que par l'API, le serveur n'a aucun port d'administration ouvert ni clé stockée, et le secret ne quitte Parameter Store qu'au moment du déploiement. Chaque version déployée est identifiée par son commit, ce qui permet de revenir en arrière en une commande._

### Étape 11 – Intégration et déploiement continus (GitHub Actions) [ ]

- **Quoi :** un workflow d'intégration continue (CI) qui, sur chaque Pull Request, compile, vérifie le formatage, teste, mesure la couverture et lance l'analyse SonarCloud (Quality Gate bloquant, comme l'exige le Bloc 2), et un workflow de déploiement (CD) qui, à chaque fusion dans `main` **approuvée par le Product Owner** (mise en production manuelle du Bloc 2), teste, construit l'image, la pousse dans ECR et la déploie sur EC2 avec le script de l'étape 10.
- **Pourquoi :** plus aucun déploiement à la main, donc plus d'oubli ni de version construite sur un poste mal configuré ; une Pull Request dont les tests échouent ne peut plus être fusionnée ; chaque déploiement est tracé dans l'onglet Actions de GitHub.

```bash
git switch dev
git pull
git switch -c chore/ci-cd dev
mkdir -p .github/workflows
```

#### Étape 11a – Intégration continue

`.github/workflows/ci.yml` :

```yaml
name: CI

on:
  pull_request:
    branches: [dev, main]
  push:
    branches: [dev]

jobs:
  build-and-test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
        with:
          fetch-depth: 0 # SonarCloud compares with the history to find the new code

      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - uses: actions/setup-java@v4 # the SonarScanner for .NET needs Java 17 or later
        with:
          distribution: temurin
          java-version: 17

      - run: dotnet tool install --global dotnet-sonarscanner

      - name: Begin the SonarCloud analysis
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        run: >
          dotnet sonarscanner begin
          /k:"<clé-du-projet>"
          /o:"<organisation-sonarcloud>"
          /d:sonar.host.url="https://sonarcloud.io"
          /d:sonar.token="$SONAR_TOKEN"
          /d:sonar.qualitygate.wait=true
          /d:sonar.cs.opencover.reportsPaths="tests/**/TestResults/**/coverage.opencover.xml"
          /d:sonar.exclusions="**/Migrations/**"
          /d:sonar.coverage.exclusions="**/Program.cs,**/Migrations/**,**/Persistence/Configurations/**,**/DependencyInjection.cs"

      - run: dotnet restore
      - run: dotnet format --verify-no-changes --no-restore
      - run: dotnet build --no-restore --configuration Release
      - run: dotnet test --no-build --configuration Release --collect:"XPlat Code Coverage;Format=opencover"

      - name: End the SonarCloud analysis
        env:
          SONAR_TOKEN: ${{ secrets.SONAR_TOKEN }}
        run: dotnet sonarscanner end /d:sonar.token="$SONAR_TOKEN"
```

- `global-json-file` : GitHub installe la version du SDK figée dans `global.json` à l'étape 1, la même que sur les postes.
- Les machines `ubuntu-latest` de GitHub disposent de Docker : les tests d'intégration Testcontainers y tournent comme en local.
- **Ce que vérifie la CI, comme l'annonce la stratégie de tests du Bloc 2** : compilation, formatage (`dotnet format`), tests unitaires et d'intégration, couverture (Coverlet, au format OpenCover que lit SonarCloud), analyse SonarCloud.
- **`sonar.qualitygate.wait=true`** : l'étape de fin attend le verdict du Quality Gate et échoue s'il est rouge. Le contrôle `build-and-test` devient donc rouge, et la fusion est bloquée par le ruleset (étape 11e) : c'est le critère « Quality Gate SonarCloud validé » de la Definition of Done.
- **Exclusions** : les migrations générées ne sont pas analysées ; le code sans logique (`Program.cs`, configurations EF Core, enregistrement des dépendances) n'est pas compté dans la couverture. L'objectif d'au moins 80 % porte sur le code métier (Bloc 2).
- Si `dotnet format --verify-no-changes` échoue au premier passage, lancer `dotnet format` sur le poste et committer le résultat.
- Si GitHub signale une version plus récente d'une action (`@v4`, ...), mettre à jour le numéro.

#### Étape 11a bis – SonarCloud

SonarCloud est la version hébergée de SonarQube, gratuite pour un dépôt public. Sa mise en place est décrite dans `grand-cinema-docs/quality/code-quality.md`.

1. Se connecter à sonarcloud.io avec GitHub, importer l'organisation `Grand-Cinema-Studi`, puis le dépôt `grand-cinema-backend`.
2. Projet → Administration → Analysis Method : **désactiver l'analyse automatique** (elle ne prend pas en charge le C# ; l'analyse passe par la CI).
3. Relever la clé du projet et celle de l'organisation (Information du projet) et les reporter dans `ci.yml` à la place de `<clé-du-projet>` et `<organisation-sonarcloud>`.
4. My Account → Security → générer un jeton, l'enregistrer dans GitHub comme **secret** du dépôt `SONAR_TOKEN` (Settings → Secrets and variables → Actions → Secrets). Jamais dans le code.
5. Quality Gate : garder « Sonar way » (nouveau code : couverture d'au moins 80 %, au plus 3 % de duplication, notes A, points sensibles examinés).

À la première analyse, tout le code est du « nouveau code » : si le gate échoue sur la couverture, ajouter des tests d'endpoints (`WebApplicationFactory`) plutôt que d'élargir les exclusions. Chaque problème relevé et sa correction sont consignés dans `code-quality.md` (règle, fichier, problème, correction, Pull Request) : c'est la preuve demandée au Dossier 3, Question 1 du Bloc 3.

#### Étape 11a ter – Dependabot

Le Bloc 2 cite les alertes Dependabot dans la stratégie de sécurité. `.github/dependabot.yml` :

```yaml
version: 2
updates:
  - package-ecosystem: nuget
    directory: "/"
    schedule:
      interval: weekly
    target-branch: dev
  - package-ecosystem: github-actions
    directory: "/"
    schedule:
      interval: weekly
    target-branch: dev
```

Puis Settings → Code security : activer **Dependabot alerts** et **Dependabot security updates**. Dependabot ouvre une Pull Request vers `dev` quand une bibliothèque a une mise à jour ou une faille connue ; elle passe par la CI comme les autres.

#### Étape 11b – Accès de GitHub à AWS sans clé (OIDC)

GitHub Actions s'authentifie auprès d'AWS par OpenID Connect : à chaque exécution, GitHub présente un jeton signé qui prouve « je suis le workflow de tel dépôt, sur telle branche », et AWS lui donne des identifiants temporaires valables le temps du workflow. Aucune clé d'accès AWS n'est stockée dans GitHub, donc aucune clé ne peut fuiter.

1. Console IAM → Identity providers → Add provider → **OpenID Connect** ; Provider URL : `https://token.actions.githubusercontent.com` ; Audience : `sts.amazonaws.com`.
2. IAM → Roles → Create role → **Custom trust policy**, nom `grandcinema-github-deploy`, avec cette politique de confiance :

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Principal": {
        "Federated": "arn:aws:iam::<compte>:oidc-provider/token.actions.githubusercontent.com"
      },
      "Action": "sts:AssumeRoleWithWebIdentity",
      "Condition": {
        "StringEquals": {
          "token.actions.githubusercontent.com:aud": "sts.amazonaws.com",
          "token.actions.githubusercontent.com:sub": "repo:Grand-Cinema-Studi/grand-cinema-backend:environment:production"
        }
      }
    }
  ]
}
```

La condition `sub` limite le rôle aux jobs **de ce dépôt** rattachés à l'**environnement GitHub `production`** (étape 11e) : seul un déploiement approuvé dans cet environnement obtient des identifiants ; une Pull Request ou une autre branche ne peut pas déployer. Quand un job déclare un environnement, GitHub écrit `environment:<nom>` dans le jeton, à la place de la branche.

3. Ajouter au rôle une politique en ligne `deploy-grandcinema` :

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "ecr:GetAuthorizationToken",
      "Resource": "*"
    },
    {
      "Effect": "Allow",
      "Action": [
        "ecr:BatchCheckLayerAvailability",
        "ecr:BatchGetImage",
        "ecr:GetDownloadUrlForLayer",
        "ecr:InitiateLayerUpload",
        "ecr:UploadLayerPart",
        "ecr:CompleteLayerUpload",
        "ecr:PutImage"
      ],
      "Resource": "arn:aws:ecr:eu-west-3:<compte>:repository/grandcinema-api"
    },
    {
      "Effect": "Allow",
      "Action": "ssm:SendCommand",
      "Resource": [
        "arn:aws:ec2:eu-west-3:<compte>:instance/<id-instance>",
        "arn:aws:ssm:eu-west-3::document/AWS-RunShellScript"
      ]
    },
    {
      "Effect": "Allow",
      "Action": "ssm:GetCommandInvocation",
      "Resource": "*"
    }
  ]
}
```

Le workflow peut seulement pousser des images dans ce registre et lancer une commande sur ce serveur : rien d'autre dans le compte.

#### Étape 11c – Variables du dépôt

GitHub → Settings → Secrets and variables → Actions → onglet **Variables** → New repository variable :

| Nom | Valeur |
|---|---|
| `AWS_REGION` | `eu-west-3` |
| `AWS_ROLE_ARN` | ARN du rôle `grandcinema-github-deploy` (`arn:aws:iam::<compte>:role/grandcinema-github-deploy`) |
| `ECR_REPOSITORY` | `grandcinema-api` |
| `EC2_INSTANCE_ID` | identifiant de l'instance (`i-...`) |
| `API_URL` | `http://<elastic-ip>` |

Ce sont des **variables**, pas des secrets : aucune n'est confidentielle, et le seul vrai secret (la chaîne de connexion) reste dans Parameter Store.

#### Étape 11d – Déploiement continu

`.github/workflows/deploy.yml` :

```yaml
name: Deploy

on:
  push:
    branches: [main]
  workflow_dispatch:

permissions:
  id-token: write
  contents: read

concurrency:
  group: deploy-production
  cancel-in-progress: false

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - run: dotnet test --configuration Release

  deploy:
    needs: test
    runs-on: ubuntu-latest
    environment: production # waits for the Product Owner's approval (step 11e)
    steps:
      - uses: actions/checkout@v4

      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: ${{ vars.AWS_ROLE_ARN }}
          aws-region: ${{ vars.AWS_REGION }}

      - id: ecr
        uses: aws-actions/amazon-ecr-login@v2

      - name: Build and push the image
        run: |
          IMAGE="${{ steps.ecr.outputs.registry }}/${{ vars.ECR_REPOSITORY }}:${{ github.sha }}"
          docker build -t "$IMAGE" .
          docker push "$IMAGE"
          echo "IMAGE=$IMAGE" >> "$GITHUB_ENV"

      - name: Deploy on EC2
        run: |
          INSTANCE_ID="${{ vars.EC2_INSTANCE_ID }}"
          COMMAND_ID=$(aws ssm send-command \
            --instance-ids "$INSTANCE_ID" \
            --document-name AWS-RunShellScript \
            --comment "Deploy ${{ github.sha }}" \
            --parameters "{\"commands\":[\"/opt/grandcinema/run-api.sh $IMAGE\"]}" \
            --query Command.CommandId --output text)
          if ! aws ssm wait command-executed --command-id "$COMMAND_ID" --instance-id "$INSTANCE_ID"; then
            aws ssm get-command-invocation --command-id "$COMMAND_ID" --instance-id "$INSTANCE_ID" --query "[Status, StandardErrorContent]" --output text
            exit 1
          fi
          aws ssm get-command-invocation --command-id "$COMMAND_ID" --instance-id "$INSTANCE_ID" --query StandardOutputContent --output text

      - name: Check the API
        run: curl --fail --silent --show-error --retry 5 --retry-delay 5 "${{ vars.API_URL }}/health"
```

Ce que fait le workflow :

- **déclenchement** : à chaque push sur `main`, c'est-à-dire à chaque fusion de la Pull Request `dev` → `main` ; le job `deploy` attend ensuite **l'approbation du Product Owner** (environnement `production`) : c'est la mise en production manuelle décidée au Bloc 2, après vérification des critères du guide de déploiement ; `workflow_dispatch` ajoute un bouton « Run workflow » pour relancer un déploiement à la main ;
- **`permissions: id-token: write`** : autorise le workflow à demander le jeton OIDC de l'étape 11b ;
- **`concurrency`** : deux déploiements ne se chevauchent jamais, le second attend la fin du premier ;
- **`test` puis `deploy`** (`needs: test`) : rien n'est déployé si un test échoue ;
- **image étiquetée avec `github.sha`** : le commit exact, comme au déploiement manuel ;
- **Run Command (`ssm send-command`)** : AWS exécute le script de l'étape 10g sur le serveur, en `root`, sans SSH ; le workflow attend la fin et affiche la sortie du script, ou son erreur ;
- **dernière vérification** : `/health` doit répondre depuis Internet.

Si le déploiement dépasse le temps d'attente de `aws ssm wait` (environ 100 secondes, par exemple au premier téléchargement d'une image), le script continue sur le serveur : consulter le résultat dans la console Systems Manager → Run Command.

#### Étape 11e – Enregistrer et activer

```bash
git add .github/workflows
git commit -m "chore(ci): add CI and deployment workflows"
git push -u origin chore/ci-cd
```

1. Ouvrir la Pull Request vers `dev` : le workflow **CI** se lance sur la Pull Request (onglet Checks). Fusionner quand il est vert.
2. Exiger la CI avant toute fusion : Settings → Rules → Rulesets → `protect-main-and-dev` → cocher **Require status checks to pass**, ajouter le contrôle `build-and-test`, enregistrer (c'est le point laissé en attente à l'étape 3b).
3. Créer l'environnement de production : Settings → Environments → New environment `production` → **Required reviewers** : le Product Owner (ici, soi-même) ; **Deployment branches** : `main` seulement.
4. Ouvrir la Pull Request `dev` → `main` (mise en production, quand les critères du guide de déploiement sont remplis), attendre la CI, fusionner : le workflow **Deploy** se lance, puis attend l'approbation (onglet Actions → Review deployments).
5. Vérifier `http://<elastic-ip>/health`, et dans la console ECR, la nouvelle image étiquetée avec le commit de fusion.

- **Vérification :** une Pull Request lance la CI et ne peut être fusionnée que verte ; une fusion dans `main` est déployée après approbation du Product Owner ; l'onglet Actions montre l'historique des déploiements. PR : **\_\_**.

_À quoi l'étape a servi : à automatiser tout le chemin du code jusqu'à la production : chaque Pull Request est compilée et testée, chaque fusion dans `main` est testée, empaquetée, puis déployée et vérifiée après approbation du Product Owner. Pourquoi : le déploiement devient un non-événement, reproductible et tracé ; l'accès de GitHub à AWS passe par des identifiants temporaires limités à ce dépôt et à l'environnement `production`, sans aucune clé stockée ; la protection de branche garantit qu'aucun code non testé n'atteint `dev` ni `main`._

### Étape 10 G – Variante gratuite : tout en Docker sur un serveur Free plan [ ]

- **Quoi :** monter l'environnement de **recette** du Bloc 2 : l'API et sa base PostgreSQL en conteneurs Docker sur **un seul** serveur EC2, avec un compte AWS **Free plan**, une image publiée gratuitement dans GitHub Container Registry, et une distribution **CloudFront** devant le serveur pour le HTTPS (étape 10 G i). Le front Angular est publié sur S3 derrière la même distribution, depuis son propre dépôt. Remplace les étapes 10 et 11 (chemin A).
- **Pourquoi :** contrainte de budget nul. Un compte Free plan ne peut pas être facturé, et un seul petit serveur consomme le moins possible des crédits offerts. Docker Compose décrit l'API, sa migration et sa base dans un fichier : la même description tourne en local et sur AWS.

Architecture :

```text
Navigateur ── HTTPS ──► CloudFront (adresse *.cloudfront.net)
                          ├─ /api/*, /health, /swagger*, /openapi/* ──► EC2, HTTP 80 (étape 10 G i)
                          └─ tout le reste ──► S3 : fichiers du front Angular (journal du front)

EC2 t3.micro (Amazon Linux 2023 + Docker Compose), port 80 ouvert à CloudFront seulement
   api      image du back (port 8080, publié sur le port 80 du serveur)
   migrate  bundle de migrations : s'exécute, puis s'arrête
   db       PostgreSQL 16, volume Docker, aucun port publié

GitHub Container Registry   image publique du back (gratuit)
SSM Parameter Store         secrets (mot de passe PostgreSQL, clé des jetons) et réglages, chiffrés (gratuit)
SSM Session Manager         accès au serveur sans SSH (gratuit)
```

Le front et l'API sont servis par la **même adresse**, celle de CloudFront : le navigateur appelle `/api/...` sur le site qu'il affiche, donc pas de configuration CORS à prévoir. Le serveur n'accepte que les requêtes venant de CloudFront (étape 10 G i).

```bash
git switch dev
git pull
git switch -c chore/aws-free-deployment dev
```

#### Étape 10 G a – Compte AWS Free plan

1. Créer le compte sur aws.amazon.com et choisir le **Free plan** (et non le Paid plan). Le Free plan offre des crédits (100 $ à l'inscription, jusqu'à 100 $ de plus en réalisant des activités de découverte proposées par la console, comme créer un budget ou lancer une instance) utilisables pendant **6 mois**. Lire les conditions affichées à l'inscription : elles peuvent évoluer.
2. Un compte Free plan **n'est jamais facturé**. À la fin des 6 mois ou des crédits, il est fermé, sauf passage au Paid plan : prévoir la soutenance dans cette période, et sauvegarder la base avant l'échéance (étape 10 G h).
3. Activer l'authentification à deux facteurs (MFA) sur le compte racine, puis créer un accès personnel d'administration (IAM Identity Center) et l'utiliser pour la suite.
4. Choisir la région **eu-west-3 (Paris)** pour toutes les ressources.
5. Créer un budget (Billing and Cost Management → Budgets → **Zero spend budget**) pour être alerté de toute consommation hors crédits. Suivre ensuite les crédits restants dans Billing and Cost Management → Credits.
6. Vérifier que CloudFront est proposé au compte (console CloudFront → Create distribution) : c'est lui qui apporte le HTTPS et sert le front, comme au Bloc 1. À défaut, appliquer le repli décrit dans `grand-cinema-docs/deployment/environments.md` et le signaler dans la copie.

Estimation de la consommation, tarifs de Paris (à vérifier avec AWS Pricing Calculator) : instance `t3.micro` environ 9 $ par mois, adresse IPv4 publique environ 3,60 $, disque de 16 Gio environ 1,50 $, soit **environ 14 $ par mois**, environ 85 $ sur 6 mois : couvert par les crédits. CloudFront relève de son offre gratuite permanente (de l'ordre de 1 To de transfert et 10 millions de requêtes par mois : à vérifier sur la page de tarifs). Un `t3.small` doublerait le coût de l'instance : inutile, l'API et la base occupent moins de 200 Mio de mémoire.

L'AWS CLI n'est pas nécessaire sur le poste pour cette variante : tout se fait dans la console, et les images passent par GitHub.

#### Étape 10 G b – Images dans GitHub Container Registry

GitHub Container Registry (`ghcr.io`) héberge gratuitement les images **publiques**, sans limite de taille ni de durée. Les images ne contiennent aucun secret (l'étape 9 l'a vérifié avec `.dockerignore`), elles peuvent donc être publiques ; le serveur les télécharge sans authentification.

Relier l'image au dépôt : dans le `Dockerfile`, étape `runtime`, ajouter sous la ligne `FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime` :

```dockerfile
LABEL org.opencontainers.image.source=https://github.com/Grand-Cinema-Studi/grand-cinema-backend
```

GitHub rattache ainsi le paquet au dépôt, ce qui permettra au workflow de l'étape 11 G d'y publier.

Nom de l'image du back : `ghcr.io/grand-cinema-studi/grand-cinema-backend:<commit>` (tout en minuscules, obligatoire pour un nom d'image).

**Le front n'est pas une image Docker** : Angular compilé est un ensemble de fichiers statiques, publiés sur S3 et diffusés par CloudFront, comme au Bloc 1 (journal du dépôt `grand-cinema-frontend`). Il appelle l'API par des adresses relatives (`/api/schedule`), sans nom de serveur : CloudFront oriente ces chemins vers l'API.

#### Étape 10 G c – Secrets et réglages dans Parameter Store

Générer le mot de passe de la base sur le poste :

```bash
openssl rand -hex 32
```

Uniquement des caractères hexadécimaux : un `;` dans le mot de passe casserait la chaîne de connexion, où il sépare les paramètres.

Console Systems Manager → Parameter Store → Create parameter (niveau Standard, gratuit) :

| Nom | Type | Valeur |
|---|---|---|
| `/grandcinema/recette/postgres-password` | **SecureString** | le mot de passe généré |
| `/grandcinema/recette/jwt-signing-key` | **SecureString** | clé de signature des jetons (`openssl rand -base64 48`) |
| `/grandcinema/recette/swagger-enabled` | String | `false` |

Les secrets (mot de passe de la base, clé de signature des jetons) ne sont écrits nulle part ailleurs : ni dans le dépôt, ni dans une image, ni dans GitHub. Le réglage de Swagger change le comportement de la recette sans modifier le code.

#### Étape 10 G d – Serveur EC2

**Groupe de sécurité.** Console EC2 → Security Groups → Create security group `grandcinema-sg`, dans le VPC par défaut, avec une seule règle entrante : HTTP, port 80, source `0.0.0.0/0` le temps des premiers essais (elle sera réservée à CloudFront à l'étape 10 G i). Pas de port 22 (SSH), pas de port 5432 (PostgreSQL) : la base n'est joignable que par les conteneurs du serveur.

**Rôle IAM du serveur.** IAM → Roles → Create role → AWS service, cas d'usage **EC2** :

- politique gérée `AmazonSSMManagedInstanceCore` (Session Manager et commandes à distance) ;
- nom : `grandcinema-ec2-role` ;
- après création, politique en ligne `read-grandcinema-parameters` :

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "ssm:GetParameter",
      "Resource": "arn:aws:ssm:eu-west-3:<compte>:parameter/grandcinema/recette/*"
    }
  ]
}
```

`<compte>` : numéro de compte à douze chiffres, affiché en haut à droite de la console.

**Instance.** EC2 → Launch instance :

- nom : `grandcinema` ; image : **Amazon Linux 2023** (x86_64) ; type : **`t3.micro`** (marqué « Free tier eligible ») ;
- paire de clés : **Proceed without a key pair** ;
- réseau : VPC par défaut, **Auto-assign public IP : Enable**, groupe de sécurité `grandcinema-sg` ;
- stockage : 16 Gio gp3 ;
- Advanced details → **IAM instance profile : `grandcinema-ec2-role`** ; **User data** :

```bash
#!/bin/bash
dnf install -y docker cronie
systemctl enable --now docker crond

mkdir -p /usr/local/lib/docker/cli-plugins
curl -fsSL https://github.com/docker/compose/releases/latest/download/docker-compose-linux-x86_64 -o /usr/local/lib/docker/cli-plugins/docker-compose
chmod +x /usr/local/lib/docker/cli-plugins/docker-compose

dd if=/dev/zero of=/swapfile bs=1M count=2048
chmod 600 /swapfile
mkswap /swapfile
swapon /swapfile
echo '/swapfile swap swap defaults 0 0' >> /etc/fstab
```

- Docker et le greffon Docker Compose (absent des dépôts d'Amazon Linux, d'où le téléchargement) ;
- `cronie` : planificateur de tâches, pour les sauvegardes (étape 10 G h) ;
- 2 Gio de mémoire d'échange (swap) : filet de sécurité pour un serveur de 1 Gio de mémoire, pendant le téléchargement des images ou un pic de charge.

**Adresse IP fixe.** EC2 → Elastic IPs → Allocate, puis Associate à l'instance `grandcinema`. L'adresse ne change plus aux redémarrages : CloudFront s'y adresse (étape 10 G i), par le nom DNS public qui lui correspond. Une adresse IPv4 publique est décomptée des crédits, qu'elle soit fixe ou non.

**Vérifier.** EC2 → instance → Connect → **Session Manager** → Connect (quelques minutes après le premier démarrage) :

```bash
docker --version
docker compose version
free -h
```

#### Étape 10 G e – Fichiers de déploiement

Deux fichiers, versionnés dans `deploy/`.

`deploy/docker-compose.prod.yml` :

```yaml
services:
  db:
    image: postgres:16
    restart: unless-stopped
    environment:
      POSTGRES_DB: grandcinema
      POSTGRES_USER: grandcinema
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?}
    volumes:
      - db-data:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U grandcinema -d grandcinema"]
      interval: 5s
      timeout: 3s
      retries: 10

  migrate:
    image: ${API_IMAGE:?}
    entrypoint: ["/app/efbundle"]
    environment:
      ConnectionStrings__Default: Host=db;Port=5432;Database=grandcinema;Username=grandcinema;Password=${POSTGRES_PASSWORD}
    depends_on:
      db:
        condition: service_healthy

  api:
    image: ${API_IMAGE:?}
    restart: unless-stopped
    environment:
      ConnectionStrings__Default: Host=db;Port=5432;Database=grandcinema;Username=grandcinema;Password=${POSTGRES_PASSWORD}
      Jwt__SigningKey: ${JWT_SIGNING_KEY:?}
      Swagger__Enabled: ${SWAGGER_ENABLED:-false}
      TZ: Europe/Paris
    ports:
      - "80:8080"
    depends_on:
      migrate:
        condition: service_completed_successfully

volumes:
  db-data:
```

- **`db`** : aucun `ports:`, donc PostgreSQL n'est joignable que par les autres conteneurs (sous le nom `db`), jamais depuis Internet. Les données vivent dans le volume `db-data` et survivent aux redéploiements.
- **`migrate`** : la même image que l'API, lancée sur le bundle de migrations. Il attend que la base soit prête (`service_healthy`), applique les migrations, puis s'arrête.
- **`api`** : ne démarre que si `migrate` a réussi (`service_completed_successfully`). Si une migration échoue, la nouvelle version de l'API ne démarre pas.
- **`api`** publie son port 8080 sur le port 80 du serveur : c'est le seul port ouvert, et seul CloudFront peut l'atteindre (étape 10 G i). Le front n'est pas sur ce serveur : il est sur S3.
- **`Jwt__SigningKey`** : clé de signature des jetons de l'US 2.1, lue dans Parameter Store comme le mot de passe.
- **`${VARIABLE:?}`** : Compose refuse de démarrer si la variable manque, plutôt que de lancer une base sans mot de passe.
- `restart: unless-stopped` : les conteneurs redémarrent après un plantage ou un redémarrage du serveur.

`deploy/deploy.sh` :

```bash
#!/usr/bin/env bash
# Deploys the API and its database on the EC2 instance with Docker Compose.
# Usage: deploy.sh <commit-sha>
# The image ghcr.io/grand-cinema-studi/grand-cinema-backend:<commit-sha> must have been pushed.
set -euo pipefail

COMMIT="$1"
REGION="eu-west-3"
APP_DIR="/opt/grandcinema"
RAW_URL="https://raw.githubusercontent.com/Grand-Cinema-Studi/grand-cinema-backend/$COMMIT/deploy"

parameter() {
    aws ssm get-parameter --region "$REGION" --name "/grandcinema/recette/$1" --with-decryption --query Parameter.Value --output text
}

mkdir -p "$APP_DIR"
cd "$APP_DIR"

curl -fsSL "$RAW_URL/docker-compose.prod.yml" -o docker-compose.yml

umask 077
cat > .env <<EOF
POSTGRES_PASSWORD=$(parameter postgres-password)
SWAGGER_ENABLED=$(parameter swagger-enabled)
JWT_SIGNING_KEY=$(parameter jwt-signing-key)
API_IMAGE=ghcr.io/grand-cinema-studi/grand-cinema-backend:$COMMIT
EOF

docker compose pull
docker compose up -d --remove-orphans
docker image prune -af

for attempt in $(seq 1 30); do
    if curl -fs http://localhost/health > /dev/null; then
        echo "Deployed commit $COMMIT"
        exit 0
    fi
    sleep 2
done

echo "The API did not become healthy" >&2
docker compose logs --tail 50 migrate api >&2
exit 1
```

```bash
chmod +x deploy/deploy.sh
```

Ce que fait le script :

1. télécharge le fichier Compose **du commit déployé** (le dépôt est public) : le serveur n'a jamais de version périmée de ces fichiers, et rien n'est à recopier à la main ;
2. lit les paramètres dans Parameter Store (grâce au rôle du serveur) et écrit `/opt/grandcinema/.env`, lisible par `root` seul (`umask 077`) ;
3. `docker compose pull` télécharge les images, `up -d` ne recrée que les conteneurs dont l'image a changé (en général `migrate` et `api`) : la base continue de tourner ;
4. supprime les images devenues inutiles, pour ne pas remplir le disque ;
5. attend que `/health` réponde, sinon affiche les journaux de `migrate` et `api`, et échoue.

#### Étape 10 G f – Premier déploiement, à la main

Sur le poste, enregistrer et publier les fichiers (le serveur les lira sur GitHub) :

```bash
git add Dockerfile deploy/
git commit -m "chore(deploy): add free tier Docker Compose deployment"
git push -u origin chore/aws-free-deployment
```

Créer un jeton GitHub pour publier l'image à la main : GitHub → Settings (du compte) → Developer settings → Personal access tokens → **Tokens (classic)** → Generate new token, droit **`write:packages`**, expiration 7 jours. Puis :

```bash
COMMIT=$(git rev-parse HEAD)
IMAGE=ghcr.io/grand-cinema-studi/grand-cinema-backend:$COMMIT

echo <jeton> | docker login ghcr.io -u <utilisateur-github> --password-stdin
docker build -t $IMAGE .
docker push $IMAGE
echo $COMMIT
```

Sur un Mac à puce Apple : `docker build --platform linux/amd64 -t $IMAGE .` (le serveur est en x86_64).

Après ce premier envoi, dans GitHub → organisation `Grand-Cinema-Studi` → **Packages** → `grand-cinema-backend` → **Package settings** :

- **Change visibility → Public** (le serveur télécharge sans authentification) ;
- **Manage Actions access** : vérifier que le dépôt `grand-cinema-backend` y figure avec le rôle **Write**, sinon l'ajouter (pour l'étape 11 G).

Si l'envoi est refusé, vérifier dans les réglages de l'organisation (Settings → Packages) que les membres peuvent créer des paquets publics.

Sur le serveur, dans le terminal Session Manager, avec le commit affiché :

```bash
curl -fsSL https://raw.githubusercontent.com/Grand-Cinema-Studi/grand-cinema-backend/<commit>/deploy/deploy.sh -o /tmp/deploy.sh
sudo bash /tmp/deploy.sh <commit>
```

Le script est téléchargé puis exécuté en deux temps (et non `curl ... | bash`) : si le téléchargement échoue, rien n'est exécuté. Il est lu au commit exact, sur le dépôt du projet.

Résultat attendu : création du réseau et des volumes, migrations appliquées sur la base vide, puis `Deployed commit <commit>`.

#### Étape 10 G g – Vérifier

Depuis le poste, avec l'Elastic IP :

```bash
curl http://<elastic-ip>/health
curl http://<elastic-ip>/api/schedule
```

Résultats attendus : `Healthy`, puis `{"date":"<date du jour à Paris>","movies":[]}`.

**Données de démonstration** (terminal Session Manager) :

```bash
curl -fsSL https://raw.githubusercontent.com/Grand-Cinema-Studi/grand-cinema-backend/<commit>/docs/database/seed-dev.sql -o /tmp/seed-dev.sql
cd /opt/grandcinema
sudo docker compose exec -T db psql -U grandcinema -d grandcinema < /tmp/seed-dev.sql
```

**Swagger pour le jury.** Passer `/grandcinema/recette/swagger-enabled` à `true`, relancer `sudo bash /tmp/deploy.sh <commit>` (même commit : seule l'API redémarre), ouvrir `https://<distribution>.cloudfront.net/swagger` (après l'étape 10 G i). Remettre `false` et relancer après la démonstration.

**Le front** se publie depuis son propre dépôt (journal du front) : fichiers sur S3, puis comportement par défaut de la distribution créée à l'étape 10 G i.

**Revenir à une version précédente.** Relancer le script avec un commit antérieur dont l'image existe (liste dans les Packages GitHub) : `sudo bash /tmp/deploy.sh <ancien-commit>`, après avoir téléchargé le script de ce commit. Les migrations ne sont jamais annulées : une migration doit rester compatible avec la version précédente de l'API.

#### Étape 10 G h – Sauvegardes et fin du Free plan

La base n'est plus gérée par RDS : les sauvegardes sont à organiser. Sauvegarde quotidienne, sept jours conservés (terminal Session Manager) :

```bash
sudo mkdir -p /opt/grandcinema/backups
sudo tee /etc/cron.daily/grandcinema-backup > /dev/null <<'SCRIPT'
#!/bin/bash
cd /opt/grandcinema
docker compose exec -T db pg_dump -U grandcinema grandcinema | gzip > backups/grandcinema-$(date +%F).sql.gz
find backups -name '*.sql.gz' -mtime +7 -delete
SCRIPT
sudo chmod 755 /etc/cron.daily/grandcinema-backup
sudo /etc/cron.daily/grandcinema-backup
ls -lh /opt/grandcinema/backups
```

Ces sauvegardes sont sur le disque du serveur : elles protègent d'une erreur de manipulation, pas de la perte du serveur. **Avant la fin du Free plan**, récupérer la dernière sauvegarde (par exemple en l'affichant dans le terminal Session Manager avec `zcat`, ou en la déposant temporairement dans un stockage) ; les images, elles, restent sur GitHub, indépendamment d'AWS.

#### Étape 10 G i – CloudFront devant le serveur (HTTPS, une seule adresse)

CloudFront est le CDN du Bloc 1. Il apporte le HTTPS sans nom de domaine (adresse `https://<distribution>.cloudfront.net`) et devient l'unique adresse de l'application : les chemins de l'API vont au serveur, tout le reste ira aux fichiers du front sur S3 (journal du front).

1. **Nom DNS public du serveur** : EC2 → instance `grandcinema` → **Public IPv4 DNS** (`ec2-<ip>.eu-west-3.compute.amazonaws.com`). CloudFront exige un nom, pas une adresse IP ; ce nom suit l'Elastic IP.
2. **Créer la distribution** : CloudFront → Create distribution :
   - origine : ce nom DNS, protocole **HTTP only**, port 80, nom `api` ;
   - comportement par défaut (provisoire, jusqu'à la publication du front) : origine `api` ;
   - **Viewer protocol policy** : Redirect HTTP to HTTPS ;
   - **Allowed HTTP methods** : GET, HEAD, OPTIONS, PUT, POST, PATCH, DELETE ;
   - **Cache policy** : `CachingDisabled` (les réponses de l'API ne sont jamais mises en cache) ;
   - **Origin request policy** : `AllViewerExceptHostHeader` (transmet l'en-tête `Authorization`, la chaîne de requête et les cookies) ;
   - pas de pare-feu WAF (payant) ; classe de prix : Europe et Amérique du Nord.
3. **Comportements de l'API** : ajouter `/api/*`, `/health`, `/swagger*` et `/openapi/*`, avec les mêmes réglages et l'origine `api`. Ce sont eux qui resteront dirigés vers le serveur quand le comportement par défaut passera au front.
4. **Réserver le serveur à CloudFront** : groupe de sécurité `grandcinema-sg` → modifier la règle HTTP : source **liste de préfixes gérée** `com.amazonaws.global.cloudfront.origin-facing`. L'Elastic IP ne répond plus depuis Internet ; seules les requêtes passées par CloudFront arrivent.
5. **Adresse réelle du visiteur** : derrière CloudFront, l'API voit l'adresse de CloudFront, et la limitation à 10 tentatives par minute (US 2.1) s'appliquerait à tous les visiteurs à la fois. CloudFront transmet l'adresse du visiteur dans `X-Forwarded-For`. Sur une branche `chore/forwarded-headers`, dans `src/GrandCinema.Api/Program.cs` :

   ```csharp
   builder.Services.Configure<ForwardedHeadersOptions>(options =>
   {
       options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
       // Only CloudFront can reach the server (security group), and its addresses are not known in advance.
       options.KnownIPNetworks.Clear();
       options.KnownProxies.Clear();
   });
   ```

   et `app.UseForwardedHeaders();` en **premier** middleware, avant `UseExceptionHandler`. Avec la limite par défaut d'un seul intermédiaire, l'API retient la dernière adresse ajoutée par CloudFront, celle du visiteur : une adresse inventée par le client dans l'en-tête est ignorée. (Avant .NET 10, la propriété s'appelle `KnownNetworks`.)
6. **Vérifier** :

   ```bash
   curl https://<distribution>.cloudfront.net/health          # Healthy
   curl https://<distribution>.cloudfront.net/api/schedule    # programmation du jour
   curl -m 5 http://<elastic-ip>/health                       # plus de réponse : le serveur est réservé à CloudFront
   ```

   Puis, dans Swagger (`https://<distribution>.cloudfront.net/swagger`), `POST /api/tokens` avec un compte de démonstration et `GET /api/users/me` avec le jeton : `200` prouve que l'en-tête `Authorization` traverse CloudFront.
7. **Variable GitHub** `API_URL` : `https://<distribution>.cloudfront.net` (étape 11 G). C'est aussi l'adresse donnée dans le tableau d'accès de la copie du Bloc 3.

Limites assumées de cette variante :

- **un seul serveur** pour l'API et la base, sans Auto Scaling : s'il tombe, l'application est indisponible le temps de le relancer ;
- **HTTP entre CloudFront et le serveur** : le navigateur parle en HTTPS à CloudFront, mais le jeton circule en clair sur le tronçon CloudFront → serveur ; correctif de production : nom de domaine, certificat et répartiteur de charge (HTTPS de bout en bout, critère de mise en production du Bloc 2) ;
- **sauvegardes manuelles**, sans restauration à un instant précis comme RDS ;
- **durée limitée** aux 6 mois du Free plan.

Passer plus tard à l'architecture du chemin A ne demande aucun changement de code : remplacer le service `db` par une base RDS revient à changer la chaîne de connexion.

- **Vérification :** `/health` répond `Healthy` par l'adresse CloudFront ; `/api/schedule` renvoie la date du jour à Paris ; seul le port 80 est ouvert, et seulement à CloudFront ; aucun secret dans le dépôt, les images ou GitHub ; une sauvegarde existe dans `/opt/grandcinema/backups`. PR : **\_\_**.

_À quoi l'étape a servi : à mettre en ligne la recette sans aucun coût, en restant sur AWS et sur l'architecture du Bloc 1 pour le front. Pourquoi : le Free plan garantit l'absence de facturation, un seul serveur `t3.micro` reste largement dans les crédits, et GitHub Container Registry évite tout coût de stockage d'images. La sécurité n'est pas sacrifiée : HTTPS pour le navigateur, un seul port ouvert et réservé à CloudFront, base invisible depuis Internet, mot de passe chiffré dans Parameter Store et lu au déploiement, accès au serveur sans SSH. Les compromis (un serveur, sauvegardes à la main, durée de 6 mois) sont connus et réversibles._

### Étape 11 G – Variante gratuite : intégration et déploiement continus [ ]

- **Quoi :** les mêmes workflows qu'à l'étape 11, adaptés à la variante gratuite : l'image est publiée dans GitHub Container Registry, et le déploiement lance `deploy/deploy.sh` sur le serveur. Comme le prévoit le Bloc 2, la **recette est déployée automatiquement à chaque fusion dans `dev`** ; la mise en production (chemin A) reste manuelle, après validation du Product Owner.
- **Pourquoi :** GitHub Actions est gratuit pour un dépôt public ; GitHub publie dans son propre registre avec le jeton fourni à chaque workflow, sans aucun secret à créer.

```bash
git switch dev
git pull
git switch -c chore/ci-cd dev
mkdir -p .github/workflows
```

**Intégration continue.** Créer `.github/workflows/ci.yml` exactement comme à l'étape 11a.

**Accès de GitHub à AWS (OIDC).** Créer le fournisseur d'identité et le rôle `grandcinema-github-deploy` comme à l'étape 11b (politique de confiance limitée au dépôt et à l'environnement GitHub `recette` : `sub` = `repo:Grand-Cinema-Studi/grand-cinema-backend:environment:recette`), mais avec cette politique de permissions, sans ECR :

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "ssm:SendCommand",
      "Resource": [
        "arn:aws:ec2:eu-west-3:<compte>:instance/<id-instance>",
        "arn:aws:ssm:eu-west-3::document/AWS-RunShellScript"
      ]
    },
    {
      "Effect": "Allow",
      "Action": "ssm:GetCommandInvocation",
      "Resource": "*"
    }
  ]
}
```

Le workflow peut seulement lancer une commande sur ce serveur.

**Variables du dépôt** (Settings → Secrets and variables → Actions → Variables) : `AWS_REGION` (`eu-west-3`), `AWS_ROLE_ARN`, `EC2_INSTANCE_ID`, `API_URL` (`https://<distribution>.cloudfront.net`, étape 10 G i).

**Déploiement continu.** `.github/workflows/deploy.yml` :

```yaml
name: Deploy

on:
  push:
    branches: [dev] # every merge into dev updates the acceptance environment (Bloc 2)
  workflow_dispatch:

permissions:
  id-token: write
  contents: read
  packages: write

concurrency:
  group: deploy-recette
  cancel-in-progress: false

env:
  IMAGE: ghcr.io/grand-cinema-studi/grand-cinema-backend:${{ github.sha }}

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4

      - uses: actions/setup-dotnet@v4
        with:
          global-json-file: global.json

      - run: dotnet test --configuration Release

  deploy:
    needs: test
    runs-on: ubuntu-latest
    environment: recette
    steps:
      - uses: actions/checkout@v4

      - uses: docker/login-action@v3
        with:
          registry: ghcr.io
          username: ${{ github.actor }}
          password: ${{ secrets.GITHUB_TOKEN }}

      - name: Build and push the image
        run: |
          docker build -t "$IMAGE" .
          docker push "$IMAGE"

      - uses: aws-actions/configure-aws-credentials@v4
        with:
          role-to-assume: ${{ vars.AWS_ROLE_ARN }}
          aws-region: ${{ vars.AWS_REGION }}

      - name: Deploy on EC2
        run: |
          INSTANCE_ID="${{ vars.EC2_INSTANCE_ID }}"
          SCRIPT_URL="https://raw.githubusercontent.com/${{ github.repository }}/${{ github.sha }}/deploy/deploy.sh"
          COMMAND_ID=$(aws ssm send-command \
            --instance-ids "$INSTANCE_ID" \
            --document-name AWS-RunShellScript \
            --comment "Deploy ${{ github.sha }}" \
            --parameters "{\"commands\":[\"curl -fsSL $SCRIPT_URL -o /tmp/deploy.sh\",\"bash /tmp/deploy.sh ${{ github.sha }}\"]}" \
            --query Command.CommandId --output text)
          if ! aws ssm wait command-executed --command-id "$COMMAND_ID" --instance-id "$INSTANCE_ID"; then
            aws ssm get-command-invocation --command-id "$COMMAND_ID" --instance-id "$INSTANCE_ID" --query "[Status, StandardErrorContent]" --output text
            exit 1
          fi
          aws ssm get-command-invocation --command-id "$COMMAND_ID" --instance-id "$INSTANCE_ID" --query StandardOutputContent --output text

      - name: Check the API
        run: curl --fail --silent --show-error --retry 5 --retry-delay 5 "${{ vars.API_URL }}/health"
```

Différences avec l'étape 11d :

- **`packages: write`** et `docker/login-action` : le workflow publie l'image dans GitHub Container Registry avec le jeton `GITHUB_TOKEN`, créé automatiquement pour chaque exécution et expiré à sa fin ;
- **le nom de l'image est écrit en minuscules** (`env.IMAGE`) : le nom du dépôt contient des majuscules, interdites dans un nom d'image ;
- **déclenchement sur `dev`** et **environnement `recette`** : chaque fusion dans `dev` met à jour la recette, où se fait la recette métier au fil du sprint (Bloc 2) ; seul un job de l'environnement `recette` obtient le rôle AWS ;
- **le serveur reçoit deux commandes** : télécharger `deploy.sh` au commit déployé, puis l'exécuter (Run Command s'exécute en `root`, sans `sudo`).

Enregistrer et activer, comme à l'étape 11e :

```bash
git add .github/workflows
git commit -m "chore(ci): add CI and deployment workflows"
git push -u origin chore/ci-cd
```

Créer l'environnement GitHub `recette` (Settings → Environments → New environment, **Deployment branches** : `dev` seulement). Pull Request vers `dev` (CI verte, contrôle `build-and-test` obligatoire dans le ruleset), puis fusion : le workflow **Deploy** publie l'image et met à jour la recette. Le jeton classique de l'étape 10 G f n'est plus utile : le révoquer (GitHub → Settings → Developer settings → Personal access tokens).

- **Vérification :** une fusion dans `dev` publie l'image `ghcr.io/grand-cinema-studi/grand-cinema-backend:<commit>` et la déploie sur la recette ; `/health` répond par l'adresse CloudFront ; l'onglet Actions garde l'historique. PR : **\_\_**.

_À quoi l'étape a servi : à automatiser la variante gratuite de bout en bout, toujours sans coût : GitHub Actions et GitHub Container Registry sont gratuits pour un dépôt public, et l'accès à AWS passe par OIDC, limité à une seule action sur un seul serveur. Pourquoi : chaque fusion dans `dev` produit une image identifiée par son commit et la met en ligne en quelques minutes, sans geste manuel ni secret stocké dans GitHub._

---

## Phase 2 – Comptes et connexion (US 2.1)

### Étape 12 – Créer un compte et se connecter (US 2.1) [ ] (code et tests sur la branche `feature/us-2-1-sign-up-and-sign-in` ; Pull Request vers `dev` à ouvrir)

- **Quoi :** développer l'US 2.1 de bout en bout : inscription d'un spectateur, connexion, profil de l'utilisateur connecté, hachage des mots de passe, jeton JWT, accès refusé par défaut, limitation des tentatives, erreurs au format Problem Details, bouton « Authorize » dans Swagger, comptes de démonstration.
- **Pourquoi :** l'US 2.3 (réserver) exige un utilisateur connecté (critère 1 de l'issue #9), et les US 5.2 et 5.3 un responsable (`Manager`). C'est aussi la story qui porte le plus de règles de sécurité (OWASP : authentification défaillante, défaillances cryptographiques, contrôle d'accès).

Critères d'acceptation (issue #18) :

1. Quand je saisis des informations valides, mon compte est créé et je suis connecté.
2. Si l'adresse e-mail est déjà utilisée, la création est refusée avec un message clair.
3. Si mes identifiants sont faux, la connexion est refusée avec un message qui ne dit pas lequel est faux.
4. Mon mot de passe n'est jamais stocké en clair.

Méthode suivie pour cette story : le code et les tests ont été écrits ensemble, en un seul passage, à partir de la liste de tests de l'étape 12c. L'historique ne montre donc pas un commit rouge puis un commit vert pour chaque cycle. Pour prouver que chaque test sait échouer, on applique la règle de l'étape 5e bis : casser volontairement le code, constater le rouge, rétablir (tableau de l'étape 12j). La vérification est notée dans le message de commit.

#### Étape 12a – Décision d'architecture (ADR 0002) et documentation

Les choix de l'US 2.1 (jeton JWT plutôt que cookie, `PasswordHasher` d'ASP.NET Core plutôt qu'Argon2, accès refusé par défaut) sont consignés dans un ADR, comme annoncé dans `security.md`. Dans le dépôt de documentation :

```bash
cd ../grand-cinema-docs
git switch main
git pull
git switch -c docs/adr-authentication
```

Fichiers concernés (déjà écrits) :

- `adr/0002-authentication-jwt.md` : contexte, décision, alternatives écartées (cookie `HttpOnly`, ASP.NET Core Identity complet, fournisseur externe, Argon2), conséquences ;
- `adr/README.md` : ligne 0002 ajoutée à l'index, décision retirée de la liste « à rédiger » ;
- `security/security.md` : section « Comptes » réécrite (plus « à venir »), ligne sur la politique de repli, tableau OWASP mis à jour ;
- `api/api-conventions.md` : trois endpoints ajoutés au tableau, sections « Format des erreurs » et « Authentification » passées de « à venir » à « en place ».

```bash
git add adr security api
git commit -m "docs(adr): add authentication decision (JWT, PBKDF2)"
git push -u origin docs/adr-authentication
```

Ouvrir la Pull Request vers `main` du dépôt de documentation, la fusionner (voir l'étape 3c), puis revenir au backend : `cd ../grand-cinema-backend`.

_À quoi la sous-étape a servi : à écrire la décision avant le code, avec les options écartées et leurs raisons. Pourquoi : « pourquoi un JWT et pas un cookie ? » est une question attendue à l'oral ; l'ADR donne une réponse argumentée et datée, et la Pull Request du backend peut le citer._

#### Étape 12b – Branche et paquets

```bash
git switch dev
git pull
git switch -c feature/us-2-1-sign-up-and-sign-in dev
```

Paquets (à ajouter avec `dotnet add`, qui choisit la dernière version stable et l'écrit dans le `.csproj`) :

```bash
# Infrastructure : hachage des mots de passe (PasswordHasher) et création des jetons JWT
dotnet add src/GrandCinema.Infrastructure package Microsoft.Extensions.Identity.Core
dotnet add src/GrandCinema.Infrastructure package Microsoft.IdentityModel.JsonWebTokens
# Api : validation des jetons reçus dans l'en-tête Authorization
dotnet add src/GrandCinema.Api package Microsoft.AspNetCore.Authentication.JwtBearer
# Tests d'Infrastructure : horloge simulée pour l'expiration du jeton
dotnet add tests/GrandCinema.Infrastructure.Tests package Microsoft.Extensions.TimeProvider.Testing
```

- `Microsoft.Extensions.Identity.Core` contient seulement le cœur d'ASP.NET Core Identity, dont `PasswordHasher` : ni tables, ni pages, ni gestionnaire d'utilisateurs.
- Vérifier que tous les paquets `Microsoft.IdentityModel.*` ont la **même version** (un mélange de versions provoque des erreurs à l'exécution lors de la validation des jetons) :

```bash
dotnet list src/GrandCinema.Api package --include-transitive | grep IdentityModel
```

Si les versions diffèrent, ajouter à l'Api le paquet en retard à la version la plus haute affichée, par exemple : `dotnet add src/GrandCinema.Api package Microsoft.IdentityModel.Protocols.OpenIdConnect --version <version la plus haute>`.

_À quoi la sous-étape a servi : à ouvrir la branche de l'US 2.1 et à n'ajouter que les trois briques utiles, chacune dans la couche qui en a besoin. Pourquoi : la couche Application ne référence aucun de ces paquets ; elle ne connaît que ses interfaces (`IPasswordHasher`, `IAccessTokenGenerator`), ce qui garde le métier testable sans cryptographie réelle._

#### Étape 12c – Des critères aux tests

| Critère | Test | Niveau |
|---|---|---|
| 1 – Compte créé et utilisateur connecté | `When_valid_information_is_entered_Then_the_account_is_created_and_the_user_is_logged_in` | unitaire (`RegisterUserHandlerTests`) |
| 1 – Inscription publique = spectateur (règle de `security.md`) | `When_an_account_is_created_Then_its_role_is_customer` | unitaire |
| 1 – Données invalides refusées, champ par champ | `When_the_information_is_invalid_Then_the_account_creation_is_refused_and_each_invalid_field_is_listed` | unitaire |
| 1 – Adresse en minuscules (règle de `security.md`) | `When_the_email_has_uppercase_letters_or_spaces_Then_it_is_stored_in_lowercase_without_spaces` | unitaire |
| 2 – Adresse déjà utilisée | `When_the_email_is_already_used_Then_the_account_creation_is_refused_with_a_clear_message` | unitaire |
| 2 – Unicité garantie par la base, même en cas d'inscriptions simultanées | `When_the_email_is_already_used_Then_the_second_account_is_refused_by_the_database` | intégration (`UserRepositoryTests`) |
| 1 – Le compte enregistré se relit | `When_a_user_is_added_Then_it_can_be_found_by_email_and_by_id` | intégration |
| 3 – Connexion réussie | `When_the_credentials_are_correct_Then_the_user_is_logged_in` | unitaire (`AuthenticateUserHandlerTests`) |
| 3 – Mot de passe faux | `When_the_password_is_wrong_Then_the_login_is_refused_without_saying_which_credential_is_wrong` | unitaire |
| 3 – Adresse inconnue : même message | `When_the_email_is_unknown_Then_the_login_is_refused_with_the_same_message_as_a_wrong_password` | unitaire |
| 3 – Adresse saisie avec des majuscules | `When_the_email_is_typed_with_uppercase_letters_Then_the_account_is_found` | unitaire |
| 4 – Le mot de passe passe par le hachage | `When_an_account_is_created_Then_the_password_is_stored_hashed` | unitaire |
| 4 – L'empreinte ne contient pas le mot de passe | `When_a_password_is_hashed_Then_the_hash_does_not_contain_the_password` | unitaire (`AspNetCorePasswordHasherTests`) |
| 4 – Empreinte salée | `When_the_same_password_is_hashed_twice_Then_the_hashes_differ_because_they_are_salted` | unitaire |
| 4 – Vérification | `When_the_right_password_is_given_Then_it_matches_its_hash`, `When_a_wrong_password_is_given_Then_it_does_not_match_the_hash` | unitaire |
| Session – jeton signé, porte l'identifiant et le rôle | `When_a_token_is_generated_Then_it_is_signed_and_identifies_the_user_and_their_role` | unitaire (`JwtAccessTokenGeneratorTests`) |
| Session – expiration | `When_a_token_is_generated_Then_it_expires_after_the_configured_delay` | unitaire |
| Session – jeton falsifié refusé | `When_a_token_is_checked_with_another_key_Then_it_is_rejected` | unitaire |
| Profil de l'utilisateur connecté | `When_the_signed_in_user_exists_Then_their_profile_is_returned`, `When_the_account_no_longer_exists_Then_user_not_found_is_returned` | unitaire (`GetCurrentUserHandlerTests`) |

Les tests du hachage et du jeton sont dans le projet de tests d'Infrastructure, mais sans l'étiquette `Integration` : ils n'ont besoin ni de Docker ni de base.

#### Étape 12d – Le résultat métier : `Result<T>` et `Error`

Jusqu'ici, un handler renvoyait toujours une donnée. L'US 2.1 introduit des **échecs attendus** (adresse déjà utilisée, identifiants faux) : ce ne sont pas des bugs, donc pas des exceptions (règle de `api-conventions.md`). Fichiers dans `src/GrandCinema.Application/Abstractions/Results/` :

- `Error.cs` : un type (`Validation`, `Unauthorized`, `Forbidden`, `NotFound`, `Conflict`), un code stable (`email-already-used`) et un message ; pour une erreur de validation, la liste des champs en cause ;
- `ErrorType.cs` : l'énumération des types ;
- `Result.cs` : soit une valeur, soit une erreur. Les conversions implicites permettent d'écrire `return AuthErrors.EmailAlreadyUsed;` ou `return dto;` dans un handler. `IsSuccess` et `IsFailure` sont annotés (`MemberNotNullWhen`) : après `if (result.IsFailure)`, le compilateur sait que `result.Error` n'est pas nul.

Côté Api, `src/GrandCinema.Api/Errors/ErrorMappings.cs` traduit une `Error` en réponse **Problem Details** (RFC 9457) : `Validation` → 400 (avec le champ `errors`), `Unauthorized` → 401, `Forbidden` → 403, `NotFound` → 404, `Conflict` → 409, et `type` vaut `/errors/<code>`. C'est ce `type` que le front utilise pour choisir son message en français.

Exemple de réponse à une inscription avec une adresse déjà prise :

```json
{
  "type": "/errors/email-already-used",
  "title": "An account already exists with this email address.",
  "status": 409
}
```

_Pourquoi pas des exceptions : une exception coûte cher, se lit mal (le `throw` est loin du `catch`) et se confond avec les vraies pannes. Le résultat rend l'échec visible dans la signature du handler, et le test le vérifie comme n'importe quelle valeur._

#### Étape 12e – Inscription : `RegisterUserHandler`

Fichiers dans `src/GrandCinema.Application/` :

- `Abstractions/Persistence/IUserRepository.cs` : `TryAddAsync` (renvoie `false` si l'adresse est déjà prise), `GetByEmailAsync`, `GetByIdAsync`. Nom en `Repository` et non `ReadRepository` : il écrit aussi (convention des standards de code) ;
- `Abstractions/Security/IPasswordHasher.cs`, `IAccessTokenGenerator.cs`, `AccessToken.cs` : les contrats de sécurité, implémentés dans Infrastructure ;
- `Features/Auth/AuthErrors.cs` : les trois erreurs de la fonctionnalité ;
- `Features/Auth/EmailNormalizer.cs` : `Trim().ToLowerInvariant()`, partagé par l'inscription et la connexion ;
- `Features/Auth/DTOs/` : `UserDto` (sans le mot de passe ni son empreinte), `AuthTokenDto` (jeton, expiration, utilisateur), et `Mappings/UserMappings.cs` ;
- `Features/Auth/Commands/RegisterUserCommand.cs` et `RegisterUserHandler.cs`.

Ce que fait le handler, dans l'ordre :

1. valide les données (adresse au format e-mail et de 256 caractères au plus, mot de passe de 12 à 128 caractères, prénom et nom obligatoires) et renvoie **toutes** les erreurs d'un coup ;
2. construit l'utilisateur : adresse normalisée, empreinte du mot de passe, date de création en UTC (`TimeProvider`), rôle **toujours** `Customer` ;
3. demande au repository de l'enregistrer ; s'il répond `false`, renvoie `AuthErrors.EmailAlreadyUsed` ;
4. génère le jeton et renvoie l'`AuthTokenDto` : l'utilisateur est connecté dès l'inscription (critère 1).

Tests : `tests/GrandCinema.Application.Tests/Features/Auth/Commands/RegisterUserHandlerTests.cs`. Le repository, le hacheur et le générateur de jetons sont des doublures (Moq) ; le repository simulé garde dans `_savedUser` l'utilisateur qu'on lui passe, pour vérifier ce qui serait enregistré.

_Pourquoi pas de vérification « l'adresse existe-t-elle ? » avant l'enregistrement : deux inscriptions simultanées avec la même adresse passeraient toutes les deux la vérification (même situation de concurrence que le bug 0001). C'est l'index unique de la base qui tranche, et le repository traduit son refus en `false`. Une seule source de vérité, et une requête de moins._

_Pourquoi 12 caractères : c'est le minimum recommandé par la CNIL quand le mot de passe est le seul facteur d'authentification (délibération 2022-100). Le maximum de 128 évite qu'un mot de passe de plusieurs mégaoctets ne coûte un hachage très long (déni de service)._

#### Étape 12f – Connexion et profil : `AuthenticateUserHandler`, `GetCurrentUserHandler`

- `Features/Auth/Commands/AuthenticateUserCommand.cs` et `AuthenticateUserHandler.cs` : cherche l'utilisateur par son adresse normalisée, vérifie le mot de passe, renvoie un jeton. Adresse inconnue **ou** mot de passe faux : la même erreur `AuthErrors.InvalidCredentials` (critère 3).
- `Features/Auth/Queries/GetCurrentUserQuery.cs` et `GetCurrentUserHandler.cs` : le profil de l'utilisateur connecté, dont l'identifiant vient du jeton. Il sert au front à afficher « Bonjour Léa » et à vérifier qu'une session est encore valable.
- `DependencyInjection.cs` d'Application : une ligne par nouveau handler.

La connexion est une **commande** et non une requête : elle ne modifie pas la base, mais c'est une action (elle délivre un droit d'accès) et elle peut échouer pour une raison métier.

Tests : `tests/GrandCinema.Application.Tests/Features/Auth/Commands/AuthenticateUserHandlerTests.cs` et `Features/Auth/Queries/GetCurrentUserHandlerTests.cs`.

#### Étape 12g – Infrastructure : utilisateurs, hachage, jetons

Fichiers dans `src/GrandCinema.Infrastructure/` :

- `Persistence/Repositories/UserRepository.cs` : `TryAddAsync` enregistre l'utilisateur et intercepte **uniquement** la violation d'unicité de PostgreSQL (code `23505`, `PostgresErrorCodes.UniqueViolation`) pour renvoyer `false` ; toute autre erreur remonte normalement. Lectures en `AsNoTracking`.
- `Security/AspNetCorePasswordHasher.cs` : adaptateur autour de `PasswordHasher<User>` d'ASP.NET Core. Format v3 : PBKDF2 avec HMAC-SHA512, 100 000 itérations, sel aléatoire de 128 bits ; l'algorithme et le nombre d'itérations sont écrits dans l'empreinte elle-même, ce qui permet de les augmenter plus tard sans invalider les mots de passe existants.
- `Security/JwtOptions.cs` : émetteur, audience, clé de signature, durée de validité (60 minutes par défaut), lus dans la section `Jwt` de la configuration. Une clé absente ou de moins de 32 octets arrête l'application avec un message clair : HMAC-SHA256 exige une clé de 256 bits.
- `Security/JwtAccessTokenGenerator.cs` : crée le jeton avec `JsonWebTokenHandler` ; heure lue par `TimeProvider`, ce qui permet de tester l'expiration.
- `Security/AppClaimTypes.cs` : les noms des informations du jeton (`sub`, `email`, `role`, `jti`), partagés par le générateur et l'Api.
- `DependencyInjection.cs` : enregistrement du repository, du hacheur, du générateur et de `JwtOptions`. `JwtOptions` est lu **à la première utilisation**, pas au démarrage : les outils `dotnet ef` construisent l'application sans configuration JWT (comme le bundle de migrations de l'étape 9), et ne doivent pas échouer pour autant.

Tests dans `tests/GrandCinema.Infrastructure.Tests/` : `Persistence/Repositories/UserRepositoryTests.cs` (intégration, PostgreSQL jetable), `Security/AspNetCorePasswordHasherTests.cs`, `Security/JwtAccessTokenGeneratorTests.cs`.

_Pourquoi un adaptateur autour de `PasswordHasher` : la couche Application ne dépend pas d'ASP.NET Core (règle des standards de code), et changer d'algorithme (Argon2) ne toucherait qu'une classe._

#### Étape 12h – API : endpoints, accès refusé par défaut, limitation, erreurs, Swagger

Fichiers dans `src/GrandCinema.Api/` :

| Fichier | Rôle |
|---|---|
| `Endpoints/Auth/RegisterUserEndpoints.cs` | `POST /api/users` : `201 Created` (en-tête `Location: /api/users/me`) avec le jeton, ou `400` / `409` |
| `Endpoints/Auth/AuthenticateUserEndpoints.cs` | `POST /api/tokens` : `200` avec le jeton, ou `401` |
| `Endpoints/Auth/CurrentUserEndpoints.cs` | `GET /api/users/me` : jeton obligatoire |
| `Endpoints/Screenings/ScheduleEndpoints.cs` | ajout de `.AllowAnonymous()` : la programmation reste publique |
| `Errors/ErrorMappings.cs` | `Error` → Problem Details (étape 12d) |
| `Security/AuthenticationSetup.cs` | validation des jetons JWT, politique de repli, limitation des tentatives |
| `Security/ClaimsPrincipalExtensions.cs` | `GetUserId()` : l'identifiant de l'utilisateur, lu dans le jeton |
| `OpenApi/BearerSecuritySchemeTransformer.cs` | bouton **Authorize** dans Swagger UI |
| `Program.cs` | branchement de l'ensemble |
| `appsettings.json` | section `Jwt` (émetteur, audience, durée), **sans** la clé |

Routes : la convention de l'API veut des ressources au pluriel et le verbe HTTP comme action. Créer un compte, c'est créer un utilisateur (`POST /api/users`) ; se connecter, c'est obtenir un jeton (`POST /api/tokens`) ; `/api/users/me` désigne l'utilisateur du jeton, sans identifiant dans l'URL.

Points de `AuthenticationSetup.cs` :

- `MapInboundClaims = false` : ASP.NET Core garde les noms courts du jeton (`sub`, `role`) au lieu de les renommer en longues URI .NET ; `NameClaimType` et `RoleClaimType` lui disent où lire l'identifiant et le rôle (`RequireRole("Manager")` fonctionnera ainsi en US 5.2).
- `ClockSkew` de 30 secondes au lieu de 5 minutes par défaut : un jeton expiré est refusé presque aussitôt.
- **Politique de repli** (`SetFallbackPolicy`) : tout endpoint qui ne déclare rien exige un utilisateur connecté. Un endpoint public doit le dire (`AllowAnonymous`) : un oubli ferme l'accès au lieu de l'ouvrir. C'est pourquoi `/api/schedule`, `/health`, `/` et `/openapi/v1.json` portent maintenant `.AllowAnonymous()`.
- **Limitation** : 10 appels par minute et par adresse IP sur l'inscription et la connexion, `429 Too Many Requests` au-delà ; contre les attaques par force brute sur les mots de passe.

Ordre des middlewares dans `Program.cs` : gestion des exceptions (réponse 500 générique, sans trace technique), pages de statut, Swagger, **puis** authentification, autorisation et limitation. Swagger UI passe avant l'autorisation : ses fichiers restent accessibles sans jeton, alors que la politique de repli s'applique au reste.

_Pourquoi `UseAuthentication()` et `UseAuthorization()` sont écrits explicitement : ASP.NET Core les ajouterait seul, mais au début du pipeline, donc avant Swagger UI, dont les pages seraient alors refusées par la politique de repli._

#### Étape 12i – Configuration et données de démonstration

Clé de signature en développement, hors du dépôt (comme la chaîne de connexion à l'étape 7) :

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/GrandCinema.Api
dotnet user-secrets list --project src/GrandCinema.Api
```

(Sans `openssl`, n'importe quelle chaîne aléatoire de plus de 32 caractères convient : elle ne sert qu'en local.)

Comptes de démonstration ajoutés à `docs/database/seed-dev.sql`, rechargés par `scripts/reset-demo-screenings.sh` :

| Rôle | Adresse | Mot de passe |
|---|---|---|
| Responsable (`Manager`) | `manager@grand-cinema.test` | `Manager-Demo-2026!` |
| Spectateur (`Customer`) | `client@grand-cinema.test` | `Client-Demo-2026!` |

Leur colonne `PasswordHash` contient une empreinte au format v3 de `PasswordHasher` (PBKDF2, HMAC-SHA512, 100 000 itérations), jamais le mot de passe. Ces comptes servent au tableau « identifiants admin et utilisateur » demandé en tête du dossier du Bloc 3 ; ils ne doivent jamais exister sur une vraie production.

Sur la recette et en production :

- clé de signature : paramètre chiffré `/grandcinema/recette/jwt-signing-key`, lu par le script de déploiement et passé au conteneur de l'API en variable d'environnement `Jwt__SigningKey` (étapes 10 G c et 10 G e) ; en production (chemin A), même principe sous `/grandcinema/prod/` ;
- HTTPS : apporté par CloudFront entre le navigateur et l'application (étape 10 G i) ; le tronçon CloudFront → serveur reste en HTTP, limite écrite dans le guide de déploiement ;
- prendre en compte l'en-tête `X-Forwarded-For` envoyé par CloudFront (`UseForwardedHeaders`, étape 10 G i), sinon l'API voit l'adresse IP de CloudFront et la limitation devient commune à tous les visiteurs.

#### Étape 12j – Vérifier

```bash
dotnet build
dotnet test --filter "US=2.1"
dotnet test
```

Résultat attendu : 21 tests de l'US 2.1 réussis (19 unitaires, 2 d'intégration), et toute la suite reste verte.

**Prouver que les tests savent échouer** (sans rien commiter, une modification à la fois, puis la défaire) :

| Modification temporaire | Test(s) qui doivent passer au rouge |
|---|---|
| `RegisterUserHandler` : `Role = UserRole.Manager` | `..._Then_its_role_is_customer` |
| `RegisterUserHandler` : `PasswordHash = command.Password` | `..._Then_the_password_is_stored_hashed` |
| `RegisterUserHandler` : `Email = command.Email` (sans normaliser) | `..._stored_in_lowercase_without_spaces` |
| `RegisterUserHandler` : ignorer le `false` de `TryAddAsync` | `When_the_email_is_already_used_Then_the_account_creation_is_refused_with_a_clear_message` |
| `RegisterUserHandler` : supprimer le bloc de validation | `..._each_invalid_field_is_listed` |
| `AuthenticateUserHandler` : supprimer `!_passwordHasher.Verify(...)` | `When_the_password_is_wrong_...` |
| `AuthenticateUserHandler` : renvoyer `AuthErrors.UserNotFound` si l'utilisateur est inconnu | `When_the_email_is_unknown_Then_the_login_is_refused_with_the_same_message_as_a_wrong_password` |
| `UserRepository` : supprimer le `catch` | `When_the_email_is_already_used_Then_the_second_account_is_refused_by_the_database` (exception au lieu de `false`) |
| `JwtAccessTokenGenerator` : `AddMinutes(_options.ExpirationMinutes + 1)` | `..._expires_after_the_configured_delay` |

Essai manuel, API lancée (`dotnet run --project src/GrandCinema.Api`) et données de démonstration chargées :

```bash
API=http://localhost:5073

# Inscription : 201 et un jeton
curl -i -X POST $API/api/users -H "Content-Type: application/json" \
  -d '{"email":"lea.dupont@example.com","password":"festival-jazz-2026","firstName":"Léa","lastName":"Dupont"}'

# Même adresse : 409, type /errors/email-already-used
curl -i -X POST $API/api/users -H "Content-Type: application/json" \
  -d '{"email":"LEA.DUPONT@example.com","password":"festival-jazz-2026","firstName":"Léa","lastName":"Dupont"}'

# Données invalides : 400 et la liste des champs
curl -i -X POST $API/api/users -H "Content-Type: application/json" \
  -d '{"email":"pas-une-adresse","password":"court","firstName":"","lastName":"Dupont"}'

# Mauvais mot de passe : 401, même message qu'une adresse inconnue
curl -i -X POST $API/api/tokens -H "Content-Type: application/json" \
  -d '{"email":"client@grand-cinema.test","password":"mauvais-mot-de-passe"}'

# Connexion du compte de démonstration, jeton gardé dans une variable (jq requis)
TOKEN=$(curl -s -X POST $API/api/tokens -H "Content-Type: application/json" \
  -d '{"email":"client@grand-cinema.test","password":"Client-Demo-2026!"}' | jq -r .accessToken)

# Profil : 401 sans jeton, 200 avec
curl -i $API/api/users/me
curl -i $API/api/users/me -H "Authorization: Bearer $TOKEN"

# La programmation reste publique : 200 sans jeton
curl -i $API/api/schedule
```

Dans Swagger (`/swagger`) : `POST /api/tokens` avec le compte de démonstration, copier `accessToken`, cliquer sur **Authorize**, coller le jeton, puis `GET /api/users/me` répond `200`. Onze connexions en moins d'une minute : la onzième reçoit `429`.

Contrôle en base : `docker compose exec db psql -U <utilisateur> -d <base> -c 'SELECT "Email", "PasswordHash", "Role" FROM "Users";'` ; la colonne `PasswordHash` commence par `AQAAAAIAAYagAAAAE` (format v3) et ne contient aucun mot de passe.

#### Étape 12k – Enregistrer

Des commits par couche, pour que l'historique se lise comme la construction de la story :

```bash
git add src/GrandCinema.Application/Abstractions src/GrandCinema.Application/Features/Auth/AuthErrors.cs \
        src/GrandCinema.Application/Features/Auth/EmailNormalizer.cs src/GrandCinema.Application/Features/Auth/DTOs \
        src/GrandCinema.Application/Features/Auth/Commands/RegisterUser* \
        tests/GrandCinema.Application.Tests/Features/Auth/Commands/RegisterUserHandlerTests.cs
git commit -m "feat(us-2.1): register a customer account" -m "Verified red by temporarily breaking role, hashing, email normalization, duplicate check and validation."

git add src/GrandCinema.Application tests/GrandCinema.Application.Tests
git commit -m "feat(us-2.1): sign in and read the signed-in user" -m "Verified red by removing the password check and by returning a distinct error for an unknown email."

git add src/GrandCinema.Infrastructure tests/GrandCinema.Infrastructure.Tests
git commit -m "feat(us-2.1): store users, hash passwords and issue JWT" -m "Verified red by removing the unique violation catch and by shifting the token expiry."

git add src/GrandCinema.Api
git commit -m "feat(us-2.1): add auth endpoints, default deny policy and rate limiting"

git add docs README.md from_scratch.md
git commit -m "docs(us-2.1): demo accounts, README and journal"

git push -u origin feature/us-2-1-sign-up-and-sign-in
```

Ouvrir la Pull Request vers `dev` (voir l'étape 3c), avec dans la description : les 4 critères et leurs tests, le lien vers l'ADR 0002, et la capture de `dotnet test`. Fusionner en merge commit.

- **Vérification :** 21 tests de l'US 2.1 verts, suite complète verte ; dans Swagger, inscription (`201`), doublon (`409`), connexion (`200`), mauvais mot de passe (`401`), profil sans jeton (`401`) et avec jeton (`200`) ; la programmation reste accessible sans jeton ; aucune empreinte ne contient de mot de passe en clair. PR backend : **\_\_**. PR documentation (ADR 0002) : **\_\_**.

_À quoi l'étape a servi : à donner une identité aux utilisateurs, condition des US 2.3, 5.2 et 5.3, en appliquant les règles de sécurité écrites avant le code : mots de passe hachés et salés, message d'échec neutre, accès refusé par défaut, limitation des tentatives, secrets hors du dépôt. Pourquoi : chaque critère d'acceptation a son test, chaque règle de sécurité a son mécanisme dans le framework (aucune cryptographie écrite à la main), et le résultat métier (`Result<T>`) fixe la façon dont toutes les stories suivantes renverront leurs erreurs._

### Questions probables du jury (phase 2)

- **Pourquoi un JWT et pas un cookie de session ?** L'API reste sans état : aucune session à partager si l'on ajoute des instances pendant le festival. Le cookie `HttpOnly` protège mieux contre le vol par XSS, mais demande une protection CSRF ; ce compromis est écrit dans l'ADR 0002, avec ce qui ferait changer d'avis.
- **Que contient le jeton, et peut-on le modifier ?** L'identifiant, l'adresse et le rôle, en clair (encodés en Base64, pas chiffrés) : rien de secret. Il est signé par HMAC-SHA256 avec une clé que seul le serveur connaît : changer le rôle casse la signature, et le jeton est refusé (test `When_a_token_is_checked_with_another_key_Then_it_is_rejected`).
- **Comment déconnecter un utilisateur ?** Le front oublie le jeton. Côté serveur, un JWT reste valable jusqu'à son expiration (60 minutes) : c'est la limite acceptée du MVP. Une révocation demanderait une liste de jetons révoqués ou des jetons courts avec un jeton de rafraîchissement.
- **Pourquoi PBKDF2 et pas SHA-256 ?** SHA-256 est conçu pour être rapide : un attaquant en teste des milliards par seconde. PBKDF2 répète le calcul 100 000 fois, avec un sel différent par mot de passe : deux mots de passe identiques ont des empreintes différentes, et les tables précalculées sont inutiles.
- **Comment empêchez-vous de savoir si une adresse a un compte ?** Même erreur pour une adresse inconnue et un mot de passe faux (critère 3). Limite connue : l'inscription, elle, répond `409` pour une adresse prise, comme le demande le critère 2 ; la limitation à 10 appels par minute freine l'énumération.
- **Pourquoi l'accès est-il refusé par défaut ?** Avec la politique de repli, un endpoint oublié est fermé, pas ouvert : l'erreur la moins grave. Les endpoints publics sont listés explicitement (`AllowAnonymous`), donc faciles à relire.
- **Pourquoi ne pas vérifier l'unicité de l'adresse avant d'enregistrer ?** Deux inscriptions simultanées passeraient toutes les deux la vérification. L'index unique de PostgreSQL est la seule garantie fiable ; le repository traduit son refus en `409`. C'est la même idée que la contrainte `UNIQUE(ScreeningId, SeatId)` contre la double vente.
- **Où est la clé de signature ?** Jamais dans le dépôt : user-secrets en développement, Parameter Store (chiffré) en production, injectée en variable d'environnement `Jwt__SigningKey`. Si la clé manque ou fait moins de 32 octets, la première requête qui en a besoin échoue, avec un message explicite dans les journaux.

---

## Référence – Parcours d'une requête dans le code (US 1.1)

Exemple : le visiteur ouvre la programmation du 18 juillet dans le front Angular, qui appelle `GET /api/schedule?date=2026-07-18`.

```text
Angular                GET /api/schedule?date=2026-07-18
   │
   ▼
┌─ Api ─────────────────────────────────────────────────────────────┐
│ 1. Endpoint Minimal API                                           │
│    lit la date dans l'URL → crée GetScheduleQuery(2026-07-18)     │
│    demande un IQueryHandler<GetScheduleQuery, ScheduleDto>        │
└───────────────┬───────────────────────────────────────────────────┘
                │ l'injection de dépendances fournit GetScheduleHandler
                ▼
┌─ Application ─────────────────────────────────────────────────────┐
│ 2. GetScheduleHandler.Handle(query)                               │
│    date = query.Date, ou aujourd'hui via TimeProvider             │
│    appelle IScreeningReadRepository.GetByDateAsync(date)  ──────┐ │
│                                                                 │ │
│ 4. reçoit des entités Screening                          ◄────┐ │ │
│    les regroupe par film, les trie par heure                  │ │ │
│    construit et renvoie un ScheduleDto                        │ │ │
└───────────────┬───────────────────────────────────────────────┼─┼─┘
                │                                               │ │
                │                     ┌─ Infrastructure ────────┼─▼─┐
                │                     │ 3. ScreeningReadRepository  │
                │                     │    requête LINQ (EF Core)   │
                │                     │    → SQL → PostgreSQL       │
                │                     │    → lignes → entités       │
                │                     └─────────────────────────────┘
                ▼
┌─ Api ─────────────────────────────────────────────────────────────┐
│ 5. Results.Ok(scheduleDto) → sérialisé en JSON → HTTP 200         │
└───────────────────────────────────────────────────────────────────┘
   │
   ▼
Angular affiche les films et leurs horaires
```

1. **Endpoint (Api).** Reçoit la requête HTTP, extrait la date de l'URL et la transforme en `GetScheduleQuery`. Aucune logique métier : il traduit le HTTP en requête CQRS. Il ne crée jamais le handler lui-même : il demande l'interface `IQueryHandler<GetScheduleQuery, ScheduleDto>` et l'**injection de dépendances** fournit l'objet concret. Les correspondances interface → classe sont déclarées une fois au démarrage, dans `Program.cs`. Le conteneur construit toute la chaîne : le handler demande un repository et un `TimeProvider`, le repository demande le `DbContext`, et ainsi de suite. Durée de vie `Scoped` : une instance par requête HTTP, donc un seul `DbContext` partagé pendant toute la requête.
2. **Handler (Application).** Fixe la date (celle demandée, sinon aujourd'hui) et appelle le repository. Il ne connaît que l'interface `IScreeningReadRepository` : il ignore que PostgreSQL existe.
3. **Repository (Infrastructure).** Traduit la demande métier en requête EF Core. EF Core génère le SQL (un `SELECT` avec deux jointures, vers les films et les salles, et un filtre sur l'heure de début), l'envoie à PostgreSQL et reconstruit des entités `Screening` à partir des lignes reçues. Deux choix à justifier :
   - filtre en intervalle « supérieur ou égal au début du jour et strictement inférieur au lendemain » : PostgreSQL peut utiliser l'index sur `StartsAt` ;
   - `AsNoTracking` : EF Core ne surveille pas des objets qui ne seront pas modifiés, ce qui est plus rapide en lecture.
4. **Retour dans le handler.** Regroupe les séances par film, les trie par heure et construit le `ScheduleDto`. Les entités ne sont jamais renvoyées au front : le DTO ne contient que ce que l'écran affiche, n'expose pas le modèle interne et évite les références circulaires lors de la sérialisation JSON.
5. **Retour dans l'endpoint.** `Results.Ok(...)` produit une réponse HTTP 200 ; ASP.NET Core sérialise le DTO en JSON.

La donnée change de forme à chaque frontière :

| Étape                        | Forme de la donnée              |
| ---------------------------- | ------------------------------- |
| Entrée HTTP                  | `?date=2026-07-18` (texte)      |
| Api → Application            | `GetScheduleQuery`              |
| Infrastructure ↔ PostgreSQL  | requête SQL et lignes de tables |
| Infrastructure → Application | entités `Screening`             |
| Application → Api            | `ScheduleDto`                   |
| Sortie HTTP                  | JSON                            |

Lien avec les tests :

- **tests unitaires (étape 5)** : le parcours commence directement au point 2. Le test crée lui-même le handler, et la doublure `Mock<IScreeningReadRepository>` remplace le point 3 ;
- **test d'intégration (après l'étape 7)** : vérifie le point 3 avec une vraie base PostgreSQL ;
- **endpoint et Swagger (étape 7 ter)** : mettent en place les points 1 et 5.

_À quoi sert cette section : à avoir sous les yeux le trajet complet d'une requête, de l'URL appelée par Angular jusqu'à PostgreSQL et retour, en sachant dans quelle couche se trouve chaque traitement. Pourquoi : c'est une question classique de soutenance (« décrivez le parcours d'une requête dans votre application ») ; ce schéma montre concrètement le rôle de chaque couche de la Clean Architecture, de l'injection de dépendances et des abstractions._

## Aide-mémoire des commandes récurrentes

```bash
dotnet build                                              # compiler
dotnet test                                               # lancer les tests
dotnet test --collect:"XPlat Code Coverage"               # tests avec couverture (Coverlet)
dotnet test --filter "Category!=Integration"             # tests unitaires seuls (sans Docker)
dotnet run --project src/GrandCinema.Api                  # lancer l'API
docker compose up -d                                      # démarrer PostgreSQL
docker compose down                                       # arrêter PostgreSQL (les données restent dans le volume)
dotnet ef migrations add <Nom> --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api --output-dir Persistence/Migrations   # nouvelle migration
dotnet ef database update --project src/GrandCinema.Infrastructure --startup-project src/GrandCinema.Api                                         # appliquer les migrations
docker compose exec -T db psql -U <utilisateur> -d <base> < docs/database/seed-dev.sql   # données de démonstration
scripts/reset-demo-screenings.sh                         # remettre les séances de démonstration à aujourd'hui et demain
docker build -t grandcinema-api:local .                  # construire l'image de l'API
sudo /opt/grandcinema/run-api.sh <image>                 # chemin A, sur le serveur EC2 : déployer une image
sudo bash /tmp/deploy.sh <commit>                        # chemin B, sur le serveur EC2 : déployer un commit (script téléchargé au préalable)
git switch -c feature/us-<n>-<slug> dev               # nouvelle branche de fonctionnalité
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/GrandCinema.Api   # clé de signature des jetons (développement)
git push -u origin <branche>                              # publier une branche
```

## Questions probables du jury (phase 1)

- **Pourquoi une contrainte d'unicité et une transaction ?** La contrainte garantit l'intégrité même en cas d'erreur de code ; la transaction permet une gestion d'erreur propre et un message clair à l'utilisateur.
- **Pourquoi sept tables et non cinq ?** Normalisation (troisième forme normale) et capacité décrite par salle.
- **Pourquoi pas MediatR ?** Licence commerciale depuis 2025, et le besoin est couvert par deux interfaces simples.
- **Pourquoi le code est-il en anglais alors que le MCD est en français ?** L'anglais est la langue de l'écosystème .NET ; une seule langue évite les noms hybrides, et le tableau de correspondance en tête de ce journal fait le lien avec le Bloc 1.
- **Pourquoi ranger la couche Application par fonctionnalité ?** Tout ce qui concerne un cas d'usage (requête ou commande, gestionnaire, DTO) est au même endroit, et ajouter une story ne touche qu'un dossier ; les interfaces partagées (repositories) restent dans Abstractions pour qu'une fonctionnalité ne dépende pas d'une autre. Les endpoints de l'Api suivent le même découpage (`Endpoints/Screenings/` pour `Features/Screenings/`) : une fonctionnalité porte le même nom dans les deux couches.
- **Décrivez le parcours d'une requête dans votre application.** Voir la section « Référence – Parcours d'une requête dans le code » : endpoint (Api), handler (Application), repository (Infrastructure), PostgreSQL, puis retour en DTO et en JSON.
- **Pourquoi un repository pour la programmation et une projection SQL pour le suivi des réservations ?** La programmation lit peu de données et sa logique (regroupement, tri, date par défaut) se teste en unitaire dans le handler ; le suivi calcule des agrégats, qu'il vaut mieux faire calculer par PostgreSQL que charger en entités. La règle sera écrite dans l'ADR de la stratégie de lecture, dans le dépôt de documentation (`grand-cinema-docs/adr`), avant l'US 5.3 (voir la phase 5).
- **Pourquoi des données de démonstration en SQL et pas avec EF Core ?** Pour montrer la maîtrise du SQL (insertions, `ON CONFLICT`, dates calculées par PostgreSQL) et disposer d'un script rejouable aussi en production pour une démonstration. `HasData` aurait écrit ces données dans les migrations, donc en production, avec des dates figées ; il est réservé aux données réelles et stables (salles, places).
- **Pourquoi Clean Architecture ?** Domaine indépendant du framework et de la base, testabilité, cohérence avec l'architecture modulaire et évolutive du Bloc 1.

## Questions probables du jury (mise en ligne)

- **Pourquoi EC2 et Docker ?** L'image Docker est identique en local, dans la CI et sur AWS ; EC2 laisse voir chaque brique (serveur, réseau, droits). Un service plus intégré (Elastic Beanstalk, ECS) masquerait ces mécanismes ; l'image permettrait d'y passer sans changer le code.
- **Où sont les secrets en production ?** Dans Parameter Store, chiffrés (SecureString). Le serveur les lit grâce à son rôle IAM au moment du déploiement ; ils ne sont ni dans le dépôt, ni dans l'image, ni dans GitHub.
- **Comment GitHub Actions accède-t-il à AWS ?** Par OIDC : des identifiants temporaires, limités à ce dépôt et à un environnement GitHub (`recette`, ou `production` après approbation du Product Owner), avec les seuls droits de pousser une image et de lancer le script de déploiement. Aucune clé AWS n'est stockée.
- **La base est-elle exposée sur Internet ?** Non : RDS sans accès public, et son pare-feu n'accepte que le groupe de sécurité de l'API. En recette, PostgreSQL n'a aucun port publié : seuls les conteneurs du serveur l'atteignent. Le serveur lui-même n'a aucun port d'administration ouvert (Session Manager au lieu de SSH).
- **Quand les migrations sont-elles appliquées ?** Par le script de déploiement, avec le bundle de la nouvelle image, avant de la démarrer. Jamais au démarrage de l'API : si elles échouent, l'ancienne version continue de tourner.
- **Comment revenir en arrière ?** Chaque image est étiquetée avec son commit : relancer le script avec l'étiquette précédente. Les migrations doivent rester compatibles avec la version précédente.
- **Pourquoi PostgreSQL en conteneur plutôt que RDS ?** Contrainte de budget nul : le compte Free plan n'est jamais facturé et un seul serveur consomme le moins de crédits possible. Les compromis sont connus (sauvegardes planifiées à la main, un seul serveur) ; passer à RDS ne demande que de changer la chaîne de connexion.
- **Pourquoi CloudFront devant le serveur ?** C'est le CDN du Bloc 1 : il sert le front depuis S3 et oriente `/api/*` vers l'API, sous une seule adresse (donc pas de CORS), avec le HTTPS sans nom de domaine. Le serveur n'accepte que les adresses de CloudFront. Limite connue : HTTP entre CloudFront et le serveur ; en production, répartiteur de charge et certificat.
- **Pourquoi la recette se déploie seule, et pas la production ?** C'est la stratégie du Bloc 2 : chaque fusion dans `dev` part en recette, pour la recette métier au fil du sprint ; la mise en production est une décision du Product Owner, après vérification des critères (environnement GitHub `production` avec approbation).
- **Où est l'Auto Scaling du Bloc 1 ?** Pas en recette, par budget. L'API est sans état (jeton JWT, blocage des places en base) : on ajoute des instances derrière un répartiteur de charge sans changer le code, et la base passe sur RDS en changeant la chaîne de connexion.
- **Comment est réglé le problème du fuseau horaire ?** `TZ=Europe/Paris` dans le conteneur : « aujourd'hui » est le jour à Paris, même sur un serveur en UTC.

## Phases suivantes (à compléter)

- Phase 2 : authentification (US 2.1) : voir l'étape 12 ; à compléter par l'acceptation de la politique de confidentialité et la suppression du compte (critères RGPD du Bloc 2) ; clé de signature et `X-Forwarded-For` au déploiement (étapes 10 G e et 10 G i).
- Phase 3 : places restantes (US 2.2). L'US 1.1 est terminée aux étapes 7 bis et 7 ter (l'étape 7 quater, films à l'affiche, reste à faire) ; l'US 1.2 (consultation sans compte) est couverte côté API par la programmation publique.
- Phase 4 : réservation avec verrouillage et expiration (US 2.3), bug de double réservation, test de reproduction et correctif.
- Phase 5 : consultation des réservations par le caissier, programmation et suivi des réservations (US 5.1, 5.2, 5.3) ; avant l'US 5.3, rédiger l'ADR de la stratégie de lecture dans le dépôt de documentation (détaillé ci-dessous).
- Phase 6 : paiement Stripe en mode test (US 3.1), billet dématérialisé avec QR code stocké sur S3 (US 3.2), scan du billet (US 4.1).
- Phase 7 : tests de l'API complète (`WebApplicationFactory`), supervision CloudWatch (alarmes de la recette : état de l'instance, processeur, erreurs 5xx de CloudFront), tests de préproduction décrits au Bloc 2 (charge JMeter, scan OWASP ZAP, bout en bout Selenium avec le front). SonarCloud, la couverture et Dependabot sont en place dès l'étape 11 (ou 11 G), et le déploiement continu de la recette depuis l'étape 11 G.

### Phase 5 – Étape préalable à l'US 5.3 : ADR de la stratégie de lecture [ ]

- **Quoi :** rédiger un ADR (Architecture Decision Record), c'est-à-dire une fiche courte qui consigne une décision d'architecture : son contexte, la décision, les alternatives écartées et les conséquences. Ici : quand le côté lecture passe par un repository (entités) et quand il passe par une projection SQL directe en DTO.
- **Pourquoi :** l'US 5.3 (suivi des réservations) introduit des lectures d'agrégats (comptages, totaux), pour lesquelles charger toutes les réservations en entités serait du gaspillage. Le projet va donc mélanger deux approches de lecture : ce mélange n'est sain que si la règle est écrite, connue et justifiée, et non laissée au choix du moment. L'ADR répond aussi aux questions D2 Q1 et Q4 (bonnes pratiques) : il prouve que les choix d'architecture sont réfléchis et tracés.
- **Où :** tous les ADR du projet, front et back, sont dans le dépôt de documentation, [`grand-cinema-docs/adr`](https://github.com/Grand-Cinema-Studi/grand-cinema-docs/tree/main/adr), avec une seule numérotation. L'ADR n'est donc pas écrit dans ce dépôt, et il ne porte pas le numéro 0001 (déjà pris par le choix du framework backend) : il prend le **prochain numéro libre** de l'index `adr/README.md`, noté `NNNN` ci-dessous.

Branche de travail, dans le dépôt de documentation :

```bash
cd ../grand-cinema-docs
git switch main
git pull
git switch -c docs/adr-read-side-strategy
```

Créer `adr/NNNN-read-side-strategy.md` (nom de fichier en anglais, contenu en français) à partir du modèle `adr/template.md` (format de Michael Nygard, le plus répandu), avec ce contenu :

```markdown
# ADR NNNN – Stratégie du côté lecture (CQRS)

- Statut : Accepté
- Concerne : backend
- Liens : US 5.3 (issue #21), Pull Request du backend qui l'applique

## Contexte

- CQRS appliqué au niveau des opérations : requêtes et commandes séparées, chacune avec son handler ; un seul modèle (entités du domaine) et une seule base PostgreSQL.
- Volumes faibles pour la programmation (quelques dizaines de séances par jour).
- Stratégie de tests du Bloc 2 : TDD, logique des handlers testée en unitaire avec Moq.
- L'US 5.3 introduit des lectures d'agrégats (nombre de réservations, places vendues, taux de remplissage).

## Décision

1. Écriture : entités et repository, pour protéger les règles métier (capacité, non-double vente).
2. Lecture simple d'une entité ou d'une petite liste : repository en lecture seule, nommé `I<Entity>ReadRepository`, placé dans `Abstractions/Persistence`, qui renvoie des entités ; la logique reste dans le handler et se teste en unitaire.
3. Lecture d'agrégats, de statistiques ou de gros volumes : projection SQL directe en DTO, interface nommée `I<Sujet>Queries`, placée dans `Features/<Feature>/Queries`, implémentée dans `Infrastructure/Persistence/Queries` ; testée en intégration avec PostgreSQL.
4. Passage d'une lecture du cas 2 au cas 3 uniquement sur un besoin constaté (écran lent, requête lourde), jamais par anticipation.

## Alternatives écartées

- Tout en repository : charger des centaines d'entités pour n'en garder que des totaux gaspille mémoire et requêtes (US 5.3).
- Tout en projection : la logique des handlers part dans le SQL et ne se teste plus qu'en intégration, contraire à la stratégie TDD du Bloc 2 ; deux mécanismes à maîtriser dès le départ, sans gain mesurable sur de petits volumes.
- Bases de lecture et d'écriture séparées (CQRS complet avec événements) : complexité et cohérence différée injustifiées pour ce projet.

## Conséquences

- Positives : chaque lecture utilise la technique adaptée ; les noms (`ReadRepository` ou `Queries`) indiquent l'approche ; règle connue de tous.
- Négatives : deux approches à connaître ; les projections exigent des tests d'intégration (base PostgreSQL de test).
- À surveiller : un écran de lecture simple qui devient lent est le signal de passer du cas 2 au cas 3.
```

Dans `adr/README.md` :

- ajouter la ligne `| [NNNN](NNNN-read-side-strategy.md) | Stratégie du côté lecture (CQRS) | Accepté |` au tableau de l'index ;
- retirer « stratégie du côté lecture (repository ou projection SQL), avant l'US 5.3 » de la liste des décisions à rédiger.

Règle de vie d'un ADR : une fois accepté, il n'est plus modifié. Si la décision change, on écrit un nouvel ADR dont le statut indique qu'il **remplace** celui-ci, et le statut de l'ancien devient « Remplacé par l'ADR <numéro> ». L'historique des décisions reste ainsi lisible.

Enregistrer et intégrer :

```bash
git add adr
git commit -m "docs(adr): add read side strategy decision"
git push -u origin docs/adr-read-side-strategy
```

Ouvrir la Pull Request vers `main` du dépôt de documentation et la fusionner (voir l'étape 3c). La Pull Request du backend qui développe l'US 5.3 cite cet ADR dans sa description.

- **Vérification :** `adr/NNNN-read-side-strategy.md` est présent sur `main` du dépôt de documentation et figure dans l'index `adr/README.md` ; l'interface de lecture de l'US 5.3 respecte la règle (nom en `Queries`, rangée dans `Features/Reservations/Queries`). PR : **\_\_**.

_À quoi l'étape a servi : à écrire noir sur blanc la règle qui décide, pour chaque lecture, entre repository et projection SQL, avec les raisons et les alternatives écartées. Pourquoi : un mélange de techniques n'est acceptable que s'il suit une règle connue ; l'ADR évite les choix arbitraires, guide les développements suivants et montre au jury une démarche d'architecture professionnelle et tracée. Il est rangé avec les autres ADR, dans le dépôt de documentation, pour que toutes les décisions du projet se lisent au même endroit._
