import { useEffect, useState } from 'react';
import { Bar, Doughnut } from 'react-chartjs-2';
import { ArcElement, BarElement, CategoryScale, Chart as ChartJS, DoughnutController, Legend, LinearScale, Tooltip } from 'chart.js';
import ErrorMessage from '../../components/ErrorMessage';
import { useAuth } from '../../context/AuthContext';
import { canManageWorkspace } from '../../utils/permissions';
import { getApiErrorMessage } from '../../services/apiErrors';
import { getDashboardAttentionTasks, getDashboardSummary } from '../../services/dashboardApi';

ChartJS.register(CategoryScale, LinearScale, BarElement, DoughnutController, ArcElement, Tooltip, Legend);

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
  const { user } = useAuth();
  const canManage = canManageWorkspace(user);
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
  const taskStatusChart = {
    labels: ['To Do', 'In Progress', 'Completed', 'Blocked'],
    datasets: [{
      data: [count(summary?.todoTasks), count(summary?.inProgressTasks), completedTasks, count(summary?.blockedTasks)],
      backgroundColor: ['#8aa3c7', '#4e7dd2', '#43a879', '#df8a62'],
      borderRadius: 6,
      maxBarThickness: 64
    }]
  };
  const riskCounts = {
    low: count(summary?.lowRiskTasks),
    medium: count(summary?.mediumRiskTasks),
    high: count(summary?.highRiskTasks)
  };
  const riskChart = {
    labels: [`Low (${riskCounts.low})`, `Medium (${riskCounts.medium})`, `High (${riskCounts.high})`],
    datasets: [{
      data: [riskCounts.low, riskCounts.medium, riskCounts.high],
      backgroundColor: ['#43a879', '#e8ad45', '#df6b62'],
      borderColor: '#fff',
      borderWidth: 3
    }]
  };
  const riskChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    layout: { padding: 4 },
    plugins: { legend: { position: 'bottom', labels: { boxWidth: 12, boxHeight: 12, padding: 12, font: { size: 11 } } } }
  };
  const taskStatusChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    layout: { padding: { top: 4, right: 6, bottom: 0, left: 0 } },
    plugins: { legend: { display: false } },
    scales: {
      x: { ticks: { font: { size: 11 }, maxRotation: 0, minRotation: 0 } },
      y: { beginAtZero: true, ticks: { precision: 0, font: { size: 10 } } }
    }
  };
  const overview = [
    { label: 'Projects', value: count(summary?.totalProjects) },
    { label: 'Completed projects', value: count(summary?.completedProjects) },
    { label: 'Active sprints', value: count(summary?.activeSprints) },
    { label: 'Tasks', value: totalTasks },
    { label: 'Completed tasks', value: completedTasks },
    { label: 'Overdue', value: count(summary?.overdueTasks) },
    { label: 'Blocked', value: count(summary?.blockedTasks) }
  ].filter((item) => user?.role !== 'TeamMember' || !['Projects', 'Completed projects', 'Active sprints'].includes(item.label));

  return (
    <section className="dashboard-page simple-dashboard">
      <header className="dashboard-heading">
        <div>
          <h1>Dashboard</h1>
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

          {canManage && (
            <section className="dashboard-charts-grid" aria-label="Task and risk charts">
              <article className="dashboard-panel dashboard-chart-panel" aria-labelledby="task-status-chart-title">
                <div className="dashboard-panel-heading"><h2 id="task-status-chart-title">Tasks by status</h2></div>
                <div className="dashboard-status-chart">
                  <Bar data={taskStatusChart} options={taskStatusChartOptions} aria-label="Bar chart comparing To Do, In Progress, Completed, and Blocked tasks" role="img" />
                </div>
              </article>
              <article className="dashboard-panel dashboard-chart-panel" aria-labelledby="risk-distribution-title">
                <div className="dashboard-panel-heading"><h2 id="risk-distribution-title">Risk distribution</h2></div>
                <div className="dashboard-status-chart">
                  <Doughnut data={riskChart} options={riskChartOptions} aria-label={`Risk distribution: Low ${riskCounts.low}, Medium ${riskCounts.medium}, High ${riskCounts.high}`} role="img" />
                </div>
              </article>
            </section>
          )}

          <section className="dashboard-panel dashboard-attention-tasks-panel" aria-labelledby="attention-tasks-title">
            <div className="dashboard-panel-heading">
              <div>
                <h2 id="attention-tasks-title">Attention Required</h2>
              </div>
              {!attentionLoading && !attentionError && <span className="dashboard-total-pill">{attentionTasks.length}</span>}
            </div>
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
              <div className="dashboard-inline-state">No tasks needing attention.</div>
            )}
          </section>
        </>
      )}
    </section>
  );
}

export default DashboardPage;
