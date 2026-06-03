"use client";

import { useState } from "react";
import { useQuery, useMutation } from "@apollo/client/react";
import { GET_BANKROLL } from "@/lib/graphql/queries";
import { INITIALIZE_BANKROLL } from "@/lib/graphql/mutations";
import type { Bankroll } from "@/lib/graphql/types";
import { StatsCard } from "@/components/stats-card";
import { TrendingUp, Wallet, Shield, ArrowUpRight, ArrowDownRight } from "lucide-react";
import { cn } from "@/lib/utils";
import { AreaChart, Area, XAxis, YAxis, Tooltip, ResponsiveContainer } from "recharts";

export default function BankrollPage() {
  const [initAmount, setInitAmount] = useState("");
  const { data, loading, refetch } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);
  const [initBankroll, { loading: initing }] = useMutation(INITIALIZE_BANKROLL, {
    onCompleted: () => refetch(),
  });

  const bankroll = data?.bankroll;

  const chartData = bankroll?.transactions
    .slice(-30)
    .map((tx, i) => ({ i: i + 1, balance: tx.balanceAfter })) ?? [];

  if (loading) return <div className="text-muted-foreground">Chargement...</div>;

  if (!bankroll) {
    return (
      <div className="space-y-6">
        <h1 className="text-2xl font-bold">Bankroll Manager</h1>
        <div className="mx-auto max-w-sm rounded-lg border p-6 text-center space-y-4">
          <Wallet className="mx-auto h-12 w-12 text-muted-foreground" />
          <p className="text-muted-foreground">Initialise ta bankroll pour commencer</p>
          <div className="flex gap-2">
            <input
              type="number"
              value={initAmount}
              onChange={e => setInitAmount(e.target.value)}
              placeholder="Montant ($)"
              className="flex-1 rounded-md border bg-background px-3 py-2 text-sm"
            />
            <button
              onClick={() => initBankroll({ variables: { amount: parseFloat(initAmount) } })}
              disabled={!initAmount || initing}
              className="rounded-md bg-primary px-4 py-2 text-sm font-medium text-primary-foreground disabled:opacity-50"
            >
              Initialiser
            </button>
          </div>
        </div>
      </div>
    );
  }

  const profit = bankroll.currentAmount - bankroll.initialAmount;

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Bankroll Manager</h1>

      <div className="grid grid-cols-2 gap-4 md:grid-cols-4">
        <StatsCard
          title="Bankroll actuelle"
          value={`$${bankroll.currentAmount.toFixed(2)}`}
          sub={`Initiale: $${bankroll.initialAmount.toFixed(0)}`}
          icon={<Wallet className="h-4 w-4 text-muted-foreground" />}
        />
        <StatsCard
          title="ROI"
          value={`${bankroll.roi.toFixed(1)}%`}
          sub={`${profit >= 0 ? "+" : ""}$${profit.toFixed(2)}`}
          icon={<TrendingUp className="h-4 w-4 text-muted-foreground" />}
          positive={bankroll.roi >= 0}
        />
        <StatsCard
          title="Mise max recommandée"
          value={`$${bankroll.maxRecommendedStake.toFixed(2)}`}
          sub={`${bankroll.maxStakePercent}% de la bankroll`}
          icon={<Shield className="h-4 w-4 text-muted-foreground" />}
        />
        <StatsCard
          title="Kelly fraction"
          value={`${(bankroll.kellyFraction * 100).toFixed(0)}%`}
          sub="Facteur conservateur"
          icon={<Shield className="h-4 w-4 text-muted-foreground" />}
        />
      </div>

      {/* Graphique évolution */}
      {chartData.length > 1 && (
        <div className="rounded-lg border p-4">
          <h2 className="mb-4 text-sm font-semibold text-muted-foreground">Évolution bankroll (30 dernières transactions)</h2>
          <ResponsiveContainer width="100%" height={200}>
            <AreaChart data={chartData}>
              <defs>
                <linearGradient id="bankrollGrad" x1="0" y1="0" x2="0" y2="1">
                  <stop offset="5%" stopColor="#22c55e" stopOpacity={0.3} />
                  <stop offset="95%" stopColor="#22c55e" stopOpacity={0} />
                </linearGradient>
              </defs>
              <XAxis dataKey="i" hide />
              <YAxis hide domain={["auto", "auto"]} />
              <Tooltip
                contentStyle={{ background: "hsl(var(--card))", border: "1px solid hsl(var(--border))", borderRadius: "8px" }}
                formatter={(v) => [`$${Number(v).toFixed(2)}`, "Balance"]}
              />
              <Area type="monotone" dataKey="balance" stroke="#22c55e" fill="url(#bankrollGrad)" strokeWidth={2} dot={false} />
            </AreaChart>
          </ResponsiveContainer>
        </div>
      )}

      {/* Transactions */}
      <div>
        <h2 className="mb-3 text-lg font-semibold">Dernières transactions</h2>
        <div className="rounded-lg border overflow-hidden">
          <table className="w-full text-sm">
            <thead className="border-b bg-muted/50">
              <tr>
                <th className="px-4 py-3 text-left font-medium text-muted-foreground">Description</th>
                <th className="px-4 py-3 text-right font-medium text-muted-foreground">Montant</th>
                <th className="px-4 py-3 text-right font-medium text-muted-foreground">Balance</th>
                <th className="px-4 py-3 text-right font-medium text-muted-foreground">Date</th>
              </tr>
            </thead>
            <tbody className="divide-y">
              {[...bankroll.transactions].reverse().slice(0, 20).map((tx, i) => (
                <tr key={i} className="hover:bg-muted/25 transition-colors">
                  <td className="px-4 py-3">{tx.description}</td>
                  <td className={cn("px-4 py-3 text-right font-mono", tx.amount >= 0 ? "text-green-400" : "text-red-400")}>
                    <span className="flex items-center justify-end gap-1">
                      {tx.amount >= 0 ? <ArrowUpRight className="h-3 w-3" /> : <ArrowDownRight className="h-3 w-3" />}
                      {tx.amount >= 0 ? "+" : ""}${tx.amount.toFixed(2)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right font-mono text-muted-foreground">${tx.balanceAfter.toFixed(2)}</td>
                  <td className="px-4 py-3 text-right text-muted-foreground text-xs">
                    {new Date(tx.createdAt).toLocaleDateString("fr-FR")}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
