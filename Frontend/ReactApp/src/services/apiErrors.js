export function getApiErrorMessage(error, fallback = 'Something went wrong. Please try again.') {
  const status = error.response?.status;

  if (status === 400) {
    const errors = error.response.data?.errors;
    if (errors && typeof errors === 'object') {
      const messages = Object.values(errors).flat().filter((message) => typeof message === 'string');
      if (messages.length) {
        return messages.join(' ');
      }
    }
    return 'Please check the information you entered and try again.';
  }
  if (status === 401) {
    return error.config?.url?.includes('/auth/login')
      ? 'The email or password is incorrect. Please try again.'
      : 'Your session is no longer valid. Please sign in again.';
  }
  if (status === 403) {
    return 'You do not have permission to perform this action.';
  }
  if (status === 404) {
    return 'The requested information could not be found.';
  }
  if (status >= 500) {
    return 'The server could not complete your request. Please try again later.';
  }
  if (!error.response) {
    return 'Unable to reach the server. Check your connection and try again.';
  }
  return fallback;
}
