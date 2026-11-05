const STATUS_LABELS = {
  Todo: 'To Do',
  InProgress: 'In Progress',
  InReview: 'In Review',
  Done: 'Completed',
  OnHold: 'On hold'
};

function slug(value) {
  return String(value ?? '').toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-|-$/g, '');
}

export function StatusBadge({ status, label, className = '' }) {
  const value = status || 'Unknown';
  return <span className={`status-badge status-badge-${slug(value)} ${className}`.trim()}>{label ?? STATUS_LABELS[value] ?? value}</span>;
}

export function PriorityBadge({ priority, className = '' }) {
  const value = priority || 'Unknown';
  return <span className={`priority-badge priority-badge-${slug(value)} ${className}`.trim()}>{value}</span>;
}
