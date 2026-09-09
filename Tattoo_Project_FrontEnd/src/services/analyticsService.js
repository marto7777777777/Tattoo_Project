const allowed=new Set(["landing_page_view","artist_signup_started","artist_profile_created","subscription_checkout_started","subscription_trial_started","project_completed","first_project_completed","tenth_project_completed","subscription_first_payment_succeeded","subscription_second_payment_succeeded","subscription_payment_failed","subscription_cancelled","subscription_ended"]);
const safeKeys=new Set(["completed_project_count"]);
export function trackEvent(name,params={}){
 if(!allowed.has(name))return;
 const safe={};for(const [key,value] of Object.entries(params))if(safeKeys.has(key)&&Number.isInteger(value)&&value>=0)safe[key]=value;
 window.dataLayer=window.dataLayer||[];window.dataLayer.push({event:name,...safe});
}
