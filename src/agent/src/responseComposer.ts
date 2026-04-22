import { buildAnswerInstructions, conciergeSystemPrompt } from "./prompts.js";
import type { ConciergeAskResponse, KnowledgeSearchCandidate } from "./types.js";
import { OpenAiLlmClient } from "./llmClient.js";

export class ResponseComposer {
  constructor(private readonly llmClient: OpenAiLlmClient) {}

  async composeReply(question: string, result: ConciergeAskResponse): Promise<{ text: string; mode: string }> {
    if (!result.matchFound) {
      return {
        text: result.fallbackMessage || "I'm sorry, but I don't have a reliable answer for that right now. I've noted the question for our team to review.",
        mode: "fallback",
      };
    }

    if (this.llmClient.isConfigured) {
      const candidateContext = this.buildCandidateContext(result.candidates);
      const prompt = buildAnswerInstructions(question, candidateContext, result.fallbackMessage);
      const generated = await this.llmClient.generate([
        { role: "system", content: conciergeSystemPrompt },
        { role: "user", content: prompt },
      ]);

      if (generated) {
        return {
          text: generated,
          mode: "llm",
        };
      }
    }

    return {
      text: this.composeTemplateReply(question, result),
      mode: "template",
    };
  }

  private buildCandidateContext(candidates: KnowledgeSearchCandidate[]): string {
    const topCandidates = candidates.slice(0, 3);
    return topCandidates
        .map((candidate: KnowledgeSearchCandidate, index) => {
        const cleanedAnswer = cleanAnswer(candidate.answerText);
        return [
          `Candidate ${index + 1}`,
          `Category: ${candidate.category}`,
          `Title: ${candidate.title}`,
          `Answer: ${cleanedAnswer}`,
        ].join("\n");
      })
      .join("\n\n");
  }

  private composeTemplateReply(question: string, result: ConciergeAskResponse): string {
    const bestMatch = result.bestMatch;
    if (!bestMatch) {
      return result.fallbackMessage;
    }

    const loweredQuestion = question.toLowerCase();
    const topCandidates = result.candidates.slice(0, 3);

    if (isRecommendationIntent(loweredQuestion) && topCandidates.length > 1) {
      const lead = topCandidates[0];
      const alternates = topCandidates.slice(1, 3);
      const leadText = `${lead.title} is one of our strongest options. ${cleanAnswer(lead.answerText)}`;
      const alternateText = alternates
        .map((candidate) => `${candidate.title} is also worth considering. ${cleanAnswer(candidate.answerText)}`)
        .join(" ");

      return `Absolutely. ${leadText} ${alternateText}`.trim();
    }

    return `Certainly. ${cleanAnswer(bestMatch.answerText)}`.trim();
  }
}

function cleanAnswer(answerText: string): string {
  return answerText
    .replace(/Sub-title:\s*/gi, "")
    .replace(/Details:\s*/gi, "")
    .replace(/\r/g, "")
    .replace(/\n+/g, ". ")
    .replace(/\s+/g, " ")
    .trim();
}

function isRecommendationIntent(question: string): boolean {
  return [
    "best",
    "recommend",
    "nearby",
    "good restaurant",
    "where should",
    "what restaurant",
    "dining",
  ].some((token) => question.includes(token));
}
