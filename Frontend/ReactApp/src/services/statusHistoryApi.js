import apiClient from './apiClient';

export async function getTaskStatusHistory(taskId, signal) {
  const { data } = await apiClient.get(`/tasks/${taskId}/status-history`, { signal });
  return data;
}
