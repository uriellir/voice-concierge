import { llm, voice } from "@livekit/agents";
import { z } from "zod";
import type { AgentConfig } from "./config.js";
import { LivekitConciergeApiClient } from "./livekitApiClient.js";

export class MeridianConciergeAgent extends voice.Agent {
  constructor(config: AgentConfig, apiClient: LivekitConciergeApiClient) {
    super({
      instructions: `
You are the Meridian Voice Concierge for The Meridian Casino & Resort.

You are speaking to hotel or casino guests through voice.
Your tone must be warm, polished, professional, and aligned with luxury hospitality.
Keep answers concise, natural for speech, and helpful.
Never invent policies, hours, amenities, availability, or recommendations that are not grounded in retrieved information.
For factual guest questions about the property, always use the searchKnowledge tool before answering.
If the tool indicates no confident answer exists, apologize gracefully, explain that the question has been noted for the team, and offer help with something else.
Do not claim to complete bookings, payments, or out-of-scope concierge actions.
      `.trim(),
      tools: {
        searchKnowledge: llm.tool({
          description: "Look up grounded Meridian property information and fallback handling for guest questions.",
          parameters: z.object({
            question: z.string().describe("The guest's question in plain English."),
          }),
          execute: async ({ question }) => {
            const result = await apiClient.ask(question, config.minimumConfidence, config.topK);

            if (!result.matchFound) {
              return [
                "No confident answer was found in the knowledge base.",
                `Fallback response: ${result.fallbackMessage}`,
                "Important: apologize gracefully, mention the question has been noted, and ask if you can help with anything else.",
              ].join("\n");
            }

            return [
              `Approved spoken response: ${result.spokenResponse}`,
              `Confidence label: ${result.confidenceLabel}`,
              "Use only these retrieved facts when answering.",
            ].join("\n");
          },
        }),
      },
    });
  }
}
