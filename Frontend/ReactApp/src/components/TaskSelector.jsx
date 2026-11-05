import { useEffect, useState } from 'react';
import ErrorMessage from './ErrorMessage';
import { getTask, getTasks } from '../services/taskApi';
import { getApiErrorMessage } from '../services/apiErrors';

export default function TaskSelector({
  id, label = 'Task', value, initialTask, onChange, disabled, required = true,
  placeholder = 'Search tasks by title', ariaLabel,
  className = 'comment-task-combobox', optionsClassName = 'comment-task-options',
  clearClassName = 'comment-task-clear', fieldErrorClassName = 'comment-field-error',
  requiredClassName = 'comment-required'
}) {
  const [search, setSearch] = useState('');
  const [tasks, setTasks] = useState([]);
  const [selectedTask, setSelectedTask] = useState(initialTask ?? null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [open, setOpen] = useState(false);

  useEffect(() => {
    if (String(initialTask?.id) === String(value)) setSelectedTask(initialTask);
  }, [initialTask, value]);

  useEffect(() => {
    if (!value || String(selectedTask?.id) === String(value)) return undefined;
    let active = true;
    getTask(value).then((task) => { if (active) setSelectedTask(task); })
      .catch((requestError) => { if (active) setError(getApiErrorMessage(requestError, 'Unable to load the selected task.')); });
    return () => { active = false; };
  }, [selectedTask?.id, value]);

  useEffect(() => {
    const controller = new AbortController();
    const timer = window.setTimeout(() => {
      setLoading(true);
      setError('');
      getTasks({ page: 1, pageSize: 50, search: search.trim(), signal: controller.signal })
        .then((result) => { if (!controller.signal.aborted) setTasks(Array.isArray(result.items) ? result.items : []); })
        .catch((requestError) => {
          if (!controller.signal.aborted && requestError.code !== 'ERR_CANCELED') setError(getApiErrorMessage(requestError, 'Unable to load tasks.'));
        })
        .finally(() => { if (!controller.signal.aborted) setLoading(false); });
    }, 250);
    return () => { window.clearTimeout(timer); controller.abort(); };
  }, [search]);

  const availableTasks = String(selectedTask?.id) === String(value) && !tasks.some((task) => String(task.id) === String(value))
    ? [selectedTask, ...tasks]
    : tasks;
  const matches = availableTasks.filter((task) => task.title.toLowerCase().includes(search.toLowerCase())).slice(0, 8);

  return <div className={className}>
    {label && <label htmlFor={id}>{label}{required && <> <span className={requiredClassName} aria-hidden="true">*</span></>}</label>}
    <input id={id} aria-label={ariaLabel || label || 'Filter tasks by title'} role="combobox" aria-autocomplete="list" aria-expanded={open} aria-controls={`${id}-options`} value={String(value) === String(selectedTask?.id) ? selectedTask?.title ?? search : search} onFocus={() => setOpen(true)} onBlur={() => setOpen(false)} onChange={(event) => { setSearch(event.target.value); setSelectedTask(null); onChange('', null); setOpen(true); }} placeholder={placeholder} disabled={disabled} required={required && !value} />
    {open && <ul id={`${id}-options`} className={optionsClassName} role="listbox">
      {loading ? <li>Loading tasks…</li> : matches.map((task) => <li key={task.id}><button type="button" role="option" aria-selected={String(task.id) === String(value)} onMouseDown={(event) => event.preventDefault()} onClick={() => { setSelectedTask(task); setSearch(task.title); setOpen(false); onChange(task.id, task); }}>{task.title}</button></li>)}
      {!loading && !matches.length && <li>No tasks found.</li>}
    </ul>}
    {required && !value && <small className={fieldErrorClassName}></small>}
    {value && <button type="button" className={clearClassName} aria-label="Clear selected task" onClick={() => { setSearch(''); setSelectedTask(null); onChange('', null); }}>×</button>}
    <ErrorMessage message={error} />
  </div>;
}
