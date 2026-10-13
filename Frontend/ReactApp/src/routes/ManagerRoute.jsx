import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { canManageWorkspace } from '../utils/permissions';

function ManagerRoute() {
  const { user } = useAuth();
  return canManageWorkspace(user) ? <Outlet /> : <Navigate to="/assignments" replace />;
}

export default ManagerRoute;
