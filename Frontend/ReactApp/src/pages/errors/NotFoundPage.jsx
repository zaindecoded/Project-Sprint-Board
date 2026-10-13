import { Link } from 'react-router-dom';

function NotFoundPage() {
  return (
    <div className="content-card">
      <h1>Page not found</h1>
      <p>The page you requested does not exist.</p>
      <Link className="button button-primary" to="/dashboard">Back to dashboard</Link>
    </div>
  );
}

export default NotFoundPage;
