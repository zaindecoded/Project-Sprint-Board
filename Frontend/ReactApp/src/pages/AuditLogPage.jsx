import { useEffect, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import { EmptyState, LoadingState } from '../components/LoadingEmptyState';
import ActiveFilterChips from '../components/ActiveFilterChips';
import { getApiErrorMessage } from '../services/apiErrors';
import { getAuditLogFilters, getAuditLogs } from '../services/auditLogApi';
import '../styles/audit-log.css';

const PAGE_SIZE = 20;

function formatTimestamp(value) {
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat(undefined, {
    year: 'numeric', month: 'short', day: 'numeric', hour: 'numeric', minute: '2-digit', second: '2-digit'
  }).format(date);
}

function actionClass(action) {
  const value = String(action).toLowerCase();
  if (value.includes('login') || value.includes('logout')) return 'audit-action-auth';
  if (value.includes('create')) return 'audit-action-created';
  if (value.includes('delete')) return 'audit-action-deleted';
  if (value.includes('update') || value.includes('status')) return 'audit-action-updated';
  return 'audit-action-default';
}

function AuditLogPage() {
  const [filters, setFilters] = useState({ actions: [], entities: [] });
  const [search, setSearch] = useState('');
  const [query, setQuery] = useState('');
  const [action, setAction] = useState('');
  const [entity, setEntity] = useState('');
  const [page, setPage] = useState(1);
  const [result, setResult] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const timer = window.setTimeout(() => { setQuery(search.trim()); setPage(1); }, 250);
    return () => window.clearTimeout(timer);
  }, [search]);

  useEffect(() => {
    const controller = new AbortController();
    getAuditLogFilters({ signal: controller.signal }).then(setFilters).catch(() => {});
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError('');
    getAuditLogs({ page, pageSize: PAGE_SIZE, search: query, action, entity }, { signal: controller.signal })
      .then(setResult)
      .catch((requestError) => {
        if (requestError.code !== 'ERR_CANCELED') setError(getApiErrorMessage(requestError, 'Unable to load audit activity.'));
      })
      .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    return () => controller.abort();
  }, [page, query, action, entity]);

  const totalPages = result?.totalPages ?? 0;
  const start = result?.totalCount ? (page - 1) * PAGE_SIZE + 1 : 0;
  const end = result ? Math.min(page * PAGE_SIZE, result.totalCount) : 0;

  return (
    <section className="audit-page">
      <header className="audit-heading">
        <div><p className="eyebrow">Administration</p><h1>Audit log</h1></div>
        <div className="audit-total"><strong>{result?.totalCount ?? '—'}</strong><span>events</span></div>
      </header>

      <div className="audit-filters" role="search">
        <label className="audit-search">Search activity
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Search actor, action, entity or details" />
        </label>
        <label>Action
          <select value={action} onChange={(event) => { setAction(event.target.value); setPage(1); }}>
            <option value="">All actions</option>{(filters.actions ?? []).map((item) => <option key={item} value={item}>{item}</option>)}
          </select>
        </label>
        <label>Entity
          <select value={entity} onChange={(event) => { setEntity(event.target.value); setPage(1); }}>
            <option value="">All entities</option>{(filters.entities ?? []).map((item) => <option key={item} value={item}>{item}</option>)}
          </select>
        </label>
      </div>

      <ActiveFilterChips filters={[
        ...(query ? [{ key: 'search', label: 'Search', value: query, onRemove: () => { setSearch(''); setQuery(''); setPage(1); } }] : []),
        ...(action ? [{ key: 'action', label: 'Action', value: action, onRemove: () => { setAction(''); setPage(1); } }] : []),
        ...(entity ? [{ key: 'entity', label: 'Entity', value: entity, onRemove: () => { setEntity(''); setPage(1); } }] : [])
      ]} onClearAll={() => { setSearch(''); setQuery(''); setAction(''); setEntity(''); setPage(1); }} />

      {error && <ErrorMessage message={error} />}
      <div className="audit-table-wrap">
        <table className="audit-table">
          <thead><tr><th>Timestamp</th><th>User</th><th>Action</th><th>Entity</th></tr></thead>
          <tbody>
            {!loading && !error && result?.items?.map((item) => (
              <tr key={item.id}>
                <td className="audit-time">{formatTimestamp(item.timestamp)}</td>
                <td>{item.actor}{item.actorUserId ? <small>User #{item.actorUserId}</small> : null}</td>
                <td><span className={`audit-action ${actionClass(item.action)}`}>{item.action}</span></td>
                <td>{item.entity}{item.entityId ? <small>#{item.entityId}</small> : null}</td>
              </tr>
            ))}
            {loading && <tr><LoadingState as="td" className="audit-state" message="Loading audit activity…" /></tr>}
            {!loading && !error && result?.items?.length === 0 && <tr><EmptyState as="td" className="audit-state" title="No audit events match these filters." /></tr>}
          </tbody>
        </table>
      </div>

      <footer className="audit-pagination">
        <span>{result ? `${start}–${end} of ${result.totalCount}` : ' '}</span>
        <div><button type="button" disabled={loading || page <= 1} onClick={() => setPage((value) => value - 1)}>Previous</button>
          <span>Page {page}{totalPages ? ` of ${totalPages}` : ''}</span>
          <button type="button" disabled={loading || page >= totalPages} onClick={() => setPage((value) => value + 1)}>Next</button></div>
      </footer>
    </section>
  );
}

export default AuditLogPage;
