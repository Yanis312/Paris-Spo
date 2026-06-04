import { gql } from "@apollo/client/core";

export const SYNC_TODAY_MATCHES = gql`
  mutation SyncTodayMatches {
    syncTodayMatches {
      id
      homeTeamName
      awayTeamName
      competitionName
      kickOff
      status
    }
  }
`;

export const ANALYZE_TODAY_MATCHES = gql`
  mutation AnalyzeTodayMatches {
    analyzeTodayMatches {
      id
      aiAnalysis {
        homeWinProbability
        drawProbability
        awayWinProbability
        confidenceScore
        suggestions {
          market
          description
          bookmakerOdds
          valueEdge
          kellyFraction
          isValueBet
          bookmaker
        }
      }
    }
  }
`;

export const PLACE_BET = gql`
  mutation PlaceBet($input: PlaceBetInput!) {
    placeBet(input: $input) {
      id
      type
      status
      stake
      totalOdds
      potentialReturn
    }
  }
`;

export const SETTLE_BET = gql`
  mutation SettleBet($betId: String!, $result: BetStatus!) {
    settleBet(betId: $betId, result: $result) {
      id
      status
      actualReturn
      settledAt
    }
  }
`;

export const INITIALIZE_BANKROLL = gql`
  mutation InitializeBankroll($amount: Float!) {
    initializeBankroll(amount: $amount) {
      id
      initialAmount
      currentAmount
    }
  }
`;
