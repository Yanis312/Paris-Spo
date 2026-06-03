# Paris-Spo ⚽

Application de paris sportifs intelligente et personnelle — football uniquement (ligues européennes + Coupe du Monde).

## Stack

- **Backend** : .NET + Hot Chocolate (GraphQL) + Semantic Kernel
- **Base de données** : MongoDB
- **Frontend** : Next.js + TypeScript + Apollo Client
- **IA** : Semantic Kernel + Claude API
- **Data** : API-Football + The Odds API
- **Scraping** : Firecrawl + Reddit MCP + Apify

## Fonctionnalités

- Suggestions de paris du jour avec score de confiance
- Value betting (détection cotes mal calculées)
- Kelly Criterion — mise optimale selon bankroll
- Analyse blessures en temps réel
- Scraping forums Reddit (r/soccer, r/Ligue1, r/PremierLeague)
- Simulateur de combinés avec évaluation IA
- Historique personnel + ROI stats
- Comparateur multi-bookmaker
- Backtesting sur historique

## Modules

1. **Dashboard du jour** — matchs + top 3 suggestions
2. **Agent IA Analyste** — orchestre blessures + forums + cotes
3. **Simulateur de combinés** — cote combinée + risque IA
4. **Historique & Analytics** — ROI par compétition/bookmaker
5. **Bankroll Manager** — mise max + alertes + simulation what-if

## Structure

```
Paris-Spo/
├── backend/          # .NET solution
│   ├── API/          # Hot Chocolate GraphQL
│   ├── Domain/       # Modèles métier
│   ├── Infrastructure/ # MongoDB + APIs externes
│   └── AI/           # Semantic Kernel agents
├── frontend/         # Next.js app
└── docs/             # Documentation API
```
