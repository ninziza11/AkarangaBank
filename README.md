# Akaranga Bank

Application web de gestion bancaire développée avec **ASP.NET Core MVC**, **Entity Framework Core** et **SQL Server**.
Projet de travaux pratiques du cours *Développement Web avec ASP.NET Core*, BAC+3 Génie Logiciel,
Faculté des TIC, Université Polytechnique de Gitega (année académique 2025-2026).

- **Étudiant** : NINZIZA théodose
- **Matricule**: F2024/00054
- **Enseignant** : Ferdinand NIRAGIRA

## Présentation

Akaranga Bank est une banque commerciale fictive dont le siège est à Bujumbura, avec des agences à
Gitega, Ngozi et Rumonge. L'application gère les agences, les clients, la vérification d'identité (KYC),
les comptes, les prêts et les cartes bancaires, avec une interface accessible et disponible en
plusieurs langues.

## Technologies

| Élément | Choix |
|---|---|
| Framework | ASP.NET Core MVC (.NET 10) |
| Accès aux données | Entity Framework Core 10 (approche *Database First*) |
| Base de données | SQL Server Express (`.\SQLEXPRESS`) |
| Authentification | ASP.NET Core Identity + JWT (API mobile) |
| Interface | Razor, Bootstrap |
| Langues | Français, English, Kirundi, Kiswahili |

## Architecture

Le projet suit le schéma **Contrôleur → Service → ViewModel → Vue**.

```
AkarangaBank.Web/
├── Controllers/     Contrôleurs MVC (Agences, Clients, VerificationsKYC, Comptes, Culture...)
├── Data/            AkarangaBankContext (DbContext EF Core)
├── Models/          Entités mappées sur les 16 tables de la base
├── Services/        Logique métier (interfaces + implémentations)
├── ViewModels/      Objets échangés entre contrôleurs et vues
├── Views/           Vues Razor (.cshtml) et _Layout
├── Resources/       Fichiers de traduction (.resx : fr, en, rn, sw)
├── wwwroot/         Fichiers statiques (CSS, JS)
├── SharedResource.cs
├── Program.cs
└── appsettings.json
```

## Modules

- **Agences** : liste, détails, création, modification, suppression, siège.
- **Clients** : CRUD avec recherche et pagination.
- **Vérifications KYC** : suivi des pièces d'identité des clients.
- **Comptes** : ouverture et consultation des comptes.
- **Prêts et cartes** (chapitre 12) : modèles et base prêts ; émission de carte avec plafonds en cours.
- **Accessibilité et internationalisation** (chapitre 13) : lien d'évitement, focus visible,
  sélecteur de langue, fichiers de ressources.

## Base de données

La base est créée à partir du script de référence fourni par l'enseignant :
`AkarangaBank-Script-DB.sql` (16 tables et jeu de données de test).
Le projet n'utilise pas de migrations : le script SQL est la source de vérité.

Le fichier `carte-plafonds.sql` ajoute les plafonds de retrait, de paiement et de paiement en ligne
à la table `Cartes` (chapitre 12).

## Installation

### Prérequis

- [.NET SDK 10](https://dotnet.microsoft.com/download)
- SQL Server Express (instance `SQLEXPRESS`)
- `sqlcmd` (outil en ligne de commande SQL Server)
- Visual Studio Code avec l'extension C# (ou Visual Studio)

### Étapes

1. **Cloner ou copier le projet**, puis ouvrir un terminal dans le dossier `AkarangaBank.Web`.

2. **Créer la base de données** (attention : le script supprime la base `AkarangaBank` si elle existe) :

   ```powershell
   sqlcmd -S ".\SQLEXPRESS" -E -C -f 65001 -i AkarangaBank-Script-DB.sql
   sqlcmd -S ".\SQLEXPRESS" -E -C -f 65001 -i carte-plafonds.sql
   ```

3. **Vérifier la base** (résultat attendu : 16 tables, 5 clients, 6 comptes, 5 vérifications KYC) :

   ```powershell
   sqlcmd -S ".\SQLEXPRESS" -E -C -d AkarangaBank -Q "SELECT (SELECT COUNT(*) FROM sys.tables) AS Tables_, (SELECT COUNT(*) FROM Clients) AS Clients_, (SELECT COUNT(*) FROM Comptes) AS Comptes_, (SELECT COUNT(*) FROM VerificationsKYC) AS KYC_"
   ```

4. **Vérifier la chaîne de connexion** dans `appsettings.json` :

   ```json
   "ConnectionStrings": {
     "AkarangaBank": "Server=.\\SQLEXPRESS;Database=AkarangaBank;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

5. **Compiler et lancer** :

   ```powershell
   dotnet build
   dotnet run
   ```

   Ouvrir ensuite l'adresse affichée dans le terminal (par exemple `https://localhost:5xxx`).

## Comptes de démonstration

Créés automatiquement au démarrage, **uniquement pour l'environnement local** :

| Rôle | Nom d'utilisateur | Mot de passe |
|---|---|---|
| Administrateur | `admin` | `Admin@2026` |
| Gestionnaire | `gestionnaire` | `Akaranga@2026` |
| Guichetier | `guichetier` | `Akaranga@2026` |
| Auditeur | `auditeur` | `Akaranga@2026` |
| Client | `client` | `Akaranga@2026` |

> Ces mots de passe sont publics dans ce dépôt. Ils ne doivent jamais être utilisés en production.

## Changer de langue

Un sélecteur dans la barre de menu propose Français, English, Kirundi et Kiswahili.
Le choix est mémorisé dans un cookie pendant un an. Les textes se trouvent dans `Resources/`
(`SharedResource.resx` pour le français, puis `.en`, `.rn`, `.sw`).

## Accessibilité

Le projet vise le niveau WCAG 2.1 AA : lien d'évitement vers le contenu, attribut `lang` de la page,
navigation complète au clavier, focus visible, zones cliquables d'au moins 44 px, contrastes
suffisants, respect de la préférence « réduire les animations ».

## Problèmes fréquents

| Problème | Solution |
|---|---|
| `MSB3021` fichier verrouillé à la compilation | L'application tourne encore : arrêter avec `Ctrl + C`. |
| `Invalid column name ...` | La base est ancienne : relancer le script SQL. |
| Erreur de connexion (instance introuvable) | Démarrer le service : `Get-Service 'MSSQL$SQLEXPRESS'` (guillemets simples dans PowerShell). |
| `SharedResource` introuvable | Créer `SharedResource.cs` à la racine du projet, avec `namespace AkarangaBank.Web;`. |
| Vue qui attend un autre modèle (`InvalidOperationException`) | La vue n'est pas à jour avec son ViewModel : remplacer le fichier `.cshtml`. |

## Auteur

NINZIZA, étudiant en Génie Logiciel, Université Polytechnique de Gitega.