function ErrorMessage({ children, role = 'alert' }) {
  if (!children) {
    return null;
  }

  return <div className="message message-error" role={role}>{children}</div>;
}

export default ErrorMessage;
