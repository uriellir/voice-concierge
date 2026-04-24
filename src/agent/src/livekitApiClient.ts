type ConciergeAskRequest = {
  query: string;
  topK?: number;
  minimumConfidence?: number;
  autoRecordUnansweredWhenNoMatch?: boolean;
};

type ConciergeAskResponse = {
  matchFound: boolean;
  spokenResponse: string;
  answer?: string | null;
  confidenceLabel: string;
  fallbackMessage: string;
  shouldRecordUnanswered: boolean;
  message: string;
};

type AgentVoiceResponse = {
  voiceId: string;
  voiceName: string;
  providerVoiceId: string;
};

export class LivekitConciergeApiClient {
  constructor(private readonly baseUrl: string) {}

  async ask(query: string, minimumConfidence: number, topK: number): Promise<ConciergeAskResponse> {
    const response = await fetch(`${this.baseUrl}/api/concierge/ask`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
      },
      body: JSON.stringify({
        query,
        minimumConfidence,
        topK,
        autoRecordUnansweredWhenNoMatch: true,
      } satisfies ConciergeAskRequest),
    });

    if (!response.ok) {
      const body = await response.text();
      throw new Error(`Concierge API request failed with ${response.status}: ${body}`);
    }

    return (await response.json()) as ConciergeAskResponse;
  }

  async getActiveVoice(): Promise<AgentVoiceResponse> {
    const response = await fetch(`${this.baseUrl}/api/agent/voice`);

    if (!response.ok) {
      const body = await response.text();
      throw new Error(`Voice configuration request failed with ${response.status}: ${body}`);
    }

    return (await response.json()) as AgentVoiceResponse;
  }
}
