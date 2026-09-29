import { useState, type FormEvent } from "react";

export function LoginPage({ onLogin }: { onLogin: (email: string, password: string) => Promise<void> }) {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [error, setError] = useState("");
  const [busy, setBusy] = useState(false);

  async function submit(event: FormEvent) {
    event.preventDefault(); setError(""); setBusy(true);
    try { await onLogin(email, password); } catch (reason) { setError((reason as Error).message); }
    finally { setBusy(false); }
  }

  return (
    <main className="login-screen">
      <section className="login-card">
        <a className="brand login-brand" href="#"><span className="brand-mark">G</span><span>galent<span className="brand-light">.tax</span></span></a>
        <span className="eyebrow">SECURE TAX WORKSPACE</span><h1>Welcome back</h1><p className="muted">Sign in to continue to your 2025 Form 1040 workspace.</p>
        <form onSubmit={(event) => void submit(event)} className="stack-form">
          <label>Email address<input type="email" autoComplete="username" required maxLength={254} value={email} onChange={(event) => setEmail(event.target.value)} /></label>
          <label>Password<input type="password" autoComplete="current-password" required maxLength={128} value={password} onChange={(event) => setPassword(event.target.value)} /></label>
          {error && <p className="field-error" role="alert">{error}</p>}
          <button className="primary-button full-width" disabled={busy}>{busy ? "Signing in…" : "Sign in"}</button>
        </form>
        <p className="security-note">Access is managed by your Galent administrator.</p>
      </section>
    </main>
  );
}