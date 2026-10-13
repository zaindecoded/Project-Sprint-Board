import apiClient from './apiClient';

export async function getSprints({ page, pageSize, search, projectId, signal }) {
  const { data } = await apiClient.get('/sprints', {
    params: { page, pageSize, search, projectId: projectId || undefined },
    signal
  });
  return data;
}

export async function getSprint(id) {
  const { data } = await apiClient.get(`/sprints/${id}`);
  return data;
}

export async function createSprint(sprint) {
  const { data } = await apiClient.post('/sprints', sprint);
  return data;
}

export async function updateSprint(id, sprint) {
  const { data } = await apiClient.put(`/sprints/${id}`, sprint);
  return data;
}

export async function deleteSprint(id) {
  await apiClient.delete(`/sprints/${id}`);
}
