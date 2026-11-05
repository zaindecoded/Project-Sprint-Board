export default function Delete({ title, children, confirmLabel = 'Delete', busyLabel, busy = false, onCancel, onConfirm, className = 'project-confirm-dialog', confirmClassName = 'button project-confirm-delete', backdropClassName = 'project-confirm-backdrop' }) {
  return <div className={backdropClassName} role="presentation" onMouseDown={(event) => { if (event.target === event.currentTarget && !busy) onCancel(); }}>
    <section className={className} role="alertdialog" aria-modal="true" aria-label={title}>
      <h2>{title}</h2><p>{children}</p>
      <div><button className="button button-secondary" type="button" onClick={onCancel} disabled={busy}>Cancel</button>
        <button className={confirmClassName} type="button" onClick={onConfirm} disabled={busy}>{busy ? busyLabel || 'Working…' : confirmLabel}</button></div>
    </section>
  </div>;
}
