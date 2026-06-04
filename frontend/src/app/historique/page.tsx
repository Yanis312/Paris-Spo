"use client";

import { useQuery, useMutation } from "@apollo/client/react";
import { GET_BETS, GET_BET_STATS } from "@/lib/graphql/queries";
import { SETTLE_BET } from "@/lib/graphql/mutations";
import type { Bet, BetStats } from "@/lib/graphql/types";
import { StatsCard } from "@/components/stats-card";
import { TrendingUp, Target, DollarSign, Clock, Zap, CheckCircle2, XCircle } from "lucide-react";
import { cn } from "@/lib/utils";

const STATUS_CONFIG: Record<string, { label: string; bg: string; text: string }> = {
  PENDING: { label: "En attente", bg: "bg-amber-500/10",  text: "text-amber-500" },
  WON:     { label: "Gagné",      bg: "bg-green-500/10",  text: "text-green-500" },
  LOST:    { label: "Perdu",      bg: "bg-red-500/10",    text: "text-red-500" },
  VOID:    { label: "Nul",        bg: "bg-muted",         text: "text-muted-foreground" },
};

export default function Historique() {
  const { data: betsData, loading } = useQuery<{ bets: Bet[] }>(GET_BETS);
  const { data: statsData } = useQuery<{ betStats: BetStats }>(GET_BET_STATS);
  const [settleBet] = useMutation(SETTLE_BET, {
    refetchQueries: [{ query: GET_BETS }, { query: GET_BET_STATS }],
  });

  const bets = betsData?.bets ?? [];
  const stats = statsData?.betStats;
  const profit = stats ? stats.totalReturned - stats.totalStaked : 0;

  return (
    <div className="space-y-8">
      <div className="animate-fade-in-up">
        <h1 className="text-3xl font-bold tracking-tight">Historique</h1>
        <p className="text-muted-foreground text-sm mt-1">Tous tes paris et performance globale</p>
      </div>

      {stats && (
        <div className="grid grid-cols-2 gap-4 md:grid-cols-4 stagger">
          <StatsCard title="Total paris" value={String(stats.totalBets)}
            sub={`${stats.pending} en attente`} icon={<Clock className="h-4 w-4" />} />
          <StatsCard title="Win Rate" value={`${stats.winRate.toFixed(1)}%`}
            sub={`${stats.won}W / ${stats.lost}L`} icon={<Target className="h-4 w-4" />}
            positive={stats.winRate >= 50} />
          <StatsCard title="ROI" value={`${stats.roi.toFixed(1)}%`}
            sub={`$${stats.totalStaked.toFixed(0)} misés`} icon={<TrendingUp className="h-4 w-4" />}
            positive={stats.roi >= 0} />
          <StatsCard title="Profit net" value={`${profit >= 0 ? "+" : ""}$${profit.toFixed(2)}`}
            sub={`$${stats.totalReturned.toFixed(0)} retournés`} icon={<DollarSign className="h-4 w-4" />}
            positive={profit >= 0} />
        </div>
      )}

      {loading ? (
        <div className="space-y-2">
          {[...Array(4)].map((_, i) => <div key={i} className="skeleton h-16 rounded-xl" />)}
        </div>
      ) : bets.length === 0 ? (
        <div className="glass rounded-xl p-12 text-center">
          <Clock className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
          <p className="text-muted-foreground text-sm">Aucun pari — place ton premier depuis le Dashboard</p>
        </div>
      ) : (
        <div className="glass rounded-xl overflow-hidden animate-fade-in-up">
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border/60">
                  <th className="px-4 py-3.5 text-left text-xs font-semibold text-muted-foreground uppercase tracking-wider">Pari</th>
                  <th className="px-4 py-3.5 text-center text-xs font-semibold text-muted-foreground uppercase tracking-wider hidden md:table-cell">Type</th>
                  <th className="px-4 py-3.5 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider">Mise</th>
                  <th className="px-4 py-3.5 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider hidden sm:table-cell">Cote</th>
                  <th className="px-4 py-3.5 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider">Retour</th>
                  <th className="px-4 py-3.5 text-center text-xs font-semibold text-muted-foreground uppercase tracking-wider">Statut</th>
                  <th className="px-4 py-3.5 text-center text-xs font-semibold text-muted-foreground uppercase tracking-wider">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/40">
                {bets.map((bet) => {
                  const s = STATUS_CONFIG[bet.status] ?? STATUS_CONFIG.PENDING;
                  return (
                    <tr key={bet.id} className="hover:bg-secondary/30 transition-colors">
                      <td className="px-4 py-3.5">
                        <div className="space-y-0.5">
                          {bet.selections.map((sel, i) => (
                            <div key={i} className="text-xs">
                              <span className="font-semibold">{sel.pick}</span>
                              <span className="text-muted-foreground"> — {sel.matchDescription}</span>
                            </div>
                          ))}
                          {bet.wasAiSuggested && (
                            <div className="flex items-center gap-1 mt-1">
                              <Zap className="h-2.5 w-2.5" style={{ color: "var(--green)" }} />
                              <span className="text-[10px] font-medium" style={{ color: "var(--green)" }}>IA</span>
                            </div>
                          )}
                        </div>
                      </td>
                      <td className="px-4 py-3.5 text-center text-xs text-muted-foreground hidden md:table-cell">
                        {bet.type === "SINGLE" ? "Simple" : "Combiné"}
                      </td>
                      <td className="px-4 py-3.5 text-right font-mono text-sm font-semibold">${bet.stake.toFixed(2)}</td>
                      <td className="px-4 py-3.5 text-right font-mono text-xs text-muted-foreground hidden sm:table-cell">{bet.totalOdds.toFixed(2)}</td>
                      <td className="px-4 py-3.5 text-right font-mono text-sm font-semibold">
                        {bet.actualReturn != null ? (
                          <span style={{ color: bet.actualReturn > 0 ? "var(--green)" : "var(--red)" }}>
                            ${bet.actualReturn.toFixed(2)}
                          </span>
                        ) : (
                          <span className="text-muted-foreground text-xs">${bet.potentialReturn.toFixed(2)}</span>
                        )}
                      </td>
                      <td className="px-4 py-3.5 text-center">
                        <span className={cn("rounded-full px-2.5 py-1 text-[11px] font-semibold", s.bg, s.text)}>
                          {s.label}
                        </span>
                      </td>
                      <td className="px-4 py-3.5 text-center">
                        {bet.status === "PENDING" && (
                          <div className="flex justify-center gap-1.5">
                            <button
                              onClick={() => settleBet({ variables: { betId: bet.id, result: "WON" } })}
                              className="flex items-center gap-1 rounded-lg px-2.5 py-1.5 text-xs font-semibold transition-colors"
                              style={{ background: "color-mix(in oklch, var(--green) 15%, transparent)", color: "var(--green)" }}
                            >
                              <CheckCircle2 className="h-3 w-3" />Gagné
                            </button>
                            <button
                              onClick={() => settleBet({ variables: { betId: bet.id, result: "LOST" } })}
                              className="flex items-center gap-1 rounded-lg px-2.5 py-1.5 text-xs font-semibold transition-colors"
                              style={{ background: "color-mix(in oklch, var(--red) 15%, transparent)", color: "var(--red)" }}
                            >
                              <XCircle className="h-3 w-3" />Perdu
                            </button>
                          </div>
                        )}
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
}
