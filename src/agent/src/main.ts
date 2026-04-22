import {
  type JobContext,
  type JobProcess,
  ServerOptions,
  cli,
  defineAgent,
  inference,
  voice,
} from "@livekit/agents";
import * as livekit from "@livekit/agents-plugin-livekit";
import * as silero from "@livekit/agents-plugin-silero";
import { BackgroundVoiceCancellation } from "@livekit/noise-cancellation-node";
import dotenv from "dotenv";
import { fileURLToPath } from "node:url";
import { loadConfig } from "./config.js";
import { MeridianConciergeAgent } from "./livekitAgent.js";
import { LivekitConciergeApiClient } from "./livekitApiClient.js";

dotenv.config({ path: "../../.env" });
dotenv.config({ path: "../.env" });
dotenv.config({ path: ".env" });

const config = loadConfig();
const apiClient = new LivekitConciergeApiClient(config.apiBaseUrl);

export default defineAgent({
  prewarm: async (proc: JobProcess) => {
    proc.userData.vad = await silero.VAD.load();
  },
  entry: async (ctx: JobContext) => {
    const session = new voice.AgentSession({
      stt: new inference.STT({
        model: "deepgram/nova-3",
        language: "multi",
      }),
      llm: new inference.LLM({
        model: "openai/gpt-4o-mini",
      }),
      tts: new inference.TTS({
        model: "cartesia/sonic-3",
        voice: "9626c31c-bec5-4cca-baa8-f8ba9e84c8bc",
      }),
      turnDetection: new livekit.turnDetector.MultilingualModel(),
      vad: ctx.proc.userData.vad! as silero.VAD,
      voiceOptions: {
        preemptiveGeneration: true,
      },
    });

    await session.start({
      room: ctx.room,
      agent: new MeridianConciergeAgent(config, apiClient),
      inputOptions: {
        audioEnabled: true,
        textEnabled: true,
        noiseCancellation: BackgroundVoiceCancellation(),
      },
    });

    await ctx.connect();
    session.generateReply({
      instructions: "Greet the guest warmly, introduce yourself as the Meridian Voice Concierge, and offer assistance.",
    });
  },
});

cli.runApp(
  new ServerOptions({
    agent: fileURLToPath(import.meta.url),
    agentName: "meridian-concierge",
  }),
);
