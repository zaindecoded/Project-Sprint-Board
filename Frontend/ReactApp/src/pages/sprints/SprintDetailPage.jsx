import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import DetailPage from '../../components/DetailPage';
import { StatusBadge } from '../../components/badges';
import { getApiErrorMessage } from '../../services/apiErrors';
import { getSprint } from '../../services/sprintApi';
import { getProject } from '../../services/projectApi';

function formatDate(value) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
}

export default function SprintDetailPage() {
  const { id } = useParams();
  const [sprint, setSprint] = useState(null);
  const [projectName, setProjectName] = useState('');
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    getSprint(id).then(async (item) => {
      if (!active) return;
      setSprint(item);
      try {
        const project = await getProject(item.projectId);
        if (active) setProjectName(project.name);
      } catch {
        if (active) setProjectName('Project unavailable');
      }
    }).catch((requestError) => {
      if (active) setError(getApiErrorMessage(requestError, 'Unable to load this sprint.'));
    }).finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [id]);

  return <DetailPage backTo="/sprints" backLabel="Sprints" loading={loading} loadingMessage="Loading sprint…" error={error}>
    {sprint && <article className="task-detail-card">
      <div className="task-detail-heading"><div><p className="eyebrow">Sprint</p><h1>{sprint.name}</h1></div><StatusBadge status={sprint.status} className={`sprint-status-badge sprint-status-${String(sprint.status).toLowerCase()}`} /></div>
      <Link className="sprint-project-name" to={`/projects/${sprint.projectId}`}>{projectName || 'Project unavailable'}</Link>
      <p className="task-detail-description">{sprint.goal || 'No goal provided.'}</p>
      <dl className="task-detail-fields">
        <div><dt>Start date</dt><dd>{formatDate(sprint.startDate)}</dd></div>
        <div><dt>End date</dt><dd>{formatDate(sprint.endDate)}</dd></div>
      </dl>
    </article>}
  </DetailPage>;
}
