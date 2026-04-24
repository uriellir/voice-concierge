import { useState } from "react";
import type { FaqState } from "../types";
import { StatCard } from "./ui/StatCard";
import { StatusMessage } from "./ui/StatusMessage";

type FaqListPanelProps = {
  loading: boolean;
  error: string | null;
  faqState: FaqState;
};

export function FaqListPanel(props: FaqListPanelProps) {
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
        <StatCard label="Published FAQ items" value={faqState.totalFaqs} />
        <StatCard label="Knowledge categories" value={faqState.totalCategories} />
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

      {loading ? <StatusMessage>Loading FAQ items...</StatusMessage> : null}
      {error ? <StatusMessage tone="error">{error}</StatusMessage> : null}

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
          <StatusMessage>No FAQ items match your current search.</StatusMessage>
        ) : (
          <StatusMessage>No FAQ items are available in the knowledge base yet.</StatusMessage>
        )
      ) : null}
    </div>
  );
}
