# Calculatrice mobile – .NET MAUI

Application de calculatrice développée avec **.NET MAUI** (projet *Single Project*) dans le cadre de l'**Activité n°4 – Atelier de développement mobile**.

**Auteur :** LA STAR ANDREA

---

## 1. Fonctionnalités

| Fonctionnalité | Description |
|---|---|
| Opérations de base | Addition, soustraction, multiplication, division |
| Nombres décimaux | Saisie avec le bouton `.` (un seul séparateur par nombre) |
| `C` | Remise à zéro totale (saisie, opérande, opérateur, opération affichée, erreur) |
| `⌫` | Effacement du dernier caractère saisi |
| `±` | Changement de signe du nombre affiché |
| `%` | Pourcentage (voir section 5) |
| Division par zéro | Message d'erreur explicite, **sans plantage** |
| Opération en cours | Affichée en petit **au-dessus** du résultat |
| Grands nombres | Police réduite et défilement horizontal, sans débordement |

---

## 2. Prérequis et lancement

- **Visual Studio Community 2022** avec la charge de travail **« Développement d'applications multiplateformes .NET (MAUI) »**
- SDK .NET 8 (ou supérieur)
- Pour tester : émulateur Android, ou *Windows Machine* (Mode développeur de Windows activé)

**Lancer le projet :**
1. Cloner le dépôt puis ouvrir le fichier `Calculatrice.csproj` (ou la solution) dans Visual Studio.
2. Choisir la cible (émulateur Android ou *Windows Machine*) dans la barre d'outils.
3. Appuyer sur **F5**.

En ligne de commande : `dotnet run -f net8.0-windows10.0.19041.0` (adapter la version de .NET selon le `.csproj`).

---

## 3. Structure du projet

Le projet suit le modèle *Single Project* : un seul projet pour toutes les plateformes.

| Fichier | Rôle |
|---|---|
| `MainPage.xaml` | **Interface** : disposition (layouts), styles et boutons |
| `MainPage.xaml.cs` | **Logique** : gestionnaires d'événements et calculs (code-behind) |

La liaison entre les deux se fait par le nom des gestionnaires : chaque bouton déclare `Clicked="OnDigit"` dans le XAML, et la méthode `OnDigit` est définie dans le `.cs`. Les labels nommés (`x:Name="ResultLabel"`, `ExpressionLabel`, `ResultScroll`) sont accessibles directement depuis le code-behind.

---

## 4. Interface : les layouts

L'écran est organisé en arbre de layouts (5 types différents dans la même page) :

```
Grid (2 lignes : 2* / 5*)                      ← layout principal
├── Border                                      ← panneau de l'afficheur
│   └── VerticalStackLayout                     ← empile opération + résultat
│       ├── HorizontalStackLayout               ← petite barre d'état
│       ├── Label ExpressionLabel               ← opération en cours
│       └── ScrollView (horizontal)             ← grands nombres
│           └── Label ResultLabel               ← résultat
└── Grid (5 lignes × 4 colonnes, toutes en *)   ← clavier de 20 touches
```

| Layout | Justification |
|---|---|
| **Grid** (page et clavier) | Répartit l'espace par proportions (afficheur `2*`, clavier `5*`) et aligne les 20 touches en 5×4 quelle que soit l'orientation. |
| **Border** | Encadre l'afficheur avec un fond et des coins arrondis. |
| **VerticalStackLayout** | Empile naturellement l'opération en cours au-dessus du résultat. |
| **HorizontalStackLayout** | Aligne les éléments de la barre d'état. |
| **ScrollView** (horizontal) | Permet aux grands nombres de défiler au lieu de déborder. |

**Adaptation aux écrans :** les lignes et colonnes utilisent des tailles en étoile (`*`), donc les boutons se redimensionnent automatiquement (rotation, petits écrans). Les boutons ont un `MinimumHeightRequest` de 0 et un `Padding` de 0 pour pouvoir rétrécir en mode paysage.

**Styles :** trois styles de boutons (`Digit`, `Action`, `Op`) héritent d'un style `Base` commun (`BasedOn`), ce qui évite de répéter les propriétés et distingue visuellement chiffres, actions (C, ⌫, %) et opérateurs (orange).

---

## 5. Logique de calcul (`MainPage.xaml.cs`)

### 5.1 État de la calculatrice

Cinq variables privées décrivent l'état à tout moment :

| Variable | Rôle |
|---|---|
| `_current` | Nombre en cours de saisie (texte affiché) |
| `_left` | Opérande de gauche, déjà validé (`decimal?`, vide si aucun) |
| `_op` | Opérateur en attente (`+`, `−`, `×`, `÷`) |
| `_startNew` | Si `true`, le prochain chiffre **remplace** l'affichage au lieu de s'y ajouter |
| `_error` | Indique que l'affichage est en état d'erreur |

Le nombre saisi est gardé sous forme de **texte** (`string`) pour conserver exactement ce que tape l'utilisateur (par exemple `"0."` ou `"-0"`), puis converti en `decimal` seulement au moment du calcul.

### 5.2 Gestionnaires d'événements

| Méthode | Comportement |
|---|---|
| `OnDigit` | Ajoute le chiffre. Remplace le `0` initial. Limite la saisie à **15 chiffres**. Si `_startNew`, repart d'un nombre neuf. |
| `OnDecimal` | Ajoute `.` seulement s'il n'y en a pas déjà un. |
| `OnClear` | Appelle `Reset()` : tout est remis à zéro. |
| `OnBackspace` | Retire le dernier caractère ; revient à `0` s'il ne reste rien (ou seulement `-`). Sans effet juste après un résultat. |
| `OnSign` | Ajoute ou retire le préfixe `-`. Sans effet sur `0`. |
| `OnPercent` | Voir ci-dessous. |
| `OnOperator` | Mémorise l'opérande et l'opérateur, enchaîne les calculs. |
| `OnEquals` | Calcule et affiche le résultat. |

### 5.3 Le cycle d'un calcul

Exemple : `12 + 5 =`

1. **`1`, `2`** : `_current = "12"`.
2. **`+`** : `_left = 12`, `_op = "+"`, `_startNew = true`. L'opération `12 +` s'affiche au-dessus.
3. **`5`** : comme `_startNew` est vrai, `_current` est remplacé par `"5"`.
4. **`=`** : `Compute(12, "+", 5)` donne `17`. L'affichage du haut devient `12 + 5 =`, le résultat est `17`. `_left` et `_op` sont vidés, et `_startNew = true` : un nouveau chiffre démarrera un nouveau calcul, tandis qu'un opérateur continue à partir du résultat.

### 5.4 Enchaînement d'opérations

Si l'utilisateur tape `2 + 3 ×`, l'appui sur `×` déclenche d'abord le calcul de `2 + 3 = 5`, puis mémorise `5 ×`. Le calcul se fait donc **de gauche à droite**, comme sur une calculatrice simple (pas de priorité de la multiplication sur l'addition).
Si deux opérateurs sont tapés à la suite (`_startNew` vrai), le second **remplace** simplement le premier.

### 5.5 Pourcentage

- Après `+` ou `−` : le pourcentage est calculé **sur l'opérande de gauche**. Exemple : `200 + 10 %` donne `10 % de 200 = 20`, donc `200 + 20 = 220`.
- Sinon (avec `×`, `÷` ou sans opération) : simple division par 100. Exemple : `50 %` donne `0.5`.

### 5.6 La méthode `Compute`

```csharp
private decimal? Compute(decimal a, string op, decimal b)
```

Elle effectue l'opération et renvoie `null` en cas d'erreur (l'erreur est alors déjà affichée) :
- **Division par zéro** : `b == 0` est testé avant de diviser, puis `ShowError("Division par zéro impossible")`.
- **Dépassement** : une `OverflowException` est interceptée (`try/catch`) et donne « Nombre trop grand ».

### 5.7 Gestion des erreurs

`ShowError` place l'application en état d'erreur : l'affichage indique `Erreur`, le message explicatif apparaît au-dessus, et `_left` / `_op` sont vidés. Dans cet état, les opérateurs, `=`, `%` et `±` sont ignorés ; appuyer sur un chiffre, sur `.`, sur `⌫` ou sur `C` **réinitialise** la calculatrice. L'application ne plante jamais.

### 5.8 Affichage (`Refresh`)

Après chaque action, `Refresh()` :
1. met à jour `ResultLabel` ;
2. adapte la taille de police selon la longueur du texte (48 → 36 → 28) ;
3. fait défiler le `ScrollView` jusqu'à la fin pour que les derniers chiffres restent visibles.

---

## 6. Choix techniques

- **`decimal` plutôt que `double`** : évite les erreurs d'arrondi binaire (`0.1 + 0.2` donne bien `0.3`).
- **`CultureInfo.InvariantCulture`** : le point `.` est toujours utilisé comme séparateur décimal, quelle que soit la langue du téléphone, ce qui évite les erreurs d'analyse du texte.
- **Format `"G29"`** : affiche le nombre sans zéros inutiles.
- **Un gestionnaire par famille de boutons** (`OnDigit`, `OnOperator`) qui lit `((Button)sender).Text`, au lieu de 20 méthodes quasi identiques.

---

## 7. Tests manuels à effectuer

| Test | Résultat attendu |
|---|---|
| `7 × 8 =` | `56` |
| `0.1 + 0.2 =` | `0.3` |
| `5 ÷ 0 =` | `Erreur` + « Division par zéro impossible », pas de plantage |
| `200 + 10 %` puis `=` | `220` |
| `9 ⌫` | `0` |
| `±` sur `5` | `-5` |
| `2 + 3 ×` | affichage intermédiaire `5 ×` |
| Saisie de plus de 15 chiffres | la saisie est bloquée à 15 chiffres |
| Rotation en paysage | aucune déformation ni débordement |
| Petit écran (émulateur 4") | touches visibles et cliquables |

---

## 8. Limites et pistes d'amélioration

- Pas de priorité des opérateurs (calcul strictement de gauche à droite).
- La répétition de `=` (recalcul avec le dernier opérande) n'est pas gérée.
- Pas d'historique ni de mémoire (M+, M−, MR).
- Pistes d'extension : racine carrée, puissance, mémoire, historique des calculs, thème clair/sombre.
