import { useEffect, useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import ErrorMessage from '../components/ErrorMessage';
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

  return (
    <section className="task-detail-page">
      <Link to="/dashboard" className="task-detail-back">← Dashboard</Link>
      {loading ? <p role="status">Loading task…</p> : error ? <ErrorMessage>{error}</ErrorMessage> : task && (
        <article className="task-detail-card">
          <div className="task-detail-heading"><div><p className="eyebrow">Task details</p><h1>{task.title}</h1></div><span className={`dashboard-status-pill dashboard-status-${String(task.status).toLowerCase()}`}>{({ InProgress: 'In Progress', Todo: 'To Do', Done: 'Completed', InReview: 'In Review' })[task.status] || task.status}</span></div>
          <p className="task-detail-description">{task.description || 'No description provided.'}</p>
          <dl className="task-detail-fields">
            <div><dt>Priority</dt><dd>{task.priority}</dd></div>
            <div><dt>Due date</dt><dd>{formatDate(task.dueDate)}</dd></div>
            <div><dt>Progress</dt><dd>{task.progressPercentage}%</dd></div>
            <div><dt>Estimate</dt><dd>{task.estimateHours} hours</dd></div>
            {task.blockerInformation && <div><dt>Blocker</dt><dd>{task.blockerInformation}</dd></div>}
          </dl>
        </article>
      )}
    </section>
  );
}

export default TaskDetailPage;
