import { Navigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function ProtectedRoute({ children, roles = [] }) {
  const { isLoggedIn, roles: userRoles } = useAuth();

  if (!isLoggedIn) {
    return <Navigate to="/login" replace />;
  }

  if (roles.length > 0 && !roles.some((role) => userRoles.includes(role))) {
    return <Navigate to="/explore" replace />;
  }

  return children;
}

export default ProtectedRoute;
