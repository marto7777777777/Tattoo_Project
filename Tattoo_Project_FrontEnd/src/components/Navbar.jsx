import { useEffect, useRef, useState } from "react";
import { NavLink, useNavigate } from "react-router-dom";
import { getMyProfile } from "../api/profileApi";
import { useAuth } from "../context/AuthContext";
import UserAvatar from "./UserAvatar";
import "../styles/navbar.css";

const Icon = ({ name }) => {
  const paths = {
    home: <><path d="M3 10.5 12 3l9 7.5"/><path d="M5 9.5V21h14V9.5"/><path d="M9 21v-7h6v7"/></>,
    ai: <><path d="M12 3 9.8 8.2 4 10l5.8 1.8L12 17l2.2-5.2L20 10l-5.8-1.8L12 3Z"/><path d="m5 17-.9 2.1L2 20l2.1.9L5 23l.9-2.1L8 20l-2.1-.9L5 17Z"/></>,
    explore: <><circle cx="12" cy="12" r="9"/><path d="m15.5 8.5-2.2 4.8-4.8 2.2 2.2-4.8 4.8-2.2Z"/></>,
    bookings: <><rect x="3" y="5" width="18" height="16" rx="2"/><path d="M16 3v4M8 3v4M3 10h18"/></>,
    heart: <path d="M20.8 4.6a5.5 5.5 0 0 0-7.8 0L12 5.6l-1-1a5.5 5.5 0 0 0-7.8 7.8l1 1L12 21l7.8-7.6 1-1a5.5 5.5 0 0 0 0-7.8Z"/>,
    studio: <><path d="M4 21h16"/><path d="M6 21V9l6-5 6 5v12"/><path d="M9 21v-6h6v6"/></>,
    user: <><circle cx="12" cy="8" r="4"/><path d="M4 21a8 8 0 0 1 16 0"/></>,
    logout: <><path d="M10 17l5-5-5-5"/><path d="M15 12H3"/><path d="M21 19V5a2 2 0 0 0-2-2h-6"/></>,
    admin: <><path d="M12 3l7 3v5c0 4.6-2.8 8.4-7 10-4.2-1.6-7-5.4-7-10V6l7-3Z"/><path d="M9 12l2 2 4-4"/></>,
  };
  return <svg viewBox="0 0 24 24" aria-hidden="true">{paths[name]}</svg>;
};

function Navbar() {
  const navigate = useNavigate();
  const menuRef = useRef(null);
  const { isLoggedIn, isClient, isArtist, isAdmin, logout, user } = useAuth();
  const [menuOpen, setMenuOpen] = useState(false);
  const [profile, setProfile] = useState(null);

  useEffect(() => {
    if (!isLoggedIn) return setProfile(null);
    let ignore = false;
    getMyProfile().then((data) => !ignore && setProfile(data)).catch(() => !ignore && setProfile(null));
    return () => { ignore = true; };
  }, [isLoggedIn]);

  useEffect(() => {
    const close = (event) => {
      if (menuRef.current && !menuRef.current.contains(event.target)) setMenuOpen(false);
    };
    document.addEventListener("mousedown", close);
    return () => document.removeEventListener("mousedown", close);
  }, []);

  const displayName = profile?.firstName || user?.userName || user?.email || "User";
  const roleLabel = isAdmin ? "Administrator" : isArtist ? "Artist & Client" : isClient ? "Client" : "Member";

  const handleLogout = () => {
    logout();
    setMenuOpen(false);
    navigate("/login");
  };

  const navItem = (to, icon, label) => (
    <NavLink to={to} className={({ isActive }) => `app-nav-link ${isActive ? "active" : ""}`}>
      <Icon name={icon} />
      <span>{label}</span>
    </NavLink>
  );

  return (
    <>
      <aside className="app-sidebar">
        <NavLink className="brand-lockup" to="/">
          <span className="brand-mark">IR</span>
          <span><strong>InkRoute</strong><small>Studio workflow</small></span>
        </NavLink>

        <nav className="app-nav">
          <p className="nav-section-label">Workspace</p>
          {navItem("/", "home", "Overview")}
          {navItem("/explore", "explore", "Discover studios")}
          {isLoggedIn && navItem("/ai-studio", "ai", "AI Tattoo Studio")}
          {isAdmin && navItem("/admin", "admin", "Admin control")}
          {isClient && navItem("/bookings", "bookings", "My bookings")}
          {isClient && navItem("/favorites", "heart", "Saved artists")}
          {isArtist && navItem("/my-studio", "studio", "My studio")}
        </nav>

        {!isLoggedIn ? (
          <div className="sidebar-auth-card">
            <span className="sidebar-auth-glow" />
            <p>Start your next tattoo project.</p>
            <NavLink to="/register">Create account</NavLink>
            <NavLink className="ghost-auth-link" to="/login">Sign in</NavLink>
          </div>
        ) : (
          <div className="sidebar-profile" ref={menuRef}>
            <button type="button" onClick={() => setMenuOpen((v) => !v)}>
              <UserAvatar
                firstName={profile?.firstName || user?.userName}
                lastName={profile?.lastName}
                email={profile?.email || user?.email}
                imageUrl={profile?.profileImageUrl}
                size="medium"
              />
              <span className="sidebar-profile-copy"><strong>{displayName}</strong><small>{roleLabel}</small></span>
              <span className="profile-more">•••</span>
            </button>
            {menuOpen && (
              <div className="sidebar-profile-menu">
                <button onClick={() => navigate("/profile/user")}><Icon name="user" /> Profile settings</button>
                <button className="danger-menu-item" onClick={handleLogout}><Icon name="logout" /> Log out</button>
              </div>
            )}
          </div>
        )}
      </aside>

      <header className="mobile-app-bar">
        <NavLink className="mobile-brand" to="/"><span className="brand-mark">IR</span><strong>InkRoute</strong></NavLink>
        {isLoggedIn ? (
          <button className="mobile-avatar-button" onClick={() => navigate("/profile/user")}>
            <UserAvatar firstName={profile?.firstName} lastName={profile?.lastName} email={profile?.email || user?.email} imageUrl={profile?.profileImageUrl} size="small" />
          </button>
        ) : <NavLink className="mobile-login" to="/login">Sign in</NavLink>}
      </header>

      <nav className="mobile-bottom-nav">
        {navItem("/", "home", "Home")}
        {navItem("/explore", "explore", "Explore")}
        {isLoggedIn && navItem("/ai-studio", "ai", "AI")}
        {isAdmin && navItem("/admin", "admin", "Admin")}
        {isClient && navItem("/bookings", "bookings", "Bookings")}
        {isArtist ? navItem("/my-studio", "studio", "Studio") : isClient && navItem("/favorites", "heart", "Saved")}
      </nav>
    </>
  );
}

export default Navbar;
