import { useCallback, useEffect, useMemo, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import ActiveFilterChips from '../components/ActiveFilterChips';
import { useAuth } from '../context/AuthContext';
import { canManageWorkspace } from '../utils/permissions';
import { getAssignmentDisplayStatus, getTodayDateInputValue } from '../utils/displayStatus';
import { getApiErrorMessage } from '../services/apiErrors';
import {
  createAssignment,
  deleteAssignment,
  getAssignment,
  getAssignments,
  updateAssignment
} from '../services/assignmentApi';
import { completeAssignedTask, getTask, getTasks } from '../services/taskApi';
import { getUsers } from '../services/userApi';

const PAGE_SIZE = 10;

function formatDate(value) {
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
      minute: '2-digit'
    }).format(date);
}

function formatShortDate(value) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
}

function initials(name) {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase();
}

function dateTimeInputValue(value) {
  if (!value) {
    return '';
  }
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) {
    return '';
  }
  return new Date(date.getTime() - date.getTimezoneOffset() * 60000).toISOString().slice(0, 16);
}

function currentDateTimeInputValue() {
  return dateTimeInputValue(new Date());
}

function userLabel(userId, currentUser, users = []) {
  if (currentUser?.id === userId) {
    return currentUser.name || currentUser.email || 'Signed-in user';
  }
  const user = users.find((item) => item.id === userId);
  if (user) return `${user.firstName} ${user.lastName}`.trim();
  return `User · ${userId}`;
}

function getValidationMessage(error, fallback = 'Unable to save this assignment.') {
  const errors = error.response?.data?.errors;
  if (Array.isArray(errors)) {
    const messages = errors.filter((item) => typeof item === 'string');
    if (messages.length) {
      return messages.join(' ');
    }
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

function TaskPicker({ id, label, value, initialTask, onChange, disabled, required = true }) {
  const [searchInput, setSearchInput] = useState('');
  const [tasks, setTasks] = useState([]);
  const [selectedTask, setSelectedTask] = useState(initialTask ?? null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [open, setOpen] = useState(false);

  useEffect(() => {
    if (String(initialTask?.id) === String(value)) {
      setSelectedTask(initialTask);
    }
  }, [initialTask, value]);

  useEffect(() => {
    if (!value || String(selectedTask?.id) === String(value)) {
      return undefined;
    }
    let active = true;
    getTask(value)
      .then((task) => {
        if (active) {
          setSelectedTask(task);
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(getApiErrorMessage(requestError, 'Unable to load the selected task.'));
        }
      });
    return () => {
      active = false;
    };
  }, [selectedTask?.id, value]);

  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setLoading(true);
      setError('');
      getTasks({ page: 1, pageSize: 50, search: searchInput.trim(), signal: controller.signal })
        .then((result) => {
          if (!controller.signal.aborted) {
            setTasks(Array.isArray(result.items) ? result.items : []);
          }
        })
        .catch((requestError) => {
          if (!controller.signal.aborted && requestError.code !== 'ERR_CANCELED') {
            setError(getApiErrorMessage(requestError, 'Unable to load tasks.'));
          }
        })
        .finally(() => {
          if (!controller.signal.aborted) {
            setLoading(false);
          }
        });
    }, 250);
    return () => {
      window.clearTimeout(timer);
      controller.abort();
    };
  }, [searchInput]);

  const availableTasks = useMemo(() => {
    if (selectedTask && String(selectedTask.id) === String(value) && !tasks.some((task) => String(task.id) === String(value))) {
      return [selectedTask, ...tasks];
    }
    return tasks;
  }, [selectedTask, tasks, value]);

  return (
    <div className="project-field project-field-wide assignment-task-picker">
        <label htmlFor={id}>{label}{required && <> <span className="assignment-required" aria-hidden="true">*</span></>}</label>
        <input
          id={id}
          role="combobox"
          aria-autocomplete="list"
          aria-expanded={open}
          aria-controls={`${id}-options`}
          value={String(value) === String(selectedTask?.id) ? selectedTask?.title ?? searchInput : searchInput}
          placeholder="Search and select a task"
          onFocus={() => setOpen(true)}
          onBlur={() => setOpen(false)}
          onChange={(event) => { setSearchInput(event.target.value); setSelectedTask(null); onChange('', null); setOpen(true); }}
          disabled={disabled}
          required={required && !value}
        />
        {open && <ul id={`${id}-options`} className="assignment-task-options" role="listbox">
          {loading ? <li>Loading tasks…</li> : availableTasks.filter((task) => task.title.toLowerCase().includes(searchInput.toLowerCase())).slice(0, 8).map((task) => (
            <li key={task.id}><button type="button" role="option" aria-selected={String(task.id) === String(value)} onMouseDown={(event) => event.preventDefault()} onClick={() => { setSelectedTask(task); setSearchInput(task.title); setOpen(false); onChange(task.id, task); }}>{task.title}</button></li>
          ))}
          {!loading && availableTasks.filter((task) => task.title.toLowerCase().includes(searchInput.toLowerCase())).length === 0 && <li>No tasks found.</li>}
        </ul>}
        {required && !value && <small className="assignment-field-error"></small>}
        {value && <button className="assignment-task-clear" type="button" aria-label="Clear selected task" onClick={() => { setSearchInput(''); setSelectedTask(null); onChange('', null); }}>×</button>}
        <ErrorMessage>{error}</ErrorMessage>
      </div>
  );
}

function TaskFilter({ value, initialTask, onChange }) {
  return <TaskPicker id="assignment-task-filter" label="Task" value={value} initialTask={initialTask} onChange={onChange} required={false} />;
}

function AssignmentForm({ assignment, onCancel, onSaved }) {
  const [form, setForm] = useState({
    taskId: '',
    userId: '',
    assignedAt: currentDateTimeInputValue(),
    roleInTask: 'Assignee',
    isActive: true
  });
  const [selectedTask, setSelectedTask] = useState(null);
  const [users, setUsers] = useState([]);
  const [usersLoading, setUsersLoading] = useState(true);
  const [usersError, setUsersError] = useState('');
  const [loadingAssignment, setLoadingAssignment] = useState(Boolean(assignment));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [duplicateAssignment, setDuplicateAssignment] = useState(false);
  const taskEndDate = selectedTask?.dueDate ? String(selectedTask.dueDate).slice(0, 10) : '';

  useEffect(() => {
    let active = true;
    if (!assignment) {
      setForm({ taskId: '', userId: '', assignedAt: currentDateTimeInputValue(), roleInTask: 'Assignee', isActive: true });
      setLoadingAssignment(false);
      return () => {
        active = false;
      };
    }

    setLoadingAssignment(true);
    setError('');
    Promise.all([getAssignment(assignment.id), getTask(assignment.taskId)])
      .then(([item, task]) => {
        if (active) {
          setSelectedTask(task);
          setForm({
            taskId: item.taskId,
            userId: item.userId,
            assignedAt: dateTimeInputValue(item.assignedAt),
            roleInTask: item.roleInTask ?? '',
            isActive: item.isActive
          });
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(getApiErrorMessage(requestError, 'Unable to load this assignment.'));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingAssignment(false);
        }
      });

    return () => {
      active = false;
    };
  }, [assignment]);

  useEffect(() => {
    const controller = new AbortController();
    getUsers({ signal: controller.signal })
      .then((items) => {
        if (!controller.signal.aborted) {
          setUsers(Array.isArray(items) ? items : []);
        }
      })
      .catch((requestError) => {
        if (!controller.signal.aborted && requestError.code !== 'ERR_CANCELED') {
          setUsersError(getApiErrorMessage(requestError, 'Unable to load project members.'));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setUsersLoading(false);
        }
      });
    return () => controller.abort();
  }, []);

  useEffect(() => {
    if (!form.taskId || !form.userId || !form.roleInTask) {
      setDuplicateAssignment(false);
      return undefined;
    }
    const controller = new AbortController();
    getAssignments({ page: 1, pageSize: 100, taskId: form.taskId, isActive: 'true', signal: controller.signal })
      .then((result) => {
        const duplicate = (result.items || []).some((item) => item.userId === Number(form.userId)
          && item.roleInTask === form.roleInTask && item.id !== assignment?.id);
        if (!controller.signal.aborted) setDuplicateAssignment(duplicate);
      })
      .catch(() => { if (!controller.signal.aborted) setDuplicateAssignment(false); });
    return () => controller.abort();
  }, [assignment?.id, form.roleInTask, form.taskId, form.userId]);

  function changeField(event) {
    const { name, value, type, checked } = event.target;
    setError('');
    setForm((current) => ({ ...current, [name]: type === 'checkbox' ? checked : name === 'userId' ? (value ? Number(value) : '') : value }));
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (saving) {
      return;
    }
    if (!form.taskId || !form.userId || !['Assignee', 'Reviewer', 'Observer'].includes(form.roleInTask)) {
      setError('Select a task, project member, and role before saving.');
      return;
    }
    if (duplicateAssignment) {
      setError('This user already has this role on the selected task.');
      return;
    }
    if (!assignment && form.assignedAt && form.assignedAt.slice(0, 10) < getTodayDateInputValue()) {
      setError('Assignment date must be today or a future date.');
      return;
    }
    const assignmentDate = form.assignedAt ? form.assignedAt.slice(0, 10) : getTodayDateInputValue();
    const assignmentDateChanged = !assignment
      || Number(form.taskId) !== assignment.taskId
      || (form.assignedAt && form.assignedAt.slice(0, 10) !== String(assignment.assignedAt ?? '').slice(0, 10));
    if (assignmentDateChanged && taskEndDate && assignmentDate > taskEndDate) {
      setError('Assignment date cannot be later than the task due date.');
      return;
    }

    setSaving(true);
    setError('');
    const body = {
      taskId: Number(form.taskId),
      userId: Number(form.userId),
      roleInTask: form.roleInTask.trim()
    };
    if (assignment && form.assignedAt) {
      body.assignedAt = new Date(form.assignedAt).toISOString();
    }

    try {
      if (assignment) {
        body.isActive = form.isActive;
        await updateAssignment(assignment.id, body);
        onSaved('Assignment updated successfully.');
      } else {
        await createAssignment({ ...body, roleInTask: body.roleInTask || null });
        onSaved('Assignment created successfully.');
      }
    } catch (requestError) {
      setError(getValidationMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  const memberOptions = users.filter((user) => user.role === 'TeamMember');
  const selectedUserExists = memberOptions.some((user) => String(user.id) === String(form.userId));

  return (
    <section className="assignment-modal" role="dialog" aria-modal="true" aria-labelledby="assignment-form-title">
      <div className="project-form-heading">
        <div>
          <h2 id="assignment-form-title">{assignment ? 'Edit assignment' : 'Create assignment'}</h2>
        </div>
        <button className="assignment-modal-close" type="button" aria-label="Close assignment form" onClick={onCancel} disabled={saving}>×</button>
      </div>
      <ErrorMessage>{error}</ErrorMessage>

      {loadingAssignment ? (
        <p className="project-loading" role="status">Loading assignment details…</p>
      ) : (
        <form className="project-form" onSubmit={handleSubmit}>
          <TaskPicker
            id="assignment-task"
            label="Task"
            value={form.taskId}
            initialTask={selectedTask}
            onChange={(taskId, task) => {
              setForm((current) => ({ ...current, taskId, userId: '' }));
              setSelectedTask(task);
              setError('');
            }}
            disabled={saving}
          />
          <div className="project-field project-field-wide">
            <label htmlFor="assignment-user">Project member <span className="assignment-required" aria-hidden="true">*</span></label>
            <select id="assignment-user" name="userId" value={form.userId} onChange={changeField} required disabled={saving || usersLoading || Boolean(usersError) || !form.taskId}>
              <option value="">{usersLoading ? 'Loading project members…' : 'Select a project member'}</option>
              {memberOptions.map((user) => (
                <option key={user.id} value={user.id}>{`${user.firstName} ${user.lastName} · ${user.email}`}</option>
              ))}
              {form.userId && !selectedUserExists && <option value={form.userId}>{`Current member · ${form.userId}`}</option>}
            </select>
            {form.taskId && !form.userId && <small className="assignment-field-error"></small>}
            <ErrorMessage>{usersError}</ErrorMessage>
          </div>
          {assignment && <div className="project-field">
            <label htmlFor="assignment-assigned-at">Assigned at</label>
            <input
              id="assignment-assigned-at"
              name="assignedAt"
              type="datetime-local"
              value={form.assignedAt}
              onChange={changeField}
              max={taskEndDate ? `${taskEndDate}T23:59` : undefined}
              disabled={saving}
            />
          </div>}
          <div className="project-field">
            <label htmlFor="assignment-role">Role in task <span className="assignment-required" aria-hidden="true">*</span></label>
            <select
              id="assignment-role"
              name="roleInTask"
              value={form.roleInTask}
              onChange={changeField}
              disabled={saving}
              required
            ><option value="">Select a role</option><option>Assignee</option><option>Reviewer</option><option>Observer</option></select>
          </div>
          {duplicateAssignment && <small className="assignment-field-error project-field-wide">This user already has this role on the selected task.</small>}
          {assignment && (
            <div className="project-field project-field-wide">
              <label className="assignment-active-field" htmlFor="assignment-active">
                <input
                  id="assignment-active"
                  name="isActive"
                  type="checkbox"
                  checked={form.isActive}
                  onChange={changeField}
                  disabled={saving}
                />
                Assignment is active
              </label>
              <span className="assignment-help"></span>
            </div>
          )}
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving || usersLoading || !form.taskId || !form.userId || !form.roleInTask || duplicateAssignment}>
              {saving ? 'Saving…' : assignment ? 'Save changes' : 'Create assignment'}
            </button>
          </div>
        </form>
      )}
    </section>
  );
}

function AssignedTaskActions({ task, onUpdated }) {
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  async function complete() {
    setSaving(true);
    setError('');
    try {
      await completeAssignedTask(task.id);
      await onUpdated();
    } catch (requestError) {
      setError(getValidationMessage(requestError, 'Unable to mark this task complete.'));
    } finally {
      setSaving(false);
    }
  }

  if (!task || task.status === 'Done' || task.status === 'Cancelled') return task?.status === 'Done' ? 'Complete' : task?.status === 'Cancelled' ? 'Cancelled' : null;

  return (
    <div className="task-row-actions assignment-task-actions">
      <button className="user-icon-button" type="button" aria-label={saving ? 'Saving completion' : 'Mark task complete'} title="Mark task complete" onClick={complete} disabled={saving}>
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m5 12 4 4L19 6"/></svg>
      </button>
      {error && <span role="alert">{error}</span>}
    </div>
  );
}

function AssignmentsPage() {
  const { user: currentUser } = useAuth();
  const canManage = canManageWorkspace(currentUser);
  const [assignments, setAssignments] = useState([]);
  const [users, setUsers] = useState([]);
  const [taskDetails, setTaskDetails] = useState({});
  const [filters, setFilters] = useState({ taskId: '', userId: '', isActive: '' });
  const [sortBy, setSortBy] = useState('assignedAt');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState('');
  const [taskDetailsError, setTaskDetailsError] = useState('');
  const [notice, setNotice] = useState('');
  const [editingAssignment, setEditingAssignment] = useState(null);
  const [formOpen, setFormOpen] = useState(false);
  const [deletingId, setDeletingId] = useState(null);
  const [assignmentToRemove, setAssignmentToRemove] = useState(null);

  useEffect(() => {
    if (!formOpen && !assignmentToRemove) return undefined;
    function closeOnEscape(event) {
      if (event.key === 'Escape') {
        if (assignmentToRemove) setAssignmentToRemove(null);
        else setFormOpen(false);
      }
    }
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [assignmentToRemove, formOpen]);

  useEffect(() => {
    if (!canManage) return undefined;
    const controller = new AbortController();
    getUsers({ signal: controller.signal })
      .then((items) => {
        if (!controller.signal.aborted) setUsers(Array.isArray(items) ? items : []);
      })
      .catch(() => {});
    return () => controller.abort();
  }, [canManage]);

  const loadAssignments = useCallback(async (signal) => {
    setLoading(true);
    setListError('');
    setTaskDetailsError('');
    try {
      const result = await getAssignments({ page, pageSize: PAGE_SIZE, ...filters, sortBy, signal });
      const items = Array.isArray(result.items) ? result.items : [];
      setAssignments(items);
      setTotalCount(Number(result.totalCount) || 0);
      setTotalPages(Number(result.totalPages) || 0);
      if (result.page && result.page !== page) {
        setPage(result.page);
      }

      const taskIds = [...new Set([...items.map((item) => item.taskId), filters.taskId].filter(Boolean))];
      const detailResults = await Promise.all(taskIds.map(async (taskId) => {
        try {
          return { taskId, task: await getTask(taskId) };
        } catch (error) {
          return { taskId, task: null, error };
        }
      }));
      if (!signal?.aborted) {
        setTaskDetails(Object.fromEntries(detailResults.map(({ taskId, task }) => [taskId, task])));
        const failedTask = detailResults.find((item) => item.error && item.error.response?.status !== 404);
        if (failedTask) {
          setTaskDetailsError(getApiErrorMessage(failedTask.error, 'Unable to load one or more task details.'));
        }
      }
    } catch (requestError) {
      if (requestError.code !== 'ERR_CANCELED') {
        setListError(getApiErrorMessage(requestError, 'Unable to load assignments.'));
      }
    } finally {
      if (!signal?.aborted) {
        setLoading(false);
      }
    }
  }, [filters, page, sortBy]);

  useEffect(() => {
    const controller = new AbortController();
    loadAssignments(controller.signal);
    return () => controller.abort();
  }, [loadAssignments]);

  function updateFilter(name, value) {
    setPage(1);
    setFilters((current) => ({ ...current, [name]: value }));
  }

  function clearFilters() {
    setFilters({ taskId: '', userId: '', isActive: '' });
    setPage(1);
  }

  function toggleSort(column) {
    setPage(1);
    setSortBy((current) => current === column ? `-${column}` : column);
  }

  function openCreateForm() {
    setNotice('');
    setEditingAssignment(null);
    setFormOpen(true);
  }

  function openEditForm(assignment) {
    setNotice('');
    setEditingAssignment(assignment);
    setFormOpen(true);
  }

  async function handleSaved(message) {
    setFormOpen(false);
    setEditingAssignment(null);
    setNotice(message);
    await loadAssignments();
  }

  async function handleDelete(assignment) {
    setDeletingId(assignment.id);
    setListError('');
    setNotice('');
    try {
      await deleteAssignment(assignment.id);
      setAssignmentToRemove(null);
      setNotice('Assignment removed successfully.');
      if (assignments.length === 1 && page > 1) {
        setPage((current) => current - 1);
      } else {
        await loadAssignments();
      }
    } catch (error) {
      setListError(getValidationMessage(error, 'Unable to remove this assignment.'));
    } finally {
      setDeletingId(null);
    }
  }

  const userIds = [...new Set([
    ...users.map((user) => user.id),
    currentUser?.id,
    filters.userId,
    ...assignments.map((item) => item.userId)
  ].filter(Boolean))];
  return (
    <section className="projects-page assignments-page">
      <div className="projects-heading">
        <div>
          <h1>Assignments</h1>
        </div>
        {canManage && !formOpen && (
          <button className="button button-primary" type="button" onClick={openCreateForm} disabled={!currentUser?.id}>
            Create assignment
          </button>
        )}
      </div>

      {notice && <div className="assignment-toast" role="status">{notice}</div>}
      {!currentUser?.id && (
        <ErrorMessage>Your signed-in user details are unavailable, so assignments cannot be created.</ErrorMessage>
      )}

      {canManage && formOpen && (
        <div className="assignment-modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) { setFormOpen(false); setEditingAssignment(null); } }}>
        <AssignmentForm
          key={editingAssignment?.id ?? 'new'}
          assignment={editingAssignment}
          onCancel={() => {
            setFormOpen(false);
            setEditingAssignment(null);
          }}
          onSaved={handleSaved}
        />
        </div>
      )}

      <div className="assignments-toolbar">
        <span className="project-count">{loading ? 'Loading…' : `${totalCount ? (page - 1) * PAGE_SIZE + 1 : 0}-${Math.min(page * PAGE_SIZE, totalCount)} of ${totalCount}`}</span>
      </div>

      <ActiveFilterChips filters={[
        ...(filters.taskId ? [{ key: 'task', label: 'Task', value: taskDetails[filters.taskId]?.title || `Task ${filters.taskId}`, onRemove: () => updateFilter('taskId', '') }] : []),
        ...(filters.userId ? [{ key: 'user', label: 'User', value: userLabel(Number(filters.userId), currentUser, users), onRemove: () => updateFilter('userId', '') }] : []),
        ...(filters.isActive ? [{ key: 'status', label: 'Status', value: filters.isActive === 'true' ? 'Active' : 'Complete', onRemove: () => updateFilter('isActive', '') }] : [])
      ]} onClearAll={clearFilters} />

      <ErrorMessage>{listError}</ErrorMessage>
      <ErrorMessage>{taskDetailsError}</ErrorMessage>

      {loading ? (
        <div className="project-state" role="status">Loading assignments…</div>
      ) : listError ? null : assignments.length === 0 ? (
        <div className="project-state project-empty">
          <h2>{Object.values(filters).some(Boolean) ? 'No matching assignments' : 'No assignments yet'}</h2>
          <p>{Object.values(filters).some(Boolean) ? 'Try changing your filters.' : canManage ? 'Create an assignment to get started.' : 'No assignments are available yet.'}</p>
          {!Object.values(filters).some(Boolean) && canManage && !formOpen && currentUser?.id && (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>Create your first assignment</button>
          )}
        </div>
      ) : (
        <div className="task-table-wrap">
          <table className="task-table">
            <thead>
              <tr>
                <th scope="col"><button className="assignment-sort" type="button" onClick={() => toggleSort('task')}>Task{sortBy.endsWith('task') ? (sortBy.startsWith('-') ? ' ↓' : ' ↑') : ''}</button></th>
                <th scope="col"><button className="assignment-sort" type="button" onClick={() => toggleSort('user')}>Assigned user{sortBy.endsWith('user') ? (sortBy.startsWith('-') ? ' ↓' : ' ↑') : ''}</button></th>
                <th scope="col">Role in task</th>
                <th scope="col"><button className="assignment-sort" type="button" onClick={() => toggleSort('assignedAt')}>Assigned at{sortBy.toLowerCase().endsWith('assignedat') ? (sortBy.startsWith('-') ? ' ↓' : ' ↑') : ''}</button></th>
                <th scope="col" className="assignment-status-cell">Status</th>
                <th scope="col" className="assignment-actions-cell">Actions</th>
              </tr>
            </thead>
            <tbody>
              {assignments.map((assignment) => {
                const task = taskDetails[assignment.taskId];
                const displayStatus = getAssignmentDisplayStatus(assignment, task);
                return (
                  <tr key={assignment.id}>
                    <td data-label="Task">
                      <strong>{task?.title || `Task · ${assignment.taskId}`}</strong>
                      {task?.description ? <span className="task-description-cell">{task.description}</span> : <span className="assignment-no-description">No description</span>}
                    </td>
                    <td data-label="Assigned user"><span className="assignment-user-cell"><span className="assignment-avatar" aria-hidden="true">{initials(assignment.userName || userLabel(assignment.userId, currentUser, users))}</span>{assignment.userName || userLabel(assignment.userId, currentUser, users)}</span></td>
                    <td data-label="Role in task"><span className={`assignment-role-badge assignment-role-${String(assignment.roleInTask || '').toLowerCase()}`}>{assignment.roleInTask || '—'}</span></td>
                    <td data-label="Assigned at" title={formatDate(assignment.assignedAt)}>{formatShortDate(assignment.assignedAt)}</td>
                    <td data-label="Status" className="assignment-status-cell">
                      <span className={`assignment-status-badge ${assignment.isActive ? 'assignment-status-active' : 'assignment-status-complete'}`}>
                        {assignment.isActive ? 'Active' : 'Complete'}
                      </span>
                    </td>
                    <td data-label="Actions" className="assignment-actions-cell">
                      {canManage && <div className="task-row-actions">
                        <button className="user-icon-button" type="button" aria-label="Edit" title="Edit" onClick={() => openEditForm(assignment)}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z"/></svg></button>
                        <button className="user-icon-button assignment-remove-icon" type="button" aria-label="Remove assignment" title="Remove assignment" onClick={() => setAssignmentToRemove(assignment)} disabled={deletingId === assignment.id}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h18M8 6V4h8v2m3 0-1 14H6L5 6m4 4v6m6-6v6"/></svg></button>
                      </div>}
                      {currentUser?.role === 'TeamMember' && task && <AssignedTaskActions task={task} onUpdated={() => loadAssignments()} />}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <nav className="project-pagination" aria-label="Assignment pages">
        <button className="button button-secondary" type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={page <= 1 || loading}>Previous</button>
        <span>Page {page} of {totalPages}</span>
        <button className="button button-secondary" type="button" onClick={() => setPage((current) => current + 1)} disabled={totalPages === 0 || page >= totalPages || loading}>Next</button>
      </nav>
      {assignmentToRemove && <div className="assignment-confirm-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setAssignmentToRemove(null); }}><section className="assignment-confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="assignment-remove-title"><h2 id="assignment-remove-title">Remove assignment?</h2><p>Remove {userLabel(assignmentToRemove.userId, currentUser, users)} from {taskDetails[assignmentToRemove.taskId]?.title || 'this task'}? This can't be undone.</p><div><button className="button button-secondary" type="button" onClick={() => setAssignmentToRemove(null)}>Cancel</button><button className="button assignment-confirm-delete" type="button" disabled={deletingId === assignmentToRemove.id} onClick={() => handleDelete(assignmentToRemove)}>{deletingId === assignmentToRemove.id ? 'Removing…' : 'Remove assignment'}</button></div></section></div>}
    </section>
  );
}

export default AssignmentsPage;
