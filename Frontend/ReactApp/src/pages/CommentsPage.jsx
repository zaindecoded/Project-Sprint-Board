import { useCallback, useEffect, useRef, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import FormField from '../components/FormField';
import { StatusBadge } from '../components/badges';
import { EmptyState, LoadingState } from '../components/LoadingEmptyState';
import ActiveFilterChips from '../components/ActiveFilterChips';
import Pagination from '../components/Pagination';
import Delete from '../components/Delete';
import SearchFilterToolbar from '../components/SearchFilterToolbar';
import Modal from '../components/Modal';
import IconActionButton from '../components/IconActionButton';
import TaskSelector from '../components/TaskSelector';
import { Link } from 'react-router-dom';
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
import { getTask } from '../services/taskApi';
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

function relativeTime(value) {
  const seconds = Math.round((new Date(value).getTime() - Date.now()) / 1000);
  const abs = Math.abs(seconds);
  const [amount, unit] = abs < 60 ? [seconds, 'second'] : abs < 3600 ? [Math.round(seconds / 60), 'minute'] : abs < 86400 ? [Math.round(seconds / 3600), 'hour'] : abs < 2592000 ? [Math.round(seconds / 86400), 'day'] : abs < 31536000 ? [Math.round(seconds / 2592000), 'month'] : [Math.round(seconds / 31536000), 'year'];
  return new Intl.RelativeTimeFormat(undefined, { numeric: 'auto' }).format(amount, unit);
}

function initials(name) {
  return name.split(/\s+/).filter(Boolean).slice(0, 2).map((part) => part[0]).join('').toUpperCase();
}

function CommentFeedCard({ comment, task, author, canEdit, onEdit, onDelete, deleting }) {
  const messageRef = useRef(null);
  const [expanded, setExpanded] = useState(false);
  const [isLong, setIsLong] = useState(false);

  useEffect(() => {
    const element = messageRef.current;
    if (!element) return undefined;
    setExpanded(false);
    setIsLong(false);
    const measure = () => { if (element.scrollHeight > element.clientHeight + 1) setIsLong(true); };
    const frame = requestAnimationFrame(measure);
    const observer = new ResizeObserver(measure);
    observer.observe(element);
    return () => { cancelAnimationFrame(frame); observer.disconnect(); };
  }, [comment.message]);

  const edited = comment.updatedAt && new Date(comment.updatedAt) > new Date(comment.createdAt);
  return <article className="comment-feed-card">
    <header className="comment-feed-header">
      <span className="comment-avatar" aria-hidden="true">{initials(author)}</span>
      <div className="comment-feed-byline"><strong>{author}</strong><time dateTime={comment.createdAt} title={formatDate(comment.createdAt)}>{relativeTime(comment.createdAt)}</time>
        {edited && <span className="comment-edited" title={`Edited ${formatDate(comment.updatedAt)}`}>Edited</span>}
      </div>
      {canEdit && <div className="comment-feed-actions">
        <button className="user-icon-button" type="button" aria-label="Edit" title="Edit" onClick={() => onEdit(comment)}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z"/></svg></button>
        <button className="user-icon-button comment-delete-icon" type="button" aria-label="Delete" title="Delete" onClick={() => onDelete(comment)} disabled={deleting}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h18M8 6V4h8v2m3 0-1 14H6L5 6m4 4v6m6-6v6"/></svg></button>
      </div>}
    </header>
    <Link className="comment-task-chip" to={`/tasks/${comment.taskId}`}>{task?.title || `Task · ${comment.taskId}`}</Link>
    <p ref={messageRef} className={`comment-feed-message${expanded ? ' comment-feed-message-expanded' : ''}`}>{comment.message}</p>
    {isLong && <button type="button" className="comment-show-toggle" onClick={() => setExpanded((value) => !value)}>{expanded ? 'Show less' : 'Show more'}</button>}
  </article>;
}

function userLabel(userId, currentUser, users = []) {
  if (currentUser?.id === userId) {
    return currentUser.name || `${currentUser.firstName || ''} ${currentUser.lastName || ''}`.trim() || currentUser.email || userId;
  }
  const user = users.find((item) => item.id === userId);
  if (user) return `${user.firstName} ${user.lastName}`.trim();
  return userId ? `User · ${userId}` : 'Unknown user';
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

function TaskFilter({ value, initialTask, onChange }) {
  return <TaskSelector id="comments-task-filter" label="" value={value} initialTask={initialTask} required={false} placeholder="All tasks · search by title" onChange={(taskId) => onChange(taskId)} />;
}

function CommentForm({ comment, currentUser, users, onCancel, onSaved }) {
  const [form, setForm] = useState({ taskId: '', message: '' });
  const [selectedTask, setSelectedTask] = useState(null);
  const [commentUserId, setCommentUserId] = useState('');
  const [loadingComment, setLoadingComment] = useState(Boolean(comment));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const authorName = userLabel(commentUserId || currentUser?.id, currentUser, users);
  const valid = Boolean(form.taskId && form.message.trim() && form.message.trim().length <= MAX_MESSAGE_LENGTH && currentUser?.id);

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
    const body = { taskId: Number(form.taskId), userId: Number(commentUserId || currentUser.id), message };
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
    <section className="comment-modal" role="dialog" aria-modal="true" aria-labelledby="comment-form-title">
      <div className="project-form-heading">
        <div>
          <p className="eyebrow">{comment ? 'Comment details' : 'New comment'}</p>
          <h2 id="comment-form-title">{comment ? 'Edit comment' : 'Create comment'}</h2>
        </div>
        <div className="comment-modal-author"><span className="comment-avatar" aria-hidden="true">{initials(authorName)}</span><span>Commenting as <strong>{authorName}</strong></span></div>
        <button className="comment-modal-close" type="button" aria-label="Close comment form" onClick={onCancel} disabled={saving}>×</button>
      </div>
      <ErrorMessage message={error} />

      {loadingComment ? (
        <LoadingState message="Loading comment details…" as="p" className="project-loading" />
      ) : (
        <form className="project-form" onSubmit={handleSubmit}>
          <TaskSelector
            id="comment-task"
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
            <FormField id="comment-message" label="Comment" required><textarea
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
            /></FormField>
            {!form.message.trim() && <small className="comment-field-error"></small>}
            <span className="comment-character-count">{form.message.length}/{MAX_MESSAGE_LENGTH}</span>
          </div>
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving || !valid}>
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
  const [commentToDelete, setCommentToDelete] = useState(null);

  useEffect(() => {
    if (!formOpen) return undefined;
    function closeOnEscape(event) { if (event.key === 'Escape') setFormOpen(false); }
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [formOpen]);

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

  function clearFilters() {
    setSearchInput('');
    setSearch('');
    setTaskId('');
    setUserId('');
    setPage(1);
  }

  async function handleSaved(message) {
    setFormOpen(false);
    setEditingComment(null);
    setNotice(message);
    await loadComments();
  }

  function handleDelete(comment) {
    setCommentToDelete(comment);
  }

  async function confirmDelete() {
    const comment = commentToDelete;
    if (!comment) return;
    setDeletingId(comment.id);
    setListError('');
    setNotice('');
    try {
      await deleteComment(comment.id);
      setCommentToDelete(null);
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
          <h1>Comments</h1>
        </div>
        {!formOpen && (
          <button className="button button-primary" type="button" onClick={openCreateForm} disabled={!currentUser?.id}>
            Create comment
          </button>
        )}
      </div>

      {notice && <div className="comments-toast" role="status">{notice}</div>}
      {!currentUser?.id && (
        <ErrorMessage message="Your signed-in user details are unavailable, so comments cannot be created." />
      )}

      {formOpen && (
        <Modal backdropClassName="comment-modal-backdrop" onClose={() => { setFormOpen(false); setEditingComment(null); }}>
        <CommentForm
          key={editingComment?.id ?? 'new'}
          comment={editingComment}
          currentUser={currentUser}
          users={users}
          onCancel={() => {
            setFormOpen(false);
            setEditingComment(null);
          }}
          onSaved={handleSaved}
        />
        </Modal>
      )}

      <SearchFilterToolbar className="comments-toolbar">
          <input
            className="comments-search"
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Search comment, task, or user"
            aria-label="Search comments by text, task, or user"
          />
        <TaskFilter
          value={taskId}
          initialTask={taskDetails[taskId]}
          onChange={(value) => {
            setTaskId(value);
            setPage(1);
          }}
        />
        {canManage && (
            <select aria-label="Filter comments by author"
              value={userId}
              onChange={(event) => {
                setUserId(event.target.value);
                setPage(1);
              }}
            >
              <option value="">All authors</option>
              {userIds.map((id) => (
                <option key={id} value={id}>{userLabel(id, currentUser, users)}</option>
              ))}
            </select>
        )}
        <span className="project-count">{loading || searchInput.trim() !== search ? 'Loading…' : `${totalCount ? (page - 1) * PAGE_SIZE + 1 : 0}-${Math.min(page * PAGE_SIZE, totalCount)} of ${totalCount}`}</span>
      </SearchFilterToolbar>

      <ActiveFilterChips filters={[
        ...(search ? [{ key: 'search', label: 'Search', value: search, onRemove: () => { setSearchInput(''); setSearch(''); setPage(1); } }] : []),
        ...(taskId ? [{ key: 'task', label: 'Task', value: taskDetails[taskId]?.title || `Task ${taskId}`, onRemove: () => { setTaskId(''); setPage(1); } }] : []),
        ...(userId ? [{ key: 'user', label: 'Author', value: userLabel(Number(userId), currentUser, users), onRemove: () => { setUserId(''); setPage(1); } }] : [])
      ]} onClearAll={clearFilters} />

      <ErrorMessage message={listError} />
      <ErrorMessage message={taskDetailsError} />

      {loading ? (
        <LoadingState message="Loading comments…" />
      ) : listError ? null : comments.length === 0 ? (
        <EmptyState title={search || taskId || userId ? 'No comments match your filters' : 'No comments yet'} description={search || taskId || userId ? 'Try changing your search or filters.' : 'Add a comment to a task to get started.'} action={!search && !taskId && !userId && !formOpen && currentUser?.id ? (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>Create your first comment</button>
          ) : null} />
      ) : (
        <>
        <div className="comments-feed">
          {comments.map((comment) => {
            const author = comment.userName || userLabel(comment.userId, currentUser, users);
            const canEdit = String(currentUser?.role).toLowerCase() === 'admin' || String(comment.userId) === String(currentUser?.id);
            return <CommentFeedCard key={comment.id} comment={comment} task={taskDetails[comment.taskId]} author={author} canEdit={canEdit} onEdit={openEditForm} onDelete={handleDelete} deleting={deletingId === comment.id} />;
          })}
        </div>
        <div className="comments-legacy-table">
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
                      <strong>{task?.title || `Task · ${comment.taskId}`}</strong>
                      <span className="task-description-cell">{task?.description || 'No description available'}</span>
                    </td>
                    <td data-label="Comment"><span className="comment-message-cell">{comment.message}</span></td>
                    <td data-label="Created by">{comment.userName || userLabel(comment.userId, currentUser, users)}</td>
                    <td data-label="Created date">{formatDate(comment.createdAt)}</td>
                    <td data-label="Updated date">{formatDate(comment.updatedAt)}</td>
                    <td data-label="Status">
                      <StatusBadge status={comment.isActive ? 'Active' : 'Inactive'} className={`project-status project-status-${comment.isActive ? 'active' : 'inactive'}`} />
                    </td>
                    <td data-label="Actions">
                      {canManage && <div className="task-row-actions">
                        <IconActionButton action="edit" label="Edit comment" onClick={() => openEditForm(comment)} />
                        <IconActionButton action="delete" label={deletingId === comment.id ? 'Deleting comment' : 'Delete comment'} onClick={() => handleDelete(comment)} disabled={deletingId === comment.id} />
                      </div>}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        </div>
        </>
      )}

      <Pagination page={page} totalPages={totalPages} onPageChange={setPage} disabled={loading} label="Comment pages" />
      {commentToDelete && <Delete title="Delete comment?" className="project-confirm-dialog" backdropClassName="project-confirm-backdrop" busyLabel="Deleting…" onCancel={() => setCommentToDelete(null)} onConfirm={confirmDelete} busy={deletingId === commentToDelete.id}>Delete this comment? It will be removed from the normal comment list.</Delete>}
    </section>
  );
}

export default CommentsPage;
