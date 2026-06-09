"use client";

import { useQuery, useMutation } from "@apollo/client/react";
import { GET_UPCOMING_MATCHES, SYNC_WORLD_CUP, GET_BANKROLL } from "@/lib/graphql/queries";
import { ANALYZE_TODAY_MATCHES } from "@/lib/graphql/mutations";
import type { Match, Bankroll } from "@/lib/graphql/types";
import { MatchCard } from "@/components/match-card";
import { RefreshCw, Trophy, Calendar } from "lucide-react";

export default function Calendrier() {
  const { data, loading, refetch } = useQuery<{ upcomingMatches: Match[] }>(GET_UPCOMING_MATCHES);
  const { data: bankrollData } = useQuery<{ bankroll: Bankroll }>(GET_BANKROLL);
  const [syncWC, { loading: syncing }] = useMutation(SYNC_WORLD_CUP, { onCompleted: () => refetch() });

  const matches = data?.upcomingMatches ?? [];
  const bankroll = bankrollData?.bankroll;

  // groupe par jour
  const byDay = matches.reduce<Record<string, Match[]>>((acc, m) => {
    const day = new Date(m.kickOff).toLocaleDateString("fr-FR", { weekday: "long", day: "numeric", month: "long" });
    (acc[day] ??= []).push(m);
    return acc;
  }, {});

  return (
    <div className="space-y-8">
      <div className="flex flex-col sm:flex-row sm:items-end justify-between gap-4">
        <div className="animate-fade-in-up">
          <h1 className="text-3xl md:text-4xl font-bold tracking-tight flex items-center gap-2">
            <Trophy className="h-7 w-7" style={{ color: "var(--amber)" }} />
            Coupe du Monde 2026
          </h1>
          <p className="text-muted-foreground text-sm mt-1.5">
            {matches.length} matchs à venir · {Object.keys(byDay).length} jours
          </p>
        </div>
        <button onClick={() => syncWC()} disabled={syncing}
          className="flex items-center gap-2 rounded-xl px-4 py-2.5 text-sm font-semibold text-white transition-all hover:scale-[1.02] disabled:opacity-60"
          style={{ background: "linear-gradient(120deg, var(--grad-1), var(--grad-2))" }}>
          <RefreshCw className={`h-4 w-4 ${syncing ? "animate-spin" : ""}`} />
          {syncing ? "Chargement..." : "Charger calendrier WC"}
        </button>
      </div>

      {loading ? (
        <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">{[...Array(6)].map((_,i)=><div key={i} className="skeleton h-56"/>)}</div>
      ) : matches.length === 0 ? (
        <div className="card-base p-12 text-center">
          <Calendar className="h-10 w-10 text-muted-foreground mx-auto mb-3" />
          <p className="font-semibold mb-1">Aucun match chargé</p>
          <p className="text-muted-foreground text-sm">Clique sur &ldquo;Charger calendrier WC&rdquo; pour les 72 matchs</p>
        </div>
      ) : (
        <div className="space-y-8">
          {Object.entries(byDay).map(([day, dayMatches]) => (
            <section key={day} className="animate-fade-in-up">
              <div className="flex items-center gap-3 mb-4 sticky top-14 lg:top-0 bg-background/80 backdrop-blur-sm py-2 z-10">
                <span className="flex h-8 w-8 items-center justify-center rounded-lg shrink-0"
                  style={{ background: "color-mix(in srgb, var(--amber) 14%, transparent)", color: "var(--amber)" }}>
                  <Calendar className="h-4 w-4" />
                </span>
                <h2 className="text-lg font-bold capitalize">{day}</h2>
                <span className="text-sm text-muted-foreground">{dayMatches.length} match{dayMatches.length>1?"s":""}</span>
              </div>
              <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-3">
                {dayMatches.map(m => <MatchCard key={m.id} match={m} bankroll={bankroll} />)}
              </div>
            </section>
          ))}
        </div>
      )}
    </div>
  );
}
