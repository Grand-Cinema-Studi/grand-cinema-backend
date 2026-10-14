- main = production, alimentée uniquement depuis dev ou une branche hotfix ;
- dev = intégration. `dev` est la branche `develop` demandée par l'énoncé : même rôle (branche d'intégration protégée, cible de toutes les Pull Requests, seule source de `main` avec les hotfix), seul le nom est abrégé. Elle n'est donc pas renommée ;
- préfixes de branches : feature, fix, chore, docs, test, hotfix ;
- nom d'une branche de fonctionnalité = préfixe + numéro de user story + court intitulé (exemple : feature/us-2-3-reserve-places) ;
- messages de commit au format Conventional Commits (feat, fix, test, docs, chore) ;
- une branche = une user story = une Pull Request relue avec une checklist.

Détail du workflow (protections des branches, Pull Requests, mode de fusion, messages de commit) : [git-workflow.md](https://github.com/Grand-Cinema-Studi/grand-cinema-docs/blob/main/development/git-workflow.md), dans le dépôt de documentation.