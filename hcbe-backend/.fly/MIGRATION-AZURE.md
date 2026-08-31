# Migration Azure → Fly.io

Guide pour migrer le backend HCBE depuis Azure App Service vers Fly.io.

## Pourquoi migrer ?

| Aspect | Azure App Service | Fly.io |
|--------|------------------|---------|
| **Coût** | Payant (~$13/mois minimum) | Gratuit (Free Tier) |
| **Setup** | Plus complexe | Simple (`flyctl deploy`) |
| **Docker** | Limité | Natif |
| **Cold starts** | Plus longs | Plus rapides (~5s) |
| **CLI** | Azure CLI complexe | flyctl simple |
| **Logs** | Via portail web | `flyctl logs` |

## ⚠️ Avant de commencer

**Important** : Cette migration est réversible. Tes données Azure restent intactes.

1. **Backup ta base de données Azure** (si tu en as une en prod)
2. **Note tes variables d'environnement** actuelles
3. **Garde ton déploiement Azure** actif pendant les tests

## Migration étape par étape

### 1. Préparer Fly.io

```bash
# Installer Fly CLI
brew install flyctl

# Se connecter
flyctl auth login

# Lancer l'app (depuis hcbe-backend/)
cd hcbe-backend
flyctl launch --no-deploy
```

Répondre aux questions :
- **App name** : `hcbe-backend` (ou un nom unique)
- **Region** : `yul` (Montreal) ou `yyz` (Toronto)
- **PostgreSQL** : Non
- **Redis** : Non

### 2. Créer le stockage persistant

```bash
# Volume pour DB et uploads
flyctl volumes create hcbe_data --size 1 --region yul
```

### 3. Migrer les variables d'environnement

#### Récupérer les variables d'Azure

```bash
# Via Azure CLI
az webapp config appsettings list --name hcbe-backend --resource-group YOUR_RESOURCE_GROUP

# Ou via le portail Azure:
# App Service > Configuration > Application settings
```

#### Les configurer sur Fly.io

```bash
# Utiliser le script automatique
./.fly/configure-secrets.sh

# Ou manuellement
flyctl secrets set \
  JwtSettings__Secret="TON_JWT_SECRET" \
  ConnectionStrings__DefaultConnection="Data Source=/app/data/hcbe.db" \
  Cors__AllowedOrigins__0="https://ton-frontend.vercel.app" \
  Cors__AllowedOrigins__1="http://localhost:5173"
```

### 4. Migrer la base de données

#### Option A : Base vierge (recommandé pour test)

```bash
# Déployer et créer un nouvel admin
flyctl deploy
flyctl ssh console
dotnet HcbeApi.dll CreateAdmin
exit
```

#### Option B : Copier la base Azure

Si tu as des données en production à conserver :

```bash
# 1. Télécharger la DB depuis Azure (via SFTP/Kudu)
# Se connecter au Kudu Console : https://hcbe-backend.scm.azurewebsites.net/
# Naviguer vers /home/site/wwwroot
# Télécharger hcbe.db

# 2. Uploader vers Fly.io
flyctl ssh console

# Dans le SSH Fly.io:
# Copier la DB localement puis :
cat > /app/data/hcbe.db  # Coller le contenu (ou utiliser scp)
exit
```

**Méthode alternative (plus simple)** :

```bash
# 1. Télécharger depuis Azure
# Via Kudu : https://hcbe-backend.scm.azurewebsites.net/DebugConsole
# Télécharger /home/site/wwwroot/hcbe.db

# 2. Uploader vers Fly.io
flyctl ssh sftp shell
put /chemin/local/vers/hcbe.db /app/data/hcbe.db
exit
```

### 5. Migrer les fichiers uploadés

Si tu as des fichiers dans `wwwroot/uploads` sur Azure :

```bash
# 1. Télécharger depuis Azure (via Kudu)
# Kudu Console > wwwroot/uploads > Download as zip

# 2. Décompresser localement

# 3. Uploader vers Fly.io
flyctl ssh sftp shell
mkdir /app/data/uploads
put -r /chemin/local/vers/uploads/* /app/data/uploads/
exit
```

### 6. Déployer sur Fly.io

```bash
flyctl deploy
```

### 7. Tester l'application Fly.io

```bash
# Health check
curl https://hcbe-backend.fly.dev/

# Tester l'API complète
# Utiliser Postman avec la nouvelle URL

# Voir les logs en temps réel
flyctl logs
```

### 8. Mettre à jour le frontend

#### Créer un environnement de staging

`hcbe-frontend/.env.staging` :
```env
VITE_API_URL=https://hcbe-backend.fly.dev
VITE_ENABLE_MEMBER_LOGIN=true
```

#### Tester avec le frontend

```bash
cd hcbe-frontend
npm run dev -- --mode staging
```

#### Si tout fonctionne, mettre en production

`hcbe-frontend/.env.production` :
```env
VITE_API_URL=https://hcbe-backend.fly.dev
```

Redéployer le frontend sur Vercel.

### 9. Configurer le domaine personnalisé (optionnel)

Si tu veux garder `api.hcbe.ca` :

```bash
# Ajouter le certificat
flyctl certs add api.hcbe.ca

# Fly.io te donnera les DNS records
flyctl certs show api.hcbe.ca

# Configurer chez ton registrar :
# Supprimer l'ancien A record vers Azure
# Ajouter le nouveau A/AAAA vers Fly.io
```

### 10. Période de test parallèle

**Recommandé** : Garder Azure actif pendant 1-2 semaines pour :
- Comparer les performances
- Vérifier la stabilité
- Rollback facile si problème

## Migration CI/CD

### Désactiver le workflow Azure

```bash
# Renommer pour désactiver
mv .github/workflows/azure-backend-deploy.yml .github/workflows/azure-backend-deploy.yml.disabled
```

### Activer le workflow Fly.io

```bash
# Générer le token
flyctl auth token

# L'ajouter dans GitHub
# Settings > Secrets and variables > Actions
# Créer: FLY_API_TOKEN

# Le workflow dans .github/workflows/fly-deploy.yml
# sera automatiquement actif
```

## Rollback vers Azure (si nécessaire)

Si tu dois revenir en arrière :

```bash
# 1. Le frontend
# Rechanger VITE_API_URL vers l'URL Azure
# Redéployer sur Vercel

# 2. Réactiver le workflow Azure
mv .github/workflows/azure-backend-deploy.yml.disabled .github/workflows/azure-backend-deploy.yml

# 3. Push vers main pour redéployer sur Azure
git push

# 4. (Optionnel) Suspendre Fly.io
flyctl scale count 0
```

Tes données Azure n'ont pas été touchées, tout remarche immédiatement.

## Désactiver Azure définitivement

**Seulement après 2+ semaines de production stable sur Fly.io** :

```bash
# Via Azure CLI
az webapp delete --name hcbe-backend --resource-group YOUR_RESOURCE_GROUP

# Ou via le portail Azure
# App Service > Delete
```

Économie : ~$13-20/mois selon ton plan Azure.

## Comparaison des commandes

| Action | Azure | Fly.io |
|--------|-------|--------|
| Déployer | `git push` (via CI/CD) | `flyctl deploy` |
| Logs | Portail web ou `az` CLI | `flyctl logs` |
| SSH | Kudu Console | `flyctl ssh console` |
| Variables | Portail web | `flyctl secrets set` |
| Restart | Portail web | `flyctl apps restart` |
| Scale | Portail web | `flyctl scale` |

## Coûts comparés (1 an)

| Service | Azure App Service | Fly.io |
|---------|------------------|---------|
| Compute | $156+ | $0 (Free Tier) |
| Storage | Inclus | $0 (Free Tier) |
| Bandwidth | Limité | 160GB/mois |
| **Total** | **~$156-240/an** | **$0/an** |

**Économie** : ~$200/an 💰

## Checklist de migration

- [ ] Backup de la base Azure
- [ ] Notes des variables d'environnement
- [ ] Fly CLI installé et configuré
- [ ] App Fly.io créée
- [ ] Volume créé
- [ ] Secrets configurés
- [ ] Base de données migrée (ou créée neuve)
- [ ] Fichiers uploads migrés (si nécessaire)
- [ ] Premier déploiement Fly.io réussi
- [ ] Tests API complets
- [ ] Frontend configuré en staging
- [ ] Tests end-to-end
- [ ] Frontend en production avec nouvelle URL
- [ ] CI/CD migré vers Fly.io
- [ ] Période de test parallèle (1-2 semaines)
- [ ] Surveillance des logs/métriques
- [ ] Désactivation Azure (après validation)

## Support migration

Si tu rencontres des problèmes :

1. **Les deux environnements sont indépendants** — Azure continue de fonctionner
2. **Vérifier les logs** : `flyctl logs`
3. **Community Fly.io** : https://community.fly.io/
4. **Support Fly.io** : support@fly.io
5. **Rollback facile** : juste rechanger l'URL du frontend

---

**Prêt à migrer ?**

Commence par un test sans toucher à Azure :

```bash
cd hcbe-backend
./.fly/deploy.sh
```
