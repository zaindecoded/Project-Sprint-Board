import apiClient from './apiClient';

export async function getComments({ page, pageSize, search, taskId, userId, signal }) {
  const { data } = await apiClient.get('/comments', {
    params: {
      page,
      pageSize,
      search,
      taskId: taskId || undefined,
      userId: userId || undefined
    },
    signal
  });
  return data;
}

export async function getComment(id) {
  const { data } = await apiClient.get(`/comments/${id}`);
  return data;
}

export async function createComment(comment) {
  const { data } = await apiClient.post('/comments', comment);
  return data;
}

export async function updateComment(id, comment) {
  const { data } = await apiClient.put(`/comments/${id}`, comment);
  return data;
}

export async function deleteComment(id) {
  await apiClient.delete(`/comments/${id}`);
}
