import { useEffect, useState } from "react";

function getPeriodDays() {
  return window.matchMedia("(max-width: 700px)").matches ? 7 : 14;
}

export function useResponsiveBookingPeriod() {
  const [periodDays, setPeriodDays] = useState(getPeriodDays);

  useEffect(() => {
    const media = window.matchMedia("(max-width: 700px)");
    const update = () => setPeriodDays(media.matches ? 7 : 14);
    media.addEventListener("change", update);
    return () => media.removeEventListener("change", update);
  }, []);

  return periodDays;
}
