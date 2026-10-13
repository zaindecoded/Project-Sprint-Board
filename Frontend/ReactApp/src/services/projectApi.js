import apiClient from './apiClient';

export async function getProjects({ page, pageSize, search, signal }) {
  const { data } = await apiClient.get('/projects', {
    params: { page, pageSize, search },
    signal
  });
  return data;
}

export async function getProject(id) {
  const { data } = await apiClient.get(`/projects/${id}`);
  return data;
}

export async function createProject(project) {
  const { data } = await apiClient.post('/projects', project);
  return data;
}

export async function updateProject(id, project) {
  const { data } = await apiClient.put(`/projects/${id}`, project);
  return data;
}

export async function deleteProject(id) {
  await apiClient.delete(`/projects/${id}`);
}
