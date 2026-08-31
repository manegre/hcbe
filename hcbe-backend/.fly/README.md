# HCBE Backend - Fly.io Deployment Files

Ce dossier contient tous les fichiers nécessaires pour déployer le backend HCBE sur Fly.io.

## 📁 Fichiers

### Configuration

- **`fly.toml`** (racine du projet) — Configuration principale Fly.io
  - Définit la région, les ressources, le health check
  - Configuration des volumes persistants

- **`appsettings.Production.json`** — Settings pour l'environnement production
  - Connection string vers le volume persistant
  - CORS configuré pour le frontend
  - Chemin des uploads optimisé

### Documentation

- **`QUICKSTART.md`** — ⚡ Guide ultra-rapide (5 minutes)
  - TL;DR pour déployer en 6 commandes
  - Parfait pour un premier déploiement

- **`deploy-guide.md`** — 📚 Guide complet et détaillé
  - Toutes les commandes expliquées
  - Troubleshooting
  - Monitoring et scaling
  - CI/CD GitHub Actions
  - Domaine personnalisé

- **`README.md`** — Ce fichier (vue d'ensemble)

### Scripts automatisés

- **`configure-secrets.sh`** — 🔐 Configure tous les secrets Fly.io
  - JWT secret
  - Connection string
  - CORS origins
  - Interactif et guidé

- **`pre-deploy-check.sh`** — 🔍 Vérification pré-déploiement
  - Vérifie que flyctl est installé
  - Vérifie la configuration
  - Vérifie les secrets
  - Liste les actions manquantes

- **`deploy.sh`** — 🚀 Déploiement en une commande
  - Exécute le pre-deploy check
  - Demande confirmation
  - Lance le déploiement
  - Affiche les prochaines étapes

## 🚀 Utilisation rapide

### Première fois (déploiement initial)

```bash
# 1. Installer Fly CLI et se connecter
brew install flyctl
flyctl auth login

# 2. Depuis le dossier hcbe-backend
cd /Users/fabrice/Dev/hcbe/hcbe-backend

# 3. Lancer l'app sur Fly.io
flyctl launch --no-deploy

# 4. Créer le volume persistant
flyctl volumes create hcbe_data --size 1 --region yul

# 5. Configurer les secrets (script interactif)
./.fly/configure-secrets.sh

# 6. Déployer
flyctl deploy

# 7. Initialiser l'admin (une seule fois)
flyctl ssh console
dotnet HcbeApi.dll CreateAdmin
exit
```

### Déploiements suivants

```bash
# Option 1: Script automatique
./.fly/deploy.sh

# Option 2: Commande directe
flyctl deploy
```

## 📋 Checklist de déploiement

- [ ] Fly CLI installé (`brew install flyctl`)
- [ ] Connecté à Fly.io (`flyctl auth login`)
- [ ] App créée (`flyctl launch --no-deploy`)
- [ ] Volume créé (`flyctl volumes create hcbe_data --size 1 --region yul`)
- [ ] Secrets configurés (`./.fly/configure-secrets.sh`)
- [ ] Première deploy (`flyctl deploy`)
- [ ] Admin initialisé (`flyctl ssh console` → `dotnet HcbeApi.dll CreateAdmin`)
- [ ] API testée (`curl https://hcbe-backend.fly.dev/`)
- [ ] Frontend mis à jour avec la nouvelle URL

## 🔗 Ressources

- **App URL** : `https://hcbe-backend.fly.dev`
- **Dashboard** : `https://fly.io/apps/hcbe-backend`
- **Docs Fly.io** : https://fly.io/docs/
- **Community** : https://community.fly.io/
- **Status** : https://status.flyio.net/

## 💰 Coûts

Configuration actuelle : **100% GRATUIT** ✅

Free tier Fly.io :
- 3 shared-cpu machines (256MB RAM)
- 3GB de volumes persistants
- 160GB de bandwidth/mois

Notre config :
- 1 machine shared-cpu-1x avec 256MB RAM
- 1GB volume persistant
- Auto-sleep après inactivité (optimisation gratuite)

## 🆘 Support

Si tu rencontres un problème :

1. **Vérifier les logs** : `flyctl logs`
2. **Vérifier le statut** : `flyctl status`
3. **Lancer le diagnostic** : `./.fly/pre-deploy-check.sh`
4. **Consulter le guide** : [deploy-guide.md](deploy-guide.md)
5. **Community Fly.io** : https://community.fly.io/

## 🔄 CI/CD

Pour activer le déploiement automatique via GitHub Actions :

1. Générer un token : `flyctl auth token`
2. L'ajouter dans GitHub : Settings > Secrets > Actions > `FLY_API_TOKEN`
3. Le workflow `.github/workflows/fly-deploy.yml` s'activera automatiquement

---

**Prêt à déployer ?**

```bash
./.fly/deploy.sh
```
