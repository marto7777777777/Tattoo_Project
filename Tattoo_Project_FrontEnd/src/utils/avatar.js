export const avatarColors = [
  "#2563eb",
  "#16a34a",
  "#ca8a04",
  "#dc2626",
  "#7c3aed",
  "#0891b2",
  "#db2777",
  "#ea580c",
];

export function getAvatarColor(value) {
  const text = value || "InkFlow";
  let sum = 0;

  for (let i = 0; i < text.length; i += 1) {
    sum += text.charCodeAt(i);
  }

  return avatarColors[sum % avatarColors.length];
}

export function getInitials(firstName, lastName, fallback = "U") {
  const first = firstName?.trim()?.charAt(0) || fallback.charAt(0);
  const last = lastName?.trim()?.charAt(0) || "";
  return `${first}${last}`.toUpperCase();
}
