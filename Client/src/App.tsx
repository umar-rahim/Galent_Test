import { useCallback, useEffect, useState } from "react";
import "./styles.css";
import type { Submission, UserSession } from "./types";
import { api, expiryKey, sessionKey, tokenKey } from "./services/api";
import { AdminPage } from "./features/admin/AdminPage";
import { LoginPage } from "./features/auth/LoginPage";
import { DashboardPage } from "./features/dashboard/DashboardPage";
import { ReviewPage } from "./features/review/ReviewPage";
import { SubmissionPage } from "./features/submissions/SubmissionPage";
import { SubmissionsPage } from "./features/submissions/SubmissionsPage";

function App() {
  const [user, setUser] = useState<UserSession | null>(() => {
    const saved = sessionStorage.getItem(sessionKey);
    return saved ? JSON.parse(saved) as UserSession : null;
  });
  const [expiresAtUtc, setExpiresAtUtc] = useState(() => sessionStorage.getItem(expiryKey));
  const [page, setPage] = useState("dashboard");
  const [submissions, setSubmissions] = useState<Submission[]>([]);
  const [activeId, setActiveId] = useState<string | null>(null);
  const [notice, setNotice] = useState<{ kind: "success" | "error"; text: string } | null>(null);
  const [busy, setBusy] = useState(false);

  const canPrepare = user?.roles.includes("Preparer") ?? false;
  const canReview = user?.roles.includes("Reviewer") || user?.roles.includes("Admin") || false;
  const isAdmin = user?.roles.includes("Admin") ?? false;

  useEffect(() => {
    function handleUnauthorized() {
      setUser(null);
      setSubmissions([]);
      setActiveId(null);
      setPage("dashboard");
      setExpiresAtUtc(null);
    }
    window.addEventListener("galent:unauthorized", handleUnauthorized);
    return () => window.removeEventListener("galent:unauthorized", handleUnauthorized);
  }, []);

  useEffect(() => {
    if (!user || !expiresAtUtc) return;
    const millisecondsUntilExpiry = new Date(expiresAtUtc).getTime() - Date.now();
    if (millisecondsUntilExpiry <= 0) {
      window.dispatchEvent(new Event("galent:unauthorized"));
      return;
    }
    const timeout = window.setTimeout(() => window.dispatchEvent(new Event("galent:unauthorized")), millisecondsUntilExpiry);
    return () => window.clearTimeout(timeout);
  }, [user, expiresAtUtc]);

  const loadSubmissions = useCallback(async () => {
    if (!user) return;
    setBusy(true);
    try {
      setSubmissions(await api<Submission[]>("/api/submissions"));
    } catch (error) {
      setNotice({ kind: "error", text: (error as Error).message });
    } finally {
      setBusy(false);
    }
  }, [user]);

  useEffect(() => { void loadSubmissions(); }, [loadSubmissions]);

  async function login(email: string, password: string) {
    const result = await api<{ accessToken: string; expiresAtUtc: string; user: UserSession }>("/api/auth/login", {
      method: "POST", body: JSON.stringify({ email, password }),
    });
    sessionStorage.setItem(tokenKey, result.accessToken);
    sessionStorage.setItem(sessionKey, JSON.stringify(result.user));
    sessionStorage.setItem(expiryKey, result.expiresAtUtc);
    setExpiresAtUtc(result.expiresAtUtc);
    setUser(result.user);
    setPage("dashboard");
    setNotice({ kind: "success", text: "Signed in." });
  }

  async function logout() {
    try { await api("/api/auth/logout", { method: "POST" }); } catch { /* local logout must still complete if the token expired */ }
    sessionStorage.removeItem(tokenKey);
    sessionStorage.removeItem(sessionKey);
    sessionStorage.removeItem(expiryKey);
    setUser(null);
    setExpiresAtUtc(null);
    setSubmissions([]);
    setActiveId(null);
    setPage("dashboard");
  }

  async function createSubmission() {
    try {
      const result = await api<{ id: string }>("/api/submissions", { method: "POST" });
      setActiveId(result.id);
      setPage("form");
    } catch (error) { setNotice({ kind: "error", text: (error as Error).message }); }
  }

  function openSubmission(id: string, view: "form" | "details") {
    setActiveId(id);
    setPage(view);
    setNotice(null);
  }

  if (!user) return <LoginPage onLogin={login} />;

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <a className="brand" href="#home" onClick={(event) => { event.preventDefault(); setPage("dashboard"); }}>
          <span className="brand-mark">G</span><span>galent<span className="brand-light">.tax</span></span>
        </a>
        <div className="profile-card"><div className="avatar">{user.email.slice(0, 1).toUpperCase()}</div><div><strong>{user.email}</strong><small>{user.roles.join(" · ")}</small></div></div>
        <nav aria-label="Main navigation" className="nav-list">
          <button className={page === "dashboard" ? "nav-button active" : "nav-button"} onClick={() => setPage("dashboard")}>Overview</button>
          <button className={page === "submissions" ? "nav-button active" : "nav-button"} onClick={() => { setPage("submissions"); void loadSubmissions(); }}>Submissions</button>
          {canReview && <button className={page === "review" ? "nav-button active" : "nav-button"} onClick={() => setPage("review")}>Review queue</button>}
          {isAdmin && <button className={page === "admin" ? "nav-button active" : "nav-button"} onClick={() => setPage("admin")}>Team management</button>}
        </nav>
        <button className="logout-button" onClick={() => void logout()}>Sign out</button>
      </aside>

      <main className="main-content">
        <header className="topbar"><div><span className="eyebrow">TAX YEAR 2025</span><h1>{pageTitle(page)}</h1></div><div className="topbar-user">{user.email}</div></header>
        {notice && <div className={`notice ${notice.kind}`} role={notice.kind === "error" ? "alert" : "status"}><span>{notice.text}</span><button aria-label="Dismiss notification" onClick={() => setNotice(null)}>×</button></div>}
        {page === "dashboard" && <DashboardPage user={user} submissions={submissions} busy={busy} onCreate={() => void createSubmission()} onOpen={(id) => openSubmission(id, "details")} onAll={() => { setPage("submissions"); void loadSubmissions(); }} />}
        {page === "submissions" && <SubmissionsPage submissions={submissions} busy={busy} canPrepare={canPrepare} onCreate={() => void createSubmission()} onOpen={(id) => openSubmission(id, canPrepare ? "form" : "details")} />}
        {(page === "form" || page === "details") && activeId && <SubmissionPage id={activeId} editable={page === "form" && canPrepare} canReview={canReview} onSaved={async () => { await loadSubmissions(); setNotice({ kind: "success", text: "Draft saved." }); }} onValidated={(findings) => setNotice(findings.length ? { kind: "error", text: `${findings.length} validation finding(s) need attention.` } : { kind: "success", text: "Validation passed." })} onBack={() => { setPage("submissions"); void loadSubmissions(); }} />}
        {page === "review" && <ReviewPage submissions={submissions} busy={busy} onOpen={(id) => openSubmission(id, "details")} />}
        {page === "admin" && isAdmin && <AdminPage />}
      </main>
    </div>
  );
}

function pageTitle(page: string): string {
  switch (page) {
    case "form": return "Prepare Form 1040";
    case "details": return "Submission details";
    case "submissions": return "My submissions";
    case "review": return "Review queue";
    case "admin": return "Team management";
    default: return "Your workspace";
  }
}

export default App;