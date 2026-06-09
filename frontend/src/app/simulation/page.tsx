"use client";

import { useState } from "react";
import { useQuery } from "@apollo/client/react";
import { DAILY_SIMULATION } from "@/lib/graphql/queries";
import { StatsCard } from "@/components/stats-card";
import { cn } from "@/lib/utils";
import { TrendingUp, DollarSign, Target, Zap, Play, CheckCircle2, XCircle } from "lucide-react";

interface SimBet {
  match: string; competition: string; pick: string; odds: number;
  probability: number; valueEdge: number; kellyFraction: number;
  stake: number; potentialReturn: number; expectedValue: number;
  result?: string | null; actualReturn: number;
}
interface Sim {
  dailyBudget: number; totalMatches: number; valueBetCount: number;
  totalStaked: number; expectedReturn: number; expectedProfit: number;
  actualReturn: number; actualProfit: number; hasResults: boolean;
  bets: SimBet[];
}

export default function Simulation() {
  const [budget, setBudget] = useState("100");
  const [active, setActive] = useState(100);

  const { data, loading, refetch } = useQuery<{ dailySimulation: Sim }>(DAILY_SIMULATION, {
    variables: { dailyBudget: active },
  });

  const sim = data?.dailySimulation;

  function run() {
    const v = parseFloat(budget) || 100;
    setActive(v);
    refetch({ dailyBudget: v });
  }

  const roiExpected = sim && sim.totalStaked > 0 ? (sim.expectedProfit / sim.totalStaked) * 100 : 0;
  const roiActual = sim && sim.hasResults && sim.totalStaked > 0 ? (sim.actualProfit / sim.totalStaked) * 100 : 0;

  return (
    <div className="space-y-8">
      <div className="animate-fade-in-up">
        <h1 className="text-3xl md:text-4xl font-bold tracking-tight">Simulation journalière</h1>
        <p className="text-muted-foreground text-sm mt-1.5">
          Combien tu gagnes si tu mises X$/jour sur les prédictions IA
        </p>
      </div>

      {/* Budget input */}
      <div className="card-base p-5 flex flex-col sm:flex-row items-stretch sm:items-end gap-3 animate-fade-in-up">
        <div className="flex-1">
          <label className="text-xs text-muted-foreground uppercase tracking-wider">Budget quotidien</label>
          <div className="relative mt-1.5">
            <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">$</span>
            <input type="number" value={budget} onChange={e => setBudget(e.target.value)}
              className="w-full rounded-xl border border-border bg-secondary pl-7 pr-3 py-2.5 text-lg font-bold font-mono focus:border-primary focus:outline-none transition-colors" />
          </div>
        </div>
        <button onClick={run}
          className="flex items-center justify-center gap-2 rounded-xl px-6 py-2.5 text-sm font-semibold text-white transition-all hover:scale-[1.02]"
          style={{ background: "linear-gradient(120deg, var(--grad-1), var(--grad-2))" }}>
          <Play className="h-4 w-4" /> Simuler
        </button>
      </div>

      {loading ? (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">{[...Array(4)].map((_,i)=><div key={i} className="skeleton h-28"/>)}</div>
      ) : !sim ? null : sim.valueBetCount === 0 ? (
        <div className="card-base p-12 text-center">
          <Zap className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
          <p className="font-semibold mb-1">Aucun value bet aujourd&apos;hui</p>
          <p className="text-muted-foreground text-sm">
            Sur {sim.totalMatches} matchs analysés, aucun avantage mathématique détecté.
            Les cotes sont efficaces — pas de pari rentable. C&apos;est normal et honnête.
          </p>
        </div>
      ) : (
        <>
          {/* Résumé */}
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 stagger">
            <StatsCard title="Misé" accent="blue" value={`$${sim.totalStaked.toFixed(2)}`}
              sub={`${sim.valueBetCount} value bets`} icon={<DollarSign className="h-5 w-5" />} />
            <StatsCard title="Gain attendu" accent="green" value={`$${sim.expectedReturn.toFixed(2)}`}
              sub={`EV statistique`} icon={<Target className="h-5 w-5" />} />
            <StatsCard title="Profit attendu" accent="purple"
              value={`${sim.expectedProfit >= 0 ? "+" : ""}$${sim.expectedProfit.toFixed(2)}`}
              sub={`ROI ${roiExpected >= 0 ? "+" : ""}${roiExpected.toFixed(1)}%`}
              icon={<TrendingUp className="h-5 w-5" />} positive={sim.expectedProfit >= 0} />
            {sim.hasResults ? (
              <StatsCard title="Profit réel" accent={sim.actualProfit >= 0 ? "green" : "red"}
                value={`${sim.actualProfit >= 0 ? "+" : ""}$${sim.actualProfit.toFixed(2)}`}
                sub={`ROI réel ${roiActual >= 0 ? "+" : ""}${roiActual.toFixed(1)}%`}
                icon={<CheckCircle2 className="h-5 w-5" />} positive={sim.actualProfit >= 0} />
            ) : (
              <StatsCard title="Résultat réel" accent="amber" value="—"
                sub="Matchs pas encore joués" icon={<Target className="h-5 w-5" />} />
            )}
          </div>

          {/* Projections */}
          <div className="card-base p-5 animate-fade-in-up">
            <h2 className="text-sm font-semibold mb-3 flex items-center gap-2">
              <TrendingUp className="h-4 w-4" style={{ color: "var(--primary)" }} />
              Projection si tu mises ${active}/jour
            </h2>
            <div className="grid grid-cols-3 gap-4 text-center">
              {[["7 jours", 7], ["30 jours", 30], ["1 an", 365]].map(([label, days]) => (
                <div key={label as string} className="rounded-xl bg-secondary/40 p-4">
                  <p className="text-xs text-muted-foreground">{label}</p>
                  <p className="text-xl font-bold mt-1" style={{ color: sim.expectedProfit >= 0 ? "var(--green)" : "var(--red)" }}>
                    {sim.expectedProfit >= 0 ? "+" : ""}${(sim.expectedProfit * (days as number)).toFixed(0)}
                  </p>
                </div>
              ))}
            </div>
            <p className="text-[11px] text-muted-foreground mt-3">
              Projection théorique basée sur l&apos;espérance mathématique (EV). Variance réelle élevée — un jour sans value bet = $0.
            </p>
          </div>

          {/* Détail paris */}
          <div>
            <h2 className="text-xl font-bold tracking-tight mb-4">Répartition du budget</h2>
            <div className="card-base overflow-hidden">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-border">
                    <th className="px-4 py-3 text-left text-xs font-semibold text-muted-foreground uppercase tracking-wider">Match</th>
                    <th className="px-4 py-3 text-left text-xs font-semibold text-muted-foreground uppercase tracking-wider">Pari</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider">Cote</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider">Edge</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider">Mise</th>
                    <th className="px-4 py-3 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider">Si gagné</th>
                    <th className="px-4 py-3 text-center text-xs font-semibold text-muted-foreground uppercase tracking-wider">Résultat</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-border/40">
                  {sim.bets.map((b, i) => (
                    <tr key={i} className="hover:bg-secondary/30 transition-colors">
                      <td className="px-4 py-3">
                        <div className="font-medium">{b.match}</div>
                        <div className="text-[10px] text-muted-foreground">{b.competition}</div>
                      </td>
                      <td className="px-4 py-3 text-xs">{b.pick}</td>
                      <td className="px-4 py-3 text-right font-mono">{b.odds.toFixed(2)}</td>
                      <td className="px-4 py-3 text-right font-mono font-semibold" style={{ color: "var(--green)" }}>+{b.valueEdge.toFixed(1)}%</td>
                      <td className="px-4 py-3 text-right font-mono font-semibold">${b.stake.toFixed(2)}</td>
                      <td className="px-4 py-3 text-right font-mono text-muted-foreground">${b.potentialReturn.toFixed(2)}</td>
                      <td className="px-4 py-3 text-center">
                        {b.result === "WON" ? (
                          <span className="inline-flex items-center gap-1 text-xs font-semibold" style={{ color: "var(--green)" }}>
                            <CheckCircle2 className="h-3 w-3" />+${b.actualReturn.toFixed(2)}
                          </span>
                        ) : b.result === "LOST" ? (
                          <span className="inline-flex items-center gap-1 text-xs font-semibold" style={{ color: "var(--red)" }}>
                            <XCircle className="h-3 w-3" />Perdu
                          </span>
                        ) : (
                          <span className="text-xs text-muted-foreground">En attente</span>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
