import { useEffect, useState } from "react";

function NetworkStatus() {
  const [isOnline, setIsOnline] = useState(() => navigator.onLine);

  useEffect(() => {
    const markOnline = () => setIsOnline(true);
    const markOffline = () => setIsOnline(false);

    window.addEventListener("online", markOnline);
    window.addEventListener("offline", markOffline);

    return () => {
      window.removeEventListener("online", markOnline);
      window.removeEventListener("offline", markOffline);
    };
  }, []);

  if (isOnline) {
    return null;
  }

  return (
    <div className="network-status" role="status" aria-live="polite">
      You are offline. Saved screens remain available, but live data and actions
      need an internet connection.
    </div>
  );
}

export default NetworkStatus;
