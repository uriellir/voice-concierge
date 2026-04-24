import type { UnansweredQuestionsResponse } from "./types";

declare const __API_BASE_URL__: string;

const apiBaseUrl = __API_BASE_URL__;

export async function fetchUnansweredQuestions(): Promise<UnansweredQuestionsResponse> {
  const response = await fetch(`${apiBaseUrl}/api/admin/unanswered`);

  if (!response.ok) {
    throw new Error(`Failed to load unanswered questions: ${response.status}`);
  }

  return (await response.json()) as UnansweredQuestionsResponse;
}
