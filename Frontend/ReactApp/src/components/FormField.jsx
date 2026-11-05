import { cloneElement, isValidElement } from 'react';

function FormField({ id, label, required = false, hint, error, children }) {
  const describedBy = [hint && `${id}-hint`, error && `${id}-error`].filter(Boolean).join(' ') || undefined;
  const control = isValidElement(children) && describedBy
    ? cloneElement(children, {
      'aria-describedby': [children.props['aria-describedby'], describedBy].filter(Boolean).join(' '),
      'aria-invalid': error ? true : children.props['aria-invalid']
    })
    : children;

  return (
    <>
      <label htmlFor={id}>{label}{required && <> <span className="form-field-required" aria-hidden="true">*</span></>}</label>
      {control}
      {hint && <small id={`${id}-hint`} className="form-field-hint">{hint}</small>}
      {error && <small id={`${id}-error`} className="form-field-error" role="alert">{error}</small>}
    </>
  );
}

export default FormField;
