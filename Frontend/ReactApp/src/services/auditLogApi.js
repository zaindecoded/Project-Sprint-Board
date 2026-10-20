import apiClient from './apiClient';

export async function getAuditLogs(params, { signal } = {}) {
  const { data } = await apiClient.get('/auditlogs', { params, signal });
  return data;
}

export async function getAuditLogFilters({ signal } = {}) {
  const { data } = await apiClient.get('/auditlogs/filters', { signal });
  return data;
}
