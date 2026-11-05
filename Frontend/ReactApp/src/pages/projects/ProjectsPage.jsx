import { useCallback, useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import ErrorMessage from '../../components/ErrorMessage';
import FormField from '../../components/FormField';
import { StatusBadge } from '../../components/badges';
import { EmptyState, LoadingState } from '../../components/LoadingEmptyState';
import ActiveFilterChips from '../../components/ActiveFilterChips';
import Pagination from '../../components/Pagination';
import IconActionButton from '../../components/IconActionButton';
import Delete from '../../components/Delete';
import SearchFilterToolbar from '../../components/SearchFilterToolbar';
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
      statusId: ({ Planned: 1, Active: 2, OnHold: 3, Completed: 4, Cancelled: 5 })[form.status] ?? 2
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

      <ErrorMessage message={error} />

      {loadingProject ? (
        <LoadingState message="Loading project details…" as="p" className="project-loading" />
      ) : (
        <form className="project-form" onSubmit={handleSubmit} noValidate>
          <div className="project-field project-field-wide">
            <FormField id="project-name" label="Project name" required><input
              id="project-name"
              name="name"
              value={form.name}
              onChange={changeField}
              maxLength={200}
              disabled={saving}
            /></FormField>
            {!nameValid && <span className="project-field-error"></span>}
          </div>
          <div className="project-field project-field-wide">
            <FormField id="project-description" label="Description"><textarea
              id="project-description"
              name="description"
              value={form.description}
              onChange={changeField}
              maxLength={1000}
              rows={3}
              disabled={saving}
            /></FormField>
          </div>
          <div className="project-field">
            <FormField id="project-start-date" label="Start date"><input
              id="project-start-date"
              name="startDate"
              type="date"
              value={form.startDate}
              onChange={changeField}
              min={!project ? getTodayDateInputValue() : undefined}
              disabled={saving}
            /></FormField>
            {!datesValid && <span className="project-field-error">End date must be on or after the start date.</span>}
          </div>
          <div className="project-field">
            <FormField id="project-end-date" label="End date"><input
              id="project-end-date"
              name="endDate"
              type="date"
              value={form.endDate}
              onChange={changeField}
              min={!project ? (form.startDate > getTodayDateInputValue() ? form.startDate : getTodayDateInputValue()) : (form.startDate || undefined)}
              disabled={saving}
            /></FormField>
          </div>
          <div className="project-field">
            <FormField id="project-status" label="Project status"><select id="project-status" name="status" value={form.status} onChange={changeField} disabled={saving}>
              <option value="Planned">Planned</option>
              <option value="Active">Active</option>
              <option value="OnHold">On hold</option>
              <option value="Completed">Completed</option>
              <option value="Cancelled">Cancelled</option>
            </select></FormField>
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

      <SearchFilterToolbar className="projects-toolbar">
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
        <span className="project-count">{totalCount ? (page - 1) * PAGE_SIZE + 1 : 0}-{Math.min(page * PAGE_SIZE, totalCount)} of {totalCount}</span>
      </SearchFilterToolbar>

      <ActiveFilterChips filters={[
        ...(search ? [{ key: 'search', label: 'Search', value: search, onRemove: () => { setSearchInput(''); setSearch(''); setPage(1); } }] : []),
        ...(status ? [{ key: 'status', label: 'Status', value: status, onRemove: () => { setStatus(''); setPage(1); } }] : [])
      ]} onClearAll={() => { setSearchInput(''); setSearch(''); setStatus(''); setSortBy('newest'); setPage(1); }} />

      <ErrorMessage message={listError} />

      {loading ? (
        <LoadingState message="Loading projects…" />
      ) : projects.length === 0 ? (
        <EmptyState title={search || status ? 'No matching projects' : 'No projects yet'} description={search || status ? 'Try adjusting your search or filters.' : 'Create a project to get started.'} action={!search && !status && canManage && !formOpen ? (
            <button className="button button-secondary" type="button" onClick={openCreateForm}>
              Create your first project
            </button>
          ) : null} />
      ) : (
        <div className="project-list">
          {projects.map((project) => (
            <article className="project-card project-card-clickable" key={project.id} role="link" tabIndex="0" onClick={() => navigate(`/projects/${project.id}`)} onKeyDown={(event) => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); navigate(`/projects/${project.id}`); } }}>
              <div className="project-card-main">
                <div className="project-card-title-row">
                  <h2>{project.name}</h2>
                  <StatusBadge status={getProjectDisplayStatus(project)} className={`project-status project-status-${String(project.status || '').toLowerCase().replace(/[^a-z]/g, '')}`} />
                </div>
                <p className="project-inline-dates">{formatInlineDate(project.startDate)} <span aria-hidden="true">→</span> {formatInlineDate(project.endDate)}</p>
              </div>
              {canManage && <div className="project-actions">
                <IconActionButton action="edit" label="Edit project" onClick={(event) => { event.stopPropagation(); openEditForm(project); }} />
                <IconActionButton action="delete" className="user-icon-button project-delete-icon" label={deletingId === project.id ? 'Deleting project' : 'Delete project'} onClick={(event) => { event.stopPropagation(); setProjectToDelete(project); }} disabled={deletingId === project.id} />
              </div>}
            </article>
          ))}
        </div>
      )}

      <Pagination page={page} totalPages={totalPages} onPageChange={setPage} disabled={loading} label="Project pages" />
      {projectToDelete && <Delete title="Delete project?" className="project-confirm-dialog" backdropClassName="project-confirm-backdrop" onCancel={() => setProjectToDelete(null)} onConfirm={() => handleDelete(projectToDelete)} busy={deletingId === projectToDelete.id} busyLabel="Deleting…">Delete {projectToDelete.name}? This can't be undone.</Delete>}
    </section>
  );
}

export default ProjectsPage;
