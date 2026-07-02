export function decodeJwt(token) {
  if (!token) return null;

  try {
    const base64Url = token.split(".")[1];
    const base64 = base64Url.replace(/-/g, "+").replace(/_/g, "/");
    const jsonPayload = decodeURIComponent(
      atob(base64)
        .split("")
        .map((char) => `%${(`00${char.charCodeAt(0).toString(16)}`).slice(-2)}`)
        .join("")
    );

    return JSON.parse(jsonPayload);
  } catch {
    return null;
  }
}

export function getRolesFromToken(token) {
  const payload = decodeJwt(token);

  if (!payload) return [];

  const roleClaim =
    payload.role ||
    payload.roles ||
    payload["http://schemas.microsoft.com/ws/2008/06/identity/claims/role"];

  if (!roleClaim) return [];

  return Array.isArray(roleClaim) ? roleClaim : [roleClaim];
}

export function getUserFromToken(token) {
  const payload = decodeJwt(token);

  if (!payload) return null;

  return {
    id:
      payload.nameid ||
      payload.sub ||
      payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"],
    userName:
      payload.unique_name ||
      payload.name ||
      payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name"],
    email:
      payload.email ||
      payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress"],
    roles: getRolesFromToken(token),
  };
}
