#!/bin/bash
# Script pour configurer tous les secrets Fly.io nécessaires

set -e

APP_NAME=$(grep 'app = ' fly.toml | cut -d'"' -f2)

echo "🔐 Configuration des secrets Fly.io"
echo "===================================="
echo ""
echo "App: $APP_NAME"
echo ""

# Check if app exists
if ! flyctl apps list | grep -q "$APP_NAME"; then
    echo "❌ App '$APP_NAME' not found"
    echo "   Run: flyctl launch --no-deploy"
    exit 1
fi

echo "✅ App found"
echo ""

# JWT Secret
echo "📝 Configuration JWT Secret..."
JWT_SECRET="${JWT_SECRET:-$(openssl rand -base64 48)}"
flyctl secrets set \
  "JwtSettings__Secret=${JWT_SECRET}" \
  -a "$APP_NAME"
unset JWT_SECRET

echo ""
echo "📝 Configuration Connection String..."
flyctl secrets set \
  "ConnectionStrings__DefaultConnection=Data Source=/app/data/hcbe.db" \
  -a "$APP_NAME"

echo ""
echo "📝 Configuration CORS - Frontend URLs..."
echo ""
echo "Entrez l'URL de votre frontend Vercel (ex: https://hcbe.vercel.app)"
read -p "URL frontend: " FRONTEND_URL

if [ -z "$FRONTEND_URL" ]; then
    echo "⚠️  Aucune URL fournie, utilisation de l'URL par défaut"
    FRONTEND_URL="https://hcbe-frontend.vercel.app"
fi

flyctl secrets set \
  "Cors__AllowedOrigins__0=${FRONTEND_URL}" \
  "Cors__AllowedOrigins__1=http://localhost:5173" \
  "Cors__AllowedOrigins__2=http://localhost:3000" \
  -a "$APP_NAME"

echo ""
echo "✅ Tous les secrets ont été configurés!"
echo ""
echo "📋 Vérification:"
flyctl secrets list -a "$APP_NAME"

echo ""
echo "🎯 Prochaine étape:"
echo "   Déployer l'application: flyctl deploy"
