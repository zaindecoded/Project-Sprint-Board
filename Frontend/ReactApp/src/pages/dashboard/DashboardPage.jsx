import { useEffect, useState } from 'react';
import ErrorMessage from '../../components/ErrorMessage';
import { getApiErrorMessage } from '../../services/apiErrors';
import { getDashboardAttentionTasks, getDashboardSummary } from '../../services/dashboardApi';

function count(value) {
  const number = Number(value);
  return Number.isFinite(number) ? number : 0;
}

function formatDate(value) {
  if (!value) return 'No due date';
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? 'No due date'
    : new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
}

function DashboardPage() {
  const [summary, setSummary] = useState(null);
  const [attentionTasks, setAttentionTasks] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [attentionLoading, setAttentionLoading] = useState(true);
  const [attentionError, setAttentionError] = useState('');

  useEffect(() => {
    const controller = new AbortController();
    getDashboardSummary(controller.signal)
      .then(setSummary)
      .catch((requestError) => {
        if (requestError.code !== 'ERR_CANCELED') setError(getApiErrorMessage(requestError, 'Unable to load dashboard summary.'));
      })
      .finally(() => {
        if (!controller.signal.aborted) setLoading(false);
      });
    getDashboardAttentionTasks(controller.signal)
      .then((result) => setAttentionTasks(Array.isArray(result.items) ? result.items : []))
      .catch((requestError) => {
        if (requestError.code !== 'ERR_CANCELED') setAttentionError(getApiErrorMessage(requestError, 'Unable to load tasks needing attention.'));
      })
      .finally(() => {
        if (!controller.signal.aborted) setAttentionLoading(false);
      });
    return () => controller.abort();
  }, []);

  const totalTasks = count(summary?.totalTasks);
  const completedTasks = count(summary?.completedTasks);
  const completion = totalTasks ? Math.round((completedTasks / totalTasks) * 100) : 0;
  const overview = [
    { label: 'Projects', value: count(summary?.totalProjects) },
    { label: 'Active sprints', value: count(summary?.activeSprints) },
    { label: 'Tasks', value: totalTasks },
    { label: 'Completed', value: completedTasks },
    { label: 'Overdue', value: count(summary?.overdueTasks) },
    { label: 'Blocked', value: count(summary?.blockedTasks) }
  ];

  return (
    <section className="dashboard-page simple-dashboard">
      <header className="dashboard-heading">
        <div>
          <h1>Dashboard</h1>
          <p className="projects-subtitle">A quick view of project work and tasks that need attention.</p>
        </div>
      </header>

      {loading ? (
        <div className="project-state" role="status">Loading dashboard…</div>
      ) : error ? (
        <ErrorMessage>{error}</ErrorMessage>
      ) : (
        <>
          <section className="simple-dashboard-grid" aria-label="Workspace totals">
            {overview.map((item) => (
              <article className="simple-dashboard-card" key={item.label}>
                <span>{item.label}</span>
                <strong>{item.value}</strong>
              </article>
            ))}
          </section>

          <section className="dashboard-panel simple-dashboard-progress" aria-labelledby="task-progress-title">
            <div className="dashboard-panel-heading">
              <div>
                <p className="dashboard-panel-eyebrow">Task progress</p>
                <h2 id="task-progress-title">Completed work</h2>
              </div>
              <strong>{completion}%</strong>
            </div>
            <div className="dashboard-progress-track" aria-label={`${completedTasks} of ${totalTasks} tasks completed`}>
              <span style={{ width: `${completion}%` }} />
            </div>
            <p>{completedTasks} of {totalTasks} {totalTasks === 1 ? 'task' : 'tasks'} completed</p>
          </section>

          <section className="dashboard-panel dashboard-attention-tasks-panel" aria-labelledby="attention-tasks-title">
            <div className="dashboard-panel-heading">
              <div>
                <p className="dashboard-panel-eyebrow">Follow up</p>
                <h2 id="attention-tasks-title">Tasks needing attention</h2>
              </div>
              {!attentionLoading && !attentionError && <span className="dashboard-total-pill">{attentionTasks.length}</span>}
            </div>
            <p className="dashboard-attention-description">Open tasks marked critical or rated high risk.</p>
            {attentionLoading ? (
              <div className="dashboard-inline-state" role="status">Loading tasks…</div>
            ) : attentionError ? (
              <ErrorMessage>{attentionError}</ErrorMessage>
            ) : attentionTasks.length ? (
              <ul className="dashboard-priority-task-list">
                {attentionTasks.map((task) => (
                  <li className="dashboard-priority-task" key={task.id}>
                    <div className="dashboard-priority-task-main">
                      <strong>{task.title}</strong>
                      <div className="dashboard-priority-task-meta">
                        <span>{task.status}</span>
                        <span>Due {formatDate(task.dueDate)}</span>
                      </div>
                    </div>
                    <div className="dashboard-priority-task-badges">
                      {task.priority === 'Critical' && <span className="dashboard-task-badge dashboard-badge-critical">Critical</span>}
                      {task.riskLevel === 'High' && <span className="dashboard-task-badge dashboard-badge-high-risk">High risk</span>}
                    </div>
                  </li>
                ))}
              </ul>
            ) : (
              <div className="dashboard-inline-state">No critical or high-risk open tasks.</div>
            )}
          </section>
        </>
      )}
    </section>
  );
}

export default DashboardPage;
