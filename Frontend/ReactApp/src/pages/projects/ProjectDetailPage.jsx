import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import DetailPage from '../../components/DetailPage';
import { StatusBadge } from '../../components/badges';
import { getApiErrorMessage } from '../../services/apiErrors';
import { getProject } from '../../services/projectApi';

function formatDate(value) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
}

export default function ProjectDetailPage() {
  const { id } = useParams();
  const [project, setProject] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    let active = true;
    getProject(id).then((data) => { if (active) setProject(data); })
      .catch((requestError) => { if (active) setError(getApiErrorMessage(requestError, 'Unable to load this project.')); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [id]);

  return <DetailPage backTo="/projects" backLabel="Projects" loading={loading} loadingMessage="Loading project…" error={error}>
    {project && <article className="task-detail-card">
      <div className="task-detail-heading"><div><p className="eyebrow">Project</p><h1>{project.name}</h1></div><StatusBadge status={project.status} className={`project-status project-status-${String(project.status).toLowerCase().replace(/[^a-z]/g, '')}`} /></div>
      <p className="task-detail-description">{project.description || 'No description provided.'}</p>
      <p className="project-inline-dates">{formatDate(project.startDate)} <span aria-hidden="true">→</span> {formatDate(project.endDate)}</p>
    </article>}
  </DetailPage>;
}
