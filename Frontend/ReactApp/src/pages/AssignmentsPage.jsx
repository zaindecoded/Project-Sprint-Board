import { useCallback, useEffect, useMemo, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import { useAuth } from '../context/AuthContext';
import { canManageWorkspace } from '../utils/permissions';
import { getAssignmentDisplayStatus, getTodayDateInputValue, getTodayDateTimeMin } from '../utils/displayStatus';
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

function TaskPicker({ id, label, value, initialTask, onChange, disabled }) {
  const [searchInput, setSearchInput] = useState('');
  const [tasks, setTasks] = useState([]);
  const [selectedTask, setSelectedTask] = useState(initialTask ?? null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

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
    <>
      <div className="project-field project-field-wide">
        <label htmlFor={`${id}-search`}>Search tasks</label>
        <input
          id={`${id}-search`}
          type="search"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder="Search by task title"
          disabled={disabled}
        />
      </div>
      <div className="project-field project-field-wide">
        <label htmlFor={id}>{label} <span aria-hidden="true">*</span></label>
        <select
          id={id}
          value={value}
          onChange={(event) => {
            const task = tasks.find((item) => String(item.id) === event.target.value)
              ?? (String(selectedTask?.id) === event.target.value ? selectedTask : null);
            setSelectedTask(task);
            onChange(event.target.value ? Number(event.target.value) : '', task);
          }}
          required
          disabled={disabled || loading}
        >
          <option value="">{loading ? 'Loading tasks…' : 'Select a task'}</option>
          {availableTasks.map((task) => (
            <option key={task.id} value={task.id}>{task.title}</option>
          ))}
        </select>
        <ErrorMessage>{error}</ErrorMessage>
      </div>
    </>
  );
}

function TaskFilter({ value, initialTask, onChange }) {
  const [searchInput, setSearchInput] = useState('');
  const [tasks, setTasks] = useState([]);
  const [selectedTask, setSelectedTask] = useState(initialTask ?? null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

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
    <div className="assignment-task-filter-wrap">
      <label className="project-filter assignment-task-filter">
        <span>Task</span>
        <input
          type="search"
          value={searchInput}
          onChange={(event) => setSearchInput(event.target.value)}
          placeholder="Search tasks"
          aria-label="Search tasks for assignment filter"
        />
        <select
          aria-label="Filter assignments by task"
          value={value}
          onChange={(event) => {
            const task = availableTasks.find((item) => String(item.id) === event.target.value) ?? null;
            setSelectedTask(task);
            onChange(event.target.value ? Number(event.target.value) : '');
          }}
          disabled={loading}
        >
          <option value="">{loading ? 'Loading tasks…' : 'All tasks'}</option>
          {availableTasks.map((task) => <option key={task.id} value={task.id}>{task.title}</option>)}
        </select>
      </label>
      <ErrorMessage>{error}</ErrorMessage>
    </div>
  );
}

function AssignmentForm({ assignment, onCancel, onSaved }) {
  const [form, setForm] = useState({
    taskId: '',
    userId: '',
    assignedAt: '',
    roleInTask: '',
    isActive: true
  });
  const [selectedTask, setSelectedTask] = useState(null);
  const [users, setUsers] = useState([]);
  const [usersLoading, setUsersLoading] = useState(true);
  const [usersError, setUsersError] = useState('');
  const [loadingAssignment, setLoadingAssignment] = useState(Boolean(assignment));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const taskEndDate = selectedTask?.dueDate ? String(selectedTask.dueDate).slice(0, 10) : '';

  useEffect(() => {
    let active = true;
    if (!assignment) {
      setForm((current) => ({ ...current, userId: '' }));
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
    if (!form.taskId || !form.userId) {
      setError('Select a task and user before saving.');
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
    if (form.assignedAt) {
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
    <section className="project-form-card" aria-labelledby="assignment-form-title">
      <div className="project-form-heading">
        <div>
          <p className="eyebrow">{assignment ? 'Assignment details' : 'New assignment'}</p>
          <h2 id="assignment-form-title">{assignment ? 'Edit assignment' : 'Create assignment'}</h2>
        </div>
        <button className="button button-secondary" type="button" onClick={onCancel} disabled={saving}>Cancel</button>
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
              setForm((current) => ({ ...current, taskId }));
              setSelectedTask(task);
              setError('');
            }}
            disabled={saving}
          />
          <div className="project-field project-field-wide">
            <label htmlFor="assignment-user">Project member <span aria-hidden="true">*</span></label>
            <select id="assignment-user" name="userId" value={form.userId} onChange={changeField} required disabled={saving || usersLoading || Boolean(usersError)}>
              <option value="">{usersLoading ? 'Loading project members…' : 'Select a project member'}</option>
              {memberOptions.map((user) => (
                <option key={user.id} value={user.id}>{`${user.firstName} ${user.lastName} · ${user.email}`}</option>
              ))}
              {form.userId && !selectedUserExists && <option value={form.userId}>{`Current assignee · ${form.userId}`}</option>}
            </select>
            <ErrorMessage>{usersError}</ErrorMessage>
          </div>
          <div className="project-field">
            <label htmlFor="assignment-assigned-at">Assigned at</label>
            <input
              id="assignment-assigned-at"
              name="assignedAt"
              type="datetime-local"
              value={form.assignedAt}
              onChange={changeField}
              min={!assignment ? getTodayDateTimeMin() : undefined}
              max={taskEndDate ? `${taskEndDate}T23:59` : undefined}
              disabled={saving}
            />
          </div>
          <div className="project-field">
            <label htmlFor="assignment-role">Role in task</label>
            <input
              id="assignment-role"
              name="roleInTask"
              value={form.roleInTask}
              onChange={changeField}
              maxLength={100}
              disabled={saving}
            />
          </div>
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
              <span className="assignment-help">Deactivating an assignment records its unassigned time.</span>
            </div>
          )}
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving || !form.userId}>
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
      <button className="button button-primary" type="button" onClick={complete} disabled={saving}>
        {saving ? 'Saving…' : 'Mark complete'}
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
      const result = await getAssignments({ page, pageSize: PAGE_SIZE, ...filters, signal });
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
  }, [filters, page]);

  useEffect(() => {
    const controller = new AbortController();
    loadAssignments(controller.signal);
    return () => controller.abort();
  }, [loadAssignments]);

  function updateFilter(name, value) {
    setPage(1);
    setFilters((current) => ({ ...current, [name]: value }));
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
    const taskTitle = taskDetails[assignment.taskId]?.title || 'this task';
    if (!window.confirm(`Remove this assignment from "${taskTitle}"? It will be soft-deleted and marked inactive.`)) {
      return;
    }
    setDeletingId(assignment.id);
    setListError('');
    setNotice('');
    try {
      await deleteAssignment(assignment.id);
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

      {notice && <div className="project-notice" role="status">{notice}</div>}
      {!currentUser?.id && (
        <ErrorMessage>Your signed-in user details are unavailable, so assignments cannot be created.</ErrorMessage>
      )}

      {canManage && formOpen && (
        <AssignmentForm
          key={editingAssignment?.id ?? 'new'}
          assignment={editingAssignment}
          onCancel={() => {
            setFormOpen(false);
            setEditingAssignment(null);
          }}
          onSaved={handleSaved}
        />
      )}

      <div className="projects-toolbar task-toolbar">
        <TaskFilter
          value={filters.taskId}
          initialTask={taskDetails[filters.taskId]}
          onChange={(value) => updateFilter('taskId', value)}
        />
        {canManage && <label className="project-filter">
          <span>User</span>
          <select value={filters.userId} onChange={(event) => updateFilter('userId', event.target.value)}>
            <option value="">All users</option>
            {userIds.map((userId) => <option key={userId} value={userId}>{userLabel(userId, currentUser, users)}</option>)}
          </select>
        </label>}
        {canManage && <label className="project-filter">
          <span>Assignment status</span>
          <select value={filters.isActive} onChange={(event) => updateFilter('isActive', event.target.value)}>
            <option value="">All assignments</option>
            <option value="true">Active</option>
            <option value="false">Inactive</option>
          </select>
        </label>}
        <span className="project-count">{totalCount} {totalCount === 1 ? 'assignment' : 'assignments'}</span>
      </div>

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
                <th scope="col">Task</th>
                <th scope="col">Assigned user</th>
                <th scope="col">Role in task</th>
                <th scope="col">Assigned at</th>
                <th scope="col">Unassigned at</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
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
                      <span className="task-description-cell">{task?.description || 'No description available'}</span>
                    </td>
                    <td data-label="Assigned user">{assignment.userName || userLabel(assignment.userId, currentUser, users)}</td>
                    <td data-label="Role in task">{assignment.roleInTask || '—'}</td>
                    <td data-label="Assigned at">{formatDate(assignment.assignedAt)}</td>
                    <td data-label="Unassigned at">{formatDate(assignment.unassignedAt)}</td>
                    <td data-label="Status">
                      <span className={`project-status${displayStatus === 'Active' ? ' project-status-active' : ''}${displayStatus === 'Overdue' ? ' project-status-overdue' : ''}`}>
                        {displayStatus}
                      </span>
                    </td>
                    <td data-label="Actions">
                      {canManage && <div className="task-row-actions">
                        <button className="button button-secondary" type="button" onClick={() => openEditForm(assignment)}>Edit</button>
                        <button
                          className="button button-danger"
                          type="button"
                          onClick={() => handleDelete(assignment)}
                          disabled={deletingId === assignment.id}
                        >
                          {deletingId === assignment.id ? 'Removing…' : 'Delete'}
                        </button>
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
    </section>
  );
}

export default AssignmentsPage;
