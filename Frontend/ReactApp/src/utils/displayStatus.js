function todayKey() {
  const today = new Date();
  return `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`;
}

export function getTodayDateInputValue() {
  return todayKey();
}

export function getTodayDateTimeMin() {
  return `${todayKey()}T00:00`;
}

export function isPastDate(value) {
  const dateKey = value ? String(value).slice(0, 10) : '';
  return /^\d{4}-\d{2}-\d{2}$/.test(dateKey) && dateKey < todayKey();
}

export function getProjectDisplayStatus(project) {
  const status = project.status || (project.isActive ? 'Active' : 'Inactive');
  if (!project.isActive) return 'Inactive';
  if (isPastDate(project.endDate) && !['completed', 'cancelled'].includes(status.toLowerCase())) return 'Overdue';
  return status;
}

export function getTaskDisplayStatus(task) {
  if (isPastDate(task.dueDate) && !['done', 'cancelled'].includes((task.status || '').toLowerCase())) return 'Overdue';
  return task.status || '—';
}

export function getTaskActivityStatus(task) {
  if (!task.isActive) return 'Inactive';
  return isPastDate(task.dueDate) ? 'Overdue' : 'Active';
}

export function getAssignmentDisplayStatus(assignment, task) {
  if (!assignment.isActive) return 'Inactive';
  if ((task?.status || '').toLowerCase() === 'done') return 'Complete';
  if (task && isPastDate(task.dueDate)) return 'Overdue';
  return 'Active';
}
