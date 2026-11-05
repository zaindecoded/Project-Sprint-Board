export function LoadingState({ message, className = 'project-state', as: Element = 'div' }) {
  return <Element className={className} role="status">{message}</Element>;
}

export function EmptyState({ title, description, action, className = 'project-state project-empty', as: Element = 'div' }) {
  return (
    <Element className={className}>
      <h2>{title}</h2>
      {description && <p>{description}</p>}
      {action}
    </Element>
  );
}
