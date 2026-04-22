export type ConciergeAskRequest = {
  query: string;
  topK?: number;
  minimumConfidence?: number;
  autoRecordUnansweredWhenNoMatch?: boolean;
};

export type KnowledgeSearchCandidate = {
  knowledgeItemId: string;
  title: string;
  category: string;
  answerText: string;
  factsJson?: string | null;
  combinedScore: number;
  keywordScore: number;
  vectorScore: number;
  matchedTokens: string[];
  meaningfulMatchedTokens: string[];
};

export type ConciergeAskResponse = {
  matchFound: boolean;
  query: string;
  minimumConfidence: number;
  answer?: string | null;
  spokenResponse: string;
  confidence?: number | null;
  confidenceLabel: "high" | "medium" | "low" | "none" | string;
  shouldRecordUnanswered: boolean;
  fallbackMessage: string;
  unansweredQuestionId?: string | null;
  unansweredQuestionCount?: number | null;
  bestMatch?: KnowledgeSearchCandidate | null;
  candidates: KnowledgeSearchCandidate[];
  message: string;
};

export type AgentReply = {
  transcript: string;
  spokenReply: string;
  matchFound: boolean;
  confidenceLabel: string;
  shouldEscalateForReview: boolean;
  debug?: {
    backendMessage: string;
    bestCategory?: string;
    bestTitle?: string;
    score?: number;
    composer?: string;
  };
};
