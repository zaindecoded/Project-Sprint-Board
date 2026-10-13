import { useCallback, useEffect, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import { useAuth } from '../context/AuthContext';
import { canManageWorkspace } from '../utils/permissions';
import { getApiErrorMessage } from '../services/apiErrors';
import {
  createComment,
  deleteComment,
  getComment,
  getComments,
  updateComment
} from '../services/commentApi';
import { getTask, getTasks } from '../services/taskApi';
import { getUsers } from '../services/userApi';

const PAGE_SIZE = 10;
const MAX_MESSAGE_LENGTH = 2000;

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

function userLabel(userId, currentUser, users = []) {
  if (currentUser?.id === userId) {
    return currentUser.name || currentUser.email || userId;
  }
  const user = users.find((item) => item.id === userId);
  if (user) return `${user.firstName} ${user.lastName}`.trim();
  return userId ? `User · ${String(userId).slice(0, 8)}` : 'Unknown user';
}

function getValidationMessage(error, fallback = 'Unable to save this comment.') {
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

function TaskSelector({ id, value, initialTask, onChange, disabled }) {
  const [search, setSearch] = useState('');
  const [tasks, setTasks] = useState([]);
  const [selectedTask, setSelectedTask] = useState(initialTask ?? null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (initialTask?.id === value) {
      setSelectedTask(initialTask);
    }
  }, [initialTask, value]);

  useEffect(() => {
    if (!value || selectedTask?.id === value) {
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
      getTasks({ page: 1, pageSize: 50, search: search.trim(), signal: controller.signal })
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
  }, [search]);

  const availableTasks = selectedTask?.id === value && !tasks.some((task) => task.id === value)
    ? [selectedTask, ...tasks]
    : tasks;

  return (
    <div className="project-field project-field-wide">
      <label htmlFor={`${id}-search`}>Search tasks</label>
      <input
        id={`${id}-search`}
        type="search"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        placeholder="Search by task title"
        disabled={disabled}
      />
      <label htmlFor={id}>Task <span aria-hidden="true">*</span></label>
      <select
        id={id}
        value={value}
        onChange={(event) => {
          const task = availableTasks.find((item) => item.id === event.target.value) ?? null;
          setSelectedTask(task);
          onChange(event.target.value, task);
        }}
        required
        disabled={disabled || loading}
      >
        <option value="">{loading ? 'Loading tasks…' : 'Select a task'}</option>
        {availableTasks.map((task) => <option key={task.id} value={task.id}>{task.title}</option>)}
      </select>
      <ErrorMessage>{error}</ErrorMessage>
    </div>
  );
}

function TaskFilter({ value, initialTask, onChange }) {
  const [search, setSearch] = useState('');
  const [tasks, setTasks] = useState([]);
  const [selectedTask, setSelectedTask] = useState(initialTask ?? null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    if (initialTask?.id === value) {
      setSelectedTask(initialTask);
    }
  }, [initialTask, value]);

  useEffect(() => {
    if (!value || selectedTask?.id === value) {
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
      getTasks({ page: 1, pageSize: 50, search: search.trim(), signal: controller.signal })
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
  }, [search]);

  const availableTasks = selectedTask?.id === value && !tasks.some((task) => task.id === value)
    ? [selectedTask, ...tasks]
    : tasks;

  return (
    <label className="project-filter comment-task-filter">
      <span>Task</span>
      <input
        type="search"
        value={search}
        onChange={(event) => setSearch(event.target.value)}
        placeholder="Search tasks"
        aria-label="Search tasks for comment filter"
      />
      <select
        aria-label="Filter comments by task"
        value={value}
        onChange={(event) => {
          const task = availableTasks.find((item) => item.id === event.target.value) ?? null;
          setSelectedTask(task);
          onChange(event.target.value);
        }}
        disabled={loading}
      >
        <option value="">{loading ? 'Loading tasks…' : 'All tasks'}</option>
        {availableTasks.map((task) => <option key={task.id} value={task.id}>{task.title}</option>)}
      </select>
      {error && <span className="comment-filter-error">{error}</span>}
    </label>
  );
}

function CommentForm({ comment, currentUser, onCancel, onSaved }) {
  const [form, setForm] = useState({ taskId: '', message: '' });
  const [selectedTask, setSelectedTask] = useState(null);
  const [commentUserId, setCommentUserId] = useState('');
  const [loadingComment, setLoadingComment] = useState(Boolean(comment));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;
    if (!comment) {
      setForm({ taskId: '', message: '' });
      setCommentUserId(currentUser?.id ?? '');
      setLoadingComment(false);
      return () => {
        active = false;
      };
    }

    setLoadingComment(true);
    setError('');
    Promise.all([getComment(comment.id), getTask(comment.taskId)])
      .then(([item, task]) => {
        if (active) {
          setSelectedTask(task);
          setCommentUserId(item.userId);
          setForm({ taskId: item.taskId, message: item.message ?? '' });
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(getApiErrorMessage(requestError, 'Unable to load this comment.'));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingComment(false);
        }
      });

    return () => {
      active = false;
    };
  }, [comment, currentUser?.id]);

  async function handleSubmit(event) {
    event.preventDefault();
    if (saving) {
      return;
    }
    const message = form.message.trim();
    if (!form.taskId || !currentUser?.id || !message) {
      setError('Select a task and enter a comment before saving.');
      return;
    }
    if (message.length > MAX_MESSAGE_LENGTH) {
      setError(`Comment must be ${MAX_MESSAGE_LENGTH} characters or less.`);
      return;
    }

    setSaving(true);
    setError('');
    const body = { taskId: form.taskId, userId: commentUserId || currentUser.id, message };
    try {
      if (comment) {
        await updateComment(comment.id, body);
        onSaved('Comment updated successfully.');
      } else {
        await createComment(body);
        onSaved('Comment created successfully.');
      }
    } catch (requestError) {
      setError(getValidationMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="project-form-card" aria-labelledby="comment-form-title">
      <div className="project-form-heading">
        <div>
          <p className="eyebrow">{comment ? 'Comment details' : 'New comment'}</p>
          <h2 id="comment-form-title">{comment ? 'Edit comment' : 'Create comment'}</h2>
        </div>
        <button className="button button-secondary" type="button" onClick={onCancel} disabled={saving}>Cancel</button>
      </div>
      <ErrorMessage>{error}</ErrorMessage>

      {loadingComment ? (
        <p className="project-loading" role="status">Loading comment details…</p>
      ) : (
        <form className="project-form" onSubmit={handleSubmit}>
          <TaskSelector
            id="comment-task"
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
            <label htmlFor="comment-message">Comment <span aria-hidden="true">*</span></label>
            <textarea
              id="comment-message"
              value={form.message}
              onChange={(event) => {
                setForm((current) => ({ ...current, message: event.target.value }));
                setError('');
              }}
              maxLength={MAX_MESSAGE_LENGTH}
              rows={5}
              required
              disabled={saving}
            />
            <span className="comment-character-count">{form.message.length}/{MAX_MESSAGE_LENGTH}</span>
          </div>
          <div className="project-field project-field-wide">
            <span className="project-field-label">Created by</span>
            <span>{userLabel(commentUserId || currentUser?.id, currentUser)}</span>
          </div>
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving || !currentUser?.id}>
              {saving ? 'Saving…' : comment ? 'Save changes' : 'Create comment'}
            </button>
          </div>
        </form>
      )}
    </section>
  );
}

function CommentsPage() {
  const { user: currentUser } = useAuth();
  const canManage = canManageWorkspace(currentUser);
  const [comments, setComments] = useState([]);
  const [users, setUsers] = useState([]);
  const [taskDetails, setTaskDetails] = useState({});
  const [taskDetailsError, setTaskDetailsError] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [taskId, setTaskId] = useState('');
  const [userId, setUserId] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState('');
  const [notice, setNotice] = useState('');
  const [editingComment, setEditingComment] = useState(null);
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

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  const loadComments = useCallback(async (signal) => {
    setLoading(true);
    setListError('');
    setTaskDetailsError('');
    try {
      const result = await getComments({
        page,
        pageSize: PAGE_SIZE,
        search,
        taskId,
        userId,
        signal
      });
      const items = Array.isArray(result.items) ? result.items : [];
      setComments(items);
      setTotalCount(Number(result.totalCount) || 0);
      setTotalPages(Number(result.totalPages) || 0);
      if (result.page && result.page !== page) {
        setPage(result.page);
      }

      const taskIds = [...new Set([...items.map((item) => item.taskId), taskId].filter(Boolean))];
      const taskResults = await Promise.all(taskIds.map(async (id) => {
        try {
          return { id, task: await getTask(id) };
        } catch (error) {
          return { id, task: null, error };
        }
      }));
      if (!signal?.aborted) {
        setTaskDetails(Object.fromEntries(taskResults.map(({ id, task }) => [id, task])));
        const failedTask = taskResults.find((item) => item.error && item.error.response?.status !== 404);
        if (failedTask) {
          setTaskDetailsError(getApiErrorMessage(failedTask.error, 'Unable to load one or more task details.'));
        }
      }
    } catch (requestError) {
      if (requestError.code !== 'ERR_CANCELED') {
        setListError(getApiErrorMessage(requestError, 'Unable to load comments.'));
      }
    } finally {
      if (!signal?.aborted) {
        setLoading(false);
      }
    }
  }, [page, search, taskId, userId]);

  useEffect(() => {
    const controller = new AbortController();
    loadComments(controller.signal);
    return () => controller.abort();
  }, [loadComments]);

  function openCreateForm() {
    setNotice('');
    setEditingComment(null);
    setFormOpen(true);
  }

  function openEditForm(comment) {
    setNotice('');
    setEditingComment(comment);
    setFormOpen(true);
  }

  async function handleSaved(message) {
    setFormOpen(false);
    setEditingComment(null);
    setNotice(message);
    await loadComments();
  }

  async function handleDelete(comment) {
    if (!window.confirm('Delete this comment? It will be removed from the normal comment list.')) {
      return;
    }
    setDeletingId(comment.id);
    setListError('');
    setNotice('');
    try {
      await deleteComment(comment.id);
      setNotice('Comment deleted successfully.');
      if (comments.length === 1 && page > 1) {
        setPage((current) => current - 1);
      } else {
        await loadComments();
      }
    } catch (error) {
      setListError(getValidationMessage(error, 'Unable to delete this comment.'));
    } finally {
      setDeletingId(null);
    }
  }

  const userIds = [...new Set([
    ...users.map((user) => user.id),
    currentUser?.id,
    userId,
    ...comments.map((comment) => comment.userId)
  ].filter(Boolean))];

  return (
    <section className="projects-page comments-page">
      <div className="projects-heading">
        <div>
          <p className="eyebrow">Workspace</p>
          <h1>Comments</h1>
          <p className="projects-subtitle">{canManage ? 'Read and manage comments on your tasks.' : 'Read task comments and add your own.'}</p>
        </div>
        {!formOpen && (
          <button className="button button-primary" type="button" onClick={openCreateForm} disabled={!currentUser?.id}>
            Create comment
          </button>
        )}
      </div>

      {notice && <div className="project-notice" role="status">{notice}</div>}
      {!currentUser?.id && (
        <ErrorMessage>Your signed-in user details are unavailable, so comments cannot be created.</ErrorMessage>
      )}

      {formOpen && (
        <CommentForm
          key={editingComment?.id ?? 'new'}
          comment={editingComment}
          currentUser={currentUser}
          onCancel={() => {
            setFormOpen(false);
            setEditingComment(null);
          }}
          onSaved={handleSaved}
        />
      )}

      <div className="projects-toolbar task-toolbar">
        <label className="project-search">
          <span>Search comments</span>
          <input
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Search comment, task, or user"
            aria-label="Search comments by text, task, or user"
          />
        </label>
        <TaskFilter
          value={taskId}
          initialTask={taskDetails[taskId]}
          onChange={(value) => {
            setTaskId(value);
            setPage(1);
          }}
        />
        <label className="project-filter">
          <span>Created by</span>
          <select
            value={userId}
            onChange={(event) => {
              setUserId(event.target.value);
              setPage(1);
            }}
          >
            <option value="">All users</option>
            {userIds.map((id) => (
              <option key={id} value={id}>{userLabel(id, currentUser, users)}</option>
            ))}
          </select>
        </label>
        <span className="project-count">{totalCount} {totalCount === 1 ? 'comment' : 'comments'}</span>
      </div>

      <ErrorMessage>{listError}</ErrorMessage>
      <ErrorMessage>{taskDetailsError}</ErrorMessage>

      {loading ? (
        <div className="project-state" role="status">Loading comments…</div>
      ) : listError ? null : comments.length === 0 ? (
        <div className="project-state project-empty">
          <h2>{search || taskId || userId ? 'No matching comments' : 'No comments yet'}</h2>
          <p>{search || taskId || userId ? 'Try changing your search or filters.' : 'Add a comment to a task to get started.'}</p>
          {!search && !taskId && !userId && !formOpen && currentUser?.id && (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>Create your first comment</button>
          )}
        </div>
      ) : (
        <div className="task-table-wrap">
          <table className="task-table comment-table">
            <thead>
              <tr>
                <th scope="col">Task</th>
                <th scope="col">Comment</th>
                <th scope="col">Created by</th>
                <th scope="col">Created date</th>
                <th scope="col">Updated date</th>
                <th scope="col">Status</th>
                <th scope="col">Actions</th>
              </tr>
            </thead>
            <tbody>
              {comments.map((comment) => {
                const task = taskDetails[comment.taskId];
                return (
                  <tr key={comment.id}>
                    <td data-label="Task">
                      <strong>{task?.title || `Task · ${comment.taskId.slice(0, 8)}`}</strong>
                      <span className="task-description-cell">{task?.description || 'No description available'}</span>
                    </td>
                    <td data-label="Comment"><span className="comment-message-cell">{comment.message}</span></td>
                    <td data-label="Created by">{comment.userName || userLabel(comment.userId, currentUser, users)}</td>
                    <td data-label="Created date">{formatDate(comment.createdAt)}</td>
                    <td data-label="Updated date">{formatDate(comment.updatedAt)}</td>
                    <td data-label="Status">
                      <span className={`project-status${comment.isActive ? ' project-status-active' : ''}`}>
                        {comment.isActive ? 'Active' : 'Inactive'}
                      </span>
                    </td>
                    <td data-label="Actions">
                      {canManage && <div className="task-row-actions">
                        <button className="button button-secondary" type="button" onClick={() => openEditForm(comment)}>Edit</button>
                        <button
                          className="button button-danger"
                          type="button"
                          onClick={() => handleDelete(comment)}
                          disabled={deletingId === comment.id}
                        >
                          {deletingId === comment.id ? 'Deleting…' : 'Delete'}
                        </button>
                      </div>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
      )}

      <nav className="project-pagination" aria-label="Comment pages">
        <button className="button button-secondary" type="button" onClick={() => setPage((current) => Math.max(1, current - 1))} disabled={page <= 1 || loading}>Previous</button>
        <span>Page {page} of {totalPages}</span>
        <button className="button button-secondary" type="button" onClick={() => setPage((current) => current + 1)} disabled={totalPages === 0 || page >= totalPages || loading}>Next</button>
      </nav>
    </section>
  );
}

export default CommentsPage;
