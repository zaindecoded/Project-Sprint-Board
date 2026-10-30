function ActiveFilterChips({ filters, onClearAll }) {
  if (!filters.length) return null;

  return (
    <div className="active-filter-chips" aria-label="Active filters">
      {filters.map(({ key, label, value, onRemove }) => (
        <button key={key} type="button" className="active-filter-chip" aria-label={`Remove ${label} filter: ${value}`} onClick={onRemove}>
          {label}: {value}<span aria-hidden="true">×</span>
        </button>
      ))}
      <button type="button" className="active-filters-clear" onClick={onClearAll}>Clear all</button>
    </div>
  );
}

export default ActiveFilterChips;
