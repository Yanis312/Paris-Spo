"use client";

import { useQuery, useMutation } from "@apollo/client/react";
import { GET_TODAY_MATCHES, GET_BET_STATS, GET_BANKROLL } from "@/lib/graphql/queries";
import { SYNC_TODAY_MATCHES, ANALYZE_TODAY_MATCHES } from "@/lib/graphql/mutations";
import type { Match, BetStats, Bankroll } from "@/lib/graphql/types";
import { MatchCard } from "@/components/match-card";
import { StatsCard } from "@/components/stats-card";
import { RefreshCw, TrendingUp, Wallet, Target, Trophy, Zap, Sparkles } from "lucide-react";

export default function Dashboard() {
  const { data: matchesData, loading, refetch } = useQuery<{ todayMatches: Match[] }>(GET_TODAY_MATCHES);
  const { data: statsData } = useQuery<{ betStats: BetStats }>(GET_BET_STATS);
  const { data: bankrollData } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);

  const [syncMatches, { loading: syncing }] = useMutation(SYNC_TODAY_MATCHES, { onCompleted: () => refetch() });
  const [analyzeAll, { loading: analyzing }] = useMutation(ANALYZE_TODAY_MATCHES, { onCompleted: () => refetch() });

  const matches = matchesData?.todayMatches ?? [];
  const stats = statsData?.betStats;
  const bankroll = bankrollData?.bankroll;
  const settled = stats ? stats.won + stats.lost : 0;
  const valueBetCount = matches.filter(m => m.aiAnalysis?.suggestions.some(s => s.isValueBet)).length;
  const analyzedCount = matches.filter(m => m.aiAnalysis).length;

  const today = new Date().toLocaleDateString("fr-FR", { weekday: "long", day: "numeric", month: "long", year: "numeric" });

  return (
    <div className="space-y-8">
      {/* Hero header */}
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
        <div className="animate-fade-in-up">
          <h1 className="text-3xl md:text-4xl font-bold tracking-tight">
            Bon retour <span className="gradient-text">👋</span>
          </h1>
          <p className="text-muted-foreground capitalize mt-1.5 text-sm">{today}</p>
        </div>
        <div className="flex gap-2 animate-fade-in-up" style={{ animationDelay: "80ms" }}>
          <button onClick={() => analyzeAll()} disabled={analyzing || matches.length === 0}
            className="flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-white transition-all hover:scale-[1.02] disabled:opacity-50 disabled:hover:scale-100"
            style={{ background: "linear-gradient(120deg, var(--grad-1), var(--grad-2))" }}>
            <Sparkles className={`h-4 w-4 ${analyzing ? "animate-pulse" : ""}`} />
            {analyzing ? "Analyse IA..." : "Analyser tout"}
          </button>
          <button onClick={() => syncMatches()} disabled={syncing}
            className="flex items-center gap-2 rounded-xl border border-border bg-card hover:bg-secondary text-sm font-medium px-4 py-2.5 transition-all disabled:opacity-60">
            <RefreshCw className={`h-4 w-4 ${syncing ? "animate-spin" : ""}`} />
            <span className="hidden sm:inline">{syncing ? "Sync..." : "Sync"}</span>
          </button>
        </div>
      </div>

      {/* Stats */}
      <div className="grid grid-cols-2 gap-4 lg:grid-cols-4 stagger">
        <StatsCard title="Bankroll" accent="green"
          value={bankroll ? `$${bankroll.currentAmount.toFixed(0)}` : "—"}
          sub={bankroll ? `ROI ${bankroll.roi >= 0 ? "+" : ""}${bankroll.roi.toFixed(1)}%` : "Non initialisé"}
          icon={<Wallet className="h-5 w-5" />} positive={bankroll ? bankroll.roi >= 0 : undefined} />
        <StatsCard title="Win Rate" accent="blue"
          value={settled > 0 ? `${stats!.winRate.toFixed(0)}%` : "—"}
          sub={stats ? `${stats.won}W · ${stats.lost}L` : "Aucun pari"}
          icon={<Target className="h-5 w-5" />} positive={settled > 0 ? stats!.winRate >= 50 : undefined} />
        <StatsCard title="ROI Global" accent="purple"
          value={stats && stats.totalStaked > 0 ? `${stats.roi >= 0 ? "+" : ""}${stats.roi.toFixed(0)}%` : "—"}
          sub={stats && stats.totalStaked > 0 ? `$${stats.totalStaked.toFixed(0)} misés` : "—"}
          icon={<TrendingUp className="h-5 w-5" />} positive={stats && stats.totalStaked > 0 ? stats.roi >= 0 : undefined} />
        <StatsCard title="Value Bets" accent="amber"
          value={String(valueBetCount)}
          sub={`${matches.length} matchs · ${analyzedCount} analysés`}
          icon={<Zap className="h-5 w-5" />} />
      </div>

      {/* Value alert */}
      {valueBetCount > 0 && (
        <div className="animate-fade-in-up flex items-center gap-3 rounded-xl px-4 py-3.5"
          style={{ background: "color-mix(in srgb, var(--green) 8%, transparent)", border: "1px solid color-mix(in srgb, var(--green) 25%, transparent)" }}>
          <span className="flex h-8 w-8 items-center justify-center rounded-lg shrink-0" style={{ background: "color-mix(in srgb, var(--green) 18%, transparent)" }}>
            <Zap className="h-4 w-4" style={{ color: "var(--green)" }} />
          </span>
          <p className="text-sm font-medium">
            <span style={{ color: "var(--green)" }} className="font-bold">{valueBetCount} value bet{valueBetCount > 1 ? "s" : ""}</span>
            <span className="text-muted-foreground"> détecté{valueBetCount > 1 ? "s" : ""} — avantage mathématique vs bookmakers</span>
          </p>
        </div>
      )}

      {/* Matches */}
      <section>
        <div className="flex items-center justify-between mb-4">
          <h2 className="text-xl font-bold tracking-tight flex items-center gap-2">
            <Trophy className="h-5 w-5" style={{ color: "var(--primary)" }} />
            Matchs du jour
          </h2>
          {matches.length > 0 && (
            <span className="text-sm text-muted-foreground">{matches.length} match{matches.length > 1 ? "s" : ""}</span>
          )}
        </div>

        {loading ? (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {[...Array(3)].map((_, i) => <div key={i} className="skeleton h-64" />)}
          </div>
        ) : matches.length === 0 ? (
          <div className="card-base p-12 text-center">
            <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl" style={{ background: "color-mix(in srgb, var(--primary) 12%, transparent)" }}>
              <Trophy className="h-7 w-7" style={{ color: "var(--primary)" }} />
            </div>
            <p className="font-semibold mb-1">Aucun match chargé</p>
            <p className="text-muted-foreground text-sm">Clique sur &ldquo;Sync&rdquo; pour charger les matchs du jour</p>
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3 stagger">
            {matches.map(match => <MatchCard key={match.id} match={match} bankroll={bankroll} />)}
          </div>
        )}
      </section>
    </div>
  );
}
