import { ConciergeApiClient } from "./apiClient.js";
import type { AgentConfig } from "./config.js";
import { OpenAiLlmClient } from "./llmClient.js";
import { ResponseComposer } from "./responseComposer.js";
import type { AgentReply } from "./types.js";

export class ConciergeAgent {
  private readonly responseComposer: ResponseComposer;

  constructor(
    private readonly apiClient: ConciergeApiClient,
    private readonly config: AgentConfig,
  ) {
    this.responseComposer = new ResponseComposer(
      new OpenAiLlmClient(config.llmApiKey, config.llmBaseUrl, config.llmModel),
    );
  }

  async handleGuestQuestion(question: string): Promise<AgentReply> {
    const result = await this.apiClient.askConcierge({
      query: question,
      topK: this.config.topK,
      minimumConfidence: this.config.minimumConfidence,
      autoRecordUnansweredWhenNoMatch: true,
    });

    const composedReply = await this.responseComposer.composeReply(question, result);

    return {
      transcript: question,
      spokenReply: composedReply.text,
      matchFound: result.matchFound,
      confidenceLabel: result.confidenceLabel,
      shouldEscalateForReview: !result.matchFound,
      debug: {
        backendMessage: result.message,
        bestCategory: result.bestMatch?.category,
        bestTitle: result.bestMatch?.title,
        score: result.bestMatch?.combinedScore,
        composer: composedReply.mode,
      },
    };
  }
}
