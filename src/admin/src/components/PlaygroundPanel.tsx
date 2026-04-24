import { useEffect, useRef, useState } from "react";
import { askConcierge } from "../api";
import type { FaqState, VoiceState } from "../types";
import { StatCard } from "./ui/StatCard";
import { StatusMessage } from "./ui/StatusMessage";

type PlaygroundPanelProps = {
  voiceState: VoiceState;
  faqState: FaqState;
};

export function PlaygroundPanel(props: PlaygroundPanelProps) {
  const { voiceState, faqState } = props;
  const recognitionRef = useRef<BrowserSpeechRecognition | null>(null);
  const sessionActiveRef = useRef(false);
  const [sessionActive, setSessionActive] = useState(false);
  const [isListening, setIsListening] = useState(false);
  const [isSpeaking, setIsSpeaking] = useState(false);
  const [status, setStatus] = useState("Idle");
  const [error, setError] = useState<string | null>(null);
  const [transcript, setTranscript] = useState("");
  const [reply, setReply] = useState("");
  const [browserVoices, setBrowserVoices] = useState<SpeechSynthesisVoice[]>([]);

  const speechSupported =
    typeof window !== "undefined" &&
    ("SpeechRecognition" in window || "webkitSpeechRecognition" in window) &&
    "speechSynthesis" in window;

  useEffect(() => {
    if (typeof window === "undefined" || !("speechSynthesis" in window)) {
      return;
    }

    const loadVoices = () => setBrowserVoices(window.speechSynthesis.getVoices());
    loadVoices();
    window.speechSynthesis.addEventListener("voiceschanged", loadVoices);

    return () => {
      window.speechSynthesis.removeEventListener("voiceschanged", loadVoices);
    };
  }, []);

  useEffect(() => {
    return () => {
      sessionActiveRef.current = false;
      recognitionRef.current?.stop?.();
      if (typeof window !== "undefined" && "speechSynthesis" in window) {
        window.speechSynthesis.cancel();
      }
    };
  }, []);

  async function startConversation() {
    if (!speechSupported) {
      setError("This browser does not support the embedded voice test mode.");
      return;
    }

    setError(null);
    sessionActiveRef.current = true;
    setSessionActive(true);
    setStatus("Connecting test mode...");
    await beginListening();
  }

  function endConversation() {
    sessionActiveRef.current = false;
    setSessionActive(false);
    setIsListening(false);
    setIsSpeaking(false);
    setStatus("Conversation ended");
    recognitionRef.current?.stop?.();

    if (typeof window !== "undefined" && "speechSynthesis" in window) {
      window.speechSynthesis.cancel();
    }
  }

  async function beginListening() {
    if (!speechSupported || !sessionActiveRef.current) {
      return;
    }

    const RecognitionCtor = window.SpeechRecognition || window.webkitSpeechRecognition;
    if (!RecognitionCtor) {
      setError("Speech recognition is not available in this browser.");
      return;
    }

    recognitionRef.current?.stop?.();
    const recognition = new RecognitionCtor();
    recognition.lang = "en-US";
    recognition.continuous = false;
    recognition.interimResults = false;

    recognition.onstart = () => {
      setIsListening(true);
      setStatus("Listening...");
    };

    recognition.onerror = (event) => {
      setIsListening(false);
      if (event.error === "aborted") {
        return;
      }

      setError(`Microphone error: ${event.error ?? "unknown error"}`);
      setStatus("Microphone error");
    };

    recognition.onresult = async (event) => {
      const spokenText = event.results?.[0]?.[0]?.transcript?.trim() ?? "";
      setTranscript(spokenText);
      setIsListening(false);

      if (!spokenText) {
        setStatus("No speech detected");
        return;
      }

      try {
        setStatus("Contacting concierge...");
        const spokenReply = await askConcierge(spokenText);
        setReply(spokenReply);
        await speakReply(spokenReply);
      } catch (playgroundError) {
        const message = playgroundError instanceof Error ? playgroundError.message : "Unknown error";
        setError(message);
        setStatus("Request failed");
      }
    };

    recognition.onend = () => {
      setIsListening(false);
    };

    recognitionRef.current = recognition;
    recognition.start();
  }

  async function speakReply(text: string) {
    if (!("speechSynthesis" in window)) {
      return;
    }

    window.speechSynthesis.cancel();
    setIsSpeaking(true);
    setStatus(`Speaking as ${voiceState.activeVoiceName || "concierge"}...`);

    await new Promise<void>((resolve) => {
      const utterance = new SpeechSynthesisUtterance(text);
      utterance.voice = selectBrowserVoice(browserVoices, voiceState.activeVoiceId);

      utterance.onend = async () => {
        setIsSpeaking(false);
        if (sessionActiveRef.current) {
          setStatus("Ready for the next guest question");
          await beginListening();
        } else {
          setStatus("Idle");
        }
        resolve();
      };

      utterance.onerror = () => {
        setIsSpeaking(false);
        setStatus("Speech output failed");
        resolve();
      };

      window.speechSynthesis.speak(utterance);
    });
  }

  return (
    <div className="panel-body">
      <div className="stat-grid">
        <StatCard label="Mode" value="Test Mode" />
        <StatCard label="Current voice" value={voiceState.activeVoiceName || "Loading..."} />
      </div>

      <div className="playground-hero">
        <div>
          <p className="eyebrow">Embedded Test Mode</p>
          <h3>Admin-side voice conversation playground</h3>
          <p>
            This browser test mode uses the current FAQ knowledge base and the currently selected
            concierge voice setting. Start a session, ask a question by microphone, and the panel
            will listen, fetch the concierge response, speak it back, and wait for the next turn.
          </p>
        </div>
        <div className="playground-actions">
          <button
            type="button"
            className="voice-button"
            onClick={() => void startConversation()}
            disabled={sessionActive || !speechSupported}
          >
            Start conversation
          </button>
          <button
            type="button"
            className="voice-button muted"
            onClick={endConversation}
            disabled={!sessionActive}
          >
            End conversation
          </button>
        </div>
      </div>

      <div className="playground-meta">
        <span className={`status-dot ${sessionActive ? "online" : ""}`} />
        <span>{status}</span>
        <span className="meta-separator">|</span>
        <span>{faqState.totalFaqs} FAQ items available</span>
        <span className="meta-separator">|</span>
        <span>{isListening ? "Microphone open" : isSpeaking ? "Speaking" : "Standing by"}</span>
      </div>

      {!speechSupported ? (
        <StatusMessage tone="error">
          This browser does not support the embedded voice playground. Use a Chromium-based browser
          on `localhost` for microphone and speech synthesis support.
        </StatusMessage>
      ) : null}

      {error ? <StatusMessage tone="error">{error}</StatusMessage> : null}

      <div className="playground-grid">
        <article className="playground-card">
          <span className="faq-label">Guest transcript</span>
          <p>{transcript || "The last recognized guest question will appear here."}</p>
        </article>
        <article className="playground-card">
          <span className="faq-label">Concierge reply</span>
          <p>{reply || "The spoken concierge response will appear here."}</p>
        </article>
      </div>
    </div>
  );
}

function selectBrowserVoice(voices: SpeechSynthesisVoice[], activeVoiceId: string): SpeechSynthesisVoice | null {
  if (voices.length === 0) {
    return null;
  }

  const matchers: Record<string, (voice: SpeechSynthesisVoice) => boolean> = {
    james: (voice) => voice.lang.toLowerCase().includes("en-gb") || /daniel|george|arthur/i.test(voice.name),
    sofia: (voice) => /eu|euro|span|ital|fr|victoria/i.test(`${voice.name} ${voice.lang}`),
    marcus: (voice) => voice.lang.toLowerCase().includes("en-us") || /david|guy|tony/i.test(voice.name),
    elena: (voice) => voice.lang.toLowerCase().includes("en-us") || /samantha|aria|jenny|ava/i.test(voice.name),
  };

  const matcher = matchers[activeVoiceId];
  return voices.find((voice) => matcher?.(voice)) ?? voices[0];
}

type BrowserSpeechRecognitionConstructor = new () => BrowserSpeechRecognition;

type BrowserSpeechRecognition = {
  lang: string;
  continuous: boolean;
  interimResults: boolean;
  onstart: (() => void) | null;
  onend: (() => void) | null;
  onerror: ((event: { error?: string }) => void) | null;
  onresult: ((event: { results: ArrayLike<ArrayLike<{ transcript: string }>> }) => void) | null;
  start: () => void;
  stop?: () => void;
};

declare global {
  interface Window {
    SpeechRecognition?: BrowserSpeechRecognitionConstructor;
    webkitSpeechRecognition?: BrowserSpeechRecognitionConstructor;
  }
}
