export default function Modal({ children, onClose, closeDisabled = false, className, backdropClassName, label }) {
  return <div className={backdropClassName} onMouseDown={(event) => { if (event.target === event.currentTarget && !closeDisabled) onClose(); }}>
    {className ? <div role="presentation" className={className} aria-label={label}>{children}</div> : children}
  </div>;
}
