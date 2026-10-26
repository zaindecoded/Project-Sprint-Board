import apiClient from './apiClient';

export async function getUsers({ signal, includeInactive = false } = {}) {
  const { data } = await apiClient.get('/users', { signal, params: includeInactive ? { includeInactive: true } : undefined });
  return data;
}

export async function createUser(user) {
  const { data } = await apiClient.post('/users', user);
  return data;
}

export async function updateUser(id, user) {
  const { data } = await apiClient.put(`/users/${id}`, user);
  return data;
}

export async function updateUserRole(id, role) {
  const { data } = await apiClient.patch(`/users/${id}/role`, { role });
  return data;
}

export async function deleteUser(id) {
  await apiClient.delete(`/users/${id}`);
}

export async function activateUser(id) {
  const { data } = await apiClient.patch(`/users/${id}/activate`);
  return data;
}
