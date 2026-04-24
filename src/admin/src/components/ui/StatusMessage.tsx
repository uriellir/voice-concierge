type StatusMessageProps = {
  children: string;
  tone?: "default" | "error";
};

export function StatusMessage(props: StatusMessageProps) {
  const { children, tone = "default" } = props;
  const className = tone === "error" ? "status-card error" : "status-card";

  return <div className={className}>{children}</div>;
}
