"use client";

import { useQuery, useMutation } from "@apollo/client/react";
import { GET_BETS, GET_BET_STATS } from "@/lib/graphql/queries";
import { SETTLE_BET } from "@/lib/graphql/mutations";
import type { Bet, BetStats } from "@/lib/graphql/types";
import { StatsCard } from "@/components/stats-card";
import { TrendingUp, Target, DollarSign, Clock } from "lucide-react";
import { cn } from "@/lib/utils";

const STATUS_COLORS: Record<string, string> = {
  PENDING: "text-yellow-400 bg-yellow-900/20",
  WON: "text-green-400 bg-green-900/20",
  LOST: "text-red-400 bg-red-900/20",
  VOID: "text-gray-400 bg-gray-900/20",
};

const STATUS_LABELS: Record<string, string> = {
  PENDING: "En attente",
  WON: "Gagné",
  LOST: "Perdu",
  VOID: "Nul",
};

export default function Historique() {
  const { data: betsData, loading } = useQuery<{ bets: Bet[] }>(GET_BETS);
  const { data: statsData } = useQuery<{ betStats: BetStats }>(GET_BET_STATS);
  const [settleBet] = useMutation(SETTLE_BET, {
    refetchQueries: [{ query: GET_BETS }, { query: GET_BET_STATS }],
  });

  const bets = betsData?.bets ?? [];
  const stats = statsData?.betStats;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Historique des paris</h1>

      {stats && (
        <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
          <StatsCard title="Total paris" value={String(stats.totalBets)} sub={`${stats.pending} en attente`} icon={<Clock className="h-4 w-4 text-muted-foreground" />} />
          <StatsCard title="Win Rate" value={`${stats.winRate.toFixed(1)}%`} sub={`${stats.won}W / ${stats.lost}L`} icon={<Target className="h-4 w-4 text-muted-foreground" />} positive={stats.winRate >= 50} />
          <StatsCard title="ROI" value={`${stats.roi.toFixed(1)}%`} sub={`$${stats.totalStaked.toFixed(0)} misés`} icon={<TrendingUp className="h-4 w-4 text-muted-foreground" />} positive={stats.roi >= 0} />
          <StatsCard title="Profit net" value={`$${(stats.totalReturned - stats.totalStaked).toFixed(2)}`} sub={`$${stats.totalReturned.toFixed(0)} retournés`} icon={<DollarSign className="h-4 w-4 text-muted-foreground" />} positive={stats.totalReturned >= stats.totalStaked} />
        </div>
      )}

      {loading ? (
        <div className="text-muted-foreground">Chargement...</div>
      ) : bets.length === 0 ? (
        <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
          Aucun pari enregistré — place ton premier pari depuis le Dashboard
        </div>
      ) : (
        <div className="rounded-lg border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/50">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Pari</th>
                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Type</th>
                <th className="px-4 py-3 text-right font-medium text-muted-foreground">Mise</th>
                <th className="px-4 py-3 text-right font-medium text-muted-foreground">Cote</th>
                <th className="px-4 py-3 text-right font-medium text-muted-foreground">Retour pot.</th>
                <th className="px-4 py-3 text-center font-medium text-muted-foreground">Statut</th>
                <th className="px-4 py-3 text-center font-medium text-muted-foreground">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {bets.map((bet) => (
                <tr key={bet.id} className="hover:bg-muted/25 transition-colors">
                  <td className="px-4 py-3">
                    <div className="space-y-0.5">
                      {bet.selections.map((s, i) => (
                        <div key={i} className="text-xs">
                          <span className="font-medium">{s.pick}</span>
                          <span className="text-muted-foreground"> — {s.matchDescription}</span>
                        </div>
                      ))}
                      {bet.wasAiSuggested && (
                        <span className="text-xs text-purple-400">⚡ IA</span>
                      )}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-muted-foreground">{bet.type === "SINGLE" ? "Simple" : "Combiné"}</td>
                  <td className="px-4 py-3 text-right font-mono">${bet.stake.toFixed(2)}</td>
                  <td className="px-4 py-3 text-right font-mono">{bet.totalOdds.toFixed(2)}</td>
                  <td className="px-4 py-3 text-right font-mono">
                    {bet.actualReturn != null
                      ? <span className={bet.actualReturn > 0 ? "text-green-400" : "text-red-400"}>${bet.actualReturn.toFixed(2)}</span>
                      : <span className="text-muted-foreground">${bet.potentialReturn.toFixed(2)}</span>
                    }
                  </td>
                  <td className="px-4 py-3 text-center">
                    <span className={cn("rounded-full px-2 py-0.5 text-xs font-medium", STATUS_COLORS[bet.status] ?? "")}>
                      {STATUS_LABELS[bet.status] ?? bet.status}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center">
                    {bet.status === "PENDING" && (
                      <div className="flex justify-center gap-2">
                        <button
                          onClick={() => settleBet({ variables: { betId: bet.id, result: "WON" } })}
                          className="rounded px-2 py-1 text-xs bg-green-600 hover:bg-green-700 text-white transition-colors"
                        >Gagné</button>
                        <button
                          onClick={() => settleBet({ variables: { betId: bet.id, result: "LOST" } })}
                          className="rounded px-2 py-1 text-xs bg-red-600 hover:bg-red-700 text-white transition-colors"
                        >Perdu</button>
                      </div>
                    )}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
