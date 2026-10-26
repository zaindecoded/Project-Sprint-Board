import { useEffect, useState } from 'react';
import { Bar, Doughnut } from 'react-chartjs-2';
import { Link } from 'react-router-dom';
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

function getDueDate(value) {
  if (!value) return { label: 'No due date', title: '' };
  const [year, month, day] = String(value).slice(0, 10).split('-').map(Number);
  const dueDate = new Date(year, month - 1, day);
  if (Number.isNaN(dueDate.getTime())) return { label: 'No due date', title: '' };
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const days = Math.round((dueDate - today) / 86400000);
  const label = days < 0 ? `Overdue by ${Math.abs(days)} ${Math.abs(days) === 1 ? 'day' : 'days'}`
    : days === 0 ? 'Due today'
      : days === 1 ? 'Due tomorrow' : `Due in ${days} days`;
  return { label, title: formatDate(value), overdue: days < 0, dueToday: days === 0 };
}

function getStatusLabel(status) {
  return ({ InProgress: 'In Progress', Todo: 'To Do', Done: 'Completed', InReview: 'In Review' })[status] || status;
}

function StatCard({ icon, label, value, variant = 'default' }) {
  return (
    <article className={`simple-dashboard-card simple-dashboard-card-${variant}`}>
      <span className="simple-dashboard-icon-tile"><svg viewBox="0 0 24 24" aria-hidden="true"><path d={icon} /></svg></span>
      <span className="simple-dashboard-card-copy"><span>{label}</span><strong>{value}</strong></span>
    </article>
  );
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
      backgroundColor: ['#94a3b8', '#3b82f6', '#16a34a', '#dc2626'],
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
      backgroundColor: ['#16a34a', '#d97706', '#dc2626'],
      borderColor: '#ffffff',
      borderWidth: 1
    }]
  };
  const riskTaskTotal = riskCounts.low + riskCounts.medium + riskCounts.high;
  const donutCenterText = {
    id: 'donutCenterText',
    afterDraw(chart) {
      const { ctx, chartArea } = chart;
      const centerX = (chartArea.left + chartArea.right) / 2;
      const centerY = (chartArea.top + chartArea.bottom) / 2;
      ctx.save();
      ctx.fillStyle = '#0f172a';
      ctx.textAlign = 'center';
      ctx.textBaseline = 'middle';
      ctx.font = '700 20px Inter, sans-serif';
      ctx.fillText(String(riskTaskTotal), centerX, centerY - 8);
      ctx.fillStyle = '#64748b';
      ctx.font = '12px Inter, sans-serif';
      ctx.fillText('tasks', centerX, centerY + 12);
      ctx.restore();
    }
  };
  const riskChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    layout: { padding: 4 },
    plugins: { legend: { position: 'bottom', labels: {
      boxWidth: 12, boxHeight: 12, padding: 12, font: { size: 12 },
      filter: (item, chart) => chart.datasets[0].data[item.index] > 0
    } } }
  };
  const taskStatusChartOptions = {
    responsive: true,
    maintainAspectRatio: false,
    layout: { padding: { top: 18, right: 6, bottom: 0, left: 0 } },
    plugins: { legend: { display: false } },
    scales: {
      x: { grid: { display: false }, ticks: { font: { size: 12 }, maxRotation: 0, minRotation: 0 } },
      y: { beginAtZero: true, grid: { color: '#e2e8f0' }, ticks: { precision: 0, font: { size: 12 } } }
    }
  };
  const overview = [
    { label: 'Projects', value: count(summary?.totalProjects), icon: 'M3 7h7l2 2h9v10H3z M3 7V5h7l2 2', variant: 'default' },
    { label: 'Active sprints', value: count(summary?.activeSprints), icon: 'M13 2 4 14h7l-1 8 10-13h-7z', variant: 'default' },
    { label: 'Total Tasks', value: totalTasks, icon: 'M8 6h13M8 12h13M8 18h13M3 6h.01M3 12h.01M3 18h.01', variant: 'default' },
    { label: 'Completed Tasks', value: completedTasks, icon: 'm5 12 4 4L19 6', variant: 'ok' },
    { label: 'Tasks Near Deadline', value: count(summary?.nearDeadlineTasks), icon: 'M12 8v4l3 2M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18', variant: count(summary?.nearDeadlineTasks) > 0 ? 'warn' : 'default' },
    { label: 'Blocked Tasks', value: count(summary?.blockedTasks), icon: 'M12 3 2 21h20L12 3zm0 6v5m0 3h.01', variant: count(summary?.blockedTasks) > 0 ? 'danger' : 'ok' }
  ].filter((item) => user?.role !== 'TeamMember' || !['Projects', 'Active sprints'].includes(item.label));

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
            {overview.map((item) => <StatCard key={item.label} {...item} />)}
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
                  <Doughnut data={riskChart} options={riskChartOptions} plugins={[donutCenterText]} aria-label={`Risk distribution: ${riskTaskTotal} tasks: Low ${riskCounts.low}, Medium ${riskCounts.medium}, High ${riskCounts.high}`} role="img" />
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
                {attentionTasks.map((task) => {
                  const due = getDueDate(task.dueDate);
                  return <li className="dashboard-priority-task" key={task.id}>
                    <Link className="dashboard-priority-task-link" to={`/tasks/${task.id}`}>
                    <div className="dashboard-priority-task-main">
                      <strong>{task.title}</strong>
                      <div className="dashboard-priority-task-meta">
                        <span className={`dashboard-status-pill dashboard-status-${String(task.status).toLowerCase()}`}>{getStatusLabel(task.status)}</span>
                        <span className={`dashboard-due-date${due.overdue || due.dueToday ? ' dashboard-due-urgent' : ''}`} title={due.title}>{due.label}</span>
                      </div>
                    </div>
                    <div className="dashboard-priority-task-badges">
                      {task.riskLevel === 'High' && <span className="dashboard-task-badge dashboard-badge-high-risk">High risk</span>}
                      {task.riskLevel === 'Medium' && <span className="dashboard-task-badge dashboard-badge-medium-risk">Medium risk</span>}
                      {task.riskLevel === 'Low' && <span className="dashboard-task-badge dashboard-badge-low-risk">Low risk</span>}
                      {task.nearDeadline && <span className="dashboard-task-badge dashboard-badge-near-deadline">Near deadline</span>}
                    </div>
                    </Link>
                  </li>;
                })}
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
