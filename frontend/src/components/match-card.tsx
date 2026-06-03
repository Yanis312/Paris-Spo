"use client";

import { useMutation } from "@apollo/client/react";
import { PLACE_BET } from "@/lib/graphql/mutations";
import { GET_BETS, GET_BANKROLL } from "@/lib/graphql/queries";
import type { Match, Bankroll, BetSuggestion } from "@/lib/graphql/types";
import { cn } from "@/lib/utils";
import { Zap, Clock, CheckCircle } from "lucide-react";

interface MatchCardProps {
  match: Match;
  bankroll?: Bankroll | null;
}

const STATUS_LABELS: Record<string, string> = {
  SCHEDULED: "Programmé",
  LIVE: "En direct",
  FINISHED: "Terminé",
  POSTPONED: "Reporté",
  CANCELLED: "Annulé",
};

export function MatchCard({ match, bankroll }: MatchCardProps) {
  const [placeBet, { loading }] = useMutation(PLACE_BET, {
    refetchQueries: [{ query: GET_BETS }, { query: GET_BANKROLL }],
  });

  const bestOdds = match.odds[0];
  const analysis = match.aiAnalysis;
  const kickOff = new Date(match.kickOff).toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" });

  function handlePlaceBet(suggestion: BetSuggestion) {
    const stake = bankroll
      ? Math.min(bankroll.maxRecommendedStake, bankroll.currentAmount * suggestion.kellyFraction * bankroll.kellyFraction)
      : 10;

    placeBet({
      variables: {
        input: {
          selections: [{
            matchDescription: `${match.homeTeamName} vs ${match.awayTeamName}`,
            market: suggestion.market,
            pick: suggestion.description,
            odds: suggestion.bookmakerOdds,
            status: "PENDING",
          }],
          stake: Math.max(1, Math.round(stake * 100) / 100),
          bookmaker: suggestion.bookmaker,
          wasAiSuggested: true,
        },
      },
    });
  }

  return (
    <div className="rounded-lg border bg-card text-card-foreground flex flex-col">
      {/* Header */}
      <div className="flex items-center justify-between p-4 pb-2">
        <span className="text-xs text-muted-foreground">{match.competitionName}</span>
        <div className="flex items-center gap-1 text-xs text-muted-foreground">
          {match.status === "LIVE" ? (
            <span className="flex items-center gap-1 text-green-500 font-medium">
              <span className="h-2 w-2 rounded-full bg-green-500 animate-pulse" />
              En direct
            </span>
          ) : match.status === "SCHEDULED" ? (
            <span className="flex items-center gap-1"><Clock className="h-3 w-3" />{kickOff}</span>
          ) : (
            <span>{STATUS_LABELS[match.status] ?? match.status}</span>
          )}
        </div>
      </div>

      {/* Équipes + cotes */}
      <div className="px-4 pb-3">
        <div className="flex items-center justify-between gap-2">
          <span className="font-semibold text-sm flex-1">{match.homeTeamName}</span>
          {bestOdds && (
            <div className="flex gap-1 text-xs">
              <span className="rounded bg-muted px-2 py-1 font-mono">{bestOdds.homeWin.toFixed(2)}</span>
              <span className="rounded bg-muted px-2 py-1 font-mono">{bestOdds.draw.toFixed(2)}</span>
              <span className="rounded bg-muted px-2 py-1 font-mono">{bestOdds.awayWin.toFixed(2)}</span>
            </div>
          )}
          <span className="font-semibold text-sm flex-1 text-right">{match.awayTeamName}</span>
        </div>
        {bestOdds && (
          <div className="mt-1 text-center text-xs text-muted-foreground">
            {bestOdds.bookmaker} · 1X2
          </div>
        )}
      </div>

      {/* Analyse IA */}
      {analysis && (
        <div className="border-t px-4 py-3 space-y-2">
          {/* Probabilités */}
          <div className="flex gap-1 h-2 rounded-full overflow-hidden">
            <div className="bg-blue-500" style={{ width: `${analysis.homeWinProbability * 100}%` }} />
            <div className="bg-yellow-500" style={{ width: `${analysis.drawProbability * 100}%` }} />
            <div className="bg-red-500" style={{ width: `${analysis.awayWinProbability * 100}%` }} />
          </div>
          <div className="flex justify-between text-xs text-muted-foreground">
            <span className="text-blue-400">{(analysis.homeWinProbability * 100).toFixed(0)}%</span>
            <span className="text-yellow-400">{(analysis.drawProbability * 100).toFixed(0)}%</span>
            <span className="text-red-400">{(analysis.awayWinProbability * 100).toFixed(0)}%</span>
          </div>

          {/* Confiance */}
          <div className="flex items-center gap-2">
            <span className="text-xs text-muted-foreground">Confiance</span>
            <div className="flex-1 h-1.5 rounded-full bg-muted">
              <div
                className={cn("h-full rounded-full", analysis.confidenceScore >= 70 ? "bg-green-500" : analysis.confidenceScore >= 50 ? "bg-yellow-500" : "bg-red-500")}
                style={{ width: `${analysis.confidenceScore}%` }}
              />
            </div>
            <span className="text-xs font-medium">{analysis.confidenceScore.toFixed(0)}%</span>
          </div>

          {/* Suggestions value bet */}
          {analysis.suggestions.filter(s => s.isValueBet).map((s, i) => (
            <div key={i} className="flex items-center justify-between rounded-md bg-green-950/30 border border-green-900/50 px-3 py-2">
              <div>
                <div className="flex items-center gap-1">
                  <Zap className="h-3 w-3 text-green-400" />
                  <span className="text-xs font-medium text-green-400">Value +{s.valueEdge.toFixed(1)}%</span>
                </div>
                <p className="text-xs text-muted-foreground mt-0.5">{s.description} @ {s.bookmakerOdds.toFixed(2)}</p>
              </div>
              <button
                onClick={() => handlePlaceBet(s)}
                disabled={loading}
                className="flex items-center gap-1 rounded-md bg-green-600 hover:bg-green-700 px-2 py-1 text-xs font-medium text-white transition-colors disabled:opacity-50"
              >
                <CheckCircle className="h-3 w-3" />
                Parier
              </button>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
