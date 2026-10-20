import { useState } from 'react';
import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const navItems = [
  { to: '/dashboard', label: 'Dashboard' },
  { to: '/projects', label: 'Projects' },
  { to: '/sprints', label: 'Sprints' },
  { to: '/tasks', label: 'Tasks' },
  { to: '/assignments', label: 'Assignments' },
  { to: '/comments', label: 'Comments' }
];

function Layout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [menuOpen, setMenuOpen] = useState(false);
  const visibleNavItems = user?.role === 'TeamMember'
    ? navItems.filter((item) => item.to === '/dashboard' || item.to === '/assignments' || item.to === '/comments')
    : user?.role?.toLowerCase() === 'admin'
      ? [...navItems, { to: '/users', label: 'Users' }, { to: '/audit-log', label: 'Audit log' }]
      : navItems;

  function handleLogout() {
    logout();
    navigate('/login', { replace: true });
  }

  return (
    <div className="app-shell">
      <header className="app-header">
        <div className="header-inner">
          <NavLink className="app-brand" to="/" onClick={() => setMenuOpen(false)}>
            <span>Task & Sprint Board</span>
          </NavLink>

          <button
            className="menu-toggle"
            type="button"
            aria-label={menuOpen ? 'Close navigation' : 'Open navigation'}
            aria-expanded={menuOpen}
            onClick={() => setMenuOpen((open) => !open)}
          >
            <span />
            <span />
            <span />
          </button>

          <nav className={`app-navigation ${menuOpen ? 'navigation-open' : ''}`} aria-label="Main navigation">
            <div className="nav-links">
              {visibleNavItems.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.end}
                  className={({ isActive }) => `nav-link${isActive ? ' nav-link-active' : ''}`}
                  onClick={() => setMenuOpen(false)}
                >
                  {item.label}
                </NavLink>
              ))}
            </div>
            <div className="user-menu">
              <div className="user-summary">
                <span className="user-name">{user?.name || user?.email}</span>
                <span className="user-role">{user?.role || 'Signed in'}</span>
              </div>
              <button className="button button-quiet" type="button" onClick={handleLogout}>
                Log out
              </button>
            </div>
          </nav>
        </div>
      </header>

      <main className="app-content">
        <Outlet />
      </main>
    </div>
  );
}

export default Layout;
