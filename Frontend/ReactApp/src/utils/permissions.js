export function canManageWorkspace(user) {
  return user?.role === 'Admin' || user?.role === 'ProjectManager';
}
