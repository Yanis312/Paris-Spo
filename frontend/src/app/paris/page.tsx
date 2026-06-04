"use client";

import { useQuery } from "@apollo/client/react";
import { GET_TODAY_MATCHES, GET_BANKROLL } from "@/lib/graphql/queries";
import type { Match, Bankroll } from "@/lib/graphql/types";
import { MatchCard } from "@/components/match-card";
import { Zap, Trophy } from "lucide-react";

export default function Paris() {
  const { data: matchesData, loading } = useQuery<{ todayMatches: Match[] }>(GET_TODAY_MATCHES);
  const { data: bankrollData } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);

  const matches = matchesData?.todayMatches ?? [];
  const bankroll = bankrollData?.bankroll;
  const valueBets = matches.filter(m => m.aiAnalysis?.suggestions.some(s => s.isValueBet));

  return (
    <div className="space-y-8">
      <div className="animate-fade-in-up">
        <h1 className="text-3xl md:text-4xl font-bold tracking-tight">Paris du jour</h1>
        <p className="text-muted-foreground text-sm mt-1.5">Suggestions IA et value bets détectés</p>
      </div>

      {valueBets.length > 0 && (
        <section>
          <h2 className="text-xl font-bold tracking-tight mb-4 flex items-center gap-2">
            <Zap className="h-5 w-5" style={{ color: "var(--green)" }} />
            Value bets
            <span className="text-sm font-normal text-muted-foreground">({valueBets.length})</span>
          </h2>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3 stagger">
            {valueBets.map(m => <MatchCard key={m.id} match={m} bankroll={bankroll} />)}
          </div>
        </section>
      )}

      <section>
        <h2 className="text-xl font-bold tracking-tight mb-4 flex items-center gap-2">
          <Trophy className="h-5 w-5" style={{ color: "var(--primary)" }} />
          Tous les matchs
        </h2>
        {loading ? (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {[...Array(3)].map((_, i) => <div key={i} className="skeleton h-64" />)}
          </div>
        ) : matches.length === 0 ? (
          <div className="card-base p-12 text-center text-muted-foreground text-sm">
            Sync les matchs depuis le Dashboard
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3 stagger">
            {matches.map(m => <MatchCard key={m.id} match={m} bankroll={bankroll} />)}
          </div>
        )}
      </section>
    </div>
  );
}
