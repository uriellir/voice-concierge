import { loadProjectEnv } from "./env.js";

export type AgentConfig = {
  apiBaseUrl: string;
  topK: number;
  minimumConfidence: number;
  agentName: string;
  llmApiKey: string;
  llmBaseUrl: string;
  llmModel: string;
};

export function loadConfig(): AgentConfig {
  loadProjectEnv();

  return {
    apiBaseUrl: process.env.API_BASE_URL ?? "http://localhost:5282",
    topK: Number.parseInt(process.env.CONCIERGE_TOP_K ?? "5", 10),
    minimumConfidence: Number.parseFloat(process.env.CONCIERGE_MIN_CONFIDENCE ?? "0.55"),
    agentName: process.env.CONCIERGE_AGENT_NAME ?? "Meridian Voice Concierge",
    llmApiKey: process.env.LLM_API_KEY ?? "",
    llmBaseUrl: process.env.LLM_BASE_URL ?? "https://api.openai.com/v1",
    llmModel: process.env.LLM_MODEL ?? "gpt-4.1-mini",
  };
}
