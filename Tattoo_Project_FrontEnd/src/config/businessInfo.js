export const BUSINESS_INFO = Object.freeze({
  brand: "InkRoute",
  legalNameBg: "ИНК РОУТЕ ЕООД",
  legalNameEn: "INK ROUTE Ltd.",
  companyId: "208927794",
  vatId: "BG208927794",
  email: "inkrouteteam@inkroute.app",
  website: "https://inkroute.app",
  addressBg: "гр. Асеновград 4230, ул. Стоян Джансъзов № 3, вх. Б, ет. 5, ап. 42",
  addressEn: "3 Stoyan Dzhansazov St., Entrance B, Floor 5, Apartment 42, 4230 Asenovgrad, Bulgaria",
  phone: import.meta.env.VITE_BUSINESS_PHONE?.trim() || "",
});

export const LEGAL_VERSIONS = Object.freeze({ terms: "2026-09-09", privacy: "2026-09-09" });
