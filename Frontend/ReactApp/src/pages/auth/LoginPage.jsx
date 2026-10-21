import { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../../context/AuthContext';
import ErrorMessage from '../../components/ErrorMessage';
import { getApiErrorMessage } from '../../services/apiErrors';

function LoginPage() {
  const { login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
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
      const emailPrefix = email.trim().split('@')[0];
      await login({ email: `${emailPrefix}@company.com`, password });
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

        <ErrorMessage>{error}</ErrorMessage>

        <form className="login-form" onSubmit={handleSubmit}>
          <label htmlFor="email">Company email</label>
          <div className="login-email-field">
            <input
              id="email"
              name="email"
              type="text"
              autoComplete="username"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
              placeholder="yourname"
              aria-describedby="email-domain"
              required
              disabled={loading}
            />
            <span id="email-domain" className="login-email-domain">@company.com</span>
          </div>

          <label htmlFor="password">Password</label>
          <input
            id="password"
            name="password"
            type="password"
            autoComplete="current-password"
            value={password}
            onChange={(event) => setPassword(event.target.value)}
            placeholder="Enter your password"
            required
            disabled={loading}
          />

          <button className="button button-primary login-button" type="submit" disabled={loading}>
            {loading ? 'Signing in…' : 'Sign in'}
          </button>
        </form>
      </section>
    </main>
  );
}

export default LoginPage;
