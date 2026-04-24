import { useEffect, useState } from "react";
import { fetchFaqs, fetchUnansweredQuestions, fetchVoiceOptions, setActiveVoice } from "./api";
import type { AdminSectionId, FaqItem, UnansweredQuestion, VoiceOption } from "./types";

type DashboardState = {
  totalQuestions: number;
  totalMentions: number;
  items: UnansweredQuestion[];
};

type FaqState = {
  totalFaqs: number;
  totalCategories: number;
  items: FaqItem[];
};

type VoiceState = {
  activeVoiceId: string;
  activeVoiceName: string;
  items: VoiceOption[];
};

const sections: Array<{ id: AdminSectionId; label: string; eyebrow: string }> = [
  { id: "unanswered", label: "Unanswered Queue", eyebrow: "Guest Question Review" },
  { id: "faq", label: "FAQ Management", eyebrow: "Knowledge Base Review" },
  { id: "voice", label: "Voice Settings", eyebrow: "Concierge Voice Control" },
  { id: "playground", label: "Playground", eyebrow: "Coming next" },
];

const emptyState: DashboardState = {
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
  const [dashboard, setDashboard] = useState<DashboardState>(emptyState);
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
        <aside className="sidebar">
          <div className="sidebar-card">
            <p className="sidebar-title">Admin Navigation</p>
            <nav className="sidebar-nav" aria-label="Admin menu">
              {sections.map((section) => (
                <button
                  key={section.id}
                  type="button"
                  className={section.id === selectedSection ? "nav-item active" : "nav-item"}
                  onClick={() => setSelectedSection(section.id)}
                >
                  <span className="nav-item-eyebrow">{section.eyebrow}</span>
                  <span className="nav-item-label">{section.label}</span>
                </button>
              ))}
            </nav>
          </div>
        </aside>

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
          ) : (
            <PlaceholderPanel section={selectedSection} />
          )}
        </section>
      </main>
    </div>
  );
}

function VoiceSettingsPanel(props: {
  loading: boolean;
  error: string | null;
  voiceState: VoiceState;
  savingVoiceId: string | null;
  onSelectVoice: (voiceId: string) => Promise<void>;
}) {
  const { loading, error, voiceState, savingVoiceId, onSelectVoice } = props;

  return (
    <div className="panel-body">
      <div className="stat-grid">
        <article className="stat-card">
          <span className="stat-label">Current active voice</span>
          <strong>{voiceState.activeVoiceName || "Loading..."}</strong>
        </article>
        <article className="stat-card">
          <span className="stat-label">Available voice options</span>
          <strong>{voiceState.items.length}</strong>
        </article>
      </div>

      {loading ? <div className="status-card">Loading voice settings...</div> : null}
      {error ? <div className="status-card error">{error}</div> : null}

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

function FaqListPanel(props: { loading: boolean; error: string | null; faqState: FaqState }) {
  const { loading, error, faqState } = props;
  const [searchTerm, setSearchTerm] = useState("");
  const normalizedSearch = searchTerm.trim().toLowerCase();
  const filteredItems = faqState.items.filter((item) => {
    if (!normalizedSearch) {
      return true;
    }

    return [
      item.title,
      item.category,
      item.canonicalQuestion,
      item.answerText,
    ].some((value) => value.toLowerCase().includes(normalizedSearch));
  });

  return (
    <div className="panel-body">
      <div className="stat-grid">
        <article className="stat-card">
          <span className="stat-label">Published FAQ items</span>
          <strong>{faqState.totalFaqs}</strong>
        </article>
        <article className="stat-card">
          <span className="stat-label">Knowledge categories</span>
          <strong>{faqState.totalCategories}</strong>
        </article>
      </div>

      <label className="search-field">
        <span className="faq-label">Search FAQ items</span>
        <input
          type="search"
          value={searchTerm}
          onChange={(event) => setSearchTerm(event.target.value)}
          placeholder="Search by title, category, question, or answer"
        />
      </label>

      {loading ? <div className="status-card">Loading FAQ items...</div> : null}
      {error ? <div className="status-card error">{error}</div> : null}

      {!loading && !error ? (
        filteredItems.length > 0 ? (
          <div className="faq-grid">
            {filteredItems.map((item) => (
              <article className="faq-card" key={item.id}>
                <div className="faq-card-header">
                  <span className="faq-category">{item.category}</span>
                  <strong>{item.title}</strong>
                </div>
                <div className="faq-content">
                  <div>
                    <span className="faq-label">Canonical question</span>
                    <p>{item.canonicalQuestion}</p>
                  </div>
                  <div>
                    <span className="faq-label">Answer</span>
                    <p>{item.answerText}</p>
                  </div>
                </div>
              </article>
            ))}
          </div>
        ) : faqState.items.length > 0 ? (
          <div className="status-card">No FAQ items match your current search.</div>
        ) : (
          <div className="status-card">No FAQ items are available in the knowledge base yet.</div>
        )
      ) : null}
    </div>
  );
}

function UnansweredQueuePanel(props: {
  loading: boolean;
  error: string | null;
  dashboard: DashboardState;
}) {
  const { loading, error, dashboard } = props;

  return (
    <div className="panel-body">
      <div className="stat-grid">
        <article className="stat-card">
          <span className="stat-label">Unique unanswered questions</span>
          <strong>{dashboard.totalQuestions}</strong>
        </article>
        <article className="stat-card">
          <span className="stat-label">Total guest mentions</span>
          <strong>{dashboard.totalMentions}</strong>
        </article>
      </div>

      {loading ? <div className="status-card">Loading the unanswered queue...</div> : null}
      {error ? <div className="status-card error">{error}</div> : null}

      {!loading && !error ? (
        dashboard.items.length > 0 ? (
          <div className="queue-table-wrap">
            <table className="queue-table">
              <thead>
                <tr>
                  <th>Guest question</th>
                  <th>Frequency</th>
                  <th>First seen</th>
                  <th>Last seen</th>
                </tr>
              </thead>
              <tbody>
                {dashboard.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <div className="question-cell">
                        <strong>{item.originalText}</strong>
                        <span>{item.normalizedQuestion}</span>
                      </div>
                    </td>
                    <td>
                      <span className="count-pill">{item.count}</span>
                    </td>
                    <td>{formatDate(item.firstSeenAt)}</td>
                    <td>{formatDate(item.lastSeenAt)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        ) : (
          <div className="status-card">No unanswered questions have been captured yet.</div>
        )
      ) : null}
    </div>
  );
}

function PlaceholderPanel(props: { section: Exclude<AdminSectionId, "unanswered" | "faq" | "voice"> }) {
  const messages: Record<Exclude<AdminSectionId, "unanswered" | "faq" | "voice">, string> = {
    playground: "This panel is ready for the embedded admin-side concierge test experience.",
  };

  return (
    <div className="panel-body">
      <div className="status-card">{messages[props.section]}</div>
    </div>
  );
}

function formatDate(value: string): string {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
