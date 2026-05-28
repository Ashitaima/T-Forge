import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "../store/authStore";

type ProtectedRouteProps = {
  roles?: string[];
};

export const ProtectedRoute = ({ roles }: ProtectedRouteProps) => {
  const { isAuthenticated, user } = useAuthStore();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  if (roles?.length) {
    const hasRole = roles.some((role) => user?.role === role);
    if (!hasRole) {
      return <Navigate to="/" replace />;
    }
  }

  return <Outlet />;
};
