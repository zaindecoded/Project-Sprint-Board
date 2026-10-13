const TOKEN_KEY = 'taskSprintBoard.token';

function decodeTokenPayload(token) {
  try {
    const encodedPayload = token.split('.')[1];
    const base64 = encodedPayload.replace(/-/g, '+').replace(/_/g, '/');
    const bytes = Uint8Array.from(atob(base64), (character) => character.charCodeAt(0));
    return JSON.parse(new TextDecoder().decode(bytes));
  } catch {
    return null;
  }
}

export function getTokenUser(token) {
  const payload = decodeTokenPayload(token);
  if (!payload || (payload.exp && payload.exp * 1000 <= Date.now())) {
    return null;
  }

  const role = payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
  return {
    id: payload.nameid ?? payload.sub ?? '',
    name: payload.unique_name ?? payload.name ?? payload.email ?? 'User',
    email: payload.email ?? '',
    role: Array.isArray(role) ? role[0] : role ?? '',
    expiresAt: payload.exp ? payload.exp * 1000 : null
  };
}

export function getStoredToken() {
  const token = localStorage.getItem(TOKEN_KEY);
  if (!token || !getTokenUser(token)) {
    localStorage.removeItem(TOKEN_KEY);
    return null;
  }
  return token;
}

export function storeToken(token) {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearStoredToken() {
  localStorage.removeItem(TOKEN_KEY);
}

export function getTokenExpiration(token) {
  return getTokenUser(token)?.expiresAt ?? null;
}
