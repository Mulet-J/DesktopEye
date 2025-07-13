# DesktopEye

## Prérequis

### Tous les environnements

Installez **.NET 8.0.18** sur votre système :

- **Windows** : Téléchargez depuis [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Linux/macOS** : Utilisez votre gestionnaire de paquets ou téléchargez depuis le site officiel

### Linux

Installez les dépendances supplémentaires :

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install tesseract-ocr libopencv-dev

# CentOS/RHEL/Fedora
sudo yum install tesseract opencv-devel
# ou avec dnf
sudo dnf install tesseract opencv-devel

# Arch Linux
sudo pacman -S tesseract opencv
```

### macOS

Installez Tesseract :

```bash
# Avec Homebrew
brew install tesseract

# Avec MacPorts
sudo port install tesseract
```

## Installation

1. Rendez-vous sur la page [Releases](https://github.com/Mulet-J/DesktopEye/releases/latest)
2. Téléchargez la dernière version pour votre système d'exploitation
3. Extrayez l'archive dans le dossier de votre choix

## Utilisation

Exécutez l'application :

```bash
# Linux/macOS
./DesktopEye.Desktop.MacOS

# Linux
./DesktopEye.Desktop.Linux

# Windows
./DesktopEye.Desktop.Windows.exe
```

## Support

Si vous rencontrez des problèmes, n'hésitez pas à ouvrir une [issue](https://github.com/Mulet-J/DesktopEye/issues).
