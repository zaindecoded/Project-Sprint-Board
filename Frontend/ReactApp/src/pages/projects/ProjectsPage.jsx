import { useCallback, useEffect, useState } from 'react';
import ErrorMessage from '../../components/ErrorMessage';
import { useAuth } from '../../context/AuthContext';
import { createProject, deleteProject, getProject, getProjects, updateProject } from '../../services/projectApi';
import { getApiErrorMessage } from '../../services/apiErrors';
import { canManageWorkspace } from '../../utils/permissions';
import { getProjectDisplayStatus, getTodayDateInputValue } from '../../utils/displayStatus';

const PAGE_SIZE = 10;
const EMPTY_FORM = { name: '', description: '', startDate: '', endDate: '', status: 'Active' };

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
    return Object.values(errors).flat().filter((item) => typeof item === 'string').join(' ');
  }
  if (typeof error.response?.data?.message === 'string') {
    return error.response.data.message;
  }
  return getApiErrorMessage(error, 'Unable to save this project.');
}

function ProjectForm({ project, onCancel, onSaved }) {
  const [form, setForm] = useState(EMPTY_FORM);
  const [loadingProject, setLoadingProject] = useState(Boolean(project));
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;

    if (!project) {
      setForm(EMPTY_FORM);
      setLoadingProject(false);
      return () => {
        active = false;
      };
    }

    setLoadingProject(true);
    setError('');
    getProject(project.id)
      .then((item) => {
        if (active) {
          setForm({
            name: item.name ?? '',
            description: item.description ?? '',
            startDate: dateInputValue(item.startDate),
            endDate: dateInputValue(item.endDate),
            status: item.status || 'Active'
          });
        }
      })
      .catch((requestError) => {
        if (active) {
          setError(getApiErrorMessage(requestError, 'Unable to load this project.'));
        }
      })
      .finally(() => {
        if (active) {
          setLoadingProject(false);
        }
      });

    return () => {
      active = false;
    };
  }, [project]);

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

    if (!project) {
      const today = getTodayDateInputValue();
      if (form.startDate && form.startDate < today) {
        setError('Project start date must be today or a future date.');
        return;
      }
      if (form.endDate && form.endDate < today) {
        setError('Project end date must be today or a future date.');
        return;
      }
    }

    if (form.startDate && form.endDate && form.endDate < form.startDate) {
      setError('End date must be on or after the start date.');
      return;
    }

    setSaving(true);
    setError('');
    const body = {
      name: form.name.trim(),
      description: form.description.trim() || null,
      startDate: form.startDate || null,
      endDate: form.endDate || null,
      status: form.status
    };

    try {
      if (project) {
        await updateProject(project.id, body);
        onSaved('Project updated successfully.');
      } else {
        await createProject({ ...body, status: 'Active' });
        onSaved('Project created successfully.');
      }
    } catch (requestError) {
      setError(getValidationMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  return (
    <section className="project-form-card" aria-labelledby="project-form-title">
      <div className="project-form-heading">
        <div>
          <p className="eyebrow">{project ? 'Project details' : 'New project'}</p>
          <h2 id="project-form-title">{project ? 'Edit project' : 'Create project'}</h2>
        </div>
        <button className="button button-secondary" type="button" onClick={onCancel} disabled={saving}>
          Cancel
        </button>
      </div>

      <ErrorMessage>{error}</ErrorMessage>

      {loadingProject ? (
        <p className="project-loading" role="status">Loading project details…</p>
      ) : (
        <form className="project-form" onSubmit={handleSubmit}>
          <div className="project-field project-field-wide">
            <label htmlFor="project-name">Project name <span aria-hidden="true">*</span></label>
            <input
              id="project-name"
              name="name"
              value={form.name}
              onChange={changeField}
              maxLength={200}
              required
              disabled={saving}
            />
          </div>
          <div className="project-field project-field-wide">
            <label htmlFor="project-description">Description</label>
            <textarea
              id="project-description"
              name="description"
              value={form.description}
              onChange={changeField}
              maxLength={1000}
              rows={3}
              disabled={saving}
            />
          </div>
          <div className="project-field">
            <label htmlFor="project-start-date">Start date</label>
            <input
              id="project-start-date"
              name="startDate"
              type="date"
              value={form.startDate}
              onChange={changeField}
              min={!project ? getTodayDateInputValue() : undefined}
              disabled={saving}
            />
          </div>
          <div className="project-field">
            <label htmlFor="project-end-date">End date</label>
            <input
              id="project-end-date"
              name="endDate"
              type="date"
              value={form.endDate}
              onChange={changeField}
              min={!project ? (form.startDate > getTodayDateInputValue() ? form.startDate : getTodayDateInputValue()) : (form.startDate || undefined)}
              disabled={saving}
            />
          </div>
          <div className="project-field">
            <label htmlFor="project-status">Project status</label>
            <select id="project-status" name="status" value={form.status} onChange={changeField} disabled={saving}>
              <option value="Planned">Planned</option>
              <option value="Active">Active</option>
              <option value="OnHold">On hold</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
            </select>
          </div>
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving}>
              {saving ? 'Saving…' : project ? 'Save changes' : 'Create project'}
            </button>
          </div>
        </form>
      )}
    </section>
  );
}

function ProjectsPage() {
  const { user } = useAuth();
  const canManage = canManageWorkspace(user);
  const [projects, setProjects] = useState([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState('');
  const [notice, setNotice] = useState('');
  const [editingProject, setEditingProject] = useState(null);
  const [formOpen, setFormOpen] = useState(false);
  const [deletingId, setDeletingId] = useState(null);

  useEffect(() => {
    const timer = window.setTimeout(() => {
      setPage(1);
      setSearch(searchInput.trim());
    }, 300);
    return () => window.clearTimeout(timer);
  }, [searchInput]);

  const loadProjects = useCallback(async (signal) => {
    setLoading(true);
    setListError('');
    try {
      const result = await getProjects({ page, pageSize: PAGE_SIZE, search, signal });
      setProjects(Array.isArray(result.items) ? result.items : []);
      setTotalCount(Number(result.totalCount) || 0);
      setTotalPages(Number(result.totalPages) || 0);
      if (result.page && result.page !== page) {
        setPage(result.page);
      }
    } catch (requestError) {
      if (requestError.code !== 'ERR_CANCELED') {
        setListError(getApiErrorMessage(requestError, 'Unable to load projects.'));
      }
    } finally {
      if (!signal?.aborted) {
        setLoading(false);
      }
    }
  }, [page, search]);

  useEffect(() => {
    const controller = new AbortController();
    loadProjects(controller.signal);
    return () => controller.abort();
  }, [loadProjects]);

  function openCreateForm() {
    setNotice('');
    setEditingProject(null);
    setFormOpen(true);
  }

  function openEditForm(project) {
    setNotice('');
    setEditingProject(project);
    setFormOpen(true);
  }

  async function handleSaved(message) {
    setFormOpen(false);
    setEditingProject(null);
    setNotice(message);
    await loadProjects();
  }

  async function handleDelete(project) {
    if (!window.confirm(`Delete "${project.name}"? This will remove it from the active project list.`)) {
      return;
    }

    setDeletingId(project.id);
    setListError('');
    setNotice('');
    try {
      await deleteProject(project.id);
      setNotice('Project deleted successfully.');
      const lastPageAfterDelete = projects.length === 1 && page > 1 ? page - 1 : page;
      if (lastPageAfterDelete !== page) {
        setPage(lastPageAfterDelete);
      } else {
        await loadProjects();
      }
    } catch (requestError) {
      setListError(getValidationMessage(requestError));
    } finally {
      setDeletingId(null);
    }
  }

  const canGoPrevious = page > 1;
  const canGoNext = totalPages > 0 && page < totalPages;

  return (
    <section className="projects-page">
      <div className="projects-heading">
        <div>
          <h1>Projects</h1>
        </div>
        {canManage && !formOpen && (
          <button className="button button-primary" type="button" onClick={openCreateForm}>
            Create project
          </button>
        )}
      </div>

      {notice && <div className="project-notice" role="status">{notice}</div>}

      {canManage && formOpen && (
        <ProjectForm
          key={editingProject?.id ?? 'new'}
          project={editingProject}
          onCancel={() => {
            setFormOpen(false);
            setEditingProject(null);
          }}
          onSaved={handleSaved}
        />
      )}

      <div className="projects-toolbar">
        <label className="project-search">
          <span>Search projects</span>
          <input
            type="search"
            value={searchInput}
            onChange={(event) => setSearchInput(event.target.value)}
            placeholder="Search by project name or description"
            aria-label="Search projects by name or description"
          />
        </label>
        <span className="project-count">{totalCount} {totalCount === 1 ? 'project' : 'projects'}</span>
      </div>

      <ErrorMessage>{listError}</ErrorMessage>

      {loading ? (
        <div className="project-state" role="status">Loading projects…</div>
      ) : projects.length === 0 ? (
        <div className="project-state project-empty">
          <h2>{search ? 'No matching projects' : 'No projects yet'}</h2>
          <p>{search ? 'Try a different search.' : 'Create a project to get started.'}</p>
          {!search && canManage && !formOpen && (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>
              Create your first project
            </button>
          )}
        </div>
      ) : (
        <div className="project-list">
          {projects.map((project) => (
            <article className="project-card" key={project.id}>
              <div className="project-card-main">
                <div className="project-card-title-row">
                  <h2>{project.name}</h2>
                  <span className={`project-status${getProjectDisplayStatus(project) === 'Active' ? ' project-status-active' : ''}${getProjectDisplayStatus(project) === 'Overdue' ? ' project-status-overdue' : ''}`}>
                    {getProjectDisplayStatus(project)}
                  </span>
                </div>
                <p className="project-description">{project.description || 'No description provided.'}</p>
                <dl className="project-dates">
                  <div>
                    <dt>Start date</dt>
                    <dd>{formatDate(project.startDate)}</dd>
                  </div>
                  <div>
                    <dt>End date</dt>
                    <dd>{formatDate(project.endDate)}</dd>
                  </div>
                </dl>
              </div>
              {canManage && <div className="project-actions">
                <button className="button button-secondary" type="button" onClick={() => openEditForm(project)}>
                  Edit
                </button>
                <button
                  className="button button-danger"
                  type="button"
                  onClick={() => handleDelete(project)}
                  disabled={deletingId === project.id}
                >
                  {deletingId === project.id ? 'Deleting…' : 'Delete'}
                </button>
              </div>}
            </article>
          ))}
        </div>
      )}

      <nav className="project-pagination" aria-label="Project pages">
        <button
          className="button button-secondary"
          type="button"
          onClick={() => setPage((current) => Math.max(1, current - 1))}
          disabled={!canGoPrevious || loading}
        >
          Previous
        </button>
        <span>
          Page {page} of {totalPages}
        </span>
        <button
          className="button button-secondary"
          type="button"
          onClick={() => setPage((current) => current + 1)}
          disabled={!canGoNext || loading}
        >
          Next
        </button>
      </nav>
    </section>
  );
}

export default ProjectsPage;
