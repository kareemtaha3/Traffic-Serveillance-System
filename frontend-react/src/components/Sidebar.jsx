import { NavLink } from 'react-router-dom';
import { FiGrid, FiTruck, FiMap, FiActivity, FiAlertTriangle } from 'react-icons/fi';

const links = [
  { to: '/', icon: <FiGrid />, label: 'Dashboard' },
  { to: '/cameras', icon: <FiActivity />, label: 'Cameras' },
  { to: '/vehicles', icon: <FiTruck />, label: 'Vehicles' },
  { to: '/routes', icon: <FiMap />, label: 'Routes' },
  { to: '/monitoring', icon: <FiActivity />, label: 'Monitoring' },
  { to: '/infringements', icon: <FiAlertTriangle />, label: 'Infringements' },
];

export default function Sidebar() {
  return (
    <aside className="sidebar">
      <div className="sidebar-header">
        <div className="sidebar-logo">
          <div className="sidebar-logo-icon">🚦</div>
          <div>
            <div className="sidebar-logo-text">Traffic Watch</div>
            <div className="sidebar-logo-sub">Surveillance System</div>
          </div>
        </div>
      </div>
      <nav className="sidebar-nav">
        {links.map((link) => (
          <NavLink
            key={link.to}
            to={link.to}
            end={link.to === '/'}
            className={({ isActive }) => `sidebar-link ${isActive ? 'active' : ''}`}
          >
            <span className="icon">{link.icon}</span>
            {link.label}
          </NavLink>
        ))}
      </nav>
    </aside>
  );
}
