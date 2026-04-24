import type { FaqsResponse, UnansweredQuestionsResponse, VoiceOptionsResponse } from "./types";

declare const __API_BASE_URL__: string;

const apiBaseUrl = __API_BASE_URL__;

export async function fetchUnansweredQuestions(): Promise<UnansweredQuestionsResponse> {
  const response = await fetch(`${apiBaseUrl}/api/admin/unanswered`);

  if (!response.ok) {
    throw new Error(`Failed to load unanswered questions: ${response.status}`);
  }

  return (await response.json()) as UnansweredQuestionsResponse;
}

export async function fetchFaqs(): Promise<FaqsResponse> {
  const response = await fetch(`${apiBaseUrl}/api/admin/faqs`);

  if (!response.ok) {
    throw new Error(`Failed to load FAQs: ${response.status}`);
  }

  return (await response.json()) as FaqsResponse;
}

export async function fetchVoiceOptions(): Promise<VoiceOptionsResponse> {
  const response = await fetch(`${apiBaseUrl}/api/admin/voice-options`);

  if (!response.ok) {
    throw new Error(`Failed to load voice options: ${response.status}`);
  }

  return (await response.json()) as VoiceOptionsResponse;
}

export async function setActiveVoice(voiceId: string): Promise<VoiceOptionsResponse> {
  const response = await fetch(`${apiBaseUrl}/api/admin/voice-options/active`, {
    method: "PUT",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ voiceId }),
  });

  if (!response.ok) {
    throw new Error(`Failed to update active voice: ${response.status}`);
  }

  return (await response.json()) as VoiceOptionsResponse;
}

export async function askConcierge(query: string): Promise<string> {
  const response = await fetch(`${apiBaseUrl}/api/concierge/ask`, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: JSON.stringify({ query }),
  });

  if (!response.ok) {
    throw new Error(`Failed to ask concierge: ${response.status}`);
  }

  const payload = (await response.json()) as { spokenResponse?: string; fallbackMessage?: string };
  return payload.spokenResponse || payload.fallbackMessage || "I'm sorry, but I couldn't generate a spoken response.";
}
