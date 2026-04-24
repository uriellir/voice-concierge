export type AdminSectionId = "unanswered" | "faq" | "voice" | "playground";

export type FaqItem = {
  id: string;
  title: string;
  category: string;
  canonicalQuestion: string;
  answerText: string;
  createdAt: string;
  updatedAt: string;
};

export type FaqsResponse = {
  totalFaqs: number;
  totalCategories: number;
  items: FaqItem[];
};

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
