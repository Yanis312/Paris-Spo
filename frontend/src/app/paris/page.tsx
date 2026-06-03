"use client";

import { useQuery } from "@apollo/client/react";
import { GET_TODAY_MATCHES, GET_BANKROLL } from "@/lib/graphql/queries";
import type { Match, Bankroll } from "@/lib/graphql/types";
import { MatchCard } from "@/components/match-card";
import { Zap } from "lucide-react";

export default function Paris() {
  const { data: matchesData, loading } = useQuery<{ todayMatches: Match[] }>(GET_TODAY_MATCHES);
  const { data: bankrollData } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);

  const matches = matchesData?.todayMatches ?? [];
  const bankroll = bankrollData?.bankroll;

  const valueBets = matches.filter(m =>
    m.aiAnalysis?.suggestions.some(s => s.isValueBet)
  );

  return (
    <div className="space-y-6">
      <h1 className="text-2xl font-bold">Paris du jour</h1>

      {valueBets.length > 0 && (
        <div>
          <h2 className="mb-3 flex items-center gap-2 text-lg font-semibold">
            <Zap className="h-5 w-5 text-green-400" />
            Value bets détectés
          </h2>
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {valueBets.map(match => (
              <MatchCard key={match.id} match={match} bankroll={bankroll} />
            ))}
          </div>
        </div>
      )}

      <div>
        <h2 className="mb-3 text-lg font-semibold">Tous les matchs</h2>
        {loading ? (
          <div className="text-muted-foreground">Chargement...</div>
        ) : matches.length === 0 ? (
          <div className="rounded-lg border border-dashed p-8 text-center text-muted-foreground">
            Sync les matchs depuis le Dashboard
          </div>
        ) : (
          <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
            {matches.map(match => (
              <MatchCard key={match.id} match={match} bankroll={bankroll} />
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
