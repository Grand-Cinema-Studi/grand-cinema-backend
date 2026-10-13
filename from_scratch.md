# From scratch – Backend Grand Cinéma

Journal pas à pas de la construction du backend, pensé comme un tutoriel : une entrée par action, dans l'ordre, avec les commandes exactes à lancer.

Stack : C# / .NET, Minimal API, Clean Architecture, CQRS, EF Core, PostgreSQL.
Périmètre MVP : US 1.1, 2.1, 2.2, 2.3, 5.2, 5.3 (issues du Bloc 2).

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
| Film                                                         | Movie                                                             |
| Salle                                                        | Room                                                              |
| Place                                                        | Seat                                                              |
| Séance                                                       | Screening                                                         |
| Réservation                                                  | Reservation                                                       |
| Place réservée                                               | ReservedSeat                                                      |
| Utilisateur                                                  | User                                                              |
| Rôle : Spectateur, Caissier, Contrôleur, Responsable         | UserRole : Customer, Cashier, TicketInspector, Manager            |
| Statut : En attente de paiement, Confirmée, Expirée, Annulée | ReservationStatus : PendingPayment, Confirmed, Expired, Cancelled |

Règles complémentaires :

- noms des tests : `When_<situation>_Then_<résultat attendu>`, qui reprend le « Quand / Alors » des critères d'acceptation (exemple : `When_no_screening_exists_Then_the_movie_list_is_empty`) ;
- couche Application rangée par fonctionnalité : `Features/<Feature>/Commands`, `Queries` et `DTOs` (voir l'étape 5) ;
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
- les tests de l'application n'utilisent ni base de données ni réseau : le repository est une doublure (Moq).

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

Point à retenir pour la production : « aujourd'hui » dépend du fuseau horaire. Un serveur en UTC verrait un autre jour que Paris en soirée. Prévoir plus tard un fuseau configuré pour le cinéma (à traiter avec le déploiement).

_À quoi la sous-étape a servi : à compléter le comportement de l'US 1.1 critère par critère (date choisie, aucune séance, date du jour par défaut, ordre chronologique), un cycle rouge, vert, refactor à la fois. Pourquoi : avancer par petits pas garde les tests verts entre deux cycles et limite le débogage ; à la fin, chaque critère d'acceptation est couvert par un test qui sert de non-régression._

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

**Suite de l'US 1.1, à détailler après l'étape 7 (base de données)** : implémentation de `IScreeningReadRepository` dans Infrastructure avec un test d'intégration sur PostgreSQL (le filtre par jour y est vérifié), enregistrement des gestionnaires dans l'injection de dépendances, endpoint Minimal API de consultation de la programmation, documentation Swagger, puis fusion de la Pull Request.

_À quoi la sous-étape a servi : à vérifier que toute la suite de tests passe, à mesurer la couverture et à publier la branche pour la Pull Request, en notant ce qui reste à faire pour terminer l'US 1.1 (repository réel, endpoint, Swagger). Pourquoi : seul du code testé entre dans `dev` ; la couverture est un livrable du Dossier 3 ; découper l'US en deux temps permet de développer la logique métier avant que la base de données existe._

- **Vérification :** `dotnet test --filter "US=1.1"` affiche 5 tests réussis ; l'historique de la branche montre pour chaque cycle un commit rouge, un commit vert et un commit de refactor ; aucun nom en français dans le code. Commits : **\_\_**. PR : **\_\_**.

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

### Étape 7 – Modèle de données (D1 Q2) [ ]

- **Quoi :** modéliser sept tables à partir des profils et EPICs du Bloc 1 (identifiants GUID ; noms anglais, correspondance française entre parenthèses) :
  - `Users` (utilisateurs) : Email unique, PasswordHash, LastName, FirstName, Role (`UserRole` : Customer, Cashier, TicketInspector, Manager), CreatedAt ;
  - `Movies` (films) : Title, Synopsis, DurationMinutes, Genre, PosterUrl ;
  - `Rooms` (salles) : Name unique, Capacity ;
  - `Seats` (places) : RoomId, Row, Number ; combinaison RoomId-Row-Number unique ;
  - `Screenings` (séances) : MovieId, RoomId, StartsAt, Price ; index sur RoomId et StartsAt ;
  - `Reservations` (réservations) : UserId, ScreeningId, Status (`ReservationStatus` : PendingPayment, Confirmed, Expired, Cancelled), CreatedAt, ExpiresAt, TotalAmount ;
  - `ReservedSeats` (places réservées) : ReservationId, ScreeningId, SeatId ; clé primaire sur ReservationId et SeatId ; **contrainte d'unicité sur ScreeningId et SeatId**.
- **Pourquoi :** l'énoncé cite cinq tables ; les deux ajoutées (Rooms et ReservedSeats) respectent la troisième forme normale et portent la capacité par salle. La contrainte d'unicité sur ScreeningId et SeatId garantit la non-double vente au niveau de la base, même en cas de bug applicatif. À l'expiration ou à l'annulation, les lignes de ReservedSeats sont supprimées pour libérer les places ; le statut conserve l'historique.
- **Point de vigilance :** une place doit appartenir à la salle de la séance. Ce contrôle ne se fait pas par clé étrangère : il est assuré dans le gestionnaire de réservation et couvert par un test.

Règles de mise en œuvre : compléter les entités de `Domain/Entities` (Movie, Room et Screening existent depuis l'étape 5) et ajouter les autres, sans attribut lié à EF ; placer les énumérations dans `Domain/Enums` ; configurer en Fluent API et placer le DbContext dans Infrastructure.

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

- **Vérification :** la migration s'applique sur une base vide ; le script SQL et le schéma sont versionnés dans docs/database. Commit : **\_\_**.

_À quoi l'étape a servi : à traduire le besoin métier (films, séances, places, réservations, utilisateurs) en tables, avec leur script SQL et leur schéma. Pourquoi : c'est le livrable du D1 Q2 ; la contrainte d'unicité sur séance et place est la première défense contre la double vente, qui sera le bug étudié au Dossier 3._

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

- **Vérification :** dev contient toute la phase 1, la compilation et les tests passent. PR : **\_\_**.

_À quoi l'étape a servi : à intégrer proprement le travail de la phase dans dev et à prouver qu'il compile et se teste. Pourquoi : un incrément fini et relu, tracé par des Pull Requests, montre la gestion itérative attendue par la compétence C3.1 et sert de base saine pour les user stories suivantes._

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
- **endpoint et Swagger (après l'étape 7)** : mettent en place les points 1 et 5.

_À quoi sert cette section : à avoir sous les yeux le trajet complet d'une requête, de l'URL appelée par Angular jusqu'à PostgreSQL et retour, en sachant dans quelle couche se trouve chaque traitement. Pourquoi : c'est une question classique de soutenance (« décrivez le parcours d'une requête dans votre application ») ; ce schéma montre concrètement le rôle de chaque couche de la Clean Architecture, de l'injection de dépendances et des abstractions._

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
- **Pourquoi le code est-il en anglais alors que le MCD est en français ?** L'anglais est la langue de l'écosystème .NET ; une seule langue évite les noms hybrides, et le tableau de correspondance en tête de ce journal fait le lien avec le Bloc 1.
- **Pourquoi ranger la couche Application par fonctionnalité ?** Tout ce qui concerne un cas d'usage (requête ou commande, gestionnaire, DTO) est au même endroit, et ajouter une story ne touche qu'un dossier ; les interfaces partagées (repositories) restent dans Abstractions pour qu'une fonctionnalité ne dépende pas d'une autre.
- **Décrivez le parcours d'une requête dans votre application.** Voir la section « Référence – Parcours d'une requête dans le code » : endpoint (Api), handler (Application), repository (Infrastructure), PostgreSQL, puis retour en DTO et en JSON.
- **Pourquoi un repository pour la programmation et une projection SQL pour le suivi des réservations ?** La programmation lit peu de données et sa logique (regroupement, tri, date par défaut) se teste en unitaire dans le handler ; le suivi calcule des agrégats, qu'il vaut mieux faire calculer par PostgreSQL que charger en entités. La règle est écrite dans l'ADR 0001 (`docs/adr`).
- **Pourquoi Clean Architecture ?** Domaine indépendant du framework et de la base, testabilité, cohérence avec l'architecture modulaire et évolutive du Bloc 1.

## Phases suivantes (à compléter)

- Phase 2 : authentification (US 2.1), Swagger, première user story complète avec feature branch et Pull Request.
- Phase 3 : fin de l'US 1.1 (repository, endpoint, Swagger) et places restantes (US 2.2).
- Phase 4 : réservation avec verrouillage et expiration (US 2.3), bug de double réservation, test de reproduction et correctif.
- Phase 5 : programmation et suivi des réservations (US 5.2, 5.3) ; avant l'US 5.3, rédiger l'ADR de la stratégie de lecture (détaillé ci-dessous).
- Phase 6 : tests unitaires, couverture, SonarCloud, déploiement.

### Phase 5 – Étape préalable à l'US 5.3 : ADR de la stratégie de lecture [ ]

- **Quoi :** rédiger un ADR (Architecture Decision Record), c'est-à-dire une fiche courte qui consigne une décision d'architecture : son contexte, la décision, les alternatives écartées et les conséquences. Ici : quand le côté lecture passe par un repository (entités) et quand il passe par une projection SQL directe en DTO.
- **Pourquoi :** l'US 5.3 (suivi des réservations) introduit des lectures d'agrégats (comptages, totaux), pour lesquelles charger toutes les réservations en entités serait du gaspillage. Le projet va donc mélanger deux approches de lecture : ce mélange n'est sain que si la règle est écrite, connue et justifiée, et non laissée au choix du moment. L'ADR répond aussi aux questions D2 Q1 et Q4 (bonnes pratiques) : il prouve que les choix d'architecture sont réfléchis et tracés.

Branche de travail :

```bash
git switch -c docs/adr-read-side-strategy dev
mkdir -p docs/adr
```

Créer `docs/adr/0001-read-side-strategy.md` (nom de fichier en anglais, contenu en français) avec les rubriques suivantes, au format de Michael Nygard, le plus répandu :

```markdown
# ADR 0001 – Stratégie du côté lecture (CQRS)

- Statut : Accepté
- Date : <date>

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
```

Règle de vie d'un ADR : une fois accepté, il n'est plus modifié. Si la décision change, on écrit un nouvel ADR (0002, ...) dont le statut indique qu'il **remplace** le 0001, et le statut du 0001 devient « Remplacé par l'ADR 0002 ». L'historique des décisions reste ainsi lisible.

Enregistrer et intégrer :

```bash
git add docs/adr
git commit -m "docs(adr): add read side strategy decision"
git push -u origin docs/adr-read-side-strategy
```

Ouvrir la Pull Request vers `dev` et la fusionner (voir l'étape 3c).

- **Vérification :** `docs/adr/0001-read-side-strategy.md` est présent sur `dev` ; l'interface de lecture de l'US 5.3 respecte la règle (nom en `Queries`, rangée dans `Features/Reservations/Queries`). PR : **\_\_**.

_À quoi l'étape a servi : à écrire noir sur blanc la règle qui décide, pour chaque lecture, entre repository et projection SQL, avec les raisons et les alternatives écartées. Pourquoi : un mélange de techniques n'est acceptable que s'il suit une règle connue ; l'ADR évite les choix arbitraires, guide les développements suivants et montre au jury une démarche d'architecture professionnelle et tracée._
