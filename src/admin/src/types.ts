export type AdminSectionId = "unanswered" | "faq" | "voice" | "playground";

export type VoiceOption = {
  id: string;
  name: string;
  description: string;
  isActive: boolean;
};

export type VoiceOptionsResponse = {
  activeVoiceId: string;
  activeVoiceName: string;
  items: VoiceOption[];
};

export type VoiceState = VoiceOptionsResponse;

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

export type FaqState = FaqsResponse;

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

export type DashboardState = UnansweredQuestionsResponse;

export type AdminSection = {
  id: AdminSectionId;
  label: string;
  eyebrow: string;
};
