export type MatchStatus = "SCHEDULED" | "LIVE" | "FINISHED" | "POSTPONED" | "CANCELLED";
export type BetStatus = "PENDING" | "WON" | "LOST" | "VOID" | "CASH_OUT";
export type BetType = "SINGLE" | "COMBO" | "SYSTEM";
export type MarketType = "MATCH_WINNER" | "BOTH_TEAMS_SCORE" | "OVER_UNDER" | "CORRECT_SCORE";

export interface MatchOdds {
  bookmaker: string;
  homeWin: number;
  draw: number;
  awayWin: number;
  over25?: number;
  under25?: number;
  bttsYes?: number;
}

export interface BetSuggestion {
  market: MarketType;
  description: string;
  trueOdds: number;
  bookmakerOdds: number;
  valueEdge: number;
  kellyFraction: number;
  isValueBet: boolean;
  bookmaker: string;
}

export interface AiAnalysis {
  homeWinProbability: number;
  drawProbability: number;
  awayWinProbability: number;
  confidenceScore: number;
  forumSentiment: string;
  injuryImpact: string;
  analysisSummary: string;
  suggestions: BetSuggestion[];
  generatedAt: string;
}

export interface Match {
  id: string;
  homeTeamName: string;
  awayTeamName: string;
  competitionName: string;
  kickOff: string;
  status: MatchStatus;
  odds: MatchOdds[];
  aiAnalysis?: AiAnalysis;
}

export interface BetSelection {
  matchDescription: string;
  market: MarketType;
  pick: string;
  odds: number;
  status: BetStatus;
}

export interface Bet {
  id: string;
  type: BetType;
  status: BetStatus;
  stake: number;
  totalOdds: number;
  potentialReturn: number;
  actualReturn?: number;
  bookmaker?: string;
  wasAiSuggested: boolean;
  placedAt: string;
  settledAt?: string;
  selections: BetSelection[];
}

export interface BetStats {
  totalBets: number;
  won: number;
  lost: number;
  pending: number;
  totalStaked: number;
  totalReturned: number;
  roi: number;
  winRate: number;
}

export interface BankrollTransaction {
  betId?: string;
  amount: number;
  balanceAfter: number;
  description: string;
  createdAt: string;
}

export interface Bankroll {
  id: string;
  initialAmount: number;
  currentAmount: number;
  maxStakePercent: number;
  kellyFraction: number;
  roi: number;
  maxRecommendedStake: number;
  transactions: BankrollTransaction[];
}
