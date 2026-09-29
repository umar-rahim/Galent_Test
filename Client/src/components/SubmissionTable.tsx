import type { Submission } from "../types";
import { formatDate } from "../utils/formatDate";

export function SubmissionTable({ rows, onOpen, review = false }: { rows: Submission[]; onOpen: (id: string) => void; review?: boolean }) {
  return <div className="table-wrap"><table><thead><tr><th>Taxpayer</th><th>SSN</th><th>Status</th><th>Last updated</th><th><span className="sr-only">Actions</span></th></tr></thead><tbody>{rows.map(row => <tr key={row.id}><td><strong>{row.taxpayerName || "Untitled draft"}</strong><small className="table-id">#{row.id.slice(0, 8)}</small></td><td>{row.maskedSsn || "—"}</td><td><span className={`status-pill ${row.status.toLowerCase()}`}>{row.status}</span></td><td>{formatDate(row.updatedAtUtc)}</td><td><button className="table-action" onClick={() => onOpen(row.id)}>{review ? "Review" : "Open"} <span aria-hidden="true">→</span></button></td></tr>)}</tbody></table></div>;
}