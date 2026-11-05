import { useEffect, useState } from 'react';
import { useParams } from 'react-router-dom';
import DetailPage from '../components/DetailPage';
import { PriorityBadge, StatusBadge } from '../components/badges';
import { getApiErrorMessage } from '../services/apiErrors';
import { getTask } from '../services/taskApi';

function formatDate(value) {
  if (!value) return 'No due date';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? 'No due date' : new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
}

function TaskDetailPage() {
  const { id } = useParams();
  const [task, setTask] = useState(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    getTask(id)
      .then((result) => { if (active) setTask(result); })
      .catch((requestError) => { if (active) setError(getApiErrorMessage(requestError, 'Unable to load this task.')); })
      .finally(() => { if (active) setLoading(false); });
    return () => { active = false; };
  }, [id]);

  return <DetailPage backTo="/dashboard" backLabel="Dashboard" loading={loading} loadingMessage="Loading task…" error={error}>
    {task && <article className="task-detail-card">
      <div className="task-detail-heading"><div><p className="eyebrow">Task details</p><h1>{task.title}</h1></div><StatusBadge status={task.status} className={`dashboard-status-pill dashboard-status-${String(task.status).toLowerCase()}`} /></div>
      <p className="task-detail-description">{task.description || 'No description provided.'}</p>
      <dl className="task-detail-fields">
        <div><dt>Priority</dt><dd><PriorityBadge priority={task.priority} /></dd></div>
        <div><dt>Due date</dt><dd>{formatDate(task.dueDate)}</dd></div>
        <div><dt>Progress</dt><dd>{task.progressPercentage}%</dd></div>
        <div><dt>Estimate</dt><dd>{task.estimateHours} hours</dd></div>
        {task.blockerInformation && <div><dt>Blocker</dt><dd>{task.blockerInformation}</dd></div>}
      </dl>
    </article>}
  </DetailPage>;
}

export default TaskDetailPage;
