import { useEffect, useState } from "react";
import { fetchFaqs, fetchUnansweredQuestions, fetchVoiceOptions, setActiveVoice } from "./api";
import { AdminSidebar } from "./components/AdminSidebar";
import { FaqListPanel } from "./components/FaqListPanel";
import { PlaygroundPanel } from "./components/PlaygroundPanel";
import { UnansweredQueuePanel } from "./components/UnansweredQueuePanel";
import { VoiceSettingsPanel } from "./components/VoiceSettingsPanel";
import type { AdminSection, AdminSectionId, DashboardState, FaqState, VoiceState } from "./types";

const sections: AdminSection[] = [
  { id: "unanswered", label: "Unanswered Queue", eyebrow: "Guest Question Review" },
  { id: "faq", label: "FAQ Management", eyebrow: "Knowledge Base Review" },
  { id: "voice", label: "Voice Settings", eyebrow: "Concierge Voice Control" },
  { id: "playground", label: "Playground", eyebrow: "Embedded Test Mode" },
];

const emptyDashboardState: DashboardState = {
  totalQuestions: 0,
  totalMentions: 0,
  items: [],
};

const emptyFaqState: FaqState = {
  totalFaqs: 0,
  totalCategories: 0,
  items: [],
};

const emptyVoiceState: VoiceState = {
  activeVoiceId: "",
  activeVoiceName: "",
  items: [],
};

export default function App() {
  const [selectedSection, setSelectedSection] = useState<AdminSectionId>("unanswered");
  const [dashboard, setDashboard] = useState<DashboardState>(emptyDashboardState);
  const [faqState, setFaqState] = useState<FaqState>(emptyFaqState);
  const [voiceState, setVoiceState] = useState<VoiceState>(emptyVoiceState);
  const [loadingUnanswered, setLoadingUnanswered] = useState(true);
  const [loadingFaqs, setLoadingFaqs] = useState(true);
  const [loadingVoices, setLoadingVoices] = useState(true);
  const [savingVoiceId, setSavingVoiceId] = useState<string | null>(null);
  const [unansweredError, setUnansweredError] = useState<string | null>(null);
  const [faqError, setFaqError] = useState<string | null>(null);
  const [voiceError, setVoiceError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    async function loadUnanswered() {
      try {
        setLoadingUnanswered(true);
        const data = await fetchUnansweredQuestions();

        if (!active) {
          return;
        }

        setDashboard(data);
        setUnansweredError(null);
      } catch (loadError) {
        if (!active) {
          return;
        }

        const message = loadError instanceof Error ? loadError.message : "Unknown error";
        setUnansweredError(message);
      } finally {
        if (active) {
          setLoadingUnanswered(false);
        }
      }
    }

    async function loadFaqs() {
      try {
        setLoadingFaqs(true);
        const data = await fetchFaqs();

        if (!active) {
          return;
        }

        setFaqState(data);
        setFaqError(null);
      } catch (loadError) {
        if (!active) {
          return;
        }

        const message = loadError instanceof Error ? loadError.message : "Unknown error";
        setFaqError(message);
      } finally {
        if (active) {
          setLoadingFaqs(false);
        }
      }
    }

    async function loadVoices() {
      try {
        setLoadingVoices(true);
        const data = await fetchVoiceOptions();

        if (!active) {
          return;
        }

        setVoiceState(data);
        setVoiceError(null);
      } catch (loadError) {
        if (!active) {
          return;
        }

        const message = loadError instanceof Error ? loadError.message : "Unknown error";
        setVoiceError(message);
      } finally {
        if (active) {
          setLoadingVoices(false);
        }
      }
    }

    void loadUnanswered();
    void loadFaqs();
    void loadVoices();

    return () => {
      active = false;
    };
  }, []);

  const selectedMeta = sections.find((section) => section.id === selectedSection)!;

  return (
    <div className="app-shell">
      <header className="page-header">
        <div>
          <p className="eyebrow">The Meridian Casino & Resort</p>
          <h1>Voice Concierge Admin Panel</h1>
        </div>
        <p className="header-copy">
          Review operational signals, curate FAQ coverage, and keep the concierge experience
          polished for guests around the clock.
        </p>
      </header>

      <main className="workspace">
        <AdminSidebar
          sections={sections}
          selectedSection={selectedSection}
          onSelectSection={setSelectedSection}
        />

        <section className="content-panel">
          <div className="panel-header">
            <div>
              <p className="eyebrow">{selectedMeta.eyebrow}</p>
              <h2>{selectedMeta.label}</h2>
            </div>
          </div>

          {selectedSection === "unanswered" ? (
            <UnansweredQueuePanel
              loading={loadingUnanswered}
              error={unansweredError}
              dashboard={dashboard}
            />
          ) : selectedSection === "faq" ? (
            <FaqListPanel loading={loadingFaqs} error={faqError} faqState={faqState} />
          ) : selectedSection === "voice" ? (
            <VoiceSettingsPanel
              loading={loadingVoices}
              error={voiceError}
              voiceState={voiceState}
              savingVoiceId={savingVoiceId}
              onSelectVoice={async (voiceId) => {
                try {
                  setSavingVoiceId(voiceId);
                  const updated = await setActiveVoice(voiceId);
                  setVoiceState(updated);
                  setVoiceError(null);
                } catch (updateError) {
                  const message = updateError instanceof Error ? updateError.message : "Unknown error";
                  setVoiceError(message);
                } finally {
                  setSavingVoiceId(null);
                }
              }}
            />
          ) : selectedSection === "playground" ? (
            <PlaygroundPanel voiceState={voiceState} faqState={faqState} />
          ) : (
            <div className="panel-body">
              <div className="status-card">This module is not available yet.</div>
            </div>
          )}
        </section>
      </main>
    </div>
  );
}
