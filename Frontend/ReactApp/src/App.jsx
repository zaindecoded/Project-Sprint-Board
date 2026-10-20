import { Route, Routes } from 'react-router-dom';
import Layout from './components/Layout';
import ProtectedRoute from './routes/ProtectedRoute';
import PublicOnlyRoute from './routes/PublicOnlyRoute';
import LoginPage from './pages/auth/LoginPage';
import DashboardPage from './pages/dashboard/DashboardPage';
import NotFoundPage from './pages/errors/NotFoundPage';
import ProjectsPage from './pages/projects/ProjectsPage';
import SprintsPage from './pages/sprints/SprintsPage';
import TasksPage from './pages/TasksPage';
import AssignmentsPage from './pages/AssignmentsPage';
import CommentsPage from './pages/CommentsPage';
import UsersPage from './pages/UsersPage';
import AdminRoute from './routes/AdminRoute';
import ManagerRoute from './routes/ManagerRoute';
import HomeRedirect from './routes/HomeRedirect';
import AuditLogPage from './pages/AuditLogPage';

function App() {
  return (
    <Routes>
      <Route element={<PublicOnlyRoute />}>
        <Route path="/login" element={<LoginPage />} />
      </Route>

      <Route element={<ProtectedRoute />}>
        <Route element={<Layout />}>
          <Route index element={<HomeRedirect />} />
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/assignments" element={<AssignmentsPage />} />
          <Route path="/comments" element={<CommentsPage />} />
          <Route element={<ManagerRoute />}>
            <Route path="/projects" element={<ProjectsPage />} />
            <Route path="/sprints" element={<SprintsPage />} />
            <Route path="/tasks" element={<TasksPage />} />
            <Route element={<AdminRoute />}>
              <Route path="/users" element={<UsersPage />} />
              <Route path="/audit-log" element={<AuditLogPage />} />
            </Route>
          </Route>
        </Route>
      </Route>

      <Route path="*" element={<NotFoundPage />} />
    </Routes>
  );
}

export default App;
