import { useCallback, useEffect, useMemo, useRef, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import { getProjects } from '../services/projectApi';
import { getSprint, getSprints } from '../services/sprintApi';
import { getTaskStatusHistory } from '../services/statusHistoryApi';
import { evaluateTaskRisk, getLatestTaskRisk } from '../services/intelligenceApi';
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
  const [loadingResult, setLoadingResult] = useState(true);
  const [error, setError] = useState('');
  const requestController = useRef(null);
  const hasDueDate = Boolean(task.dueDate);

  useEffect(() => {
    const controller = new AbortController();
    getLatestTaskRisk(task.id, controller.signal)
      .then(setResult)
      .catch((requestError) => {
        if (requestError.code !== 'ERR_CANCELED') {
          setError(getApiErrorMessage(requestError, 'Unable to load saved risk evaluation.'));
        }
      })
      .finally(() => setLoadingResult(false));
    return () => {
      controller.abort();
      requestController.current?.abort();
    };
  }, [task.id]);

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
    <section className="task-risk-panel" role="dialog" aria-modal="true" aria-labelledby="task-risk-title">
      <div className="task-history-heading">
        <div>
          <p className="eyebrow">Task advisory</p>
          <h2 id="task-risk-title">Delay risk: {task.title}</h2>
        </div>
        <button className="task-drawer-close" type="button" aria-label="Close risk evaluation" onClick={onClose} disabled={evaluating}>×</button>
      </div>

     
      <ErrorMessage>{error}</ErrorMessage>

      {loadingResult ? <p>Loading saved evaluation...</p> : !result ? (
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
  const selectedSprint = sprints.find((item) => String(item.id) === String(form.sprintId))
    ?? (String(existingSprint?.id) === String(form.sprintId) ? existingSprint : null);
  const sprintEndDate = selectedSprint?.endDate ? dateInputValue(selectedSprint.endDate) : '';
  const valid = Boolean(form.projectId && form.sprintId && form.title.trim() && Number(form.estimateHours) > 0 && (!form.startDate || !form.dueDate || form.dueDate >= form.startDate) && (!task || (Number.isInteger(Number(form.progressPercentage)) && Number(form.progressPercentage) >= 0 && Number(form.progressPercentage) <= 100)));

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
      [name]: name === 'projectId' || name === 'sprintId' ? (value ? Number(value) : '') : value,
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
      sprintId: Number(form.sprintId),
      title: form.title.trim(),
      description: form.description.trim(),
      priority: form.priority,
      status: form.status,
      estimateHours: Number(form.estimateHours),
      progressPercentage: progress,
      startDate: form.startDate || null,
      dueDate: form.dueDate || null,
      blockerInformation: form.blockerInformation.trim()
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
    <section className="task-drawer" role="dialog" aria-modal="true" aria-labelledby="task-form-title">
      <div className="project-form-heading">
        <div>
          <p className="eyebrow">{task ? 'Task details' : 'New task'}</p>
          <h2 id="task-form-title">{task ? 'Edit task' : 'Create task'}</h2>
        </div>
        <button className="task-drawer-close" type="button" aria-label="Close task form" onClick={onCancel} disabled={saving}>×</button>
      </div>
      <ErrorMessage>{error}</ErrorMessage>

      {loadingTask ? (
        <p className="project-loading" role="status">Loading task details…</p>
      ) : (
        <form className="project-form" onSubmit={handleSubmit}>
          <h3 className="task-form-section">Basics</h3>
          <div className="project-field">
            <label htmlFor="task-project">Project <span className="task-required" aria-hidden="true">*</span></label>
            <select id="task-project" name="projectId" value={form.projectId} onChange={changeField} required disabled={saving}>
              <option value="">Select a project</option>
              {projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
            </select>
            {!form.projectId && <small className="task-field-error">Select a project.</small>}
          </div>
          <div className="project-field">
            <label htmlFor="task-sprint">Sprint <span className="task-required" aria-hidden="true">*</span></label>
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
            {form.projectId && !form.sprintId && <small className="task-field-error">Select a sprint.</small>}
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="task-title">Title <span className="task-required" aria-hidden="true">*</span></label>
            <input id="task-title" name="title" value={form.title} onChange={changeField} maxLength={200} required disabled={saving} />
            {!form.title.trim() && <small className="task-field-error">Title is required.</small>}
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="task-description">Description</label>
            <textarea id="task-description" name="description" value={form.description} onChange={changeField} maxLength={2000} rows={3} disabled={saving} />
          </div>
          <h3 className="task-form-section">Planning</h3>
          <div className="project-field">
            <label htmlFor="task-priority">Priority <span className="task-required" aria-hidden="true">*</span></label>
            <select id="task-priority" name="priority" value={form.priority} onChange={changeField} required disabled={saving}>
              {TASK_PRIORITIES.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
            </select>
          </div>
          <div className="project-field">
            <label htmlFor="task-status">Status <span className="task-required" aria-hidden="true">*</span></label>
            <select id="task-status" name="status" value={form.status} onChange={changeField} required disabled={saving}>
              {TASK_STATUSES.map((status) => <option key={status} value={status}>{status}</option>)}
            </select>
          </div>
          <div className="project-field">
            <label htmlFor="task-estimate">Estimate (hours) <span className="task-required" aria-hidden="true">*</span></label>
            <input id="task-estimate" name="estimateHours" type="number" min="0.01" step="0.01" value={form.estimateHours} onChange={changeField} required disabled={saving} />
            {(!form.estimateHours || Number(form.estimateHours) <= 0) && <small className="task-field-error">Enter a positive estimate.</small>}
          </div>
          {task && <div className="project-field">
            <label htmlFor="task-progress">Progress (%)</label>
            <input id="task-progress" name="progressPercentage" type="number" min="0" max="100" step="1" value={form.progressPercentage} onChange={changeField} required disabled={saving} />
          </div>}
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
            {form.startDate && form.dueDate && form.dueDate < form.startDate && <small className="task-field-error">Due date must be on or after the start date.</small>}
          </div>
          {form.status === 'Blocked' && <><h3 className="task-form-section">Blocker</h3><div className="project-field project-field-wide">
            <label htmlFor="task-blocker">Blocker information</label>
            <textarea id="task-blocker" name="blockerInformation" value={form.blockerInformation} onChange={changeField} maxLength={1000} rows={2} disabled={saving} />
          </div></>}
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving || sprintsLoading || !valid}>
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
  const [filters, setFilters] = useState({ projectId: '', sprintId: '', status: '', priority: '', dueDatePreset: '', isBlocked: '' });
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
  const [moreFiltersOpen, setMoreFiltersOpen] = useState(false);

  useEffect(() => {
    if (!formOpen) return undefined;
    function closeOnEscape(event) {
      if (event.key === 'Escape') setFormOpen(false);
    }
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [formOpen]);

  useEffect(() => {
    if (!riskTask) return undefined;
    function closeOnEscape(event) {
      if (event.key === 'Escape') setRiskTask(null);
    }
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [riskTask]);

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

  function clearFilters() {
    setSearchInput('');
    setSearch('');
    setFilters({ projectId: '', sprintId: '', status: '', priority: '', dueDatePreset: '', isBlocked: '' });
    setPage(1);
  }

  const projectNames = useMemo(() => new Map(projects.map((project) => [project.id, project.name])), [projects]);
  const listSprints = useMemo(() => new Map(sprints.map((sprint) => [sprint.id, sprint])), [sprints]);
  const filterLabels = {
    projectId: ['Project', projectNames.get(Number(filters.projectId))],
    sprintId: ['Sprint', listSprints.get(Number(filters.sprintId))?.name],
    status: ['Status', ({ Todo: 'To Do', InProgress: 'In Progress', Done: 'Completed' })[filters.status] || filters.status],
    priority: ['Priority', filters.priority],
    dueDatePreset: ['Due date', ({ overdue: 'Overdue', today: 'Due today', thisWeek: 'Due this week', noDueDate: 'No due date' })[filters.dueDatePreset]],
    isBlocked: ['Blocked', filters.isBlocked === 'true' ? 'Blocked only' : 'Not blocked']
  };
  const activeFilters = Object.entries(filterLabels).filter(([key, [, value]]) => Boolean(filters[key] && value));

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
        <div className="task-drawer-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) { setFormOpen(false); setEditingTask(null); } }}>
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
        </div>
      )}

      {historyTask && (
        <TaskHistoryPanel
          key={historyTask.id}
          task={historyTask}
          onClose={() => setHistoryTask(null)}
        />
      )}

      {canManage && riskTask && (
        <div className="risk-drawer-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setRiskTask(null); }}>
        <TaskRiskPanel
          key={riskTask.id}
          task={riskTask}
          onEvaluatingChange={(isEvaluating) => setRiskEvaluatingTaskId(isEvaluating ? riskTask.id : null)}
          onClose={() => setRiskTask(null)}
        />
        </div>
      )}

      <div className="tasks-filter-bar">
        <div className="tasks-filter-controls">
          <input className="tasks-filter-search" type="search" value={searchInput} onChange={(event) => setSearchInput(event.target.value)} placeholder="Search tasks" aria-label="Search tasks by title or description" />
          <select aria-label="Filter by project" value={filters.projectId} onChange={(event) => updateFilter('projectId', event.target.value)} disabled={lookupsLoading}>
            <option value="">All projects</option>{projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
          </select>
          <select aria-label="Filter by sprint" value={filters.sprintId} onChange={(event) => updateFilter('sprintId', event.target.value)} disabled={!filters.projectId}>
            <option value="">All sprints</option>{sprints.map((sprint) => <option key={sprint.id} value={sprint.id}>{sprint.name}</option>)}
          </select>
          <select aria-label="Filter by status" value={filters.status} onChange={(event) => updateFilter('status', event.target.value)}>
            <option value="">All statuses</option>{TASK_STATUSES.map((status) => <option key={status} value={status}>{({ Todo: 'To Do', InProgress: 'In Progress', Done: 'Completed' })[status] || status}</option>)}
          </select>
          <select aria-label="Filter by priority" value={filters.priority} onChange={(event) => updateFilter('priority', event.target.value)}>
            <option value="">All priorities</option>{TASK_PRIORITIES.map((priority) => <option key={priority} value={priority}>{priority}</option>)}
          </select>
          <div className="tasks-more-wrap">
            <button className="tasks-more-button" type="button" aria-expanded={moreFiltersOpen} onClick={() => setMoreFiltersOpen((open) => !open)}>More filters</button>
            {moreFiltersOpen && <div className="tasks-more-popover">
              <label>Due date<select value={filters.dueDatePreset} onChange={(event) => updateFilter('dueDatePreset', event.target.value)}><option value="">Any time</option><option value="overdue">Overdue</option><option value="today">Due today</option><option value="thisWeek">Due this week</option><option value="noDueDate">No due date</option></select></label>
              <label>Blocked<select value={filters.isBlocked} onChange={(event) => updateFilter('isBlocked', event.target.value)}><option value="">All</option><option value="true">Blocked only</option></select></label>
            </div>}
          </div>
        </div>
        <span className="project-count">{loading || searchInput.trim() !== search ? 'Loading tasks…' : `Showing ${totalCount ? (page - 1) * PAGE_SIZE + 1 : 0}-${Math.min(page * PAGE_SIZE, totalCount)} of ${totalCount}`}</span>
      </div>

      {(activeFilters.length > 0 || search) && <div className="tasks-active-filters" aria-label="Active filters">
        {search && <button type="button" className="tasks-filter-chip" onClick={() => { setSearchInput(''); setSearch(''); setPage(1); }}>Search: {search} <span aria-hidden="true">×</span></button>}
        {activeFilters.map(([key, [label, value]]) => <button key={key} type="button" className="tasks-filter-chip" onClick={() => updateFilter(key, '')}>{label}: {value} <span aria-hidden="true">×</span></button>)}
        <button type="button" className="tasks-clear-filters" onClick={clearFilters}>Clear all</button>
      </div>}

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
                    <td data-label="Actions">
                      <div className="task-row-actions">
                        {canManage && <button className="user-icon-button" type="button" aria-label="Edit task" title="Edit task" onClick={() => openEditForm(task)}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z"/></svg></button>}
                        <button
                          className="user-icon-button"
                          type="button"
                          aria-label={historyTask?.id === task.id ? 'Hide task history' : 'View task history'}
                          title={historyTask?.id === task.id ? 'Hide history' : 'View history'}
                          aria-expanded={historyTask?.id === task.id}
                          onClick={() => setHistoryTask((current) => current?.id === task.id ? null : task)}
                        >
                          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 12a9 9 0 1 0 3-6.7L3 8"/><path d="M3 3v5h5m4-1v5l3 2"/></svg>
                        </button>
                        {canManage && !['done', 'cancelled'].includes(String(task.status).toLowerCase()) && <button
                          className="user-icon-button"
                          type="button"
                          aria-label={riskTask?.id === task.id ? 'Hide risk evaluation' : 'Evaluate task risk'}
                          title={riskTask?.id === task.id ? 'Hide risk' : 'Evaluate risk'}
                          aria-expanded={riskTask?.id === task.id}
                          disabled={riskEvaluatingTaskId === task.id}
                          onClick={() => setRiskTask((current) => current?.id === task.id ? null : task)}
                        >
                          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m12 3 10 18H2L12 3Z"/><path d="M12 9v5m0 3h.01"/></svg>
                        </button>}
                        {canManage && <button
                          className="user-icon-button"
                          type="button"
                          aria-label={deletingId === task.id ? 'Deleting task' : 'Delete task'}
                          title="Delete task"
                          onClick={() => handleDelete(task)}
                          disabled={deletingId === task.id}
                        >
                          <svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h18M8 6V4h8v2m3 0-1 14H6L5 6m4 4v6m6-6v6"/></svg>
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
