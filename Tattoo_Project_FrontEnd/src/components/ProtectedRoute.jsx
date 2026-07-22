import { Navigate, useLocation } from "react-router-dom";
import { useAuth } from "../context/AuthContext";

function ProtectedRoute({ children, roles = [] }) {
  const location = useLocation();
  const { isLoggedIn, roles: userRoles, isAdmin } = useAuth();

  if (!isLoggedIn) {
    return <Navigate to="/login" replace />;
  }

  if (!isAdmin && roles.length > 0 && !roles.some((role) => userRoles.includes(role))) {
    const returnTo = `${location.pathname}${location.search}`;

    if (roles.includes("Client")) {
      return (
        <Navigate
          to={`/create-client-profile?profileRequired=1&returnTo=${encodeURIComponent(returnTo)}`}
          replace
        />
      );
    }

    if (roles.includes("TattooArtist")) {
      return (
        <Navigate
          to="/create-artist-profile?profileRequired=1"
          replace
        />
      );
    }

    return <Navigate to="/choose-profile" replace />;
  }

  return children;
}

export default ProtectedRoute;
