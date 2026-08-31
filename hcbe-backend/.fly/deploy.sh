#!/bin/bash
# Quick deploy script for Fly.io

set -e

echo "🚀 HCBE Backend - Fly.io Quick Deploy"
echo "======================================"
echo ""

# Change to backend directory
cd "$(dirname "$0")/.."

# Run pre-deployment checks
if [ -f ".fly/pre-deploy-check.sh" ]; then
    ./.fly/pre-deploy-check.sh
    echo ""
fi

# Ask for confirmation
read -p "Ready to deploy? (y/N) " -n 1 -r
echo ""

if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    echo "❌ Deployment cancelled"
    exit 0
fi

echo ""
echo "📦 Building and deploying to Fly.io..."
echo ""

flyctl deploy

echo ""
echo "✅ Deployment complete!"
echo ""
echo "🔗 Your API is live at: https://$(grep 'app = ' fly.toml | cut -d'"' -f2).fly.dev"
echo ""
echo "📝 Next steps:"
echo "   1. Test the API: curl https://$(grep 'app = ' fly.toml | cut -d'"' -f2).fly.dev/"
echo "   2. View logs: flyctl logs"
echo "   3. Check status: flyctl status"
echo "   4. Initialize admin (if first deploy): flyctl ssh console"
echo ""
