export function EmptyState({ title, detail }: { title: string; detail: string }) {
  return <section className="empty-state"><div className="empty-icon" aria-hidden="true">✓</div><h2>{title}</h2><p>{detail}</p></section>;
}