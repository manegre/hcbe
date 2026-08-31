# Guide de déploiement Fly.io — HCBE Backend

## Prérequis

1. Installer Fly CLI :
   ```bash
   # macOS
   brew install flyctl
   
   # Ou avec curl
   curl -L https://fly.io/install.sh | sh
   ```

2. Se connecter à Fly.io :
   ```bash
   flyctl auth login
   ```

## Déploiement initial

### 1. Créer l'application

Depuis le dossier `hcbe-backend` :

```bash
cd /Users/fabrice/Dev/hcbe/hcbe-backend

# Lancer l'application (remplace hcbe-backend par un nom unique si déjà pris)
flyctl launch --no-deploy
```

Répondre aux questions :
- **App name** : `hcbe-backend` (ou un nom unique)
- **Region** : `yul` (Montreal) ou `yyz` (Toronto)
- **PostgreSQL** : Non (on utilise SQLite)
- **Redis** : Non

### 2. Créer le volume persistant

```bash
flyctl volumes create hcbe_data --size 1 --region yul
```

Ce volume conservera :
- La base de données SQLite (`/app/data/hcbe.db`)
- Les fichiers uploadés (`/app/data/uploads`)

### 3. Configurer les secrets (variables d'environnement)

```bash
# Génère un secret JWT unique, configure-le, puis retire-le du shell
JWT_SECRET="$(openssl rand -base64 48)"
flyctl secrets set JwtSettings__Secret="$JWT_SECRET"
unset JWT_SECRET

# Connection string pour le volume persistant
flyctl secrets set ConnectionStrings__DefaultConnection="Data Source=/app/data/hcbe.db"

# CORS - ajoute ton domaine frontend (sera disponible après déploiement frontend)
flyctl secrets set Cors__AllowedOrigins__0="https://ton-frontend.vercel.app"
flyctl secrets set Cors__AllowedOrigins__1="http://localhost:5173"
flyctl secrets set Cors__AllowedOrigins__2="http://localhost:3000"
```

**Important** : Les variables d'environnement hiérarchiques dans .NET utilisent `__` (double underscore) au lieu de `:`.

### 4. Déployer

```bash
flyctl deploy
```

Le déploiement :
1. Build l'image Docker
2. Pousse l'image sur le registre Fly.io
3. Démarre l'application
4. Monte le volume persistant

### 5. Vérifier le déploiement

```bash
# Ouvrir l'app dans le navigateur
flyctl open

# Voir les logs en temps réel
flyctl logs

# Vérifier le statut
flyctl status

# Voir les infos de l'app
flyctl info
```

Ton API sera accessible à : `https://hcbe-backend.fly.dev`

### 6. Initialiser la base de données

```bash
# Se connecter à la machine pour créer l'admin
flyctl ssh console

# Une fois connecté en SSH :
cd /app
dotnet HcbeApi.dll CreateAdmin
exit
```

## Commandes utiles

### Déploiements suivants

```bash
flyctl deploy
```

### Voir les logs

```bash
# Logs en temps réel
flyctl logs

# Dernières 200 lignes
flyctl logs -a hcbe-backend --lines 200
```

### Se connecter en SSH

```bash
flyctl ssh console
```

### Gérer les secrets

```bash
# Lister
flyctl secrets list

# Ajouter/Modifier
flyctl secrets set KEY=value

# Supprimer
flyctl secrets unset KEY
```

### Redémarrer l'application

```bash
flyctl apps restart
```

### Scale (ajuster les ressources)

```bash
# Changer la taille de la VM
flyctl scale vm shared-cpu-1x --memory 512

# Augmenter le nombre d'instances (haute disponibilité)
flyctl scale count 2
```

### Gérer les volumes

```bash
# Lister les volumes
flyctl volumes list

# Étendre la taille du volume
flyctl volumes extend hcbe_data --size 2

# Créer un snapshot
flyctl volumes snapshots create hcbe_data
```

## Monitoring

### Dashboard Fly.io

```bash
flyctl dashboard
```

### Métriques

```bash
flyctl metrics
```

## Rollback

Si un déploiement pose problème :

```bash
# Lister les releases
flyctl releases

# Rollback à la version précédente
flyctl releases rollback
```

## Coûts (Free Tier)

Le free tier inclut :
- **3 machines partagées** (shared-cpu-1x avec 256MB RAM chacune)
- **3GB de volumes persistants** (on utilise 1GB)
- **160GB de bandwidth sortant**

Avec la configuration actuelle (1 machine + 1GB volume), **tu restes dans le free tier**.

## Domaine personnalisé (optionnel)

Si tu veux utiliser `api.hcbe.ca` :

```bash
# Ajouter le certificat SSL
flyctl certs add api.hcbe.ca

# Fly.io te donnera les records DNS à configurer (CNAME ou A/AAAA)
flyctl certs show api.hcbe.ca
```

## Mise à jour du frontend

Une fois le backend déployé, met à jour le fichier `.env.production` du frontend :

```env
VITE_API_URL=https://hcbe-backend.fly.dev
VITE_ENABLE_MEMBER_LOGIN=true
```

## Troubleshooting

### L'app ne démarre pas

```bash
# Vérifier les logs détaillés
flyctl logs

# Vérifier que le volume est bien monté
flyctl ssh console
ls -la /app/data
```

### Problème de CORS

Assure-toi que l'origine du frontend est bien dans les secrets :

```bash
flyctl secrets list
# Si manquant :
flyctl secrets set Cors__AllowedOrigins__0="https://ton-frontend.vercel.app"
```

### Base de données corrompue

```bash
# Se connecter en SSH
flyctl ssh console

# Backup de la DB
cp /app/data/hcbe.db /app/data/hcbe.db.backup

# Recréer la DB (⚠️ perte de données)
rm /app/data/hcbe.db
dotnet HcbeApi.dll CreateAdmin
```

## CI/CD avec GitHub Actions (optionnel)

Tu peux automatiser le déploiement avec GitHub Actions :

1. Générer un token Fly.io :
   ```bash
   flyctl auth token
   ```

2. Ajouter le token dans les secrets GitHub :
   - Aller dans Settings > Secrets and variables > Actions
   - Créer `FLY_API_TOKEN` avec le token

3. Créer `.github/workflows/fly-deploy.yml` (voir fichier séparé)

---

**Note** : Fly.io démarre automatiquement les machines quand il y a du trafic et les arrête après inactivité. C'est normal et fait partie de l'optimisation du free tier.
