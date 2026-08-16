import { Navigate, Outlet } from "react-router-dom";
import { useAuthStore } from "../store/authStore";
import { useEffectiveRole } from "../hooks/useEffectiveRole";

type ProtectedRouteProps = {
  roles?: string[];
};

export const ProtectedRoute = ({ roles }: ProtectedRouteProps) => {
  const { isAuthenticated } = useAuthStore();
  const effectiveRole = useEffectiveRole();

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  // Читаємо підмінену роль, щоб режим розробника справді блокував маршрути,
  // а не лише ховав посилання на них.
  if (roles?.length && !roles.includes(effectiveRole)) {
    return <Navigate to="/" replace />;
  }

  return <Outlet />;
};
