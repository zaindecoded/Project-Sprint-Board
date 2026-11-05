export default function Pagination({ page, totalPages, onPageChange, disabled = false, label = 'Pages', className = 'project-pagination' }) {
  return <nav className={className} aria-label={label}>
    <button className="button button-secondary" type="button" onClick={() => onPageChange(Math.max(1, page - 1))} disabled={disabled || page <= 1}>Previous</button>
    <span>Page {page} of {totalPages}</span>
    <button className="button button-secondary" type="button" onClick={() => onPageChange(page + 1)} disabled={disabled || totalPages === 0 || page >= totalPages}>Next</button>
  </nav>;
}
