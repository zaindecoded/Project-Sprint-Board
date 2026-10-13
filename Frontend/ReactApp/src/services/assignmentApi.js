import apiClient from './apiClient';

export async function getAssignments({ page, pageSize, search, taskId, userId, isActive, signal }) {
  const { data } = await apiClient.get('/assignments', {
    params: {
      page,
      pageSize,
      search,
      taskId: taskId || undefined,
      userId: userId || undefined,
      isActive: isActive === '' ? undefined : isActive
    },
    signal
  });
  return data;
}

export async function getAssignment(id) {
  const { data } = await apiClient.get(`/assignments/${id}`);
  return data;
}

export async function createAssignment(assignment) {
  const { data } = await apiClient.post('/assignments', assignment);
  return data;
}

export async function updateAssignment(id, assignment) {
  const { data } = await apiClient.put(`/assignments/${id}`, assignment);
  return data;
}

export async function deleteAssignment(id) {
  await apiClient.delete(`/assignments/${id}`);
}
