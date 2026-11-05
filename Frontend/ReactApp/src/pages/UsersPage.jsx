import { useCallback, useEffect, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import FormField from '../components/FormField';
import { StatusBadge } from '../components/badges';
import { EmptyState, LoadingState } from '../components/LoadingEmptyState';
import ActiveFilterChips from '../components/ActiveFilterChips';
import { getApiErrorMessage } from '../services/apiErrors';
import { activateUser, createUser, deleteUser, getUsers, updateUser } from '../services/userApi';
import { useAuth } from '../context/AuthContext';

const EMPTY_FORM = {
  firstName: '',
  lastName: '',
  email: '',
  userName: '',
  password: '',
  role: 'TeamMember'
};
const USERS_PAGE_SIZE = 10;

function getValidationMessage(error, fallback = 'Unable to create this user.') {
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

function UsersPage() {
  const [form, setForm] = useState(EMPTY_FORM);
  const [saving, setSaving] = useState(false);
  const [error, setError] = useState('');
  const [notice, setNotice] = useState('');
  const [users, setUsers] = useState([]);
  const [loadingUsers, setLoadingUsers] = useState(true);
  const [deactivatingUserId, setDeactivatingUserId] = useState('');
  const [editingUserId, setEditingUserId] = useState(null);
  const [formOpen, setFormOpen] = useState(false);
  const [search, setSearch] = useState('');
  const [roleFilter, setRoleFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [page, setPage] = useState(1);
  const { user: currentUser } = useAuth();

  const loadUsers = useCallback(async () => {
    setLoadingUsers(true);
    try {
      const items = await getUsers({ includeInactive: true });
      setUsers(Array.isArray(items) ? items : []);
    } catch (requestError) {
      setError(getApiErrorMessage(requestError, 'Unable to load users.'));
    } finally {
      setLoadingUsers(false);
    }
  }, []);

  useEffect(() => { loadUsers(); }, [loadUsers]);

  useEffect(() => {
    if (!formOpen) return undefined;
    function closeOnEscape(event) { if (event.key === 'Escape') closeForm(); }
    window.addEventListener('keydown', closeOnEscape);
    return () => window.removeEventListener('keydown', closeOnEscape);
  }, [formOpen]);

  function closeForm() {
    setFormOpen(false);
    setEditingUserId(null);
    setForm(EMPTY_FORM);
    setError('');
  }

  function openCreateForm() {
    setEditingUserId(null);
    setForm(EMPTY_FORM);
    setError('');
    setNotice('');
    setFormOpen(true);
  }

  function changeField(event) {
    const { name, value } = event.target;
    setForm((current) => ({ ...current, [name]: value }));
    setError('');
    setNotice('');
  }

  async function handleSubmit(event) {
    event.preventDefault();
    if (saving) {
      return;
    }

    setSaving(true);
    setError('');
    setNotice('');
    try {
      const payload = editingUserId !== null ? Object.fromEntries(
        Object.entries(form).filter(([key, value]) => key === 'password' ? Boolean(value) : key === 'role' || Boolean(value.trim()))
      ) : {
        ...form,
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        userName: form.userName.trim()
      };
      const user = editingUserId !== null
        ? await updateUser(editingUserId, payload)
        : await createUser(payload);
      const isEditing = editingUserId !== null;
      setForm(EMPTY_FORM);
      setEditingUserId(null);
      setFormOpen(false);
      setNotice(`${user.firstName} ${user.lastName} was ${isEditing ? 'updated' : 'created'} successfully.`);
      await loadUsers();
    } catch (requestError) {
      setError(getValidationMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  function startEditing(user) {
    setEditingUserId(user.id);
    setForm({ firstName: user.firstName, lastName: user.lastName, email: user.email, userName: user.userName, password: '', role: user.role });
    setError('');
    setNotice('');
    setFormOpen(true);
  }

  async function changeUserStatus(user, activate) {
    if (!activate && !window.confirm(`Deactivate ${user.firstName} ${user.lastName}?`)) return;
    setDeactivatingUserId(user.id);
    setError('');
    setNotice('');
    try {
      if (activate) await activateUser(user.id);
      else await deleteUser(user.id);
      setUsers((current) => current.map((item) => (
        item.id === user.id ? { ...item, isActive: activate } : item
      )));
      setNotice(`${user.firstName} ${user.lastName} was ${activate ? 'activated' : 'deactivated'}.`);
    } catch (requestError) {
      setError(getValidationMessage(requestError, 'Unable to update this account.'));
    } finally {
      setDeactivatingUserId('');
    }
  }

  const filteredUsers = users.filter((item) => {
    const query = search.trim().toLowerCase();
    return (!query || `${item.firstName} ${item.lastName} ${item.email} ${item.userName}`.toLowerCase().includes(query))
      && (!roleFilter || item.role === roleFilter)
      && (!statusFilter || item.isActive === (statusFilter === 'active'));
  });
  const totalPages = Math.ceil(filteredUsers.length / USERS_PAGE_SIZE);
  const visibleUsers = filteredUsers.slice((page - 1) * USERS_PAGE_SIZE, page * USERS_PAGE_SIZE);
  const showingFrom = filteredUsers.length ? (page - 1) * USERS_PAGE_SIZE + 1 : 0;
  const showingTo = Math.min(page * USERS_PAGE_SIZE, filteredUsers.length);

  return (
    <section className="users-page">
      <div className="projects-heading">
        <div><p className="eyebrow">Administration</p><h1>Users</h1></div>
        {!formOpen && <button className="button button-primary" type="button" onClick={openCreateForm}>Add user</button>}
      </div>

      {notice && <div className="users-toast" role="status">{notice}</div>}
      <ErrorMessage message={error} />

      {formOpen && <div className="user-modal-backdrop" onMouseDown={(event) => { if (event.target === event.currentTarget && !saving) closeForm(); }}>
        <section className="user-modal" role="dialog" aria-modal="true" aria-labelledby="user-form-title">
          <div className="project-form-heading">
            <h2 id="user-form-title">{editingUserId !== null ? 'Edit account' : 'Add user'}</h2>
            <button className="user-modal-close" type="button" aria-label="Close" onClick={closeForm} disabled={saving}>×</button>
          </div>

        <ErrorMessage message={error} />
        <form className="project-form" onSubmit={handleSubmit}>
          <div className="project-field">
            <FormField id="user-first-name" label="First name" required={editingUserId === null}><input id="user-first-name" name="firstName" value={form.firstName} onChange={changeField} maxLength={100} autoComplete="given-name" required={editingUserId === null} disabled={saving} /></FormField>
          </div>
          <div className="project-field">
            <FormField id="user-last-name" label="Last name" required={editingUserId === null}><input id="user-last-name" name="lastName" value={form.lastName} onChange={changeField} maxLength={100} autoComplete="family-name" required={editingUserId === null} disabled={saving} /></FormField>
          </div>
          <div className="project-field">
            <FormField id="user-email" label="Email" required={editingUserId === null}><input id="user-email" name="email" type="email" value={form.email} onChange={changeField} maxLength={200} autoComplete="email" required={editingUserId === null} disabled={saving} /></FormField>
          </div>
          <div className="project-field">
            <FormField id="user-name" label="Username" required={editingUserId === null}><input id="user-name" name="userName" value={form.userName} onChange={changeField} maxLength={100} autoComplete="username" required={editingUserId === null} disabled={saving} /></FormField>
          </div>
          <div className="project-field">
            <FormField id="user-password" label={editingUserId !== null ? 'New password' : 'Temporary password'} required={editingUserId === null}><input id="user-password" name="password" type="password" value={form.password} onChange={changeField} autoComplete="new-password" required={editingUserId === null} disabled={saving} /></FormField>
          </div>
          <div className="project-field">
            <FormField id="user-role" label="Role" required={editingUserId === null}><select id="user-role" name="role" value={form.role} onChange={changeField} required={editingUserId === null} disabled={saving}>
              <option value="Admin">Admin</option>
              <option value="ProjectManager">Project Manager</option>
              <option value="TeamMember">Team Member</option>
            </select></FormField>
          </div>
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving}>
              {saving ? 'Saving…' : editingUserId !== null ? 'Save changes' : 'Create user'}
            </button>
            <button className="button button-secondary" type="button" onClick={closeForm} disabled={saving}>Cancel</button>
          </div>
        </form>
        </section>
      </div>}

      <section className="project-form-card" aria-labelledby="user-list-title">
        <h2 id="user-list-title" className="users-list-heading">All users</h2>
        <div className="users-toolbar">
          <label><span>Search</span><input type="search" value={search} onChange={(event) => { setSearch(event.target.value); setPage(1); }} placeholder="Name, email, or username" /></label>
          <label><span>Role</span><select value={roleFilter} onChange={(event) => { setRoleFilter(event.target.value); setPage(1); }}><option value="">All roles</option><option value="Admin">Admin</option><option value="ProjectManager">Project Manager</option><option value="TeamMember">Team Member</option></select></label>
          <label><span>Status</span><select value={statusFilter} onChange={(event) => { setStatusFilter(event.target.value); setPage(1); }}><option value="">All statuses</option><option value="active">Active</option><option value="inactive">Inactive</option></select></label>
          <span className="users-range">{showingFrom}-{showingTo} of {filteredUsers.length}</span>
        </div>
        <ActiveFilterChips filters={[
          ...(search.trim() ? [{ key: 'search', label: 'Search', value: search.trim(), onRemove: () => { setSearch(''); setPage(1); } }] : []),
          ...(roleFilter ? [{ key: 'role', label: 'Role', value: roleFilter === 'ProjectManager' ? 'Project Manager' : roleFilter === 'TeamMember' ? 'Team Member' : roleFilter, onRemove: () => { setRoleFilter(''); setPage(1); } }] : []),
          ...(statusFilter ? [{ key: 'status', label: 'Status', value: statusFilter === 'active' ? 'Active' : 'Inactive', onRemove: () => { setStatusFilter(''); setPage(1); } }] : [])
        ]} onClearAll={() => { setSearch(''); setRoleFilter(''); setStatusFilter(''); setPage(1); }} />
        {loadingUsers ? <LoadingState message="Loading users…" as="p" /> : filteredUsers.length === 0 ? <EmptyState title={users.length ? 'No users match these filters.' : 'No users found.'} /> : (
          <div className="task-table-wrap">
            <table className="task-table users-table">
              <thead><tr><th>Name</th><th>Email</th><th>Username</th><th>Role</th><th>Status</th><th>Actions</th></tr></thead>
              <tbody>
                {visibleUsers.map((user) => (
                  <tr key={user.id} className={user.isActive ? '' : 'user-row-inactive'}>
                    <td data-label="Name"><span className="user-name-cell"><span className="user-avatar" aria-hidden="true">{`${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toUpperCase()}</span><strong>{user.firstName} {user.lastName}</strong></span></td>
                    <td data-label="Email">{user.email}</td>
                    <td data-label="Username">{user.userName}</td>
                    <td data-label="Role">
                      <span className={`user-role-badge user-role-${user.role === 'ProjectManager' ? 'manager' : user.role === 'TeamMember' ? 'member' : 'admin'}`}>{user.role === 'ProjectManager' ? 'Project Manager' : user.role === 'TeamMember' ? 'Team Member' : user.role}</span>
                    </td>
                    <td data-label="Status"><StatusBadge status={user.isActive ? 'Active' : 'Inactive'} className={`user-status-badge ${user.isActive ? 'user-status-active' : 'user-status-inactive'}`} /></td>
                    <td data-label="Actions">
                      <div className="user-actions">
                        <button className="user-icon-button" type="button" aria-label={`Edit ${user.firstName} ${user.lastName}`} title="Edit user" onClick={() => startEditing(user)}><svg viewBox="0 0 24 24" aria-hidden="true"><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z"/></svg></button>
                        <button className="user-status-action" type="button" title={String(user.id) === String(currentUser?.id) ? 'You cannot deactivate your own account' : user.role === 'Admin' && user.isActive ? 'Another admin cannot deactivate this account' : user.isActive ? 'Deactivate user' : 'Activate user'} aria-label={user.isActive ? 'Deactivate user' : 'Activate user'} onClick={() => changeUserStatus(user, !user.isActive)} disabled={Boolean(deactivatingUserId) || String(user.id) === String(currentUser?.id) || user.role === 'Admin' && user.isActive}><svg viewBox="0 0 24 24" aria-hidden="true">{user.isActive ? <><path d="M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="10" cy="7" r="4"/><path d="M17 11h6"/></> : <><path d="M16 21v-2a4 4 0 0 0-4-4H8a4 4 0 0 0-4 4v2"/><circle cx="10" cy="7" r="4"/><path d="M17 11h6m-3-3v6"/></>}</svg></button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
        {totalPages > 1 && <nav className="users-pagination" aria-label="User pages"><button className="button button-secondary" type="button" onClick={() => setPage((value) => value - 1)} disabled={page <= 1}>Previous</button><span>Page {page} of {totalPages}</span><button className="button button-secondary" type="button" onClick={() => setPage((value) => value + 1)} disabled={page >= totalPages}>Next</button></nav>}
      </section>
    </section>
  );
}

export default UsersPage;
