import { useEffect, useMemo, useState } from "react";
import type { Finding, FindingResponse, FormData, StoredDocument } from "../../types";
import { api, apiUrl, expiryKey, sessionKey, tokenKey } from "../../services/api";
import { calculate, filingStatuses, formatSsn } from "../form1040/form1040";
import { FormEditor, ReadOnlyForm } from "../form1040/FormEditor";
import { EmptyState } from "../../components/EmptyState";
import { formatDate } from "../../utils/formatDate";

export function SubmissionPage({ id, editable, canReview, onSaved, onValidated, onBack }: {
  id: string; editable: boolean; canReview: boolean; onSaved: () => Promise<void>; onValidated: (findings: Finding[]) => void; onBack: () => void;
}) {
  const [form, setForm] = useState<FormData | null>(null);
  const [findings, setFindings] = useState<Finding[]>([]);
  const [status, setStatus] = useState("Draft");
  const [busy, setBusy] = useState(true);
  const [error, setError] = useState("");
  const [message, setMessage] = useState("");
  const [documents, setDocuments] = useState<StoredDocument[]>([]);
  const [pdfBusy, setPdfBusy] = useState(false);

  useEffect(() => {
    let mounted = true;
    setBusy(true); setError("");
    api<{ status: string; form: FormData; findings: Finding[] }>(`/api/submissions/${id}`)
      .then(result => { if (mounted) { setStatus(result.status); setForm(calculate(result.form)); setFindings(result.findings ?? []); } })
      .catch(reason => { if (mounted) setError((reason as Error).message); })
      .finally(() => { if (mounted) setBusy(false); });
    api<StoredDocument[]>(`/api/submissions/${id}/documents`)
      .then(result => { if (mounted) setDocuments(result); })
      .catch(reason => { if (mounted) setError((reason as Error).message); });
    return () => { mounted = false; };
  }, [id]);

  const calculated = useMemo(() => form ? calculate(form) : null, [form]);
  const update = (name: string, value: unknown) => {
    const nextValue = name === "taxpayerSsn" && typeof value === "string" ? formatSsn(value) : value;
    const nextForm = form ? calculate({ ...form, [name]: nextValue }) : null;
    setForm(nextForm);
    if (!nextForm) return;
    setFindings(current => current.filter(finding => {
      if (finding.field === "taxpayer.name")
        return !nextForm.taxpayerFirstName.trim() || !nextForm.taxpayerLastName.trim();
      if (finding.field === "taxpayer.ssn")
        return !/^\d{3}-\d{2}-\d{4}$/.test(nextForm.taxpayerSsn);
      if (finding.field === "filingStatus")
        return !filingStatuses.some(([statusValue]) => statusValue === nextForm.filingStatus);
      return true;
    }));
  };
  const errorsFor = (field: string) => findings.filter(item => item.field === field || item.field.startsWith(`${field}.`));

  async function saveDraft() {
    if (!form) return;
    setMessage("");
    try {
      const result = await api<{ form: FormData; status: string }>(`/api/submissions/${id}`, { method: "PUT", body: JSON.stringify({ form: calculated }) });
      setForm(calculate(result.form)); setStatus(result.status); setFindings([]); setMessage("Draft saved."); await onSaved();
    } catch (reason) { setError((reason as Error).message); }
  }

  async function downloadDocument(storedDocument: StoredDocument) {
    setError("");
    try {
      const token = sessionStorage.getItem(tokenKey);
      const response = await fetch(apiUrl(`/api/submissions/${id}/documents/${storedDocument.id}`), {
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (response.status === 401) {
        sessionStorage.removeItem(tokenKey);
        sessionStorage.removeItem(sessionKey);
        sessionStorage.removeItem(expiryKey);
        window.dispatchEvent(new Event("galent:unauthorized"));
        throw new Error("Your session expired. Sign in again to download this PDF.");
      }
      if (!response.ok) throw new Error(`PDF download failed (${response.status}).`);
      const objectUrl = URL.createObjectURL(await response.blob());
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download = `form-1040-${id.slice(0, 8)}.pdf`;
      anchor.click();
      URL.revokeObjectURL(objectUrl);
    } catch (reason) {
      setError((reason as Error).message);
    }
  }

  async function generateDocument() {
    if (!form || !calculated) return;
    setError(""); setMessage(""); setPdfBusy(true);
    try {
      if (editable && status === "Draft") {
        const saved = await api<{ form: FormData; status: string }>(`/api/submissions/${id}`, { method: "PUT", body: JSON.stringify({ form: calculated }) });
        setForm(calculate(saved.form)); setStatus(saved.status);
      }
      const token = sessionStorage.getItem(tokenKey);
      const response = await fetch(apiUrl(`/api/submissions/${id}/documents`), {
        method: "POST",
        headers: token ? { Authorization: `Bearer ${token}` } : {},
      });
      if (response.status === 401) {
        sessionStorage.removeItem(tokenKey);
        sessionStorage.removeItem(sessionKey);
        sessionStorage.removeItem(expiryKey);
        window.dispatchEvent(new Event("galent:unauthorized"));
        throw new Error("Your session expired. Sign in again to generate this PDF.");
      }
      if (response.status === 422) {
        const body = await response.json().catch(() => null);
        if (body?.findings) {
          setFindings(body.findings);
          onValidated(body.findings);
          return;
        }
      }
      if (!response.ok) throw new Error(`PDF generation failed (${response.status}).`);
      const objectUrl = URL.createObjectURL(await response.blob());
      const anchor = document.createElement("a");
      anchor.href = objectUrl;
      anchor.download = `form-1040-2025-${id.slice(0, 8)}.pdf`;
      anchor.click();
      URL.revokeObjectURL(objectUrl);
      setDocuments(await api<StoredDocument[]>(`/api/submissions/${id}/documents`));
      setMessage("PDF generated and saved locally.");
    } catch (reason) {
      setError((reason as Error).message);
    } finally {
      setPdfBusy(false);
    }
  }

  async function validate() {
    if (!form) return;
    setError("");
    try {
      if (editable && status === "Draft" && calculated) {
        const saved = await api<{ form: FormData; status: string }>(`/api/submissions/${id}`, { method: "PUT", body: JSON.stringify({ form: calculated }) });
        setForm(calculate(saved.form)); setStatus(saved.status);
      }
      const result = await api<FindingResponse>(`/api/submissions/${id}/validate`, { method: "POST" });
      setFindings(result.findings); onValidated(result.findings);
    } catch (reason) { setError((reason as Error).message); }
  }

  async function submitReturn() {
    if (!form || !calculated) return;
    setError("");
    try {
      const saved = await api<{ form: FormData; status: string }>(`/api/submissions/${id}`, { method: "PUT", body: JSON.stringify({ form: calculated }) });
      setForm(calculate(saved.form)); setStatus(saved.status);
      await api(`/api/submissions/${id}/submit`, { method: "POST" });
      setStatus("Submitted"); setMessage("Return submitted for review.");
    } catch (reason) {
      const failure = reason as Error & { body?: { findings?: Finding[] } };
      if (failure.body?.findings) setFindings(failure.body.findings);
      else setError(failure.message);
    }
  }

  if (busy) return <p className="loading">Loading return…</p>;
  if (error && !form) return <div className="error-panel" role="alert">{error}<button className="secondary-button" onClick={onBack}>Back to submissions</button></div>;
  if (!form || !calculated) return <EmptyState title="Return unavailable" detail="The submission could not be loaded." />;

  return <>
    <div className="form-toolbar"><button className="back-link" onClick={onBack}>← Submissions</button><div className="form-toolbar-meta"><span className={`status-pill ${status.toLowerCase()}`}>{status}</span><span className="submission-ref">#{id.slice(0, 8)}</span></div></div>
    {error && <div className="notice error" role="alert">{error}</div>}{message && <div className="notice success" role="status">{message}</div>}
    {findings.length > 0 && <section className="finding-summary" aria-live="polite"><h2>{findings.length} validation finding{findings.length === 1 ? "" : "s"}</h2><ul>{findings.map((item, index) => <li key={`${item.code}-${index}`}><strong>{item.field}</strong> — {item.message}</li>)}</ul></section>}
    {status === "Submitted" && documents.length > 0 && <section className="info-banner"><strong>Generated PDFs</strong>{documents.map(document => <button className="table-action" key={document.id} onClick={() => void downloadDocument(document)}>Download PDF · {formatDate(document.createdAtUtc)}</button>)}</section>}
    {!editable && <div className="info-banner"><strong>Read-only review</strong><span>Taxpayer and dependent SSNs are masked for reviewers.</span></div>}
    {editable ? <FormEditor form={calculated} update={update} errorsFor={errorsFor} /> : <ReadOnlyForm form={form} />}
    <div className="sticky-actions"><button className="secondary-button" onClick={onBack}>Back</button>{editable && status === "Draft" && <><button className="secondary-button" onClick={() => void validate()}>Validate</button><button className="secondary-button" onClick={() => void saveDraft()}>Save draft</button><button className="primary-button" onClick={() => void submitReturn()}>Submit return</button></>}{!editable && canReview && <button className="primary-button" onClick={() => void validate()}>Validate submission</button>}{status === "Submitted" && <button className="secondary-button" onClick={() => void generateDocument()} disabled={pdfBusy}>{pdfBusy ? "Generating PDF…" : "Generate PDF"}</button>}</div>
  </>;
}