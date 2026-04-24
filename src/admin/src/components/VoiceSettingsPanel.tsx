import type { VoiceState } from "../types";
import { StatCard } from "./ui/StatCard";
import { StatusMessage } from "./ui/StatusMessage";

type VoiceSettingsPanelProps = {
  loading: boolean;
  error: string | null;
  voiceState: VoiceState;
  savingVoiceId: string | null;
  onSelectVoice: (voiceId: string) => Promise<void>;
};

export function VoiceSettingsPanel(props: VoiceSettingsPanelProps) {
  const { loading, error, voiceState, savingVoiceId, onSelectVoice } = props;

  return (
    <div className="panel-body">
      <div className="stat-grid">
        <StatCard label="Current active voice" value={voiceState.activeVoiceName || "Loading..."} />
        <StatCard label="Available voice options" value={voiceState.items.length} />
      </div>

      {loading ? <StatusMessage>Loading voice settings...</StatusMessage> : null}
      {error ? <StatusMessage tone="error">{error}</StatusMessage> : null}

      {!loading && !error ? (
        <div className="voice-grid">
          {voiceState.items.map((voice) => {
            const isSaving = savingVoiceId === voice.id;

            return (
              <article className="voice-card" key={voice.id}>
                <div className="voice-card-header">
                  <div>
                    <span className="voice-name">{voice.name}</span>
                    <p>{voice.description}</p>
                  </div>
                  {voice.isActive ? <span className="active-badge">Current</span> : null}
                </div>

                <button
                  type="button"
                  className={voice.isActive ? "voice-button muted" : "voice-button"}
                  onClick={() => void onSelectVoice(voice.id)}
                  disabled={voice.isActive || isSaving}
                >
                  {voice.isActive ? "Active voice" : isSaving ? "Updating..." : "Set active"}
                </button>
              </article>
            );
          })}
        </div>
      ) : null}
    </div>
  );
}
