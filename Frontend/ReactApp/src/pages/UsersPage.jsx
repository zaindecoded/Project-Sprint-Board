import { useCallback, useEffect, useState } from 'react';
import ErrorMessage from '../components/ErrorMessage';
import { getApiErrorMessage } from '../services/apiErrors';
import { activateUser, createUser, deleteUser, getUsers } from '../services/userApi';
import { useAuth } from '../context/AuthContext';

const EMPTY_FORM = {
  firstName: '',
  lastName: '',
  email: '',
  userName: '',
  password: '',
  role: 'TeamMember'
};

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
      const user = await createUser({
        ...form,
        firstName: form.firstName.trim(),
        lastName: form.lastName.trim(),
        email: form.email.trim(),
        userName: form.userName.trim()
      });
      setForm(EMPTY_FORM);
      setNotice(`${user.firstName} ${user.lastName} was created successfully.`);
      await loadUsers();
    } catch (requestError) {
      setError(getValidationMessage(requestError));
    } finally {
      setSaving(false);
    }
  }

  async function changeUserStatus(user, activate) {
    const verb = activate ? 'Reactivate' : 'Deactivate';
    if (!window.confirm(`${verb} ${user.firstName} ${user.lastName}?`)) return;
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

  return (
    <section className="projects-page">
      <div className="projects-heading">
        <div>
          <h1>Administration</h1>
        </div>
      </div>

      {notice && <div className="project-notice" role="status">{notice}</div>}
      <ErrorMessage>{error}</ErrorMessage>

      <section className="project-form-card" aria-labelledby="user-form-title">
        <div className="project-form-heading">
          <div>
            <h2 id="user-form-title">New account</h2>
          </div>
        </div>

        <form className="project-form" onSubmit={handleSubmit}>
          <div className="project-field">
            <label htmlFor="user-first-name">First name <span aria-hidden="true">*</span></label>
            <input id="user-first-name" name="firstName" value={form.firstName} onChange={changeField} maxLength={100} autoComplete="given-name" required disabled={saving} />
          </div>
          <div className="project-field">
            <label htmlFor="user-last-name">Last name <span aria-hidden="true">*</span></label>
            <input id="user-last-name" name="lastName" value={form.lastName} onChange={changeField} maxLength={100} autoComplete="family-name" required disabled={saving} />
          </div>
          <div className="project-field">
            <label htmlFor="user-email">Email <span aria-hidden="true">*</span></label>
            <input id="user-email" name="email" type="email" value={form.email} onChange={changeField} maxLength={200} autoComplete="email" required disabled={saving} />
          </div>
          <div className="project-field">
            <label htmlFor="user-name">Username <span aria-hidden="true">*</span></label>
            <input id="user-name" name="userName" value={form.userName} onChange={changeField} maxLength={100} autoComplete="username" required disabled={saving} />
          </div>
          <div className="project-field">
            <label htmlFor="user-password">Temporary password <span aria-hidden="true">*</span></label>
            <input id="user-password" name="password" type="password" value={form.password} onChange={changeField} autoComplete="new-password" required disabled={saving} />
          </div>
          <div className="project-field">
            <label htmlFor="user-role">Role <span aria-hidden="true">*</span></label>
            <select id="user-role" name="role" value={form.role} onChange={changeField} required disabled={saving}>
              <option value="Admin">Admin</option>
              <option value="ProjectManager">Project Manager</option>
              <option value="TeamMember">Team Member</option>
            </select>
          </div>
          <div className="project-form-actions project-field-wide">
            <button className="button button-primary" type="submit" disabled={saving}>
              {saving ? 'Creating user…' : 'Create user'}
            </button>
          </div>
        </form>
      </section>

      <section className="project-form-card" aria-labelledby="user-list-title">
        <div className="project-form-heading">
          <div>
          </div>
        </div>
        {loadingUsers ? <p role="status">Loading users…</p> : users.length === 0 ? <p>No active users found.</p> : (
          <div className="task-table-wrap">
            <table className="task-table">
              <thead><tr><th>Name</th><th>Email</th><th>Username</th><th>Role</th><th>Actions</th></tr></thead>
              <tbody>
                {users.map((user) => (
                  <tr key={user.id}>
                    <td data-label="Name"><strong>{user.firstName} {user.lastName}</strong></td>
                    <td data-label="Email">{user.email}</td>
                    <td data-label="Username">{user.userName}</td>
                    <td data-label="Role">
                      {user.role === 'ProjectManager' ? 'Project Manager' : user.role === 'TeamMember' ? 'Team Member' : user.role}
                    </td>
                    <td data-label="Actions">
                      {user.role === 'Admin' && String(user.id) !== String(currentUser?.id) ? (
                        null
                      ) : (
                        <button
                          className={`button ${user.isActive ? 'button-danger' : 'button-primary'}`}
                          type="button"
                          onClick={() => changeUserStatus(user, !user.isActive)}
                          disabled={Boolean(deactivatingUserId)}
                        >
                          {deactivatingUserId === user.id ? 'Saving…' : user.isActive ? 'Deactivate' : 'Activate'}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </section>
  );
}

export default UsersPage;
