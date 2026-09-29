const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "";

export const tokenKey = "galent.accessToken";
export const sessionKey = "galent.user";
export const expiryKey = "galent.expiresAtUtc";
export const roles = ["Preparer", "Reviewer", "Admin"] as const;

export async function api<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = sessionStorage.getItem(tokenKey);
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers: {
      ...(init.body ? { "Content-Type": "application/json" } : {}),
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...init.headers,
    },
  });
  if (response.status === 204) return undefined as T;
  const body = await response.json().catch(() => null);
  if (response.status === 401 && token) {
    sessionStorage.removeItem(tokenKey);
    sessionStorage.removeItem(sessionKey);
    sessionStorage.removeItem(expiryKey);
    window.dispatchEvent(new Event("galent:unauthorized"));
  }
  if (!response.ok) {
    const message = body?.error ?? body?.title ?? body?.errors?.map((error: { description?: string }) => error.description).join(" ") ?? `Request failed (${response.status}).`;
    throw Object.assign(new Error(message), { status: response.status, body });
  }
  return body as T;
}

export function apiUrl(path: string): string {
  return `${API_BASE}${path}`;
}