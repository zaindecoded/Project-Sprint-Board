import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import ErrorMessage from '../../components/ErrorMessage';
import { useAuth } from '../../context/AuthContext';
import { createProject, deleteProject, getProject, getProjects, updateProject } from '../../services/projectApi';
import { getApiErrorMessage } from '../../services/apiErrors';
import { canManageWorkspace } from '../../utils/permissions';
import { getProjectDisplayStatus, getTodayDateInputValue } from '../../utils/displayStatus';

const PAGE_SIZE = 10;
const EMPTY_FORM = { name: '', description: '', startDate: '', endDate: '', status: 'Planned' };

function formatDate(value) {
  if (!value) {
    return '—';
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime())
    ? '—'
    : new Intl.DateTimeFormat(undefined, { year: 'numeric', month: 'short', day: 'numeric' }).format(date);
}

function formatInlineDate(value) {
  if (!value) return '—';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? '—' : new Intl.DateTimeFormat(undefined, { month: 'short', day: 'numeric', year: 'numeric' }).format(date);
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
  const nameValid = Boolean(form.name.trim());
  const datesValid = !form.startDate || !form.endDate || form.endDate >= form.startDate;

  useEffect(() => {
    function onKeyDown(event) {
      if (event.key === 'Escape' && !saving) onCancel();
    }
    window.addEventListener('keydown', onKeyDown);
    return () => window.removeEventListener('keydown', onKeyDown);
  }, [onCancel, saving]);

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
            status: item.status || 'Planned'
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

    if (!nameValid || !datesValid) {
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
        await createProject(body);
        onSaved('Project created successfully.');
      }
    } catch (requestError) {
      setError(getValidationMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  return (
    <div className="project-modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget && !saving) onCancel(); }}>
    <section className="project-modal" role="dialog" aria-modal="true" aria-labelledby="project-form-title">
      <div className="project-form-heading">
        <div>
          <p className="eyebrow">{project ? 'Project details' : 'New project'}</p>
          <h2 id="project-form-title">{project ? 'Edit project' : 'Create project'}</h2>
        </div>
        <button className="project-modal-close" type="button" aria-label="Close" onClick={onCancel} disabled={saving}>
          ×
        </button>
      </div>

      <ErrorMessage>{error}</ErrorMessage>

      {loadingProject ? (
        <p className="project-loading" role="status">Loading project details…</p>
      ) : (
        <form className="project-form" onSubmit={handleSubmit} noValidate>
          <div className="project-field project-field-wide">
            <label htmlFor="project-name">Project name <span className="required-mark" aria-hidden="true">*</span></label>
            <input
              id="project-name"
              name="name"
              value={form.name}
              onChange={changeField}
              maxLength={200}
              disabled={saving}
            />
            {!nameValid && <span className="project-field-error">Project name is required.</span>}
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
            {!datesValid && <span className="project-field-error">End date must be on or after the start date.</span>}
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
            <button className="button button-secondary" type="button" onClick={onCancel} disabled={saving}>Cancel</button>
            <button className="button button-primary" type="submit" disabled={saving || loadingProject || !nameValid || !datesValid}>
              {saving ? 'Saving…' : project ? 'Save changes' : 'Create project'}
            </button>
          </div>
        </form>
      )}
    </section>
    </div>
  );
}

function ProjectsPage() {
  const { user } = useAuth();
  const navigate = useNavigate();
  const canManage = canManageWorkspace(user);
  const [projects, setProjects] = useState([]);
  const [searchInput, setSearchInput] = useState('');
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState('');
  const [sortBy, setSortBy] = useState('newest');
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [totalPages, setTotalPages] = useState(0);
  const [loading, setLoading] = useState(true);
  const [listError, setListError] = useState('');
  const [notice, setNotice] = useState('');
  const [editingProject, setEditingProject] = useState(null);
  const [formOpen, setFormOpen] = useState(false);
  const [deletingId, setDeletingId] = useState(null);
  const [projectToDelete, setProjectToDelete] = useState(null);

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
      const result = await getProjects({ page, pageSize: PAGE_SIZE, search, status, sortBy, signal });
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
  }, [page, search, status, sortBy]);

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
    await loadProjects();
    setFormOpen(false);
    setEditingProject(null);
    setNotice(message);
  }

  async function handleDelete(project) {
    setDeletingId(project.id);
    setListError('');
    setNotice('');
    try {
      await deleteProject(project.id);
      setProjectToDelete(null);
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
        <label className="project-filter"><span>Status</span><select value={status} onChange={(event) => { setStatus(event.target.value); setPage(1); }}><option value="">All statuses</option><option value="Active">Active</option><option value="Completed">Completed</option><option value="Planned">Planned</option><option value="OnHold">On hold</option><option value="Cancelled">Cancelled</option></select></label>
        <label className="project-filter"><span>Sort by</span><select value={sortBy} onChange={(event) => { setSortBy(event.target.value); setPage(1); }}><option value="newest">Newest</option><option value="name">Name A–Z</option><option value="endDate">End date</option></select></label>
        <span className="project-count">Showing {totalCount ? (page - 1) * PAGE_SIZE + 1 : 0}-{Math.min(page * PAGE_SIZE, totalCount)} of {totalCount}</span>
      </div>

      <ErrorMessage>{listError}</ErrorMessage>

      {loading ? (
        <div className="project-state" role="status">Loading projects…</div>
      ) : projects.length === 0 ? (
        <div className="project-state project-empty">
          <h2>{search || status ? 'No matching projects' : 'No projects yet'}</h2>
          <p>{search || status ? 'Try adjusting your search or filters.' : 'Create a project to get started.'}</p>
          {(search || status) && <button className="button button-secondary" type="button" onClick={() => { setSearchInput(''); setSearch(''); setStatus(''); setSortBy('newest'); setPage(1); }}>Clear filters</button>}
          {!search && !status && canManage && !formOpen && (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>
              Create your first project
            </button>
          )}
        </div>
      ) : (
        <div className="project-list">
          {projects.map((project) => (
            <article className="project-card project-card-clickable" key={project.id} role="link" tabIndex="0" onClick={() => navigate(`/projects/${project.id}`)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); navigate(`/projects/${project.id}`); } }}>
              <div className="project-card-main">
                <div className="project-card-title-row">
                  <h2>{project.name}</h2>
                  <span className={`project-status project-status-${String(project.status || '').toLowerCase().replace(/[^a-z]/g, '')}`}>
                    {getProjectDisplayStatus(project)}
                  </span>
                </div>
                {project.description && <p className="project-description">{project.description}</p>}
                <p className="project-inline-dates">{formatInlineDate(project.startDate)} <span aria-hidden="true">→</span> {formatInlineDate(project.endDate)}</p>
              </div>
              {canManage && <div className="project-actions">
                <button className="user-icon-button" type="button" aria-label="Edit project" title="Edit project" onClick={(event) => { event.stopPropagation(); openEditForm(project); }}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z"/></svg></button>
                <button className="user-icon-button project-delete-icon" type="button" aria-label={deletingId === project.id ? 'Deleting project' : 'Delete project'} title="Delete project" onClick={(event) => { event.stopPropagation(); setProjectToDelete(project); }} disabled={deletingId === project.id}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M3 6h18M8 6V4h8v2m3 0-1 14H6L5 6m4 4v6m6-6v6"/></svg></button>
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
      {projectToDelete && <div className="project-confirm-backdrop" role="presentation"><section className="project-confirm-dialog" role="alertdialog" aria-modal="true" aria-labelledby="project-delete-title"><h2 id="project-delete-title">Delete project?</h2><p>Delete {projectToDelete.name}? This can't be undone.</p><div><button className="button button-secondary" type="button" onClick={() => setProjectToDelete(null)}>Cancel</button><button className="button project-confirm-delete" type="button" onClick={() => handleDelete(projectToDelete)} disabled={deletingId === projectToDelete.id}>{deletingId === projectToDelete.id ? 'Deleting…' : 'Delete'}</button></div></section></div>}
    </section>
  );
}

export default ProjectsPage;
