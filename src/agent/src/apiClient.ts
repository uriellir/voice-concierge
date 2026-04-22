import type { ConciergeAskRequest, ConciergeAskResponse } from "./types.js";

export class ConciergeApiClient {
  constructor(private readonly baseUrl: string) {}

  async askConcierge(request: ConciergeAskRequest): Promise<ConciergeAskResponse> {
    const response = await fetch(`${this.baseUrl}/api/concierge/ask`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        query: request.query,
        topK: request.topK,
        minimumConfidence: request.minimumConfidence,
        autoRecordUnansweredWhenNoMatch: request.autoRecordUnansweredWhenNoMatch ?? true,
      }),
    });

    if (!response.ok) {
      const body = await response.text();
      throw new Error(`Concierge API request failed with ${response.status}: ${body}`);
    }

    return (await response.json()) as ConciergeAskResponse;
  }
}
