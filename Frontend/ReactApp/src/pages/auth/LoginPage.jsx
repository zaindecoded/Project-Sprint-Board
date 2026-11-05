import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import ErrorMessage from '../../components/ErrorMessage';
import FormField from '../../components/FormField';
import { getApiErrorMessage } from '../../services/apiErrors';

function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [showPassword, setShowPassword] = useState(false);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  async function handleSubmit(event) {
    event.preventDefault();
    if (loading) {
      return;
    }

    setError('');
    setLoading(true);
    try {
      await login({ email: email.trim(), password });
      navigate(location.state?.from?.pathname || '/', { replace: true });
    } catch (loginError) {
      setError(getApiErrorMessage(loginError, 'Unable to sign in. Please try again.'));
    } finally {
      setLoading(false);
    }
  }

  return (
    <main className="login-page">
      <section className="login-card">
        
        <h1>Welcome back</h1>
        <h3 className="muted-text">Have a great day!</h3>

        <ErrorMessage message={error} />

        <form className="login-form" onSubmit={handleSubmit}>
          <FormField id="email" label="Email" required><input
            id="email"
            name="email"
            type="email"
            autoComplete="username"
            value={email}
            onChange={(event) => setEmail(event.target.value)}
            placeholder="you@company.com"
            required
            disabled={loading}
          /></FormField>

          <div className="login-password-field">
            <FormField id="password" label="Password" required>
            <input
              id="password"
              name="password"
              type={showPassword ? 'text' : 'password'}
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
              placeholder="Enter your password"
              required
              disabled={loading}
            />
            </FormField>
            <button
              className="login-password-toggle"
              type="button"
              aria-label={showPassword ? 'Hide password' : 'Show password'}
              aria-pressed={showPassword}
              onClick={() => setShowPassword((visible) => !visible)}
              disabled={loading}
            >
              {showPassword ? (
                <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 3l18 18M10.6 10.6a2 2 0 002.8 2.8M9.9 5.2A10.8 10.8 0 0112 5c5 0 9 7 9 7a15 15 0 01-3 3.8M6.2 6.2C3.8 7.8 3 12 3 12s4 7 9 7a9 9 0 003.2-.6" /></svg>
              ) : (
                <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 12s4-7 9-7 9 7 9 7-4 7-9 7-9-7-9-7z" /><circle cx="12" cy="12" r="2.5" /></svg>
              )}
            </button>
          </div>

          <button className="button button-primary login-button" type="submit" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </section>
    </main>
  );
}

export default LoginPage;
