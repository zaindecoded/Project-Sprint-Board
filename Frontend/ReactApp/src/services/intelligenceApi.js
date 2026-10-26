import apiClient from './apiClient';

export async function evaluateTaskRisk(input, signal) {
  const { data } = await apiClient.post('/intelligence/evaluate', input, { signal });
  return data;
}

export async function getLatestTaskRisk(taskId, signal) {
  const { data } = await apiClient.get(`/intelligence/tasks/${taskId}/latest`, { signal });
  return data;
}
