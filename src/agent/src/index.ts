import { createInterface } from "node:readline/promises";
import { stdin as input, stdout as output } from "node:process";
import { ConciergeApiClient } from "./apiClient.js";
import { ConciergeAgent } from "./conciergeAgent.js";
import { loadConfig } from "./config.js";
import { conciergeSystemPrompt } from "./prompts.js";

async function main(): Promise<void> {
  const config = loadConfig();
  const apiClient = new ConciergeApiClient(config.apiBaseUrl);
  const agent = new ConciergeAgent(apiClient, config);

  console.log(`${config.agentName} scaffold is running.`);
  console.log(`Backend API: ${config.apiBaseUrl}`);
  console.log("System prompt loaded:");
  console.log(conciergeSystemPrompt);
  console.log("");
  console.log("Type a guest question below. Press Enter on an empty line to exit.");

  const rl = createInterface({ input, output });

  try {
    while (true) {
      const question = (await rl.question("> ")).trim();

      if (!question) {
        break;
      }

      const reply = await agent.handleGuestQuestion(question);
      console.log("");
      console.log(`Concierge: ${reply.spokenReply}`);
      console.log(`Match found: ${reply.matchFound}`);
      console.log(`Confidence: ${reply.confidenceLabel}`);

      if (reply.debug?.bestTitle) {
        console.log(`Best match: ${reply.debug.bestCategory} / ${reply.debug.bestTitle}`);
      }

      console.log("");
    }
  } finally {
    rl.close();
  }
}

main().catch((error: unknown) => {
  console.error("Meridian voice concierge scaffold failed to start.");
  console.error(error);
  process.exitCode = 1;
});
