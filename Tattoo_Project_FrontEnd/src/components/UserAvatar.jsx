import { getAvatarColor, getInitials } from "../utils/avatar";
import { getImageUrl } from "../utils/images";

function UserAvatar({ firstName, lastName, email, imageUrl, size = "medium", className = "" }) {
  const initials = getInitials(firstName, lastName, email || "U");

  return (
    <div
      className={`user-avatar user-avatar-${size} ${className}`}
      style={{ backgroundColor: imageUrl ? undefined : getAvatarColor(email || firstName) }}
    >
      {imageUrl ? <img src={getImageUrl(imageUrl)} alt="Profile" /> : <span>{initials}</span>}
    </div>
  );
}

export default UserAvatar;
