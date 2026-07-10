import { useEffect, useRef, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { getMyProfile } from "../api/profileApi";
import { useAuth } from "../context/AuthContext";
import UserAvatar from "./UserAvatar";
import "../styles/navbar.css";

function Navbar() {
  const navigate = useNavigate();
  const menuRef = useRef(null);
  const { isLoggedIn, isClient, isArtist, logout, user } = useAuth();

  const [menuOpen, setMenuOpen] = useState(false);
  const [profileOpen, setProfileOpen] = useState(false);
  const [profile, setProfile] = useState(null);

  useEffect(() => {
    if (!isLoggedIn) {
      setProfile(null);
      return;
    }

    let ignore = false;

    getMyProfile()
      .then((data) => {
        if (!ignore) {
          setProfile(data);
        }
      })
      .catch(() => {
        if (!ignore) {
          setProfile(null);
        }
      });

    return () => {
      ignore = true;
    };
  }, [isLoggedIn]);

  useEffect(() => {
    function handleClickOutside(event) {
      if (menuRef.current && !menuRef.current.contains(event.target)) {
        setMenuOpen(false);
        setProfileOpen(false);
      }
    }

    document.addEventListener("mousedown", handleClickOutside);

    return () => {
      document.removeEventListener("mousedown", handleClickOutside);
    };
  }, []);

  function handleLogout() {
    logout();
    setMenuOpen(false);
    setProfileOpen(false);
    navigate("/login");
  }

  function goToProfileSection(section) {
    setMenuOpen(false);
    setProfileOpen(false);
    navigate(`/profile/${section}`);
  }

  const displayName =
    profile?.firstName ||
    user?.userName ||
    user?.email ||
    "User";

  return (
    <header className="navbar">
      <NavLink className="navbar-logo" to="/">
        InkRoute
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
        <div className="clean-profile-menu" ref={menuRef}>
          <button
            className="clean-profile-trigger"
            type="button"
            aria-label="Open user menu"
            onClick={() => setMenuOpen((current) => !current)}
          >
            <UserAvatar
              firstName={profile?.firstName || user?.userName}
              lastName={profile?.lastName}
              email={profile?.email || user?.email}
              imageUrl={profile?.profileImageUrl}
              size="medium"
            />

            <span className="clean-profile-name">
              {displayName}
            </span>
          </button>

          {menuOpen && (
            <div className="clean-dropdown">
              <button
                className="clean-dropdown-item clean-dropdown-parent"
                type="button"
                onClick={() => setProfileOpen((current) => !current)}
              >
                <span>Profile</span>
                <span className="clean-dropdown-arrow">›</span>
              </button>

              {profileOpen && (
                <div className="clean-profile-submenu">
                  <button type="button" onClick={() => goToProfileSection("user")}>
                    User
                  </button>

                  <button type="button" onClick={() => goToProfileSection("contact")}>
                    Contact
                  </button>

                  {isArtist && (
                    <button type="button" onClick={() => goToProfileSection("studio")}>
                      Studio Information
                    </button>
                  )}

                  {isArtist && (
                    <button type="button" onClick={() => goToProfileSection("consultation")}>
                      Consultation Settings
                    </button>
                  )}

                  {isArtist && (
                    <button type="button" onClick={() => goToProfileSection("deposit")}>
                      Deposit Settings
                    </button>
                  )}

                  {isArtist && (
                    <button type="button" onClick={() => goToProfileSection("portfolio")}>
                      Portfolio
                    </button>
                  )}
                </div>
              )}

              <div className="clean-dropdown-divider" />

              <button
                className="clean-dropdown-item clean-logout-item"
                type="button"
                onClick={handleLogout}
              >
                Logout
              </button>
            </div>
          )}
        </div>
      )}
    </header>
  );
}

export default Navbar;
