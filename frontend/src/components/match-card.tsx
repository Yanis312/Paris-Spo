"use client";

import { useMutation } from "@apollo/client/react";
import { PLACE_BET } from "@/lib/graphql/mutations";
import { GET_BETS, GET_BANKROLL } from "@/lib/graphql/queries";
import type { Match, Bankroll, BetSuggestion } from "@/lib/graphql/types";
import { cn } from "@/lib/utils";
import { Zap, Clock, CheckCircle2, Sparkles } from "lucide-react";

interface MatchCardProps {
  match: Match;
  bankroll?: Bankroll | null;
}

function competitionColor(name: string): string {
  const n = name.toLowerCase();
  if (n.includes("world cup") || n.includes("coupe du monde")) return "var(--amber)";
  if (n.includes("champions")) return "var(--blue)";
  if (n.includes("amical") || n.includes("friendly")) return "var(--purple)";
  return "var(--green)";
}

export function MatchCard({ match, bankroll }: MatchCardProps) {
  const [placeBet, { loading }] = useMutation(PLACE_BET, {
    refetchQueries: [{ query: GET_BETS }, { query: GET_BANKROLL }],
  });

  const bestOdds = match.odds[0];
  const analysis = match.aiAnalysis;
  const ko = new Date(match.kickOff);
  const kickOff = ko.toLocaleTimeString("fr-FR", { hour: "2-digit", minute: "2-digit" });
  const kickOffDate = ko.toLocaleDateString("fr-FR", { day: "2-digit", month: "short" });
  const valueBets = analysis?.suggestions.filter(s => s.isValueBet) ?? [];
  const compColor = competitionColor(match.competitionName);
  const isLive = match.status === "LIVE";

  function handlePlaceBet(suggestion: BetSuggestion) {
    const pct = bankroll ? suggestion.kellyFraction * bankroll.kellyFraction : 0.02;
    const raw = bankroll ? bankroll.currentAmount * pct : 10;
    const stake = Math.max(1, Math.min(Math.round(raw * 100) / 100, bankroll?.maxRecommendedStake ?? 100));
    placeBet({
      variables: {
        input: {
          selections: [{
            matchId: match.id,
            matchDescription: `${match.homeTeamName} vs ${match.awayTeamName}`,
            market: suggestion.market,
            pick: suggestion.description,
            odds: suggestion.bookmakerOdds,
            status: "PENDING",
          }],
          stake, bookmaker: suggestion.bookmaker, wasAiSuggested: true,
        },
      },
    });
  }

  return (
    <div className={cn(
      "card-base card-hover relative flex flex-col overflow-hidden",
      valueBets.length > 0 && "value-glow"
    )}>
      {/* top accent line */}
      <div className="h-1 w-full" style={{ background: `linear-gradient(90deg, ${compColor}, transparent)` }} />

      {/* Header */}
      <div className="flex items-center justify-between px-4 pt-3.5 pb-1">
        <span
          className="inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-[10px] font-semibold uppercase tracking-wide"
          style={{ background: `color-mix(in srgb, ${compColor} 12%, transparent)`, color: compColor }}
        >
          {match.competitionName}
        </span>
        {isLive ? (
          <span className="flex items-center gap-1.5 text-xs font-semibold text-red-500">
            <span className="h-1.5 w-1.5 rounded-full bg-red-500" style={{ animation: "livePulse 1.2s infinite" }} />
            LIVE
          </span>
        ) : (
          <span className="flex items-center gap-1.5 text-xs text-muted-foreground">
            <Clock className="h-3 w-3" />
            <span className="font-medium text-foreground">{kickOffDate}</span>
            {kickOff}
          </span>
        )}
      </div>

      {/* Teams */}
      <div className="px-4 py-3">
        <div className="flex items-center justify-between gap-3">
          <span className="font-bold text-base flex-1 truncate">{match.homeTeamName}</span>
          <span className="text-[10px] font-bold text-muted-foreground px-2 py-1 rounded-md bg-secondary shrink-0">VS</span>
          <span className="font-bold text-base flex-1 text-right truncate">{match.awayTeamName}</span>
        </div>
      </div>

      {/* Odds */}
      {bestOdds ? (
        <div className="px-4 pb-3">
          <div className="grid grid-cols-3 gap-2">
            {[
              { label: "1", sub: match.homeTeamName.slice(0, 3).toUpperCase(), value: bestOdds.homeWin, c: "var(--blue)" },
              { label: "X", sub: "NUL", value: bestOdds.draw, c: "var(--amber)" },
              { label: "2", sub: match.awayTeamName.slice(0, 3).toUpperCase(), value: bestOdds.awayWin, c: "var(--red)" },
            ].map(({ label, sub, value, c }) => (
              <button
                key={label}
                className="group flex flex-col items-center gap-0.5 rounded-xl border border-border bg-secondary/40 px-2 py-2.5 transition-all hover:border-[color:var(--bc)] hover:bg-[color:color-mix(in_srgb,var(--bc)_10%,transparent)]"
                style={{ ["--bc" as string]: c }}
              >
                <span className="text-[9px] font-semibold uppercase tracking-wider text-muted-foreground group-hover:text-foreground">{sub}</span>
                <span className="text-base font-bold font-mono" style={{ color: c }}>{value.toFixed(2)}</span>
              </button>
            ))}
          </div>
          <p className="text-[10px] text-muted-foreground mt-2 text-center">
            via {bestOdds.bookmaker}
            {bestOdds.over25 ? ` · O/U 2.5 : ${bestOdds.over25.toFixed(2)} / ${bestOdds.under25?.toFixed(2)}` : ""}
          </p>
        </div>
      ) : (
        <div className="px-4 pb-3 text-xs text-muted-foreground text-center">Cotes non disponibles</div>
      )}

      {/* AI Analysis */}
      {analysis ? (
        <div className="mt-auto border-t border-border px-4 py-3 space-y-3 bg-secondary/20">
          <div className="flex items-center justify-between">
            <span className="flex items-center gap-1.5 text-[11px] font-semibold text-muted-foreground uppercase tracking-wide">
              <Sparkles className="h-3 w-3" style={{ color: "var(--primary)" }} />
              Analyse IA
            </span>
            <span className="text-[11px] font-semibold" style={{ color: analysis.confidenceScore >= 65 ? "var(--green)" : "var(--amber)" }}>
              {analysis.confidenceScore.toFixed(0)}% confiance
            </span>
          </div>

          {/* prob bar */}
          <div>
            <div className="flex gap-0.5 h-2 rounded-full overflow-hidden">
              <div style={{ width: `${analysis.homeWinProbability*100}%`, background: "var(--blue)" }} className="transition-all duration-700" />
              <div style={{ width: `${analysis.drawProbability*100}%`, background: "var(--amber)" }} className="transition-all duration-700" />
              <div style={{ width: `${analysis.awayWinProbability*100}%`, background: "var(--red)" }} className="transition-all duration-700" />
            </div>
            <div className="flex justify-between text-[10px] font-semibold mt-1">
              <span style={{ color: "var(--blue)" }}>{(analysis.homeWinProbability*100).toFixed(0)}%</span>
              <span style={{ color: "var(--amber)" }}>{(analysis.drawProbability*100).toFixed(0)}%</span>
              <span style={{ color: "var(--red)" }}>{(analysis.awayWinProbability*100).toFixed(0)}%</span>
            </div>
          </div>

          {valueBets.length > 0 ? valueBets.map((s, i) => (
            <div key={i} className="flex items-center justify-between rounded-xl px-3 py-2.5 gap-3"
              style={{ background: "color-mix(in srgb, var(--green) 10%, transparent)", border: "1px solid color-mix(in srgb, var(--green) 30%, transparent)" }}>
              <div className="min-w-0">
                <div className="flex items-center gap-1">
                  <Zap className="h-3 w-3 shrink-0" style={{ color: "var(--green)" }} />
                  <span className="text-[11px] font-bold" style={{ color: "var(--green)" }}>VALUE +{s.valueEdge.toFixed(1)}%</span>
                </div>
                <p className="text-xs text-muted-foreground truncate mt-0.5">
                  {s.description} <span className="font-mono font-bold text-foreground">@{s.bookmakerOdds.toFixed(2)}</span>
                </p>
              </div>
              <button onClick={() => handlePlaceBet(s)} disabled={loading}
                className="shrink-0 flex items-center gap-1 rounded-lg px-3 py-2 text-xs font-bold transition-all hover:scale-105 disabled:opacity-50"
                style={{ background: "var(--green)", color: "#03110a" }}>
                <CheckCircle2 className="h-3.5 w-3.5" />Parier
              </button>
            </div>
          )) : (
            <p className="text-[11px] text-muted-foreground italic">Pas de value bet — cotes équilibrées</p>
          )}
        </div>
      ) : (
        <div className="mt-auto border-t border-border px-4 py-2.5 bg-secondary/10">
          <span className="text-[11px] text-muted-foreground">Analyse IA non lancée</span>
        </div>
      )}
    </div>
  );
}
