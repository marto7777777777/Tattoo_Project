import { createContext, useContext, useMemo, useState } from "react";
import { clearToken, getToken, setToken } from "../api/http";
import { getUserFromToken } from "../utils/auth";

const AuthContext = createContext(null);

export function AuthProvider({ children }) {
  const [token, setTokenState] = useState(getToken());

  const user = useMemo(() => getUserFromToken(token), [token]);
  const roles = user?.roles || [];

  function saveAuthToken(newToken) {
    setToken(newToken);
    setTokenState(newToken);
  }

  function logout() {
    clearToken();
    setTokenState(null);
  }

  const value = {
    token,
    user,
    roles,
    isLoggedIn: Boolean(token),
    isClient: roles.includes("Client"),
    isArtist: roles.includes("TattooArtist"),
    isAdmin: roles.includes("Admin"),
    saveAuthToken,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth() {
  return useContext(AuthContext);
}
