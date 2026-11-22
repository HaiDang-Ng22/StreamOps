function getApiBase(): string {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname || '127.0.0.1';
    return `http://${host}:5081/api/v1`;
  }
  return import.meta.env.VITE_API_URL ?? 'http://127.0.0.1:5081/api/v1';
}

const API_BASE = getApiBase();

let accessToken: string | null = null;

export function setAccessToken(token: string | null) {
  accessToken = token;
}

export async function signIn(email: string, password: string) {
  const response = await fetch(`${API_BASE}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, password }),
  });
  if (!response.ok) throw new Error('Email hoặc mật khẩu chưa đúng, hoặc tài khoản đang bị khóa.');
  const result = await response.json() as { accessToken: string; displayName: string; roles: string[]; expiresAtUtc: string };
  setAccessToken(result.accessToken);
  return result;
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const headers = new Headers(init?.headers);
  if (init?.body && !headers.has('Content-Type')) headers.set('Content-Type', 'application/json');
  if (accessToken) headers.set('Authorization', `Bearer ${accessToken}`);
  const response = await fetch(`${API_BASE}${path}`, {
    ...init,
    headers,
  });
  if (response.status === 401) {
    setAccessToken(null);
    if (typeof window !== 'undefined') window.dispatchEvent(new Event('streamops:unauthorized'));
    throw new Error('Phiên đăng nhập hết hạn. Hãy đăng nhập lại.');
  }
  if (!response.ok) {
    const body = await response.json().catch(() => null) as { title?: string; message?: string; errors?: Record<string, string[]> } | null;
    const message = body?.title ?? body?.message ?? Object.values(body?.errors ?? {}).flat().join(' ') ?? 'Không thể hoàn tất yêu cầu.';
    throw new Error(message);
  }
  if (response.status === 204) return undefined as T;
  return response.json() as Promise<T>;
}

