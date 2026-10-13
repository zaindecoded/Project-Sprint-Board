import apiClient from './apiClient';

export async function getDashboardSummary(signal) {
  const { data } = await apiClient.get('/dashboard/summary', { signal });
  return data;
}

export async function getDashboardAttentionTasks(signal) {
  const { data } = await apiClient.get('/dashboard/attention-tasks', { signal });
  return data;
}
