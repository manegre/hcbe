# HCBE Frontend

Ce projet est une application React (Vite) pour le site HCBE.

## Prérequis
- Node.js 18+ (recommandé: 20 ou 22)
- Un gestionnaire de paquets: `npm` (ou `pnpm`, `yarn` si vous préférez)

## Installation
```bash
# Depuis le dossier hcbe-frontend
npm install
# ou
pnpm install
# ou
yarn install
```

## Démarrage en développement
Lance le serveur Vite sur le port 3000.
```bash
npm run dev
# ou
pnpm dev
# ou
yarn dev
```
- Accès: http://localhost:3000
- Le `basename` du routeur vient de la variable `BASE_PATH` (par défaut `/`).

## Variables d'environnement
Ces variables influencent le build et le comportement (voir [vite.config.ts](vite.config.ts)):
- `BASE_PATH`: Base path pour le routage (ex: `/hcbe`).
- `IS_PREVIEW`: Active un mode "preview" (booléen).
- `PROJECT_ID`, `VERSION_ID`: Métadonnées utilisées au build.
- `VITE_API_URL`: URL du backend API (par défaut: `http://localhost:8080` pour Docker)

Utilisation en local:
```bash
# Exemple: démarrer avec un base path spécifique
BASE_PATH=/hcbe npm run dev

# Exemple: configurer l'URL de l'API (si backend tourne sur un autre port)
VITE_API_URL=http://localhost:5049 npm run dev
```

### Configuration de l'API Backend

Le frontend se connecte au backend via l'URL configurée dans `VITE_API_URL`:
- **Avec Docker** (par défaut): `http://localhost:8080`
- **Sans Docker** (développement local): `http://localhost:5049`

Pour changer l'URL de l'API, créez un fichier `.env` à la racine du projet frontend:
```bash
# .env
VITE_API_URL=http://localhost:8080
```

Assurez-vous que le backend Docker est démarré:
```bash
cd ../hcbe-backend
docker compose up -d
```

## Build de production
Génère les fichiers statiques dans `out/` avec sourcemaps.
```bash
npm run build
# ou
pnpm build
# ou
yarn build
```

## Prévisualisation du build
Servez le dossier compilé via Vite preview.
```bash
npm run preview
# ou
pnpm preview
# ou
yarn preview
```
- Accès par défaut: http://localhost:4173

## Intégration backend
Le frontend se connecte au backend HCBE pour l'authentification et les fonctionnalités admin. 

**Configuration requise:**
- Le backend doit être démarré (Docker recommandé: `docker compose up -d` dans `hcbe-backend`)
- L'URL de l'API est configurée via `VITE_API_URL` (par défaut: `http://localhost:8080` pour Docker)
- Le backend doit autoriser les requêtes CORS depuis `http://localhost:3000` (configuré dans `appsettings.json`)

## Scripts disponibles
Définis dans [package.json](package.json):
- `dev`: démarre le serveur de développement Vite
- `build`: construit la version production
- `preview`: sert le build pour vérification

## Déploiement
- Le contenu prêt à servir se trouve dans `out/` après `npm run build`.
- Servez `out/` via votre hébergeur/serveur web (Nginx, Apache, CDN, etc.).


Admin creds (admin@hcbe.ca / hcbe@2025!)