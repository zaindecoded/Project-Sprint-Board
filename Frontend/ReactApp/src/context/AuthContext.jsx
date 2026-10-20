import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import apiClient from '../services/apiClient';
import { clearStoredToken, getStoredToken, getTokenExpiration, getTokenUser, storeToken } from '../utils/authStorage';
import { UNAUTHORIZED_EVENT } from '../utils/authEvents';

const AuthContext = createContext(null);

function getInitialAuth() {
  const token = getStoredToken();
  return {
    token,
    user: token ? getTokenUser(token) : null
  };
}

export function AuthProvider({ children }) {
  const [auth, setAuth] = useState(getInitialAuth);

  const logout = useCallback(() => {
    clearStoredToken();
    setAuth({ token: null, user: null });
  }, []);

  const login = useCallback(async (credentials) => {
    const { data } = await apiClient.post('/auth/login', credentials);
    if (!data?.token) {
      throw new Error('The login response did not include an authentication token.');
    }

    const tokenUser = getTokenUser(data.token);
    const user = {
      id: data.userId ?? tokenUser?.id ?? '',
      name: data.name ?? tokenUser?.name ?? '',
      email: data.email ?? tokenUser?.email ?? credentials.email,
      role: data.role ?? tokenUser?.role ?? '',
      roleId: data.roleId ?? tokenUser?.roleId ?? 0,
      expiresAt: data.expiresAt ? new Date(data.expiresAt).getTime() : tokenUser?.expiresAt ?? null
    };

    storeToken(data.token);
    setAuth({ token: data.token, user });
    return user;
  }, []);

  useEffect(() => {
    const handleUnauthorized = () => logout();
    window.addEventListener(UNAUTHORIZED_EVENT, handleUnauthorized);
    return () => window.removeEventListener(UNAUTHORIZED_EVENT, handleUnauthorized);
  }, [logout]);

  useEffect(() => {
    if (!auth.token) {
      return undefined;
    }

    const expiresAt = getTokenExpiration(auth.token);
    if (!expiresAt) {
      return undefined;
    }

    const timeout = window.setTimeout(logout, Math.max(0, expiresAt - Date.now()));
    return () => window.clearTimeout(timeout);
  }, [auth.token, logout]);

  const value = useMemo(() => ({
    user: auth.user,
    token: auth.token,
    isAuthenticated: Boolean(auth.token && auth.user),
    login,
    logout
  }), [auth, login, logout]);

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider.');
  }
  return context;
}
