# RDP Manager Pro

RDP Manager Pro est une application **WPF pour Windows** conçue pour centraliser et simplifier la gestion des connexions distantes **RDP** et **SSH** dans une interface moderne, fluide et orientée productivité.

## Aperçu

Pensée pour un usage quotidien, l’application permet d’accéder rapidement à plusieurs machines distantes depuis une interface claire, avec tableau de bord, filtrage par protocole, activité récente et accès rapide depuis la zone de notification.

## Fonctionnalités

- Gestion centralisée des connexions **RDP** et **SSH**
- Interface **WPF moderne** avec thème sombre
- **Tableau de bord** récapitulatif
- **Recherche rapide** des connexions
- **Filtrage** par type de connexion
- **Activité récente** avec actions de reconnexion
- **Actions rapides** depuis l’interface principale
- **Fenêtre compacte** accessible depuis la zone de notification
- Organisation simple des connexions pour un accès plus rapide

## Captures d’écran

### Fenêtre principale

![RDP Manager Pro - Fenêtre principale](docs/main-window.png)

### Fenêtre compacte

![RDP Manager Pro - Fenêtre compacte](docs/tray_window.png)

## Technologies utilisées

- **C#**
- **WPF**
- **XAML**
- **.NET Framework 4.8**

## Architecture du projet

Le projet suit une structure claire pour faciliter la maintenance et l’évolution de l’application :

```text
RDPManager/
├── Converters/
├── docs/
├── Models/
├── Properties/
├── Resources/
├── Services/
├── ViewModels/
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml
├── MainWindow.xaml.cs
├── TrayFlyout.xaml
├── TrayFlyout.xaml.cs
├── RDPManager.csproj
└── README.md