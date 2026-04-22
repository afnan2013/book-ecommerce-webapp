import type { ReactNode } from 'react';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuthStore } from '@/stores/authStore';
import { Button } from '@/components/ui/button';

export function AdminLayout() {
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);
  const navigate = useNavigate();

  const handleLogout = () => {
    clear();
    navigate('/login', { replace: true });
  };

  return (
    <div className="min-h-screen grid grid-cols-[240px_1fr]">
      <aside className="border-r bg-muted/30 p-4 flex flex-col gap-1">
        <div className="mb-6">
          <Link to="/" className="font-semibold text-lg">
            Book Ecom
          </Link>
          <p className="text-sm text-muted-foreground">Admin</p>
        </div>
        <nav className="flex flex-col gap-1">
          <SidebarLink to="/admin/users">Users</SidebarLink>
          <SidebarLink to="/admin/roles">Roles</SidebarLink>
        </nav>
      </aside>
      <div className="flex flex-col">
        <header className="border-b px-6 py-3 flex items-center justify-between">
          <h1 className="font-medium">Admin panel</h1>
          <div className="flex items-center gap-3">
            <span className="text-sm text-muted-foreground">{user?.email}</span>
            <Button variant="outline" size="sm" onClick={handleLogout}>
              Sign out
            </Button>
          </div>
        </header>
        <main className="flex-1 p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}

function SidebarLink({ to, children }: { to: string; children: ReactNode }) {
  return (
    <NavLink
      to={to}
      className={({ isActive }) =>
        `px-3 py-2 rounded-md text-sm font-medium ${
          isActive ? 'bg-accent text-accent-foreground' : 'hover:bg-accent/50'
        }`
      }
    >
      {children}
    </NavLink>
  );
}
