"use client";

import { useState } from "react";
import { useQuery } from "@apollo/client/react";
import { GET_TODAY_MATCHES, GET_BANKROLL } from "@/lib/graphql/queries";
import type { Match, Bankroll } from "@/lib/graphql/types";
import { Plus, Trash2, TrendingUp, AlertTriangle } from "lucide-react";
import { cn } from "@/lib/utils";

interface ComboSelection {
  matchId: string;
  matchLabel: string;
  pick: string;
  odds: number;
}

export default function Simulateur() {
  const { data: matchesData } = useQuery<{ todayMatches: Match[] }>(GET_TODAY_MATCHES);
  const { data: bankrollData } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);
  const [selections, setSelections] = useState<ComboSelection[]>([]);
  const [stake, setStake] = useState("10");

  const matches = matchesData?.todayMatches ?? [];
  const bankroll = bankrollData?.bankroll;

  const totalOdds = selections.reduce((acc, s) => acc * s.odds, 1);
  const potentialReturn = totalOdds * parseFloat(stake || "0");
  const profit = potentialReturn - parseFloat(stake || "0");

  // Probabilité combinée approximative (inverse des cotes)
  const successProb = selections.reduce((acc, s) => acc * (1 / s.odds), 1);
  const expectedValue = successProb * potentialReturn - (1 - successProb) * parseFloat(stake || "0");

  const riskLevel = selections.length === 0 ? null
    : totalOdds < 2 ? "Faible"
    : totalOdds < 5 ? "Modéré"
    : totalOdds < 15 ? "Élevé"
    : "Très élevé";

  const riskColor = riskLevel === "Faible" ? "text-green-400"
    : riskLevel === "Modéré" ? "text-yellow-400"
    : "text-red-400";

  function addSelection(match: Match, pick: "home" | "draw" | "away") {
    const odds = match.odds[0];
    if (!odds) return;
    const o = pick === "home" ? odds.homeWin : pick === "draw" ? odds.draw : odds.awayWin;
    const label = pick === "home" ? match.homeTeamName : pick === "draw" ? "Nul" : match.awayTeamName;

    if (selections.find(s => s.matchId === match.id)) {
      setSelections(prev => prev.map(s => s.matchId === match.id ? { ...s, pick: label, odds: o } : s));
    } else {
      setSelections(prev => [...prev, { matchId: match.id, matchLabel: `${match.homeTeamName} vs ${match.awayTeamName}`, pick: label, odds: o }]);
    }
  }

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Simulateur de combinés</h1>

      <div className="grid gap-6 lg:grid-cols-2">
        {/* Sélection des matchs */}
        <div className="space-y-3">
          <h2 className="font-semibold">Matchs disponibles</h2>
          {matches.length === 0 ? (
            <div className="rounded-lg border border-dashed p-6 text-center text-muted-foreground text-sm">
              Sync les matchs depuis le Dashboard d&apos;abord
            </div>
          ) : (
            matches.map(match => {
              const odds = match.odds[0];
              const selected = selections.find(s => s.matchId === match.id);
              return (
                <div key={match.id} className={cn("rounded-lg border p-3 transition-colors", selected && "border-primary/50 bg-primary/5")}>
                  <div className="text-xs text-muted-foreground mb-2">{match.competitionName}</div>
                  <div className="text-sm font-medium mb-2">{match.homeTeamName} vs {match.awayTeamName}</div>
                  {odds ? (
                    <div className="flex gap-2">
                      {(["home", "draw", "away"] as const).map(pick => {
                        const o = pick === "home" ? odds.homeWin : pick === "draw" ? odds.draw : odds.awayWin;
                        const label = pick === "home" ? match.homeTeamName : pick === "draw" ? "X" : match.awayTeamName;
                        const isSelected = selected?.pick === (pick === "home" ? match.homeTeamName : pick === "draw" ? "Nul" : match.awayTeamName);
                        return (
                          <button
                            key={pick}
                            onClick={() => addSelection(match, pick)}
                            className={cn(
                              "flex-1 rounded-md border py-1.5 text-xs font-mono transition-colors",
                              isSelected ? "border-primary bg-primary text-primary-foreground" : "hover:bg-muted"
                            )}
                          >
                            {label}<br />{o.toFixed(2)}
                          </button>
                        );
                      })}
                    </div>
                  ) : (
                    <p className="text-xs text-muted-foreground">Pas de cotes disponibles</p>
                  )}
                </div>
              );
            })
          )}
        </div>

        {/* Récapitulatif */}
        <div className="space-y-4">
          <h2 className="font-semibold">Mon combiné</h2>

          {selections.length === 0 ? (
            <div className="rounded-lg border border-dashed p-6 text-center text-muted-foreground text-sm">
              <Plus className="mx-auto h-6 w-6 mb-2" />
              Sélectionne des matchs à gauche
            </div>
          ) : (
            <div className="space-y-2">
              {selections.map((s, i) => (
                <div key={i} className="flex items-center justify-between rounded-md border px-3 py-2">
                  <div>
                    <p className="text-xs text-muted-foreground">{s.matchLabel}</p>
                    <p className="text-sm font-medium">{s.pick} <span className="font-mono text-xs text-muted-foreground">@ {s.odds.toFixed(2)}</span></p>
                  </div>
                  <button onClick={() => setSelections(prev => prev.filter((_, j) => j !== i))}>
                    <Trash2 className="h-4 w-4 text-muted-foreground hover:text-red-400 transition-colors" />
                  </button>
                </div>
              ))}
            </div>
          )}

          {/* Calculs */}
          {selections.length > 0 && (
            <div className="rounded-lg border p-4 space-y-3">
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Cote combinée</span>
                <span className="font-mono font-bold">{totalOdds.toFixed(2)}</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Probabilité succès</span>
                <span className="font-mono">{(successProb * 100).toFixed(1)}%</span>
              </div>
              <div className="flex justify-between text-sm">
                <span className="text-muted-foreground">Risque</span>
                <span className={cn("font-medium", riskColor)}>{riskLevel}</span>
              </div>

              <div className="border-t pt-3">
                <label className="text-sm text-muted-foreground">Mise ($)</label>
                <input
                  type="number"
                  value={stake}
                  onChange={e => setStake(e.target.value)}
                  className="mt-1 w-full rounded-md border bg-background px-3 py-2 text-sm font-mono"
                />
                {bankroll && parseFloat(stake) > bankroll.maxRecommendedStake && (
                  <p className="mt-1 flex items-center gap-1 text-xs text-yellow-400">
                    <AlertTriangle className="h-3 w-3" />
                    Dépasse la mise max recommandée (${bankroll.maxRecommendedStake.toFixed(2)})
                  </p>
                )}
              </div>

              <div className="rounded-md bg-muted/50 p-3 space-y-2">
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Retour potentiel</span>
                  <span className="font-mono font-bold text-green-400">${potentialReturn.toFixed(2)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground">Profit potentiel</span>
                  <span className="font-mono font-bold text-green-400">+${profit.toFixed(2)}</span>
                </div>
                <div className="flex justify-between text-sm">
                  <span className="text-muted-foreground flex items-center gap-1">
                    <TrendingUp className="h-3 w-3" /> Valeur espérée
                  </span>
                  <span className={cn("font-mono font-bold", expectedValue >= 0 ? "text-green-400" : "text-red-400")}>
                    {expectedValue >= 0 ? "+" : ""}${expectedValue.toFixed(2)}
                  </span>
                </div>
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
