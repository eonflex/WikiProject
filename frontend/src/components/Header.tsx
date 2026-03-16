import { Link, useLocation } from 'react-router-dom';

export default function Header() {
  const { pathname } = useLocation();

  const navLinks = [
    { to: '/', label: 'Home' },
    { to: '/articles', label: 'Articles' },
    { to: '/articles/new', label: 'New Article' },
  ];

  return (
    <header className="header">
      <div className="header-inner">
        <Link to="/" className="header-logo">
          📚 WikiProject
        </Link>
        <nav className="header-nav">
          {navLinks.map(({ to, label }) => (
            <Link
              key={to}
              to={to}
              className={`nav-link${pathname === to ? ' nav-link--active' : ''}`}
            >
              {label}
            </Link>
          ))}
        </nav>
      </div>
    </header>
  );
}
