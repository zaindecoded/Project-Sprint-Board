import { useCallback, useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import ErrorMessage from '../../components/ErrorMessage';
import ActiveFilterChips from '../../components/ActiveFilterChips';
import { useAuth } from '../../context/AuthContext';
import { deleteSprint, getSprint, getSprints, createSprint, updateSprint } from '../../services/sprintApi';
import { getProjects } from '../../services/projectApi';
import { getApiErrorMessage } from '../../services/apiErrors';
import { canManageWorkspace } from '../../utils/permissions';
import { getTodayDateInputValue, isPastDate } from '../../utils/displayStatus';

const PAGE_SIZE = 10;
const PROJECT_PAGE_SIZE = 100;
const SPRINT_STATUSES = ['Planned', 'Active', 'Completed', 'Cancelled'];
const EMPTY_FORM = { projectId: '', name: '', startDate: getTodayDateInputValue(), endDate: '', goal: '', status: 'Planned' };

function getSprintDisplayStatus(sprint) {
  const status = sprint.status || '—';
  if (isPastDate(sprint.endDate) && !['completed', 'cancelled'].includes(status.toLowerCase())) {
    return 'Overdue';
  }
  return status;
}

function formatDate(value) {
  if (!value) {
    return '—';
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
}

function formatCompactDate(value) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric' }).format(date);
}

function daysUntil(value) {
  const due = new Date(`${dateInputValue(value)}T00:00:00`);
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  return Math.ceil((due - today) / 86400000);
}

function dateInputValue(value) {
  return value ? String(value).slice(0, 10) : '';
}

function getValidationMessage(error) {
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
  return getApiErrorMessage(error, 'Unable to save this sprint.');
}

async function loadAllProjects(signal) {
  const firstPage = await getProjects({ page: 1, pageSize: PROJECT_PAGE_SIZE, search: '', signal });
  const allProjects = Array.isArray(firstPage.items) ? [...firstPage.items] : [];
  const totalPages = Number(firstPage.totalPages) || 0;

  for (let page = 2; page <= totalPages; page += 1) {
    const result = await getProjects({ page, pageSize: PROJECT_PAGE_SIZE, search: '', signal });
    if (Array.isArray(result.items)) {
      allProjects.push(...result.items);
    }
  }
  return allProjects;
}

function SprintForm({ sprint, projects, onCancel, onSaved }) {
  const [form, setForm] = useState(EMPTY_FORM);
  const [loadingSprint, setLoadingSprint] = useState(Boolean(sprint));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const selectedProject = projects.find((project) => String(project.id) === String(form.projectId));
  const projectEndDate = selectedProject?.endDate ? dateInputValue(selectedProject.endDate) : '';
  const datesError = form.startDate && form.endDate && form.endDate <= form.startDate ? 'End date must be after the start date.' : '';
  const valid = Boolean(form.projectId && form.name.trim() && form.startDate && form.endDate && !datesError && (!projectEndDate || form.endDate <= projectEndDate));

  useEffect(() => {
    let active = true;
    if (!sprint) {
      setForm(EMPTY_FORM);
      setLoadingSprint(false);
      return () => {
        active = false;
      };
    }

    setLoadingSprint(true);
    setError('');
    getSprint(sprint.id)
      .then((item) => {
        if (active) {
          setForm({
            projectId: item.projectId ?? '',
            name: item.name ?? '',
            startDate: dateInputValue(item.startDate),
            endDate: dateInputValue(item.endDate),
            goal: item.goal ?? '',
            status: item.status ?? 'Planned'
          });
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(getApiErrorMessage(requestError, 'Unable to load this sprint.'));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingSprint(false);
        }
      });

    return () => {
      active = false;
    };
  }, [sprint]);

  function changeField(event) {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: name === 'projectId' ? (value ? Number(value) : '') : value }));
    setError('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (saving) {
      return;
    }
    if (!sprint) {
      const today = getTodayDateInputValue();
      if (form.startDate < today) {
        setError('Sprint start date must be today or a future date.');
        return;
      }
      if (form.endDate < today) {
        setError('Sprint end date must be today or a future date.');
        return;
      }
    }
    if (form.endDate <= form.startDate) {
      setError('End date must be after the start date.');
      return;
    }
    if (projectEndDate && form.endDate > projectEndDate) {
      setError('Sprint end date cannot be later than the project end date.');
      return;
    }

    setSaving(true);
    setError('');
    const body = {
      projectId: Number(form.projectId),
      name: form.name.trim(),
      startDate: form.startDate,
      endDate: form.endDate,
      goal: form.goal.trim(),
      status: sprint ? form.status.trim() : 'Planned'
    };
    try {
      if (sprint) {
        await updateSprint(sprint.id, body);
        onSaved('Sprint updated successfully.');
      } else {
        await createSprint(body);
        onSaved('Sprint created successfully.');
      }
    } catch (requestError) {
      setError(getValidationMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="sprint-modal" role="dialog" aria-modal="true" aria-labelledby="sprint-form-title">
      <div className="project-form-heading">
        <h2 id="sprint-form-title">{sprint ? 'Edit sprint' : 'Create sprint'}</h2>
        <button className="sprint-modal-close" type="button" aria-label="Close" onClick={onCancel} disabled={saving}>×</button>
      </div>

      <ErrorMessage>{error}</ErrorMessage>

      {loadingSprint ? (
        <p className="project-loading" role="status">Loading sprint details…</p>
      ) : (
        <form className="project-form" onSubmit={handleSubmit}>
          <div className="project-field project-field-wide">
            <label htmlFor="sprint-project">Project <span className="sprint-required">*</span></label>
            <select
              id="sprint-project"
              name="projectId"
              value={form.projectId}
              onChange={changeField}
              required
              disabled={saving || projects.length === 0}
            >
              <option value="">Select a project</option>
              {projects.map((project) => (
                <option key={project.id} value={project.id}>{project.name}</option>
              ))}
            </select>
            {!form.projectId && <small className="sprint-field-error"></small>}
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="sprint-name">Sprint name <span className="sprint-required">*</span></label>
            <input
              id="sprint-name"
              name="name"
              value={form.name}
              onChange={changeField}
              maxLength={200}
              required
              disabled={saving}
            />
            {!form.name.trim() && <small className="sprint-field-error"></small>}
          </div>
          <div className="project-field">
            <label htmlFor="sprint-start-date">Start date <span className="sprint-required">*</span></label>
            <input
              id="sprint-start-date"
              name="startDate"
              type="date"
              value={form.startDate}
              onChange={changeField}
              min={!sprint ? getTodayDateInputValue() : undefined}
              required
              disabled={saving}
            />
            {!form.startDate && <small className="sprint-field-error">Start date is required.</small>}
          </div>
          <div className="project-field">
            <label htmlFor="sprint-end-date">End date <span className="sprint-required">*</span></label>
            <input
              id="sprint-end-date"
              name="endDate"
              type="date"
              value={form.endDate}
              onChange={changeField}
              max={projectEndDate || undefined}
              min={!sprint ? (form.startDate > getTodayDateInputValue() ? form.startDate : getTodayDateInputValue()) : (form.startDate || undefined)}
              required
              disabled={saving}
            />
            {!form.endDate ? <small className="sprint-field-error"></small> : datesError ? <small className="sprint-field-error">{datesError}</small> : null}
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="sprint-goal">Goal</label>
            <textarea
              id="sprint-goal"
              name="goal"
              value={form.goal}
              onChange={changeField}
              maxLength={500}
              rows={3}
              disabled={saving}
            />
          </div>
          {sprint && <div className="project-field">
            <label htmlFor="sprint-status">Status</label>
            <select
              id="sprint-status"
              name="status"
              value={form.status}
              onChange={changeField}
              disabled={saving}
            >
              {!SPRINT_STATUSES.includes(form.status) && <option value={form.status}>{form.status}</option>}
              {SPRINT_STATUSES.map((status) => <option key={status} value={status}>{status}</option>)}
            </select>
          </div>}
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving || !valid || projects.length === 0}>
              {saving ? 'Saving…' : sprint ? 'Save changes' : 'Create sprint'}
            </button>
          </div>
        </form>
      )}
    </section>
  );
}

function SprintsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const canManage = canManageWorkspace(user);
  const [sprints, setSprints] = useState([]);
  const [projects, setProjects] = useState([]);
  const [projectsLoading, setProjectsLoading] = useState(true);
  const [projectsError, setProjectsError] = useState('');
  const [projectId, setProjectId] = useState('');
  const [status, setStatus] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState('');
  const [notice, setNotice] = useState('');
  const [editingSprint, setEditingSprint] = useState(null);
  const [formOpen, setFormOpen] = useState(false);
  const [deletingId, setDeletingId] = useState(null);
  const [sprintToDelete, setSprintToDelete] = useState(null);

  useEffect(() => {
    if (!formOpen && !sprintToDelete) return undefined;
    function onEscape(event) {
      if (event.key === 'Escape') {
        if (sprintToDelete) setSprintToDelete(null);
        else setFormOpen(false);
      }
    }
    window.addEventListener('keydown', onEscape);
    return () => window.removeEventListener('keydown', onEscape);
  }, [formOpen, sprintToDelete]);

  useEffect(() => {
    const controller = new AbortController();
    setProjectsLoading(true);
    setProjectsError('');
    loadAllProjects(controller.signal)
      .then(setProjects)
      .catch((error) => {
        if (error.code !== 'ERR_CANCELED') {
          setProjectsError(getApiErrorMessage(error, 'Unable to load projects.'));
        }
      })
      .finally(() => {
        if (!controller.signal.aborted) {
          setProjectsLoading(false);
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

  const loadSprints = useCallback(async (signal) => {
    setLoading(true);
    setListError('');
    try {
      const result = await getSprints({ page, pageSize: PAGE_SIZE, search, projectId, status, signal });
      setSprints(Array.isArray(result.items) ? result.items : []);
      setTotalCount(Number(result.totalCount) || 0);
      setTotalPages(Number(result.totalPages) || 0);
      if (result.page && result.page !== page) {
        setPage(result.page);
      }
    } catch (requestError) {
      if (requestError.code !== 'ERR_CANCELED') {
        setListError(getApiErrorMessage(requestError, 'Unable to load sprints.'));
      }
    } finally {
      if (!signal?.aborted) {
        setLoading(false);
      }
    }
  }, [page, projectId, search, status]);

  useEffect(() => {
    const controller = new AbortController();
    loadSprints(controller.signal);
    return () => controller.abort();
  }, [loadSprints]);

  function openCreateForm() {
    setNotice('');
    setEditingSprint(null);
    setFormOpen(true);
  }

  function openEditForm(sprint) {
    setNotice('');
    setEditingSprint(sprint);
    setFormOpen(true);
  }

  async function handleSaved(message) {
    setFormOpen(false);
    setEditingSprint(null);
    setNotice(message);
    await loadSprints();
  }

  async function handleDelete(sprint) {
    setDeletingId(sprint.id);
    setListError('');
    setNotice('');
    try {
      await deleteSprint(sprint.id);
      setSprintToDelete(null);
      setNotice(`${sprint.name} deleted successfully.`);
      if (sprints.length === 1 && page > 1) {
        setPage(page - 1);
      } else {
        await loadSprints();
      }
    } catch (error) {
      setListError(getValidationMessage(error));
    } finally {
      setDeletingId(null);
    }
  }

  const projectNames = new Map(projects.map((project) => [project.id, project.name]));

  return (
      <section className="sprints-page">
      <div className="projects-heading">
        <div><h1>Sprints</h1></div>
        {canManage && !formOpen && (
          <button className="button button-primary" type="button" onClick={openCreateForm} disabled={projectsLoading || projects.length === 0}>
            Create sprint
          </button>
        )}
      </div>

      {notice && <div className="project-notice" role="status">{notice}</div>}
      <ErrorMessage>{projectsError}</ErrorMessage>

      {canManage && formOpen && (
        <div className="sprint-modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) { setFormOpen(false); setEditingSprint(null); } }}>
        <SprintForm
          key={editingSprint?.id ?? 'new'}
          sprint={editingSprint}
          projects={projects}
          onCancel={() => {
            setFormOpen(false);
            setEditingSprint(null);
          }}
          onSaved={handleSaved}
        />
        </div>
      )}

      <div className="projects-toolbar sprint-toolbar">
        <label className="project-search">
          <span>Search sprints</span>
          <input
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Search by sprint name"
            aria-label="Search sprints by name"
          />
        </label>
        <label className="project-filter">
          <span>Project</span>
          <select
            value={projectId}
            onChange={(event) => {
              setPage(1);
              setProjectId(event.target.value);
            }}
            disabled={projectsLoading || projectsError}
          >
            <option value="">All projects</option>
            {projects.map((project) => (
              <option key={project.id} value={project.id}>{project.name}</option>
            ))}
          </select>
        </label>
        <label className="project-filter"><span>Status</span><select value={status} onChange={(event) => { setPage(1); setStatus(event.target.value); }}><option value="">All statuses</option><option value="Planned">Planned</option><option value="Active">Active</option><option value="Completed">Completed</option></select></label>
        <span className="project-count">{loading || searchInput.trim() !== search ? 'Loading sprints…' : `${totalCount ? (page - 1) * PAGE_SIZE + 1 : 0}-${Math.min(page * PAGE_SIZE, totalCount)} of ${totalCount}`}</span>
      </div>

      <ActiveFilterChips filters={[
        ...(search ? [{ key: 'search', label: 'Search', value: search, onRemove: () => { setSearchInput(''); setSearch(''); setPage(1); } }] : []),
        ...(projectId ? [{ key: 'project', label: 'Project', value: projectNames.get(Number(projectId)) || `Project ${projectId}`, onRemove: () => { setProjectId(''); setPage(1); } }] : []),
        ...(status ? [{ key: 'status', label: 'Status', value: status, onRemove: () => { setStatus(''); setPage(1); } }] : [])
      ]} onClearAll={() => { setSearchInput(''); setSearch(''); setProjectId(''); setStatus(''); setPage(1); }} />

      {!projectsLoading && projects.length === 0 && !projectsError && canManage && (
        <div className="project-notice">Create a project before adding sprints.</div>
      )}
      <ErrorMessage>{listError}</ErrorMessage>

      {loading ? (
        <div className="project-state" role="status">Loading sprints…</div>
      ) : sprints.length === 0 ? (
        <div className="project-state project-empty">
          <h2>{search || projectId || status ? 'No matching sprints' : 'No sprints yet'}</h2>
          <p>{search || projectId || status ? 'Try changing the search or filters.' : canManage ? 'Create a sprint to get started.' : 'No sprints are available yet.'}</p>
          {!search && !projectId && !status && projects.length > 0 && canManage && !formOpen && (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>
              Create your first sprint
            </button>
          )}
        </div>
      ) : (
        <div className="project-list">
          {sprints.map((sprint) => (
            <article className="project-card sprint-card sprint-card-clickable" key={sprint.id} role="link" tabIndex="0" onClick={() => navigate(`/sprints/${sprint.id}`)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); navigate(`/sprints/${sprint.id}`); } }}>
              <div className="project-card-main">
                <div className="project-card-title-row">
                  <h2>{sprint.name}</h2>
                  <span className={`sprint-status-badge sprint-status-${getSprintDisplayStatus(sprint).toLowerCase()}`}>{getSprintDisplayStatus(sprint)}</span>
                </div>
                <Link className="sprint-project-name" to={`/projects/${sprint.projectId}`} onClick={(event) => event.stopPropagation()}>{projectNames.get(sprint.projectId) || 'Project unavailable'}</Link>
                {sprint.goal && <p className="sprint-goal">{sprint.goal}</p>}
                <div className="sprint-card-footer"><span className="sprint-inline-dates">{formatCompactDate(sprint.startDate)} → {formatCompactDate(sprint.endDate)}, {new Date(sprint.endDate).getFullYear()}</span>{String(sprint.status).toLowerCase() === 'active' && <span className={`sprint-days-left${daysUntil(sprint.endDate) < 0 ? ' sprint-days-overdue' : ''}`}>{daysUntil(sprint.endDate) < 0 ? 'Overdue' : daysUntil(sprint.endDate) === 0 ? 'Ends today' : `${daysUntil(sprint.endDate)} days left`}</span>}</div>
              </div>
              {canManage && <div className="project-actions">
                <button className="user-icon-button" type="button" aria-label="Edit sprint" title="Edit sprint" onClick={(event) => { event.stopPropagation(); openEditForm(sprint); }}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z"/></svg></button>
                <button className="user-icon-button sprint-delete-icon" type="button" aria-label="Delete sprint" title="Delete sprint" onClick={(event) => { event.stopPropagation(); setSprintToDelete(sprint); }} disabled={deletingId === sprint.id}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h18M8 6V4h8v2m3 0-1 14H6L5 6m4 4v6m6-6v6"/></svg></button>
              </div>}
            </article>
          ))}
        </div>
      )}

      <nav className="project-pagination" aria-label="Sprint pages">
        <button
          className="button button-secondary"
          type="button"
          onClick={() => setPage((current) => Math.max(1, current - 1))}
          disabled={page <= 1 || loading}
        >
          Previous
        </button>
        <span>Page {page} of {totalPages}</span>
        <button
          className="button button-secondary"
          type="button"
          onClick={() => setPage((current) => current + 1)}
          disabled={totalPages === 0 || page >= totalPages || loading}
        >
          Next
        </button>
      </nav>
      {sprintToDelete && <div className="sprint-confirm-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget) setSprintToDelete(null); }}><section className="sprint-confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="sprint-delete-title"><h2 id="sprint-delete-title">Delete sprint?</h2><p>Delete {sprintToDelete.name}? This can't be undone.</p><div><button className="button button-secondary" type="button" onClick={() => setSprintToDelete(null)}>Cancel</button><button className="button sprint-confirm-delete" type="button" onClick={() => handleDelete(sprintToDelete)} disabled={deletingId === sprintToDelete.id}>{deletingId === sprintToDelete.id ? 'Deleting…' : 'Delete'}</button></div></section></div>}
    </section>
  );
}

export default SprintsPage;
