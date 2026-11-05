import { Link } from 'react-router-dom';
import ErrorMessage from './ErrorMessage';
import { LoadingState } from './LoadingEmptyState';

export default function DetailPage({ backTo, backLabel, loading, loadingMessage, error, children }) {
  return <section className="task-detail-page">
    <Link to={backTo} className="task-detail-back">← {backLabel}</Link>
    {loading ? <LoadingState message={loadingMessage} as="p" /> : error ? <ErrorMessage message={error} /> : children}
  </section>;
}
