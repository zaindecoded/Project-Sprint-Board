import apiClient from './apiClient';

export async function getUsers({ signal } = {}) {
  const { data } = await apiClient.get('/users', { signal });
  return data;
}

export async function createUser(user) {
  const { data } = await apiClient.post('/users', user);
  return data;
}

export async function updateUserRole(id, role) {
  const { data } = await apiClient.patch(`/users/${id}/role`, { role });
  return data;
}

export async function deleteUser(id) {
  await apiClient.delete(`/users/${id}`);
}
