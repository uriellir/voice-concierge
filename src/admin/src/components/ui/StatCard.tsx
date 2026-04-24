import type { ReactNode } from "react";

type StatCardProps = {
  label: string;
  value: ReactNode;
};

export function StatCard(props: StatCardProps) {
  const { label, value } = props;

  return (
    <article className="stat-card">
      <span className="stat-label">{label}</span>
      <strong>{value}</strong>
    </article>
  );
}
