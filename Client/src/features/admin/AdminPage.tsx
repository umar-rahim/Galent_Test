import { useCallback, useEffect, useState, type FormEvent } from "react";
import type { AdminUser, Role } from "../../types";
import { api, roles } from "../../services/api";
import { TextField } from "../../components/FormFields";

export function AdminPage() {
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [role, setRole] = useState<Role>("Preparer");
  const [notice, setNotice] = useState("");
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const load = useCallback(async () => { setBusy(true); try { setUsers(await api<AdminUser[]>("/api/admin/users")); } catch (reason) { setError((reason as Error).message); } finally { setBusy(false); } }, []);
  useEffect(() => { void load(); }, [load]);

  async function create(event: FormEvent) {
    event.preventDefault(); setError(""); setNotice("");
    try { await api("/api/admin/users", { method: "POST", body: JSON.stringify({ email, temporaryPassword: password, role }) }); setEmail(""); setPassword(""); setNotice("Team member added."); await load(); }
    catch (reason) { setError((reason as Error).message); }
  }

  async function changeRole(userId: string, newRole: Role) {
    setError("");
    try { await api(`/api/admin/users/${encodeURIComponent(userId)}/roles`, { method: "PUT", body: JSON.stringify({ roles: [newRole] }) }); await load(); setNotice("User role updated."); }
    catch (reason) { setError((reason as Error).message); }
  }

  return <div className="admin-layout"><section className="form-section"><div className="form-section-title"><span className="step-number">＋</span><div><h2>Add team member</h2><p>Create a local account and assign its initial role.</p></div></div><form className="form-grid three-col" onSubmit={(event) => void create(event)}><TextField label="Email address" name="email" value={email} update={(_, value) => setEmail(String(value))} /><label className="field-label">Temporary password<input type="password" minLength={12} required value={password} onChange={(event) => setPassword(event.target.value)} /><small>At least 12 characters; Identity enforces password complexity.</small></label><label className="field-label">Role<select value={role} onChange={(event) => setRole(event.target.value as Role)}>{roles.map(item => <option key={item}>{item}</option>)}</select></label><button className="primary-button admin-submit">Create user</button></form></section>
    <section className="form-section"><div className="section-heading"><div><span className="eyebrow">ACCESS CONTROL</span><h2>Team members</h2></div><button className="text-button" onClick={() => void load()}>Refresh</button></div>{error && <p className="field-error" role="alert">{error}</p>}{notice && <p className="success-text" role="status">{notice}</p>}{busy ? <p className="loading">Loading team…</p> : <div className="table-wrap"><table><thead><tr><th>Email</th><th>Role</th><th>Change role</th></tr></thead><tbody>{users.map(item => <tr key={item.id}><td>{item.email}</td><td>{item.roles.join(", ")}</td><td><select aria-label={`Role for ${item.email}`} value={item.roles[0] ?? "Preparer"} onChange={(event) => void changeRole(item.id, event.target.value as Role)}>{roles.map(option => <option key={option}>{option}</option>)}</select></td></tr>)}</tbody></table></div>}</section></div>;
}