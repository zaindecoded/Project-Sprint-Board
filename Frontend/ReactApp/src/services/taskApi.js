import apiClient from './apiClient';

export async function getTasks({ page, pageSize, search, status, priority, projectId, sprintId, dueDate, isBlocked, signal }) {
  const { data } = await apiClient.get('/tasks', {
    params: {
      page,
      pageSize,
      search,
      status: status || undefined,
      priority: priority || undefined,
      projectId: projectId || undefined,
      sprintId: sprintId || undefined,
      dueDate: dueDate || undefined,
      isBlocked: isBlocked === '' ? undefined : isBlocked
    },
    signal
  });
  return data;
}

export async function getTask(id) {
  const { data } = await apiClient.get(`/tasks/${id}`);
  return data;
}

export async function createTask(task) {
  const { data } = await apiClient.post('/tasks', task);
  return data;
}

export async function updateTask(id, task) {
  const { data } = await apiClient.put(`/tasks/${id}`, task);
  return data;
}

export async function completeAssignedTask(id) {
  const { data } = await apiClient.patch(`/tasks/${id}/complete`);
  return data;
}

export async function deleteTask(id) {
  await apiClient.delete(`/tasks/${id}`);
}
