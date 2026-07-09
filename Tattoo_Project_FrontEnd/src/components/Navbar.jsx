import { NavLink, useNavigate } from "react-router-dom";
import { useAuth } from "../context/AuthContext";
import "../styles/navbar.css";

function Navbar() {
  const navigate = useNavigate();
  const { isLoggedIn, isClient, isArtist, logout, user } = useAuth();

  function handleLogout() {
    logout();
    navigate("/login");
  }

  return (
    <header className="navbar">
      <NavLink className="navbar-logo" to="/">
        InkFlow
      </NavLink>

      <nav className="navbar-links">
        <NavLink to="/explore">Explore</NavLink>

        {isClient && <NavLink to="/bookings">Bookings</NavLink>}
        {isClient && <NavLink to="/favorites">Favorites</NavLink>}

        {isArtist && <NavLink to="/my-studio">My Studio</NavLink>}

        {!isLoggedIn && <NavLink to="/register">Register</NavLink>}
        {!isLoggedIn && <NavLink to="/login">Login</NavLink>}
      </nav>

      {isLoggedIn && (
        <div className="navbar-user">
          <span>{user?.userName || "User"}</span>
          <button onClick={handleLogout}>Logout</button>
        </div>
      )}
    </header>
  );
}

export default Navbar;
