import apiClient from './apiClient';

export async function evaluateTaskRisk(input, signal) {
  const { data } = await apiClient.post('/intelligence/evaluate', input, { signal });
  return data;
}
