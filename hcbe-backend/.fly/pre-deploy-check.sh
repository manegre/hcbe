#!/bin/bash
# Pre-deployment check script for Fly.io

set -e

echo "🔍 Checking Fly.io deployment readiness..."
echo ""

# Check if flyctl is installed
if ! command -v flyctl &> /dev/null; then
    echo "❌ flyctl is not installed"
    echo "   Install with: brew install flyctl"
    exit 1
fi
echo "✅ flyctl is installed"

# Check if user is logged in
if ! flyctl auth whoami &> /dev/null; then
    echo "❌ Not logged in to Fly.io"
    echo "   Run: flyctl auth login"
    exit 1
fi
echo "✅ Logged in to Fly.io"

# Check if fly.toml exists
if [ ! -f "fly.toml" ]; then
    echo "❌ fly.toml not found"
    echo "   Run: flyctl launch --no-deploy"
    exit 1
fi
echo "✅ fly.toml exists"

# Check if .fly/appsettings.Production.json exists
if [ ! -f ".fly/appsettings.Production.json" ]; then
    echo "⚠️  .fly/appsettings.Production.json not found"
    echo "   Production settings will use defaults"
else
    echo "✅ Production settings configured"
fi

# Check if Dockerfile exists
if [ ! -f "Dockerfile" ]; then
    echo "❌ Dockerfile not found"
    exit 1
fi
echo "✅ Dockerfile exists"

# Get app name from fly.toml
APP_NAME=$(grep 'app = ' fly.toml | cut -d'"' -f2)

if [ -z "$APP_NAME" ]; then
    echo "❌ Could not read app name from fly.toml"
    exit 1
fi
echo "✅ App name: $APP_NAME"

# Check if app exists on Fly.io
if flyctl apps list | grep -q "$APP_NAME"; then
    echo "✅ App exists on Fly.io"
    
    # Check if volume exists
    if flyctl volumes list -a "$APP_NAME" 2>/dev/null | grep -q "hcbe_data"; then
        echo "✅ Persistent volume configured"
    else
        echo "⚠️  No persistent volume found"
        echo "   Create with: flyctl volumes create hcbe_data --size 1 --region yul"
    fi
    
    # Check secrets
    echo ""
    echo "📋 Checking secrets..."
    SECRETS=$(flyctl secrets list -a "$APP_NAME" 2>/dev/null | tail -n +2 | awk '{print $1}')
    
    REQUIRED_SECRETS=(
        "JwtSettings__Secret"
        "ConnectionStrings__DefaultConnection"
    )
    
    for secret in "${REQUIRED_SECRETS[@]}"; do
        if echo "$SECRETS" | grep -q "^${secret}$"; then
            echo "   ✅ $secret"
        else
            echo "   ⚠️  $secret (missing)"
        fi
    done
    
    if echo "$SECRETS" | grep -q "Cors__AllowedOrigins"; then
        echo "   ✅ CORS configured"
    else
        echo "   ⚠️  CORS not configured"
    fi
else
    echo "⚠️  App not found on Fly.io"
    echo "   Run: flyctl launch --no-deploy"
fi

echo ""
echo "🎯 Next steps:"
echo "   1. Make sure volume exists: flyctl volumes create hcbe_data --size 1 --region yul"
echo "   2. Set required secrets (see .fly/deploy-guide.md)"
echo "   3. Deploy: flyctl deploy"
echo ""
echo "📚 Full guide: .fly/deploy-guide.md"
