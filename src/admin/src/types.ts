export type AdminSectionId = "unanswered" | "faq" | "voice" | "playground";

export type UnansweredQuestion = {
  id: string;
  originalText: string;
  normalizedQuestion: string;
  count: number;
  firstSeenAt: string;
  lastSeenAt: string;
};

export type UnansweredQuestionsResponse = {
  totalQuestions: number;
  totalMentions: number;
  items: UnansweredQuestion[];
};
