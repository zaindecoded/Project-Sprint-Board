import { Navigate, Outlet } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

function AdminRoute() {
  const { user } = useAuth();
  return user?.role?.toLowerCase() === 'admin' ? <Outlet /> : <Navigate to="/dashboard" replace />;
}

export default AdminRoute;
