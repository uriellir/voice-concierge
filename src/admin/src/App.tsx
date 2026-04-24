import { useEffect, useState } from "react";
import { fetchUnansweredQuestions } from "./api";
import type { AdminSectionId, UnansweredQuestion } from "./types";

type DashboardState = {
  totalQuestions: number;
  totalMentions: number;
  items: UnansweredQuestion[];
};

const sections: Array<{ id: AdminSectionId; label: string; eyebrow: string }> = [
  { id: "unanswered", label: "Unanswered Queue", eyebrow: "Guest Question Review" },
  { id: "faq", label: "FAQ Management", eyebrow: "Coming next" },
  { id: "voice", label: "Voice Settings", eyebrow: "Coming next" },
  { id: "playground", label: "Playground", eyebrow: "Coming next" },
];

const emptyState: DashboardState = {
  totalQuestions: 0,
  totalMentions: 0,
  items: [],
};

export default function App() {
  const [selectedSection, setSelectedSection] = useState<AdminSectionId>("unanswered");
  const [dashboard, setDashboard] = useState<DashboardState>(emptyState);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let active = true;

    async function load() {
      try {
        setLoading(true);
        const data = await fetchUnansweredQuestions();

        if (!active) {
          return;
        }

        setDashboard(data);
        setError(null);
      } catch (loadError) {
        if (!active) {
          return;
        }

        const message = loadError instanceof Error ? loadError.message : "Unknown error";
        setError(message);
      } finally {
        if (active) {
          setLoading(false);
        }
      }
    }

    void load();

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
            <UnansweredQueuePanel loading={loading} error={error} dashboard={dashboard} />
          ) : (
            <PlaceholderPanel section={selectedSection} />
          )}
        </section>
      </main>
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

function PlaceholderPanel(props: { section: Exclude<AdminSectionId, "unanswered"> }) {
  const messages: Record<Exclude<AdminSectionId, "unanswered">, string> = {
    faq: "This area is reserved for FAQ CRUD next, so the admin shell stays stable as the bonus scope grows.",
    voice: "This panel will host the four concierge voice options and active-voice selection.",
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
