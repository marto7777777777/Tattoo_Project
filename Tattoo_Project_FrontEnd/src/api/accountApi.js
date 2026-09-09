import { requestJson } from "./http";
export const deleteAccount=(password,confirmation)=>requestJson("/api/account",{method:"DELETE",body:JSON.stringify({password,confirmation})});
