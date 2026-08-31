# Déploiement Fly.io - Guide Ultra-Rapide

## TL;DR (Too Long; Didn't Read)

```bash
# 1. Installer Fly CLI
brew install flyctl && flyctl auth login

# 2. Lancer l'app
cd hcbe-backend
flyctl launch --no-deploy

# 3. Créer le volume
flyctl volumes create hcbe_data --size 1 --region yul

# 4. Configurer les secrets
JWT_SECRET="$(openssl rand -base64 48)"
flyctl secrets set \
  JwtSettings__Secret="$JWT_SECRET" \
  ConnectionStrings__DefaultConnection="Data Source=/app/data/hcbe.db"
unset JWT_SECRET

# 5. Déployer
flyctl deploy

# 6. Initialiser l'admin
flyctl ssh console
dotnet HcbeApi.dll CreateAdmin
exit
```

Ton API sera disponible à : `https://hcbe-backend.fly.dev` 🎉

---

## Commandes utiles après déploiement

```bash
# Voir les logs
flyctl logs

# Statut de l'app
flyctl status

# Redémarrer
flyctl apps restart

# Ouvrir dans le navigateur
flyctl open

# Dashboard web
flyctl dashboard
```

## Mettre à jour le frontend

Dans `hcbe-frontend/.env.production` :

```env
VITE_API_URL=https://hcbe-backend.fly.dev
```

## Coûts

**100% GRATUIT** avec la configuration actuelle ✅
- Free tier : 3 machines + 3GB storage + 160GB bandwidth
- Configuration actuelle : 1 machine + 1GB storage

## Guides détaillés

- 📖 **Guide complet** : `.fly/deploy-guide.md`
- 🔍 **Vérification pré-déploiement** : `.fly/pre-deploy-check.sh`
- 🚀 **Script de déploiement** : `.fly/deploy.sh`

## Support

- Docs Fly.io : https://fly.io/docs/
- Community : https://community.fly.io/
- Status : https://status.flyio.net/
