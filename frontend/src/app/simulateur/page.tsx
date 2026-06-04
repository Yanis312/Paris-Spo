"use client";

import { useState } from "react";
import { useQuery } from "@apollo/client/react";
import { GET_TODAY_MATCHES, GET_BANKROLL } from "@/lib/graphql/queries";
import type { Match, Bankroll } from "@/lib/graphql/types";
import { Plus, Trash2, TrendingUp, AlertTriangle, Calculator } from "lucide-react";
import { cn } from "@/lib/utils";

interface ComboSel { matchId: string; matchLabel: string; pick: string; odds: number; }

export default function Simulateur() {
  const { data: matchesData } = useQuery<{ todayMatches: Match[] }>(GET_TODAY_MATCHES);
  const { data: bankrollData } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);
  const [sels, setSels] = useState<ComboSel[]>([]);
  const [stake, setStake] = useState("10");

  const matches = matchesData?.todayMatches ?? [];
  const bankroll = bankrollData?.bankroll;

  const totalOdds = sels.reduce((a, s) => a * s.odds, 1);
  const stakeNum = parseFloat(stake || "0");
  const potReturn = totalOdds * stakeNum;
  const profit = potReturn - stakeNum;
  const successProb = sels.reduce((a, s) => a * (1 / s.odds), 1);
  const ev = successProb * potReturn - (1 - successProb) * stakeNum;

  const risk = sels.length === 0 ? null : totalOdds < 2 ? "Faible" : totalOdds < 5 ? "Modéré" : totalOdds < 15 ? "Élevé" : "Très élevé";
  const riskColor = risk === "Faible" ? "var(--green)" : risk === "Modéré" ? "var(--amber)" : "var(--red)";

  function pick(match: Match, p: "home" | "draw" | "away") {
    const o = match.odds[0]; if (!o) return;
    const odds = p === "home" ? o.homeWin : p === "draw" ? o.draw : o.awayWin;
    const label = p === "home" ? match.homeTeamName : p === "draw" ? "Nul" : match.awayTeamName;
    setSels(prev => {
      const ex = prev.find(s => s.matchId === match.id);
      if (ex) return prev.map(s => s.matchId === match.id ? { ...s, pick: label, odds } : s);
      return [...prev, { matchId: match.id, matchLabel: `${match.homeTeamName} vs ${match.awayTeamName}`, pick: label, odds }];
    });
  }

  return (
    <div className="space-y-8">
      <div className="animate-fade-in-up">
        <h1 className="text-3xl md:text-4xl font-bold tracking-tight">Simulateur de combinés</h1>
        <p className="text-muted-foreground text-sm mt-1.5">Construis ton combiné et calcule la valeur espérée</p>
      </div>

      <div className="grid gap-6 lg:grid-cols-5">
        {/* Matchs */}
        <div className="lg:col-span-3 space-y-3">
          <h2 className="font-bold flex items-center gap-2"><Calculator className="h-4 w-4" style={{ color: "var(--primary)" }} />Matchs disponibles</h2>
          {matches.length === 0 ? (
            <div className="card-base p-8 text-center text-muted-foreground text-sm">Sync les matchs depuis le Dashboard</div>
          ) : matches.map(match => {
            const o = match.odds[0];
            const sel = sels.find(s => s.matchId === match.id);
            return (
              <div key={match.id} className={cn("card-base p-4 transition-all", sel && "border-primary/50")}>
                <div className="text-[10px] text-muted-foreground uppercase tracking-wide mb-1.5 font-semibold">{match.competitionName}</div>
                <div className="text-sm font-bold mb-3">{match.homeTeamName} <span className="text-muted-foreground font-normal">vs</span> {match.awayTeamName}</div>
                {o ? (
                  <div className="grid grid-cols-3 gap-2">
                    {([["home", match.homeTeamName, o.homeWin], ["draw", "Nul", o.draw], ["away", match.awayTeamName, o.awayWin]] as const).map(([p, label, odds]) => {
                      const active = sel?.pick === (p === "home" ? match.homeTeamName : p === "draw" ? "Nul" : match.awayTeamName);
                      return (
                        <button key={p} onClick={() => pick(match, p)}
                          className={cn("flex flex-col items-center gap-0.5 rounded-xl border px-2 py-2 transition-all",
                            active ? "border-primary bg-primary/15 text-primary" : "border-border bg-secondary/40 hover:border-primary/40")}>
                          <span className="text-[9px] uppercase tracking-wide opacity-70 truncate max-w-full">{label.slice(0, 8)}</span>
                          <span className="text-sm font-bold font-mono">{odds.toFixed(2)}</span>
                        </button>
                      );
                    })}
                  </div>
                ) : <p className="text-xs text-muted-foreground">Pas de cotes</p>}
              </div>
            );
          })}
        </div>

        {/* Combiné */}
        <div className="lg:col-span-2 space-y-4">
          <h2 className="font-bold">Mon combiné</h2>
          {sels.length === 0 ? (
            <div className="card-base p-8 text-center text-muted-foreground text-sm">
              <Plus className="mx-auto h-6 w-6 mb-2 opacity-50" />
              Sélectionne des matchs
            </div>
          ) : (
            <>
              <div className="space-y-2">
                {sels.map((s, i) => (
                  <div key={i} className="card-base flex items-center justify-between px-3 py-2.5">
                    <div className="min-w-0">
                      <p className="text-[10px] text-muted-foreground truncate">{s.matchLabel}</p>
                      <p className="text-sm font-semibold">{s.pick} <span className="font-mono text-xs text-primary">@{s.odds.toFixed(2)}</span></p>
                    </div>
                    <button onClick={() => setSels(prev => prev.filter((_, j) => j !== i))}>
                      <Trash2 className="h-4 w-4 text-muted-foreground hover:text-red-500 transition-colors" />
                    </button>
                  </div>
                ))}
              </div>

              <div className="card-base p-4 space-y-3">
                <Row label="Cote combinée" value={totalOdds.toFixed(2)} bold />
                <Row label="Probabilité succès" value={`${(successProb * 100).toFixed(1)}%`} />
                <Row label="Risque" value={risk!} color={riskColor} />

                <div className="border-t border-border pt-3">
                  <label className="text-xs text-muted-foreground">Mise</label>
                  <div className="relative mt-1">
                    <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground text-sm">$</span>
                    <input type="number" value={stake} onChange={e => setStake(e.target.value)}
                      className="w-full rounded-xl border border-border bg-secondary pl-7 pr-3 py-2 text-sm font-mono focus:border-primary focus:outline-none transition-colors" />
                  </div>
                  {bankroll && stakeNum > bankroll.maxRecommendedStake && (
                    <p className="mt-1.5 flex items-center gap-1 text-[11px]" style={{ color: "var(--amber)" }}>
                      <AlertTriangle className="h-3 w-3" />Dépasse la mise max (${bankroll.maxRecommendedStake.toFixed(2)})
                    </p>
                  )}
                </div>

                <div className="rounded-xl p-3 space-y-2" style={{ background: "color-mix(in srgb, var(--primary) 6%, transparent)" }}>
                  <Row label="Retour potentiel" value={`$${potReturn.toFixed(2)}`} color="var(--green)" bold />
                  <Row label="Profit" value={`+$${profit.toFixed(2)}`} color="var(--green)" />
                  <Row label="Valeur espérée (EV)" value={`${ev >= 0 ? "+" : ""}$${ev.toFixed(2)}`} color={ev >= 0 ? "var(--green)" : "var(--red)"} bold icon={<TrendingUp className="h-3 w-3" />} />
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
}

function Row({ label, value, color, bold, icon }: { label: string; value: string; color?: string; bold?: boolean; icon?: React.ReactNode }) {
  return (
    <div className="flex items-center justify-between text-sm">
      <span className="text-muted-foreground flex items-center gap-1">{icon}{label}</span>
      <span className={cn("font-mono", bold && "font-bold")} style={{ color }}>{value}</span>
    </div>
  );
}
