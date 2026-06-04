"use client";

import { useState } from "react";
import { useQuery, useMutation } from "@apollo/client/react";
import { GET_BANKROLL } from "@/lib/graphql/queries";
import { INITIALIZE_BANKROLL } from "@/lib/graphql/mutations";
import type { Bankroll } from "@/lib/graphql/types";
import { StatsCard } from "@/components/stats-card";
import { TrendingUp, Wallet, Shield, ArrowUpRight, ArrowDownRight, Sparkles } from "lucide-react";
import { cn } from "@/lib/utils";
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer } from "recharts";

export default function BankrollPage() {
  const [initAmount, setInitAmount] = useState("");
  const { data, loading, refetch } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);
  const [initBankroll, { loading: initing }] = useMutation(INITIALIZE_BANKROLL, { onCompleted: () => refetch() });

  const bankroll = data?.bankroll;
  const chartData = bankroll?.transactions.slice(-30).map((tx, i) => ({ i: i + 1, balance: tx.balanceAfter })) ?? [];

  if (loading) return <div className="space-y-4"><div className="skeleton h-10 w-48" /><div className="grid grid-cols-2 md:grid-cols-4 gap-4">{[...Array(4)].map((_,i)=><div key={i} className="skeleton h-28"/>)}</div></div>;

  if (!bankroll) {
    return (
      <div className="space-y-8">
        <div className="animate-fade-in-up">
          <h1 className="text-3xl md:text-4xl font-bold tracking-tight">Bankroll Manager</h1>
          <p className="text-muted-foreground text-sm mt-1.5">Gère ton capital avec le critère de Kelly</p>
        </div>
        <div className="mx-auto max-w-md card-base p-8 text-center space-y-5 animate-fade-in-up">
          <div className="mx-auto flex h-16 w-16 items-center justify-center rounded-2xl" style={{ background: "linear-gradient(120deg, var(--grad-1), var(--grad-2))" }}>
            <Wallet className="h-8 w-8 text-white" />
          </div>
          <div>
            <p className="font-bold text-lg">Initialise ta bankroll</p>
            <p className="text-muted-foreground text-sm mt-1">Définis ton capital de départ pour les paris</p>
          </div>
          <div className="flex gap-2">
            <div className="relative flex-1">
              <span className="absolute left-3 top-1/2 -translate-y-1/2 text-muted-foreground">$</span>
              <input type="number" value={initAmount} onChange={e => setInitAmount(e.target.value)} placeholder="500"
                className="w-full rounded-xl border border-border bg-secondary pl-7 pr-3 py-2.5 text-sm focus:border-primary focus:outline-none transition-colors" />
            </div>
            <button onClick={() => initBankroll({ variables: { amount: parseFloat(initAmount) } })} disabled={!initAmount || initing}
              className="rounded-xl px-5 py-2.5 text-sm font-semibold text-white disabled:opacity-50 transition-all hover:scale-[1.02]"
              style={{ background: "linear-gradient(120deg, var(--grad-1), var(--grad-2))" }}>
              {initing ? "..." : "Démarrer"}
            </button>
          </div>
        </div>
      </div>
    );
  }

  const profit = bankroll.currentAmount - bankroll.initialAmount;

  return (
    <div className="space-y-8">
      <div className="animate-fade-in-up">
        <h1 className="text-3xl md:text-4xl font-bold tracking-tight">Bankroll Manager</h1>
        <p className="text-muted-foreground text-sm mt-1.5">Suivi de capital et gestion du risque</p>
      </div>

      <div className="grid grid-cols-2 lg:grid-cols-4 gap-4 stagger">
        <StatsCard title="Capital actuel" accent="green" value={`$${bankroll.currentAmount.toFixed(2)}`}
          sub={`Initial: $${bankroll.initialAmount.toFixed(0)}`} icon={<Wallet className="h-5 w-5" />} />
        <StatsCard title="ROI" accent="purple" value={`${bankroll.roi >= 0 ? "+" : ""}${bankroll.roi.toFixed(1)}%`}
          sub={`${profit >= 0 ? "+" : ""}$${profit.toFixed(2)}`} icon={<TrendingUp className="h-5 w-5" />} positive={bankroll.roi >= 0} />
        <StatsCard title="Mise max" accent="amber" value={`$${bankroll.maxRecommendedStake.toFixed(0)}`}
          sub={`${bankroll.maxStakePercent}% du capital`} icon={<Shield className="h-5 w-5" />} />
        <StatsCard title="Kelly" accent="blue" value={`${(bankroll.kellyFraction * 100).toFixed(0)}%`}
          sub="Facteur prudent" icon={<Sparkles className="h-5 w-5" />} />
      </div>

      {chartData.length > 1 && (
        <div className="card-base p-5 animate-fade-in-up">
          <h2 className="text-sm font-semibold mb-4 flex items-center gap-2">
            <TrendingUp className="h-4 w-4" style={{ color: "var(--primary)" }} />
            Évolution du capital
          </h2>
          <ResponsiveContainer width="100%" height={220}>
            <AreaChart data={chartData}>
              <defs>
                <linearGradient id="g" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="0%" stopColor="var(--green)" stopOpacity={0.35} />
                  <stop offset="100%" stopColor="var(--green)" stopOpacity={0} />
                </linearGradient>
              </defs>
              <XAxis dataKey="i" hide />
              <YAxis hide domain={["auto", "auto"]} />
              <Tooltip contentStyle={{ background: "var(--card)", border: "1px solid var(--border)", borderRadius: "12px", fontSize: "12px" }}
                formatter={(v) => [`$${Number(v).toFixed(2)}`, "Capital"]} />
              <Area type="monotone" dataKey="balance" stroke="var(--green)" fill="url(#g)" strokeWidth={2.5} dot={false} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      )}

      <div>
        <h2 className="text-xl font-bold tracking-tight mb-4">Transactions</h2>
        <div className="card-base overflow-hidden animate-fade-in-up">
          {bankroll.transactions.length === 0 ? (
            <div className="p-8 text-center text-muted-foreground text-sm">Aucune transaction</div>
          ) : (
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-border">
                  <th className="px-4 py-3 text-left text-xs font-semibold text-muted-foreground uppercase tracking-wider">Description</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider">Montant</th>
                  <th className="px-4 py-3 text-right text-xs font-semibold text-muted-foreground uppercase tracking-wider">Solde</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-border/40">
                {[...bankroll.transactions].reverse().slice(0, 20).map((tx, i) => (
                  <tr key={i} className="hover:bg-secondary/30 transition-colors">
                    <td className="px-4 py-3">{tx.description}</td>
                    <td className="px-4 py-3 text-right font-mono font-semibold">
                      <span className="inline-flex items-center gap-1" style={{ color: tx.amount >= 0 ? "var(--green)" : "var(--red)" }}>
                        {tx.amount >= 0 ? <ArrowUpRight className="h-3 w-3" /> : <ArrowDownRight className="h-3 w-3" />}
                        {tx.amount >= 0 ? "+" : ""}${tx.amount.toFixed(2)}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-right font-mono text-muted-foreground">${tx.balanceAfter.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </div>
    </div>
  );
}
