type ChatMessage = {
  role: "system" | "user";
  content: string;
};

export class OpenAiLlmClient {
  constructor(
    private readonly apiKey: string,
    private readonly baseUrl: string,
    private readonly model: string,
  ) {}

  get isConfigured(): boolean {
    return this.apiKey.trim().length > 0;
  }

  async generate(messages: ChatMessage[]): Promise<string> {
    const response = await fetch(`${this.baseUrl}/chat/completions`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Authorization: `Bearer ${this.apiKey}`,
      },
      body: JSON.stringify({
        model: this.model,
        temperature: 0.3,
        messages,
      }),
    });

    if (!response.ok) {
      const body = await response.text();
      throw new Error(`LLM request failed with ${response.status}: ${body}`);
    }

    const payload = (await response.json()) as {
      choices?: Array<{ message?: { content?: string } }>;
    };

    return payload.choices?.[0]?.message?.content?.trim() ?? "";
  }
}
