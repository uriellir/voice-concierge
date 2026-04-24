import type { DashboardState } from "../types";
import { StatCard } from "./ui/StatCard";
import { StatusMessage } from "./ui/StatusMessage";

type UnansweredQueuePanelProps = {
  loading: boolean;
  error: string | null;
  dashboard: DashboardState;
};

export function UnansweredQueuePanel(props: UnansweredQueuePanelProps) {
  const { loading, error, dashboard } = props;

  return (
    <div className="panel-body">
      <div className="stat-grid">
        <StatCard label="Unique unanswered questions" value={dashboard.totalQuestions} />
        <StatCard label="Total guest mentions" value={dashboard.totalMentions} />
      </div>

      {loading ? <StatusMessage>Loading the unanswered queue...</StatusMessage> : null}
      {error ? <StatusMessage tone="error">{error}</StatusMessage> : null}

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
          <StatusMessage>No unanswered questions have been captured yet.</StatusMessage>
        )
      ) : null}
    </div>
  );
}

function formatDate(value: string) {
  return new Intl.DateTimeFormat(undefined, {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(new Date(value));
}
