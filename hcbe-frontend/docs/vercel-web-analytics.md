# Vercel Web Analytics — guide opérateur

## Prérequis

- Web Analytics est **déjà activé** sur le projet Vercel `hcbe-frontend`.
- Le front embarque `<Analytics />` via `@vercel/analytics/react` (monté dans `App.tsx`).

## Où lire les stats

1. Ouvrir [Vercel Dashboard](https://vercel.com).
2. Projet **hcbe-frontend**.
3. Onglet **Analytics**.

Tu y trouves notamment :

- pages les plus consultées ;
- tendances de trafic (filtres jour / semaine / période) ;
- visiteurs / vues selon le plan Vercel.

Il n’y a **pas** de page dédiée dans l’admin HCBE en V1.

## Après un déploiement

1. Déployer le frontend en production.
2. Visiter une page publique du site.
3. Attendre quelques minutes, puis rafraîchir Analytics.

Si aucune vue n’apparaît : vérifier que Web Analytics est toujours activé sur le projet, et que le build de prod inclut bien `@vercel/analytics`.

## Hors scope V1

- Événements custom (clics newsletter, formulaires)
- Speed Insights
- Graphiques dans `/admin`
