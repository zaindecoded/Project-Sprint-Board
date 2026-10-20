import { Navigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

function HomeRedirect() {
  const { user } = useAuth();
  return <Navigate to="/dashboard" replace />;
}

export default HomeRedirect;
