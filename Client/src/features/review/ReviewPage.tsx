import type { Submission } from "../../types";
import { EmptyState } from "../../components/EmptyState";
import { SubmissionTable } from "../../components/SubmissionTable";

export function ReviewPage({ submissions, busy, onOpen }: { submissions: Submission[]; busy: boolean; onOpen: (id: string) => void }) {
  const pending = submissions.filter(item => item.status === "Submitted");
  return <><div className="section-heading page-section-heading"><div><span className="eyebrow">REVIEWER WORKSPACE</span><h2>Submitted returns</h2></div><span className="count-pill">{pending.length} pending</span></div>{busy ? <p className="loading">Loading review queue…</p> : pending.length ? <SubmissionTable rows={pending} onOpen={onOpen} review /> : <EmptyState title="Review queue is clear" detail="Submitted returns ready for review will appear here." />}</>;
}