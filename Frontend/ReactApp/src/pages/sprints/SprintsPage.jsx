import { useCallback, useEffect, useState } from 'react';
import ErrorMessage from '../../components/ErrorMessage';
import { useAuth } from '../../context/AuthContext';
import { deleteSprint, getSprint, getSprints, createSprint, updateSprint } from '../../services/sprintApi';
import { getProjects } from '../../services/projectApi';
import { getApiErrorMessage } from '../../services/apiErrors';
import { canManageWorkspace } from '../../utils/permissions';
import { getTodayDateInputValue, isPastDate } from '../../utils/displayStatus';

const PAGE_SIZE = 10;
const PROJECT_PAGE_SIZE = 100;
const SPRINT_STATUSES = ['Planned', 'Active', 'Completed', 'Cancelled'];
const EMPTY_FORM = { projectId: '', name: '', startDate: '', endDate: '', goal: '', status: 'Planned' };

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
  const selectedProject = projects.find((project) => project.id === form.projectId);
  const projectEndDate = selectedProject?.endDate ? dateInputValue(selectedProject.endDate) : '';

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
    setForm((current) => ({ ...current, [name]: value }));
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
    if (form.endDate < form.startDate) {
      setError('End date must be on or after the start date.');
      return;
    }
    if (projectEndDate && form.endDate > projectEndDate) {
      setError('Sprint end date cannot be later than the project end date.');
      return;
    }

    setSaving(true);
    setError('');
    const body = {
      projectId: form.projectId,
      name: form.name.trim(),
      startDate: form.startDate,
      endDate: form.endDate,
      goal: form.goal.trim(),
      status: form.status.trim()
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
    <section className="project-form-card" aria-labelledby="sprint-form-title">
      <div className="project-form-heading">
        <div>
          <p className="eyebrow">{sprint ? 'Sprint details' : 'New sprint'}</p>
          <h2 id="sprint-form-title">{sprint ? 'Edit sprint' : 'Create sprint'}</h2>
        </div>
        <button className="button button-secondary" type="button" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
      </div>

      <ErrorMessage>{error}</ErrorMessage>

      {loadingSprint ? (
        <p className="project-loading" role="status">Loading sprint details…</p>
      ) : (
        <form className="project-form" onSubmit={handleSubmit}>
          <div className="project-field project-field-wide">
            <label htmlFor="sprint-project">Project <span aria-hidden="true">*</span></label>
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
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="sprint-name">Sprint name <span aria-hidden="true">*</span></label>
            <input
              id="sprint-name"
              name="name"
              value={form.name}
              onChange={changeField}
              maxLength={200}
              required
              disabled={saving}
            />
          </div>
          <div className="project-field">
            <label htmlFor="sprint-start-date">Start date <span aria-hidden="true">*</span></label>
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
          </div>
          <div className="project-field">
            <label htmlFor="sprint-end-date">End date <span aria-hidden="true">*</span></label>
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
          <div className="project-field">
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
          </div>
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving || projects.length === 0}>
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
  const canManage = canManageWorkspace(user);
  const [sprints, setSprints] = useState([]);
  const [projects, setProjects] = useState([]);
  const [projectsLoading, setProjectsLoading] = useState(true);
  const [projectsError, setProjectsError] = useState('');
  const [projectId, setProjectId] = useState('');
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
      const result = await getSprints({ page, pageSize: PAGE_SIZE, search, projectId, signal });
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
  }, [page, projectId, search]);

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
    if (!window.confirm(`Delete "${sprint.name}"? This will remove it from the active sprint list.`)) {
      return;
    }
    setDeletingId(sprint.id);
    setListError('');
    setNotice('');
    try {
      await deleteSprint(sprint.id);
      setNotice('Sprint deleted successfully.');
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
    <section className="projects-page">
      <div className="projects-heading">
        <div>
          <h1>Sprints</h1>
        </div>
        {canManage && !formOpen && (
          <button className="button button-primary" type="button" onClick={openCreateForm} disabled={projectsLoading || projects.length === 0}>
            Create sprint
          </button>
        )}
      </div>

      {notice && <div className="project-notice" role="status">{notice}</div>}
      <ErrorMessage>{projectsError}</ErrorMessage>

      {canManage && formOpen && (
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
      )}

      <div className="projects-toolbar sprint-toolbar">
        <label className="project-search">
          <span>Search sprints</span>
          <input
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Search by sprint name or goal"
            aria-label="Search sprints by name or goal"
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
        <span className="project-count">
          {projectsLoading ? 'Loading projects…' : `${totalCount} ${totalCount === 1 ? 'sprint' : 'sprints'}`}
        </span>
      </div>

      {!projectsLoading && projects.length === 0 && !projectsError && canManage && (
        <div className="project-notice">Create a project before adding sprints.</div>
      )}
      <ErrorMessage>{listError}</ErrorMessage>

      {loading ? (
        <div className="project-state" role="status">Loading sprints…</div>
      ) : sprints.length === 0 ? (
        <div className="project-state project-empty">
          <h2>{search || projectId ? 'No matching sprints' : 'No sprints yet'}</h2>
          <p>{search || projectId ? 'Try changing the search or project filter.' : canManage ? 'Create a sprint to get started.' : 'No sprints are available yet.'}</p>
          {!search && !projectId && projects.length > 0 && canManage && !formOpen && (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>
              Create your first sprint
            </button>
          )}
        </div>
      ) : (
        <div className="project-list">
          {sprints.map((sprint) => (
            <article className="project-card" key={sprint.id}>
              <div className="project-card-main">
                <div className="project-card-title-row">
                  <h2>{sprint.name}</h2>
                </div>
                <p className="sprint-project-name">{projectNames.get(sprint.projectId) || 'Project unavailable'}</p>
                <p className="project-description">{sprint.goal || 'No goal provided.'}</p>
                <dl className="project-dates">
                  <div><dt>Start date</dt><dd>{formatDate(sprint.startDate)}</dd></div>
                  <div><dt>End date</dt><dd>{formatDate(sprint.endDate)}</dd></div>
                  <div><dt>Status</dt><dd>{getSprintDisplayStatus(sprint)}</dd></div>
                </dl>
              </div>
              {canManage && <div className="project-actions">
                <button className="button button-secondary" type="button" onClick={() => openEditForm(sprint)}>Edit</button>
                <button
                  className="button button-danger"
                  type="button"
                  onClick={() => handleDelete(sprint)}
                  disabled={deletingId === sprint.id}
                >
                  {deletingId === sprint.id ? 'Deleting…' : 'Delete'}
                </button>
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
    </section>
  );
}

export default SprintsPage;
