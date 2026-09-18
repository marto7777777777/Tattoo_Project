const KEY="inkroute.cookieConsent.v2";
export const defaultConsent={essential:true,analytics:false,marketing:false};
export function readConsent(){try{return{...defaultConsent,...JSON.parse(localStorage.getItem(KEY)||"null")};}catch{return defaultConsent;}}
export function applyConsent(value){
 const consent={essential:true,analytics:Boolean(value.analytics),marketing:Boolean(value.marketing)};localStorage.setItem(KEY,JSON.stringify(consent));
 window.dataLayer=window.dataLayer||[];window.gtag=window.gtag||function(){window.dataLayer.push(arguments);};
 window.gtag("consent","update",{analytics_storage:consent.analytics?"granted":"denied",ad_storage:consent.marketing?"granted":"denied",ad_user_data:consent.marketing?"granted":"denied",ad_personalization:consent.marketing?"granted":"denied"});
 if(consent.analytics)loadGtm();window.dispatchEvent(new CustomEvent("inkroute:consent-changed",{detail:consent}));return consent;
}
export function initializeConsent(){window.dataLayer=window.dataLayer||[];window.gtag=window.gtag||function(){window.dataLayer.push(arguments);};window.gtag("consent","default",{analytics_storage:"denied",ad_storage:"denied",ad_user_data:"denied",ad_personalization:"denied",wait_for_update:500});const saved=localStorage.getItem(KEY);if(saved)applyConsent(readConsent());return Boolean(saved);}
function loadGtm(){const id=import.meta.env.VITE_GTM_CONTAINER_ID?.trim();if(!id||document.querySelector("script[data-inkroute-gtm]"))return;const script=document.createElement("script");script.async=true;script.dataset.inkrouteGtm="true";script.src=`https://www.googletagmanager.com/gtm.js?id=${encodeURIComponent(id)}`;document.head.appendChild(script);}
