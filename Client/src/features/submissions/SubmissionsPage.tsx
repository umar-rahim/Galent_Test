import type { Submission } from "../../types";
import { EmptyState } from "../../components/EmptyState";
import { SubmissionTable } from "../../components/SubmissionTable";

export function SubmissionsPage({ submissions, busy, canPrepare, onCreate, onOpen }: {
  submissions: Submission[]; busy: boolean; canPrepare: boolean; onCreate: () => void; onOpen: (id: string) => void;
}) {
  return <><div className="section-heading page-section-heading"><div><span className="eyebrow">RETURNS</span><h2>All submissions</h2></div>{canPrepare && <button className="primary-button" onClick={onCreate}>＋ New submission</button>}</div>{busy ? <p className="loading">Loading submissions…</p> : submissions.length ? <SubmissionTable rows={submissions} onOpen={onOpen} /> : <EmptyState title="No submissions found" detail="New drafts that you create will appear here." />}</>;
}