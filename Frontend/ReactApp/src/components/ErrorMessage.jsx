function ErrorMessage({ message, role = 'alert', className = '' }) {
  if (message == null || message === false || message === '') {
    return null;
  }

  return <div className={`message message-error ${className}`.trim()} role={role} aria-live={role === 'alert' ? 'assertive' : 'polite'}>{message}</div>;
}

export default ErrorMessage;
