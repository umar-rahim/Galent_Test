import type { Submission, UserSession } from "../../types";
import { EmptyState } from "../../components/EmptyState";
import { SubmissionTable } from "../../components/SubmissionTable";

export function DashboardPage({ user, submissions, busy, onCreate, onOpen, onAll }: {
  user: UserSession; submissions: Submission[]; busy: boolean; onCreate: () => void; onOpen: (id: string) => void; onAll: () => void;
}) {
  const drafts = submissions.filter(item => item.status === "Draft").length;
  const submitted = submissions.filter(item => item.status === "Submitted").length;
  return (
    <>
      <section className="welcome-banner"><div><span className="eyebrow">TAX YEAR 2025</span><h2>Good to see you, {user.email.split("@")[0]}</h2><p>Manage Form 1040 submissions, continue drafts, and review validation status.</p></div>{user.roles.includes("Preparer") && <button className="primary-button" onClick={onCreate}>＋ Start a 1040</button>}</section>
      <section className="stat-grid" aria-label="Submission summary"><Stat label="Total submissions" value={busy ? "—" : String(submissions.length)} hint="Across your workspace" /><Stat label="Drafts" value={String(drafts)} hint="Ready to continue" /><Stat label="Submitted" value={String(submitted)} hint="Completed returns" /></section>
      <div className="section-heading"><div><span className="eyebrow">RECENT ACTIVITY</span><h2>Recent submissions</h2></div><button className="text-button" onClick={onAll}>View all submissions →</button></div>
      {submissions.length === 0 ? <EmptyState title="No submissions yet" detail={user.roles.includes("Preparer") ? "Start a draft to begin preparing a 2025 return." : "Submissions will appear here when available."} /> : <SubmissionTable rows={submissions.slice(0, 5)} onOpen={onOpen} />}
      <div className="info-banner"><strong>PDF generation status</strong><span>PDF export is available after a return is submitted and passes validation.</span></div>
    </>
  );
}

function Stat({ label, value, hint }: { label: string; value: string; hint: string }) {
  return <article className="stat-card"><span>{label}</span><strong>{value}</strong><small>{hint}</small></article>;
}