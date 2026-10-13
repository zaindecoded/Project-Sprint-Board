import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import { getProjects } from '../services/projectApi';
import { getSprint, getSprints } from '../services/sprintApi';
import { getTaskStatusHistory } from '../services/statusHistoryApi';
import { evaluateTaskRisk } from '../services/intelligenceApi';
import { createTask, deleteTask, getTask, getTasks, updateTask } from '../services/taskApi';
import { getApiErrorMessage } from '../services/apiErrors';
import { useAuth } from '../context/AuthContext';
import { canManageWorkspace } from '../utils/permissions';
import { getTaskActivityStatus, getTaskDisplayStatus, getTodayDateInputValue } from '../utils/displayStatus';

const PAGE_SIZE = 10;
const LOOKUP_PAGE_SIZE = 100;
const TASK_PRIORITIES = ['Low', 'Medium', 'High', 'Critical'];
const TASK_STATUSES = ['Todo', 'InProgress', 'Blocked', 'InReview', 'Done', 'Cancelled'];
const EMPTY_FORM = {
  projectId: '',
  sprintId: '',
  title: '',
  description: '',
  priority: 'Medium',
  status: 'Todo',
  estimateHours: '',
  progressPercentage: 0,
  startDate: '',
  dueDate: '',
  blockerInformation: ''
};

function formatDate(value) {
  if (!value) {
    return '—';
  }
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
}

function dateInputValue(value) {
  return value ? String(value).slice(0, 10) : '';
}

function getValidationMessage(error, fallback = 'Unable to save this task.') {
  const errors = error.response?.data?.errors;
  if (Array.isArray(errors)) {
    return errors.filter((item) => typeof item === 'string').join(' ');
  }
  if (errors && typeof errors === 'object') {
    const messages = Object.values(errors).flat().filter((item) => typeof item === 'string');
    if (messages.length) {
      return messages.join(' ');
    }
  }
  if (typeof error.response?.data?.message === 'string') {
    return error.response.data.message;
  }
  return getApiErrorMessage(error, fallback);
}

function formatDateTime(value) {
  if (!value) {
    return '—';
  }
  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat(undefined, {
      year: 'numeric',
      month: 'short',
      day: 'numeric',
      hour: 'numeric',
      minute: '2-digit',
      second: '2-digit'
    }).format(date);
}

async function loadAllProjects(signal) {
  const first = await getProjects({ page: 1, pageSize: LOOKUP_PAGE_SIZE, search: '', signal });
  const items = Array.isArray(first.items) ? [...first.items] : [];
  for (let page = 2; page <= (Number(first.totalPages) || 0); page += 1) {
    const result = await getProjects({ page, pageSize: LOOKUP_PAGE_SIZE, search: '', signal });
    if (Array.isArray(result.items)) {
      items.push(...result.items);
    }
  }
  return items;
}

async function loadSprintsForProject(projectId, signal) {
  if (!projectId) {
    return [];
  }

  const first = await getSprints({ page: 1, pageSize: LOOKUP_PAGE_SIZE, search: '', projectId, signal });
  const items = Array.isArray(first.items) ? [...first.items] : [];
  for (let page = 2; page <= (Number(first.totalPages) || 0); page += 1) {
    const result = await getSprints({ page, pageSize: LOOKUP_PAGE_SIZE, search: '', projectId, signal });
    if (Array.isArray(result.items)) {
      items.push(...result.items);
    }
  }
  return items;
}

function TaskHistoryPanel({ task, onClose }) {
  const [history, setHistory] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    const controller = new AbortController();
    setLoading(true);
    setError('');
    getTaskStatusHistory(task.id, controller.signal)
      .then((items) => {
        if (!controller.signal.aborted) {
          setHistory(Array.isArray(items) ? items : []);
        }
      })
      .catch((requestError) => {
        if (!controller.signal.aborted && requestError.code !== 'ERR_CANCELED') {
          setError(getApiErrorMessage(requestError, 'Unable to load task status history.'));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLoading(false);
        }
      });
    return () => controller.abort();
  }, [task.id]);

  return (
    <section className="task-history-panel" aria-labelledby="task-history-title">
      <div className="task-history-heading">
        <div>
          <p className="eyebrow">Task details</p>
          <h2 id="task-history-title">Status history: {task.title}</h2>
        </div>
        <button className="button button-secondary" type="button" onClick={onClose}>Close history</button>
      </div>

      <ErrorMessage>{error}</ErrorMessage>
      {loading ? (
        <p className="project-loading" role="status">Loading task history…</p>
      ) : error ? null : history.length === 0 ? (
        <div className="task-history-empty">No status history has been recorded for this task.</div>
      ) : (
        <ol className="task-history-list">
          {history.map((entry) => (
            <li className="task-history-entry" key={entry.id}>
              <div className="task-history-transition">
                <span>{entry.oldStatus}</span>
                <span aria-hidden="true">→</span>
                <span>{entry.newStatus}</span>
              </div>
              <dl className="task-history-details">
                <div>
                  <dt>Changed by</dt>
                  <dd>{entry.changedBy || '—'}</dd>
                </div>
                <div>
                  <dt>Changed at</dt>
                  <dd>{formatDateTime(entry.changedAt)}</dd>
                </div>
                {entry.reason && (
                  <div className="task-history-reason">
                    <dt>Reason</dt>
                    <dd>{entry.reason}</dd>
                  </div>
                )}
              </dl>
            </li>
          ))}
        </ol>
      )}
    </section>
  );
}

function TaskRiskPanel({ task, onClose, onEvaluatingChange }) {
  const [result, setResult] = useState(null);
  const [evaluating, setEvaluating] = useState(false);
  const [error, setError] = useState('');
  const requestController = useRef(null);
  const hasDueDate = Boolean(task.dueDate);

  useEffect(() => () => requestController.current?.abort(), []);

  async function handleEvaluate() {
    if (evaluating || !hasDueDate) {
      return;
    }

    const controller = new AbortController();
    requestController.current = controller;
    setEvaluating(true);
    onEvaluatingChange(true);
    setError('');
    try {
      const history = await getTaskStatusHistory(task.id, controller.signal);
      const now = new Date();
      const dueDate = new Date(`${String(task.dueDate).slice(0, 10)}T00:00:00Z`);
      const today = Date.UTC(now.getUTCFullYear(), now.getUTCMonth(), now.getUTCDate());
      const daysRemaining = Math.ceil((dueDate.getTime() - today) / 86400000);
      const taskAge = Math.max(0, Math.floor((now.getTime() - new Date(task.createdAt).getTime()) / 86400000));
      const evaluation = await evaluateTaskRisk({
        taskId: task.id,
        estimateHours: task.estimateHours,
        taskAge,
        daysRemaining,
        progressPercentage: task.progressPercentage,
        isBlocked: task.status === 'Blocked',
        priority: task.priority,
        currentStatus: task.status,
        numberOfStatusChanges: Array.isArray(history) ? history.length : 0
      }, controller.signal);
      setResult(evaluation);
    } catch (requestError) {
      if (requestError.code !== 'ERR_CANCELED') {
        setError(getApiErrorMessage(requestError, 'Unable to evaluate task risk.'));
      }
    } finally {
      if (requestController.current === controller) {
        requestController.current = null;
      }
      setEvaluating(false);
      onEvaluatingChange(false);
    }
  }

  const riskFactors = result?.riskFactors
    ? result.riskFactors.split(';').map((factor) => factor.trim()).filter(Boolean)
    : [];

  return (
    <section className="task-risk-panel" aria-labelledby="task-risk-title">
      <div className="task-history-heading">
        <div>
          <p className="eyebrow">Task advisory</p>
          <h2 id="task-risk-title">Delay risk: {task.title}</h2>
        </div>
        <button className="button button-secondary" type="button" onClick={onClose} disabled={evaluating}>Close risk</button>
      </div>

      <p className="task-risk-advisory">This risk assessment is advisory and does not change the task status.</p>
      <ErrorMessage>{error}</ErrorMessage>

      {!result ? (
        <div className="task-risk-initial">
          <p>{hasDueDate ? 'Risk has not been evaluated yet.' : 'Add a due date before evaluating risk.'}</p>
          <button className="button button-primary" type="button" onClick={handleEvaluate} disabled={evaluating || !hasDueDate}>
            {evaluating ? 'Evaluating risk...' : 'Evaluate risk'}
          </button>
        </div>
      ) : (
        <div className="task-risk-result">
          <div className="task-risk-summary">
            <div>
              <span className="task-risk-label">Risk level</span>
              <strong className={`task-risk-level task-risk-level-${String(result.riskLevel).toLowerCase()}`}>{result.riskLevel}</strong>
            </div>
            <div>
              <span className="task-risk-label">Risk score</span>
              <strong>{result.riskScore} / 100</strong>
            </div>
            <div>
              <span className="task-risk-label">Confidence</span>
              <strong>{new Intl.NumberFormat(undefined, { style: 'percent', maximumFractionDigits: 0 }).format(Number(result.confidence) || 0)}</strong>
            </div>
            <div>
              <span className="task-risk-label">Evaluated at</span>
              <strong>{formatDateTime(result.timestamp)}</strong>
            </div>
          </div>
          <div className="task-risk-explanation">
            <h3>Risk factors</h3>
            {riskFactors.length ? (
              <ul>{riskFactors.map((factor, index) => <li key={`${factor}-${index}`}>{factor}</li>)}</ul>
            ) : <p>No risk factors were returned.</p>}
            <h3>Explanation</h3>
            <p>{result.explanation}</p>
          </div>
          <button className="button button-secondary" type="button" onClick={handleEvaluate} disabled={evaluating}>
            {evaluating ? 'Evaluating risk...' : 'Evaluate again'}
          </button>
        </div>
      )}
    </section>
  );
}

function TaskForm({ task, projects, onCancel, onSaved }) {
  const [form, setForm] = useState(EMPTY_FORM);
  const [sprints, setSprints] = useState([]);
  const [existingSprint, setExistingSprint] = useState(null);
  const [sprintsLoading, setSprintsLoading] = useState(false);
  const [loadingTask, setLoadingTask] = useState(Boolean(task));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const selectedSprint = sprints.find((item) => item.id === form.sprintId)
    ?? (existingSprint?.id === form.sprintId ? existingSprint : null);
  const sprintEndDate = selectedSprint?.endDate ? dateInputValue(selectedSprint.endDate) : '';

  useEffect(() => {
    let active = true;
    if (!task) {
      setForm(EMPTY_FORM);
      setLoadingTask(false);
      return () => {
        active = false;
      };
    }

    setLoadingTask(true);
    setError('');
    Promise.all([
      getTask(task.id),
      getSprint(task.sprintId)
    ])
      .then(([item, sprint]) => {
        if (active) {
          setExistingSprint(sprint);
          setForm({
            projectId: sprint.projectId ?? '',
            sprintId: item.sprintId ?? '',
            title: item.title ?? '',
            description: item.description ?? '',
            priority: item.priority ?? '',
            status: item.status ?? '',
            estimateHours: item.estimateHours ?? '',
            progressPercentage: item.progressPercentage ?? 0,
            startDate: dateInputValue(item.startDate),
            dueDate: dateInputValue(item.dueDate),
            blockerInformation: item.blockerInformation ?? ''
          });
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(getApiErrorMessage(requestError, 'Unable to load this task.'));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingTask(false);
        }
      });

    return () => {
      active = false;
    };
  }, [task]);

  useEffect(() => {
    const controller = new AbortController();
    setSprints([]);
    if (!form.projectId) {
      setSprintsLoading(false);
      return () => controller.abort();
    }

    setSprintsLoading(true);
    loadSprintsForProject(form.projectId, controller.signal)
      .then((items) => setSprints(items))
      .catch((requestError) => {
        if (requestError.code !== 'ERR_CANCELED') {
          setError(getApiErrorMessage(requestError, 'Unable to load sprints for this project.'));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setSprintsLoading(false);
        }
      });
    return () => controller.abort();
  }, [form.projectId]);

  function changeField(event) {
    const { name, value } = event.target;
    setError('');
    setForm((current) => ({
      ...current,
      [name]: value,
      ...(name === 'projectId' ? { sprintId: '' } : {})
    }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (saving) {
      return;
    }
    if (!task) {
      const today = getTodayDateInputValue();
      if (form.startDate && form.startDate < today) {
        setError('Task start date must be today or a future date.');
        return;
      }
      if (form.dueDate && form.dueDate < today) {
        setError('Task due date must be today or a future date.');
        return;
      }
    }
    if (form.startDate && form.dueDate && form.dueDate < form.startDate) {
      setError('Due date cannot be earlier than the start date.');
      return;
    }
    if (form.dueDate && sprintEndDate && form.dueDate > sprintEndDate) {
      setError('Task due date cannot be later than its sprint end date.');
      return;
    }
    if (!Number.isFinite(Number(form.estimateHours)) || Number(form.estimateHours) <= 0) {
      setError('Estimate must be greater than zero.');
      return;
    }
    const progress = Number(form.progressPercentage);
    if (!Number.isInteger(progress) || progress < 0 || progress > 100) {
      setError('Progress must be between 0 and 100.');
      return;
    }

    setSaving(true);
    setError('');
    const body = {
      sprintId: form.sprintId,
      title: form.title.trim(),
      description: form.description.trim(),
      priority: form.priority,
      status: form.status,
      estimateHours: Number(form.estimateHours),
      progressPercentage: progress,
      startDate: form.startDate || null,
      dueDate: form.dueDate || null,
      blockerInformation: form.blockerInformation.trim() || null
    };

    try {
      if (task) {
        const updateBody = { ...body };
        if (form.status.trim().toLowerCase() === String(task.status ?? '').trim().toLowerCase()) {
          delete updateBody.status;
        }
        delete updateBody.startDate;
        delete updateBody.dueDate;
        if (form.startDate) {
          updateBody.startDate = form.startDate;
        }
        if (form.dueDate) {
          updateBody.dueDate = form.dueDate;
        }
        await updateTask(task.id, updateBody);
        onSaved('Task updated successfully.');
      } else {
        await createTask(body);
        onSaved('Task created successfully.');
      }
    } catch (requestError) {
      setError(getValidationMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  const currentSprintMissing = existingSprint && !sprints.some((sprint) => sprint.id === existingSprint.id);

  return (
    <section className="project-form-card" aria-labelledby="task-form-title">
      <div className="project-form-heading">
        <div>
          <p className="eyebrow">{task ? 'Task details' : 'New task'}</p>
          <h2 id="task-form-title">{task ? 'Edit task' : 'Create task'}</h2>
        </div>
        <button className="button button-secondary" type="button" onClick={onCancel} disabled={saving}>Cancel</button>
      </div>
      <ErrorMessage>{error}</ErrorMessage>

      {loadingTask ? (
        <p className="project-loading" role="status">Loading task details…</p>
      ) : (
        <form className="project-form" onSubmit={handleSubmit}>
          <div className="project-field">
            <label htmlFor="task-project">Project <span aria-hidden="true">*</span></label>
            <select id="task-project" name="projectId" value={form.projectId} onChange={changeField} required disabled={saving}>
              <option value="">Select a project</option>
              {projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
            </select>
          </div>
          <div className="project-field">
            <label htmlFor="task-sprint">Sprint <span aria-hidden="true">*</span></label>
            <select
              id="task-sprint"
              name="sprintId"
              value={form.sprintId}
              onChange={changeField}
              required
              disabled={saving || sprintsLoading || !form.projectId || sprints.length === 0}
            >
              <option value="">{sprintsLoading ? 'Loading sprints…' : 'Select a sprint'}</option>
              {currentSprintMissing && (
                <option value={existingSprint.id}>{existingSprint.name}</option>
              )}
              {sprints.map((sprint) => <option key={sprint.id} value={sprint.id}>{sprint.name}</option>)}
            </select>
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="task-title">Title <span aria-hidden="true">*</span></label>
            <input id="task-title" name="title" value={form.title} onChange={changeField} maxLength={200} required disabled={saving} />
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="task-description">Description</label>
            <textarea id="task-description" name="description" value={form.description} onChange={changeField} maxLength={2000} rows={3} disabled={saving} />
          </div>
          <div className="project-field">
            <label htmlFor="task-priority">Priority</label>
            <select id="task-priority" name="priority" value={form.priority} onChange={changeField} required disabled={saving}>
              {TASK_PRIORITIES.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
            </select>
          </div>
          <div className="project-field">
            <label htmlFor="task-status">Status</label>
            <select id="task-status" name="status" value={form.status} onChange={changeField} required disabled={saving}>
              {TASK_STATUSES.map((status) => <option key={status} value={status}>{status}</option>)}
            </select>
          </div>
          <div className="project-field">
            <label htmlFor="task-estimate">Estimate (hours) <span aria-hidden="true">*</span></label>
            <input id="task-estimate" name="estimateHours" type="number" min="0.01" step="0.01" value={form.estimateHours} onChange={changeField} required disabled={saving} />
          </div>
          <div className="project-field">
            <label htmlFor="task-progress">Progress (%)</label>
            <input id="task-progress" name="progressPercentage" type="number" min="0" max="100" step="1" value={form.progressPercentage} onChange={changeField} required disabled={saving} />
          </div>
          <div className="project-field">
            <label htmlFor="task-start-date">Start date</label>
            <input
              id="task-start-date"
              name="startDate"
              type="date"
              value={form.startDate}
              onChange={changeField}
              min={!task ? getTodayDateInputValue() : undefined}
              disabled={saving || Boolean(task && task.startDate)}
            />
          </div>
          <div className="project-field">
            <label htmlFor="task-due-date">Due date</label>
            <input
              id="task-due-date"
              name="dueDate"
              type="date"
              value={form.dueDate}
              onChange={changeField}
              max={sprintEndDate || undefined}
              min={!task ? (form.startDate > getTodayDateInputValue() ? form.startDate : getTodayDateInputValue()) : (form.startDate || undefined)}
              disabled={saving}
            />
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="task-blocker">Blocker information</label>
            <textarea id="task-blocker" name="blockerInformation" value={form.blockerInformation} onChange={changeField} maxLength={1000} rows={2} disabled={saving} />
          </div>
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving || sprintsLoading || !form.sprintId}>
              {saving ? 'Saving…' : task ? 'Save changes' : 'Create task'}
            </button>
          </div>
        </form>
      )}
    </section>
  );
}

function TasksPage() {
  const { user } = useAuth();
  const canManage = canManageWorkspace(user);
  const [tasks, setTasks] = useState([]);
  const [projects, setProjects] = useState([]);
  const [sprints, setSprints] = useState([]);
  const [lookupsLoading, setLookupsLoading] = useState(true);
  const [lookupError, setLookupError] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [filters, setFilters] = useState({ projectId: '', sprintId: '', status: '', priority: '', dueDate: '', isBlocked: '' });
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState('');
  const [notice, setNotice] = useState('');
  const [editingTask, setEditingTask] = useState(null);
  const [formOpen, setFormOpen] = useState(false);
  const [deletingId, setDeletingId] = useState(null);
  const [taskSprintDetails, setTaskSprintDetails] = useState({});
  const [historyTask, setHistoryTask] = useState(null);
  const [riskTask, setRiskTask] = useState(null);
  const [riskEvaluatingTaskId, setRiskEvaluatingTaskId] = useState(null);

  useEffect(() => {
    const controller = new AbortController();
    setLookupsLoading(true);
    setLookupError('');
    loadAllProjects(controller.signal)
      .then(setProjects)
      .catch((error) => {
        if (error.code !== 'ERR_CANCELED') {
          setLookupError(getApiErrorMessage(error, 'Unable to load project choices.'));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setLookupsLoading(false);
        }
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  useEffect(() => {
    const controller = new AbortController();
    setSprints([]);
    if (!filters.projectId) {
      return () => controller.abort();
    }
    loadSprintsForProject(filters.projectId, controller.signal)
      .then((items) => setSprints(items))
      .catch((error) => {
        if (error.code !== 'ERR_CANCELED') {
          setLookupError(getApiErrorMessage(error, 'Unable to load sprint choices.'));
        }
      });
    return () => controller.abort();
  }, [filters.projectId]);

  const loadTasks = useCallback(async (signal) => {
    setLoading(true);
    setListError('');
    setTaskSprintDetails({});
    try {
      const result = await getTasks({
        page,
        pageSize: PAGE_SIZE,
        search,
        ...filters,
        signal
      });
      const items = Array.isArray(result.items) ? result.items : [];
      setTasks(items);
      setTotalCount(Number(result.totalCount) || 0);
      setTotalPages(Number(result.totalPages) || 0);
      if (result.page && result.page !== page) {
        setPage(result.page);
      }

      const sprintIds = [...new Set(items.map((item) => item.sprintId).filter(Boolean))];
      const details = await Promise.all(sprintIds.map(async (id) => {
        return [id, await getSprint(id)];
      }));
      if (!signal?.aborted) {
        setTaskSprintDetails(Object.fromEntries(details));
      }
    } catch (requestError) {
      if (requestError.code !== 'ERR_CANCELED') {
        setListError(getApiErrorMessage(requestError, 'Unable to load tasks.'));
      }
    } finally {
      if (!signal?.aborted) {
        setLoading(false);
      }
    }
  }, [filters, page, search]);

  useEffect(() => {
    const controller = new AbortController();
    loadTasks(controller.signal);
    return () => controller.abort();
  }, [loadTasks]);

  function updateFilter(name, value) {
    setPage(1);
    setFilters((current) => ({
      ...current,
      [name]: value,
      ...(name === 'projectId' ? { sprintId: '' } : {})
    }));
  }

  function openCreateForm() {
    setNotice('');
    setHistoryTask(null);
    setEditingTask(null);
    setFormOpen(true);
  }

  function openEditForm(task) {
    setNotice('');
    setHistoryTask(null);
    setEditingTask(task);
    setFormOpen(true);
  }

  async function handleSaved(message) {
    setFormOpen(false);
    setEditingTask(null);
    setNotice(message);
    await loadTasks();
  }

  async function handleDelete(task) {
    if (!window.confirm(`Delete "${task.title}"? This will remove it from the active task list.`)) {
      return;
    }
    setDeletingId(task.id);
    setListError('');
    setNotice('');
    try {
      await deleteTask(task.id);
      setNotice('Task deleted successfully.');
      if (tasks.length === 1 && page > 1) {
        setPage(page - 1);
      } else {
        await loadTasks();
      }
    } catch (error) {
      setListError(getValidationMessage(error, 'Unable to delete this task.'));
    } finally {
      setDeletingId(null);
    }
  }

  const projectNames = useMemo(() => new Map(projects.map((project) => [project.id, project.name])), [projects]);
  const listSprints = useMemo(() => new Map(sprints.map((sprint) => [sprint.id, sprint])), [sprints]);
  const priorities = useMemo(() => [...new Set(tasks.map((task) => task.priority).filter(Boolean))], [tasks]);
  const statuses = useMemo(() => [...new Set(tasks.map((task) => task.status).filter(Boolean))], [tasks]);

  return (
    <section className="projects-page tasks-page">
      <div className="projects-heading">
        <div>
          <h1>Tasks</h1>
        </div>
        {canManage && !formOpen && (
          <button className="button button-primary" type="button" onClick={openCreateForm} disabled={lookupsLoading || projects.length === 0}>
            Create task
          </button>
        )}
      </div>

      {notice && <div className="project-notice" role="status">{notice}</div>}
      <ErrorMessage>{lookupError}</ErrorMessage>

      {canManage && formOpen && (
        <TaskForm
          key={editingTask?.id ?? 'new'}
          task={editingTask}
          projects={projects}
          onCancel={() => {
            setFormOpen(false);
            setEditingTask(null);
          }}
          onSaved={handleSaved}
        />
      )}

      {historyTask && (
        <TaskHistoryPanel
          key={historyTask.id}
          task={historyTask}
          onClose={() => setHistoryTask(null)}
        />
      )}

      {canManage && riskTask && (
        <TaskRiskPanel
          key={riskTask.id}
          task={riskTask}
          onEvaluatingChange={(isEvaluating) => setRiskEvaluatingTaskId(isEvaluating ? riskTask.id : null)}
          onClose={() => setRiskTask(null)}
        />
      )}

      <div className="projects-toolbar task-toolbar">
        <label className="project-search">
          <span>Search tasks</span>
          <input
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Search by title or description"
            aria-label="Search tasks by title or description"
          />
        </label>
        <label className="project-filter">
          <span>Project</span>
          <select value={filters.projectId} onChange={(event) => updateFilter('projectId', event.target.value)} disabled={lookupsLoading}>
            <option value="">All projects</option>
            {projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
          </select>
        </label>
        <label className="project-filter">
          <span>Sprint</span>
          <select value={filters.sprintId} onChange={(event) => updateFilter('sprintId', event.target.value)} disabled={!filters.projectId}>
            <option value="">All sprints</option>
            {sprints.map((sprint) => <option key={sprint.id} value={sprint.id}>{sprint.name}</option>)}
          </select>
        </label>
        <label className="project-filter">
          <span>Status</span>
          <select value={filters.status} onChange={(event) => updateFilter('status', event.target.value)}>
            <option value="">All statuses</option>
            {statuses.map((status) => <option key={status} value={status}>{status}</option>)}
          </select>
        </label>
        <label className="project-filter">
          <span>Priority</span>
          <select value={filters.priority} onChange={(event) => updateFilter('priority', event.target.value)}>
            <option value="">All priorities</option>
            {priorities.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
          </select>
        </label>
        <label className="project-filter">
          <span>Due date</span>
          <input type="date" value={filters.dueDate} onChange={(event) => updateFilter('dueDate', event.target.value)} />
        </label>
        <label className="project-filter">
          <span>Blocked</span>
          <select value={filters.isBlocked} onChange={(event) => updateFilter('isBlocked', event.target.value)}>
            <option value="">All tasks</option>
            <option value="true">Blocked</option>
            <option value="false">Not blocked</option>
          </select>
        </label>
        <span className="project-count">
          {lookupsLoading ? 'Loading projects…' : `${totalCount} ${totalCount === 1 ? 'task' : 'tasks'}`}
        </span>
      </div>

      {!lookupsLoading && projects.length === 0 && !lookupError && canManage && (
        <div className="project-notice">Create a project and sprint before adding tasks.</div>
      )}
      <ErrorMessage>{listError}</ErrorMessage>

      {loading ? (
        <div className="project-state" role="status">Loading tasks…</div>
      ) : listError ? null : tasks.length === 0 ? (
        <div className="project-state project-empty">
          <h2>{search || Object.values(filters).some(Boolean) ? 'No matching tasks' : 'No tasks yet'}</h2>
          <p>{search || Object.values(filters).some(Boolean) ? 'Try changing your search or filters.' : canManage ? 'Create a task to get started.' : 'No tasks are available yet.'}</p>
          {!search && !Object.values(filters).some(Boolean) && projects.length > 0 && canManage && !formOpen && (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>Create your first task</button>
          )}
        </div>
      ) : (
        <div className="task-table-wrap">
          <table className="task-table">
            <thead>
              <tr>
                <th scope="col">Task</th>
                <th scope="col">Project / Sprint</th>
                <th scope="col">Priority</th>
                <th scope="col">Status</th>
                <th scope="col">Due date</th>
                <th scope="col">Progress</th>
                <th scope="col">Active</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {tasks.map((task) => {
                const sprint = taskSprintDetails[task.sprintId] ?? listSprints.get(task.sprintId);
                return (
                  <tr key={task.id}>
                    <td data-label="Task">
                      <strong>{task.title}</strong>
                      <span className="task-description-cell">{task.description || 'No description'}</span>
                    </td>
                    <td data-label="Project / Sprint">
                      <strong>{projectNames.get(sprint?.projectId) || 'Project not available'}</strong>
                      <span className="task-description-cell">{sprint?.name || `Sprint ${task.sprintId}`}</span>
                    </td>
                    <td data-label="Priority">{task.priority}</td>
                    <td data-label="Status">{getTaskDisplayStatus(task) === 'Overdue' ? 'Overdue' : task.status}</td>
                    <td data-label="Due date">{formatDate(task.dueDate)}</td>
                    <td data-label="Progress">{task.progressPercentage}%</td>
                    <td data-label="Active"><span className={`project-status${getTaskActivityStatus(task) === 'Active' ? ' project-status-active' : ''}${getTaskActivityStatus(task) === 'Overdue' ? ' project-status-overdue' : ''}`}>{getTaskActivityStatus(task)}</span></td>
                    <td data-label="Actions">
                      <div className="task-row-actions">
                        {canManage && <button className="button button-secondary" type="button" onClick={() => openEditForm(task)}>Edit</button>}
                        <button
                          className="button button-secondary"
                          type="button"
                          aria-expanded={historyTask?.id === task.id}
                          onClick={() => setHistoryTask((current) => current?.id === task.id ? null : task)}
                        >
                          {historyTask?.id === task.id ? 'Hide history' : 'View history'}
                        </button>
                        {canManage && <button
                          className="button button-secondary"
                          type="button"
                          aria-expanded={riskTask?.id === task.id}
                          disabled={riskEvaluatingTaskId === task.id}
                          onClick={() => setRiskTask((current) => current?.id === task.id ? null : task)}
                        >
                          {riskTask?.id === task.id ? 'Hide risk' : 'Evaluate risk'}
                        </button>}
                        {canManage && <button
                          className="button button-danger"
                          type="button"
                          onClick={() => handleDelete(task)}
                          disabled={deletingId === task.id}
                        >
                          {deletingId === task.id ? 'Deleting…' : 'Delete'}
                        </button>}
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <nav className="project-pagination" aria-label="Task pages">
        <button className="button button-secondary" type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={page <= 1 || loading}>Previous</button>
        <span>Page {page} of {totalPages}</span>
        <button className="button button-secondary" type="button" onClick={() => setPage((current) => current + 1)} disabled={totalPages === 0 || page >= totalPages || loading}>Next</button>
      </nav>
    </section>
  );
}

export default TasksPage;
