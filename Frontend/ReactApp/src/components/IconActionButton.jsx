const paths = {
  edit: <><path d="M12 20h9"/><path d="M16.5 3.5a2.1 2.1 0 0 1 3 3L8 18l-4 1 1-4Z"/></>,
  delete: <path d="M3 6h18M8 6V4h8v2m3 0-1 14H6L5 6m4 4v6m6-6v6"/>
};

export default function IconActionButton({ action, label, onClick, disabled = false, className = 'user-icon-button', title = label }) {
  return <button className={className} type="button" aria-label={label} title={title} onClick={onClick} disabled={disabled}>
    <svg viewBox="0 0 24 24" aria-hidden="true">{paths[action]}</svg>
  </button>;
}
