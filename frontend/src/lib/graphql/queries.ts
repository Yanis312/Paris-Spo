import { gql } from "@apollo/client/core";

export const DAILY_SIMULATION = gql`
  query DailySimulation($dailyBudget: Float!) {
    dailySimulation(dailyBudget: $dailyBudget) {
      date
      dailyBudget
      totalMatches
      valueBetCount
      totalStaked
      expectedReturn
      expectedProfit
      actualReturn
      actualProfit
      hasResults
      bets {
        match
        competition
        pick
        odds
        probability
        valueEdge
        kellyFraction
        stake
        potentialReturn
        expectedValue
        result
        actualReturn
      }
    }
  }
`;

export const GET_TODAY_MATCHES = gql`
  query GetTodayMatches {
    todayMatches {
      id
      homeTeamName
      awayTeamName
      competitionName
      kickOff
      status
      odds {
        bookmaker
        homeWin
        draw
        awayWin
        over25
        under25
      }
      aiAnalysis {
        homeWinProbability
        drawProbability
        awayWinProbability
        confidenceScore
        forumSentiment
        injuryImpact
        analysisSummary
        suggestions {
          market
          description
          trueOdds
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

export const GET_BETS = gql`
  query GetBets {
    bets {
      id
      type
      status
      stake
      totalOdds
      potentialReturn
      actualReturn
      bookmaker
      wasAiSuggested
      placedAt
      settledAt
      selections {
        matchDescription
        market
        pick
        odds
        status
      }
    }
  }
`;

export const GET_BET_STATS = gql`
  query GetBetStats {
    betStats {
      totalBets
      won
      lost
      pending
      totalStaked
      totalReturned
      roi
      winRate
    }
  }
`;

export const GET_BANKROLL = gql`
  query GetBankroll {
    bankroll {
      id
      initialAmount
      currentAmount
      maxStakePercent
      kellyFraction
      roi
      maxRecommendedStake
      transactions {
        betId
        amount
        balanceAfter
        description
        createdAt
      }
    }
  }
`;
