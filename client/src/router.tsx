import { createBrowserRouter } from 'react-router-dom';
import { PublicLayout } from '@/components/layout/PublicLayout';
import { AdminLayout } from '@/components/layout/AdminLayout';
import { DashboardLayout } from '@/components/layout/DashboardLayout';
import { RequireAuth } from '@/app/routes/RequireAuth';
import { RequireUserType } from '@/app/routes/RequireUserType';
import { RoleLandingRedirect } from '@/app/routes/RoleLandingRedirect';
import { LoginPage } from '@/features/auth/pages/LoginPage';
import { RegisterPage } from '@/features/auth/pages/RegisterPage';
import { ForbiddenPage } from '@/features/shared/pages/ForbiddenPage';
import { AppErrorPage } from '@/features/shared/pages/AppErrorPage';
import { BuyerHomePage } from '@/features/buyer/pages/BuyerHomePage';
import { SellerHomePage } from '@/features/seller/pages/SellerHomePage';
import { UsersListPage } from '@/features/admin-users/pages/UsersListPage';
import { UserEditPage } from '@/features/admin-users/pages/UserEditPage';
import { RolesListPage } from '@/features/admin-roles/pages/RolesListPage';
import { RoleEditPage } from '@/features/admin-roles/pages/RoleEditPage';
import { UserType } from '@/lib/types/user';

export const router = createBrowserRouter([
  {
    errorElement: <AppErrorPage />,
    children: [
      {
        element: <PublicLayout />,
        children: [
          { path: '/login', element: <LoginPage /> },
          { path: '/register', element: <RegisterPage /> },
        ],
      },
      { path: '/forbidden', element: <ForbiddenPage /> },
      {
        element: <RequireAuth />,
        children: [
          { path: '/', element: <RoleLandingRedirect /> },
          {
            path: '/admin',
            element: (
              <RequireUserType userType={UserType.Admin}>
                <AdminLayout />
              </RequireUserType>
            ),
            children: [
              { index: true, element: <UsersListPage /> },
              { path: 'users', element: <UsersListPage /> },
              { path: 'users/:id', element: <UserEditPage /> },
              { path: 'roles', element: <RolesListPage /> },
              { path: 'roles/:id', element: <RoleEditPage /> },
            ],
          },
          {
            path: '/buyer',
            element: (
              <RequireUserType userType={UserType.Buyer}>
                <DashboardLayout />
              </RequireUserType>
            ),
            children: [{ index: true, element: <BuyerHomePage /> }],
          },
          {
            path: '/seller',
            element: (
              <RequireUserType userType={UserType.Seller}>
                <DashboardLayout />
              </RequireUserType>
            ),
            children: [{ index: true, element: <SellerHomePage /> }],
          },
        ],
      },
    ],
  },
]);
