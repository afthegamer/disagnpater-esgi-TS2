# API Restaurant — Design Patterns

API REST de gestion des commandes d'un restaurant, construite pour illustrer
cinq patterns de conception répondant chacun à un besoin métier précis.

---

## Démarrage

**Prérequis** — .NET 8 SDK. Les commandes s'exécutent depuis la racine du dépôt.

**Lancer l'API :**

```bash
dotnet run --project RestaurantApi/RestaurantApi
```

La console affiche l'URL d'écoute. Swagger est disponible sur `/swagger`.

**Lancer les tests :**

```bash
dotnet test RestaurantApi
```

```
Réussi!  - échec : 0, réussite : 65, ignorée(s) : 0, total : 65
```

---

## Endpoints

| Méthode | Route | Rôle | Besoin |
|---|---|---|---|
| `GET` | `/api/menu` | Menu complet, horaires, établissement | 5 |
| `GET` | `/api/menu/categories` | Catégories de plats acceptées | 1 |
| `POST` | `/api/menu` | Créer un plat dans une catégorie | 1 |
| `GET` | `/api/pricing-policies` | Politiques tarifaires disponibles | 2 |
| `POST` | `/api/orders` | Créer une commande avec calcul de prix | 2 |
| `GET` | `/api/orders` | Lister les commandes | — |
| `GET` | `/api/orders/{id}` | Obtenir une commande | — |
| `PUT` | `/api/orders/{id}/state` | Faire progresser le workflow | 3 |

Le contrat JSON est intégralement en anglais, comme le domaine et le squelette
fourni (`id`, `name`, `price`).

### Exemples de requêtes

Swagger, sur `/swagger`, permet d'exécuter chaque endpoint sans ligne de
commande. Les exemples ci-dessous s'enchaînent : la commande créée est celle
que l'on fait ensuite progresser.

**Consulter la carte** — les identifiants servent ensuite à commander.

```bash
curl.exe -s http://localhost:5205/api/menu
```

**Créer un plat** — besoin 1, la fabrique est sollicitée par l'API.

```bash
curl.exe -s -X POST http://localhost:5205/api/menu -H "Content-Type: application/json" -d '{"category":"Entree","id":"tartare-de-boeuf","name":"Tartare de boeuf","price":12.50,"preparationTimeMinutes":10}'
```

**201 Created.** La catégorie envoyée décide du type créé : ici un `Starter`,
qui a `servingOrder: 2`. L'endpoint n'écrit jamais `new Starter(...)`. Un
identifiant déjà pris renvoie **409 Conflict**.

**Créer une commande** — besoin 2, en formule menu.

```bash
curl.exe -s -X POST http://localhost:5205/api/orders -H "Content-Type: application/json" -d '{"tableNumber":4,"pricingPolicy":"formule","menuItemIds":["salade-cesar","steak-frites","tarte-tatin"]}'
```

Entrée 8,50 € + plat 18,50 € + dessert 7,50 € = 34,50 € → facturés **25,00 €**.
La réponse expose `subtotal`, `totalPrice` et `discountApplied`.

Politiques acceptées : `standard` (défaut), `happy-hour`, `groupe`, `formule`.
Un numéro de table négatif ou un plat hors carte renvoie **400**.

**Faire progresser une commande** — besoin 3, à rejouer quatre fois en
remplaçant `{id}` par l'identifiant reçu à la création.

```bash
curl.exe -s -X PUT http://localhost:5205/api/orders/{id}/state
```

```
InPreparation  Preparation lancee - table 4, prete dans ~20 min.
Ready          Commande prete - table 4. Ordre de service : Salade Cesar, Steak frites, Tarte Tatin.
Served         Commande servie - table 4. Reste a encaisser : 25,00 EUR.
Paid           Commande reglee - table 4, 25,00 EUR encaisses. Dossier clos.
```

Le champ `message` vient de l'étape elle-même. Une cinquième tentative renvoie
**409 Conflict**, un identifiant inconnu **404 Not Found**.

**Notifications** — besoin 4, la console affiche les réactions des services
pendant cette séquence :

```
[CUISINE]     Commande recue - table 4, 3 plat(s), 25,00 EUR.
[FACTURATION] Commande ... ouverte - 25.00 EUR (formule, remise appliquee (34,50 -> 25,00)).
[CUISINE]     Preparation lancee - table 4, prete dans ~20 min.
[SALLE]       Commande prete - table 4. Ordre de service : Salade Cesar, Steak frites, Tarte Tatin.
[SALLE]       Commande servie - table 4. Reste a encaisser : 25,00 EUR.
[FACTURATION] Commande ... reglee - 25.00 EUR encaisses.
```

---

## Architecture

### Structure

```
RestaurantApi/
├── Api/
│   └── Contracts.cs               DTO d'entrée et de sortie (9 records)
├── Configuration/                 Singleton — besoin 5
│   ├── RestaurantConfiguration.cs
│   └── OpeningHours.cs
├── Domain/
│   ├── Menu/                      Factory Method — besoin 1
│   │   ├── MenuItem.cs            produit abstrait
│   │   ├── Starter / MainCourse / Dessert / Beverage
│   │   ├── IMenuItemFactory.cs    créateur + 4 fabriques concrètes
│   │   ├── MenuItemFactoryProvider.cs
│   │   └── MenuCatalog.cs         la carte, thread-safe et extensible
│   ├── Pricing/                   Strategy — besoin 2
│   │   ├── IPricingStrategy.cs
│   │   ├── Standard / HappyHour / GroupDiscount / MenuFormula
│   │   └── PricingStrategyResolver.cs
│   ├── Workflow/                  State — besoin 3
│   │   ├── IOrderState.cs
│   │   └── Received / InPreparation / Ready / Served / Paid
│   ├── Notifications/             Observer — besoin 4
│   │   ├── IOrderObserver.cs, OrderEventPublisher.cs
│   │   └── Kitchen / DiningRoom / Billing
│   └── Orders/
│       ├── Order.cs               le contexte (State + Strategy)
│       └── OrderService.cs        orchestration
└── Repositories/
    └── OrderRepository.cs         stockage in-memory
```

### Diagramme de classes

[`diagramme-classes.drawio`](diagramme-classes.drawio), à la racine du dépôt.
Une page de vue d'ensemble, puis une page par besoin.

---

## Patterns retenus, et pourquoi

### Besoin 1 — Types de plats → **Factory Method**

**Le problème.** Il y a quatre catégories de plats. Il faut pouvoir en créer
n'importe lequel sans que le code appelant connaisse les classes concrètes, et
pouvoir ajouter une catégorie plus tard.

**Ce que j'ai fait.** `MenuItem` est une classe abstraite. `Starter`,
`MainCourse`, `Dessert` et `Beverage` en héritent. Chaque catégorie a sa
fabrique, et `MenuItemFactoryProvider` les regroupe. C'est le seul endroit du
projet où un plat est créé.

**Pourquoi pas un simple `switch`.** Un `switch` marcherait, mais il faudrait le
rouvrir à chaque nouvelle catégorie. Avec une fabrique par catégorie, j'ajoute
un fichier produit, un fichier fabrique, une valeur d'enum et une ligne
d'enregistrement. Aucun code déjà écrit n'est modifié.

La catégorie n'est pas une chaîne de caractères mais une valeur portée par le
type du plat, donc elle ne peut pas être fausse. `ServingOrder`, l'ordre dans
lequel les plats sont servis, est défini sur chaque type.

`POST /api/menu` appelle le provider, donc la fabrique est utilisée depuis
l'API comme le demande le sujet.

### Besoin 2 — Calcul du prix → **Strategy**

**Le problème.** Quatre façons de calculer un prix, à choisir au moment de la
commande, sans mettre les quatre calculs dans `Order`.

**Ce que j'ai fait.** Une interface `IPricingStrategy` et une classe par
politique. `Order.ApplyPricing()` reçoit la stratégie et lui demande le total.

**Le choix important : ce que reçoit la méthode.** `CalculateTotal` reçoit la
liste des plats, pas un montant. `MenuFormulaPricing` doit vérifier qu'il y a
une entrée, un plat et un dessert. Si la méthode recevait juste un `decimal`,
cette information serait déjà perdue.

**Le choix de la politique** se fait dans `PricingStrategyResolver`, à partir du
nom reçu dans la requête. C'est le seul endroit du projet qui décide quelle
stratégie appliquer.

**Pourquoi `HappyHourPricing` reçoit un `TimeProvider`.** Si la classe appelait
`DateTime.Now`, on ne pourrait pas la tester : le test réussirait entre 15h et
19h et échouerait le reste du temps. Avec une horloge passée en paramètre, le
test choisit l'heure.

**Politique demandée et politique appliquée.** Si on demande `happy-hour` à
midi, la commande est acceptée mais payée plein tarif. `Order` garde donc le
sous-total à côté du total et expose `DiscountApplied`, sinon la commande
afficherait une remise qui n'a pas eu lieu.

**Ce qui n'est pas résolu.** Les politiques peuvent se déclencher en même temps :
une commande de 60 € à 17h avec entrée, plat et dessert entre dans les trois. Le
code n'en applique qu'une, celle demandée. Une solution serait une politique qui
interroge les autres et garde le prix le plus bas. Elle n'est pas implémentée :
le sujet ne dit pas quel prix doit l'emporter.

### Besoin 3 — Workflow → **State**

**Le problème.** Une commande passe par cinq étapes. Chaque étape peut avoir son
propre comportement, et une commande payée ne doit plus bouger.

**Ce que j'ai fait.** Une classe par étape, toutes implémentant `IOrderState`.
`Order` ne regarde jamais dans quel état il est : il demande à son état actuel
quel est le suivant, avec `Next()`.

**Comment une commande payée est bloquée.** `PaidState.Next()` renvoie `null` :
il n'y a pas d'étape suivante. Aucun test sur le statut n'est nécessaire.

**Pourquoi une seule méthode `TryAdvance()`.** Les commandes sont partagées
entre toutes les requêtes. Si l'appelant devait d'abord tester puis avancer,
deux requêtes en même temps pourraient passer le test avant que l'une des deux
ait écrit. Ici le test et le changement d'état sont dans le même verrou. Un test
le vérifie avec 32 threads : exactement quatre transitions passent.

Quand la transition est refusée, la méthode renvoie un résultat au lieu de lancer
une exception, ce qui permet à l'API de répondre 409 plutôt que 500.

**Ce que fait chaque étape.** `InPreparationState` calcule le temps d'attente en
prenant le plat le plus long, pas la somme. `ReadyState` calcule l'ordre de
service. Ce message est renvoyé dans la réponse du `PUT`.

### Besoin 4 — Notifications → **Observer**

**Le problème.** Trois services doivent être prévenus des événements sur les
commandes, chacun réagissant à ce qui le concerne, et `Order` ne doit pas les
connaître.

**Ce que j'ai fait.** `OrderEventPublisher` garde la liste des observateurs et
leur envoie chaque événement. Chaque service décide de ce qu'il traite.

**Pourquoi la liste d'abonnés n'est pas sur `Order`.** Les commandes sont créées
et terminées en permanence, alors que les trois services vivent tout le temps de
l'application. Il faudrait les réabonner à chaque nouvelle commande. La liste
est donc dans un objet à part, et `Order` ne connaît aucun service.

**Ajouter un service.** Une classe et une ligne d'enregistrement. Les
observateurs sont enregistrés sous leur interface, et une boucle les abonne au
démarrage.

**Deux précautions.** `Notify` copie la liste pendant le verrou et envoie les
notifications en dehors. Chaque observateur est appelé dans un `try/catch` :
un service en panne n'empêche pas les autres d'être prévenus et ne fait pas
échouer la commande.

### Besoin 5 — Configuration globale → **Singleton**

**Le problème.** Le menu, les horaires et quelques paramètres doivent être
accessibles partout, en une seule instance, utilisable depuis plusieurs requêtes
en même temps.

**Ce que j'ai fait.** Le Singleton classique : une instance statique, un verrou
pour la créer, et `Initialize` qui ne construit qu'une fois.

**Pourquoi l'injection est utilisée à la place.** `AddSingleton` d'ASP.NET Core
garantit la même unicité et fournit l'objet là où on en a besoin, sans variable
globale. Les deux sont en place : `RestaurantConfiguration.Instance` existe
parce que le sujet le demande, mais aucun code de l'application ne l'appelle.

Le coût du point d'accès statique se voit sur les tests : ceux de
`RestaurantConfiguration` sont réunis dans une seule classe pour que xUnit les
exécute les uns après les autres sur la même instance.

**Thread-safe partout, pas seulement la configuration.** `OrderRepository` est
lui aussi partagé par toutes les requêtes. Il stocke dans un
`ConcurrentDictionary` : un `Dictionary` ordinaire peut être abîmé par deux
`POST` simultanés.

**Pourquoi le menu est séparé de la configuration.** Le besoin 1 demande de
créer des plats via l'API, le besoin 5 veut une configuration figée au
démarrage. Les deux ne vont pas ensemble : on ne peut pas ajouter un plat dans
une collection figée.

Les deux sont donc séparés. `RestaurantConfiguration` garde les horaires et les
paramètres, qui ne changent pas pendant le service. `MenuCatalog` porte la
carte, qui peut changer. Le catalogue utilise un `ConcurrentDictionary`, donc
plusieurs requêtes peuvent y lire et y écrire. Les `MenuItem` ne sont pas
modifiables, donc un plat déjà commandé ne change pas pendant qu'une commande
est en cours. Un test le vérifie avec 64 ajouts en même temps.
