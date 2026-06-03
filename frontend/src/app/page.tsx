"use client";

import { useQuery, useMutation } from "@apollo/client/react";
import { GET_TODAY_MATCHES, GET_BET_STATS, GET_BANKROLL } from "@/lib/graphql/queries";
import { SYNC_TODAY_MATCHES } from "@/lib/graphql/mutations";
import type { Match, BetStats, Bankroll } from "@/lib/graphql/types";
import { MatchCard } from "@/components/match-card";
import { StatsCard } from "@/components/stats-card";
import { Button } from "@/components/ui/button";
import { RefreshCw, TrendingUp, Wallet, Target, Trophy } from "lucide-react";

export default function Dashboard() {
  const { data: matchesData, loading: matchesLoading, refetch } = useQuery<{ todayMatches: Match[] }>(GET_TODAY_MATCHES);
  const { data: statsData } = useQuery<{ betStats: BetStats }>(GET_BET_STATS);
  const { data: bankrollData } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);
  const [syncMatches, { loading: syncing }] = useMutation(SYNC_TODAY_MATCHES, {
    onCompleted: () => refetch(),
  });

  const matches = matchesData?.todayMatches ?? [];
  const stats = statsData?.betStats;
  const bankroll = bankrollData?.bankroll;

  const today = new Date().toLocaleDateString("fr-FR", {
    weekday: "long", day: "numeric", month: "long",
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold">Dashboard</h1>
          <p className="text-muted-foreground capitalize">{today}</p>
        </div>
        <Button onClick={() => syncMatches()} disabled={syncing} variant="outline" size="sm">
          <RefreshCw className={`mr-2 h-4 w-4 ${syncing ? "animate-spin" : ""}`} />
          {syncing ? "Sync..." : "Sync matchs"}
        </Button>
      </div>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <StatsCard
          title="Bankroll"
          value={bankroll ? `$${bankroll.currentAmount.toFixed(0)}` : "—"}
          sub={bankroll ? `ROI: ${bankroll.roi.toFixed(1)}%` : "Non initialisé"}
          icon={<Wallet className="h-4 w-4 text-muted-foreground" />}
          positive={bankroll ? bankroll.roi >= 0 : undefined}
        />
        <StatsCard
          title="Win Rate"
          value={stats ? `${stats.winRate.toFixed(1)}%` : "—"}
          sub={stats ? `${stats.won}W / ${stats.lost}L` : "Aucun pari"}
          icon={<Target className="h-4 w-4 text-muted-foreground" />}
          positive={stats ? stats.winRate >= 50 : undefined}
        />
        <StatsCard
          title="ROI Global"
          value={stats ? `${stats.roi.toFixed(1)}%` : "—"}
          sub={stats ? `$${stats.totalStaked.toFixed(0)} misés` : ""}
          icon={<TrendingUp className="h-4 w-4 text-muted-foreground" />}
          positive={stats ? stats.roi >= 0 : undefined}
        />
        <StatsCard
          title="Matchs aujourd'hui"
          value={String(matches.length)}
          sub={`${matches.filter(m => m.aiAnalysis).length} analysés IA`}
          icon={<Trophy className="h-4 w-4 text-muted-foreground" />}
        />
      </div>

      <div>
        <h2 className="mb-3 text-lg font-semibold">Matchs du jour</h2>
        {matchesLoading ? (
          <div className="text-muted-foreground">Chargement...</div>
        ) : matches.length === 0 ? (
          <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
            Aucun match — clique sur &ldquo;Sync matchs&rdquo; pour charger depuis football-data.org
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {matches.map((match) => (
              <MatchCard key={match.id} match={match} bankroll={bankroll} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
