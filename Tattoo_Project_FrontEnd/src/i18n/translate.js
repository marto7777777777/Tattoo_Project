import { tattooStyles, translations } from "./translations";

const monthNames = {
  January: "януари",
  February: "февруари",
  March: "март",
  April: "април",
  May: "май",
  June: "юни",
  July: "юли",
  August: "август",
  September: "септември",
  October: "октомври",
  November: "ноември",
  December: "декември",
};

const dynamicRules = [
  [/^Session (\d+) price$/i, (_, number) => `Цена на сесия ${number}`],
  [/^Session (\d+)$/i, (_, number) => `Сесия ${number}`],
  [/^Version (\d+)$/i, (_, number) => `Версия ${number}`],
  [/^Step (\d+) of (\d+)$/i, (_, current, total) => `Стъпка ${current} от ${total}`],
  [/^(\d+) hours?$/i, (_, number) => `${number} ч.`],
  [/^(\d+) minutes?$/i, (_, number) => `${number} мин.`],
  [/^(\d+) artists?$/i, (_, number) => `${number} ${number === "1" ? "татуист" : "татуисти"}`],
  [/^(\d+) versions?$/i, (_, number) => `${number} ${number === "1" ? "версия" : "версии"}`],
  [/^(\d+) sessions?$/i, (_, number) => `${number} ${number === "1" ? "сесия" : "сесии"}`],
  [/^(\d+) days unavailable$/i, (_, number) => `${number} ${number === "1" ? "недостъпен ден" : "недостъпни дни"}`],
  [/^Starts at (.+)$/i, (_, value) => `Започва в ${value}`],
  [/^(\d+) remaining$/i, (_, number) => `Остават ${number}`],
  [/^Remaining: (\d+)$/i, (_, number) => `Остават: ${number}`],
  [/^Created (.+)$/i, (_, value) => `Създадено ${value}`],
  [/^Sent (.+)$/i, (_, value) => `Изпратено ${value}`],
  [/^Price: (.+)$/i, (_, value) => `Цена: ${value}`],
  [/^Duration: (.+)$/i, (_, value) => `Продължителност: ${value}`],
  [/^Studio: (.+)$/i, (_, value) => `Студио: ${value}`],
  [/^Artist: (.+)$/i, (_, value) => `Татуист: ${value}`],
  [/^Client: (.+)$/i, (_, value) => `Клиент: ${value}`],
  [/^Placement: (.+)$/i, (_, value) => `Място: ${translateUiText(value, "bg")}`],
  [/^Style: (.+)$/i, (_, value) => `Стил: ${value}`],
  [/^From (.+)$/i, (_, value) => `От ${value}`],
  [/^To (.+)$/i, (_, value) => `До ${value}`],
  [/^(.+) - Full day$/i, (_, value) => `${value} — цял ден`],
  [/^(.+) - Hourly break$/i, (_, value) => `${value} — почивка в часови диапазон`],
  [/^(\d+) free slots?$/i, (_, number) => `${number} ${number === "1" ? "свободен час" : "свободни часа"}`],
  [/^(\d+) improvements? remaining$/i, (_, number) => `Остават ${number} ${number === "1" ? "подобрение" : "подобрения"}`],
  [/^Tattoo sessions \((\d+) booked(?:, (\d+) remaining)?\)$/i, (_, booked, remaining) => `Тату сесии (${booked} резервирани${remaining == null ? "" : `, остават ${remaining}`})`],
  [/^Open portfolio image (\d+)$/i, (_, number) => `Отвори снимка ${number} от портфолиото`],
  [/^Show image (\d+)$/i, (_, number) => `Покажи снимка ${number}`],
  [/^Tattoo reference (\d+)$/i, (_, number) => `Референтно изображение ${number}`],
  [/^Reference (\d+)$/i, (_, number) => `Референция ${number}`],
  [/^Requirement (\d+)$/i, (_, number) => `Изискване ${number}`],
  [/^Delete artist profile #(.+)$/i, (_, id) => `Изтрий профила на татуист №${id}`],
  [/^Delete client profile #(.+)$/i, (_, id) => `Изтрий клиентски профил №${id}`],
  [/^Permanently delete AI project #(.+)$/i, (_, id) => `Изтрий необратимо проект с изкуствен интелект №${id}`],
  [/^Permanently delete tattoo request #(.+)$/i, (_, id) => `Изтрий необратимо заявка за татуировка №${id}`],
  [/^Artist profile created\. Your join request was sent to (.+)$/i, (_, studio) => `Профилът на татуиста е създаден. Заявката за присъединяване е изпратена до ${studio}`],
  [/^Enter the code sent to (.+)$/i, (_, email) => `Въведи кода, изпратен до ${email}`],
  [/^We sent a 6-digit code to (.+)$/i, (_, email) => `Изпратихме 6-цифрен код до ${email}`],
  [/^Your request to join (.+) is pending\.$/i, (_, studio) => `Заявката ти за присъединяване към ${studio} изчаква одобрение.`],
  [/^Request: (.+)$/i, (_, request) => `Заявка: ${request}`],
  [/^Choose start and end time for every active (.+)$/i, (_, type) => `Избери начален и краен час за всеки активен период: ${translateUiText(type, "bg")}`],
  [/^Select at least one working day for (.+)$/i, (_, type) => `Избери поне един работен ден за ${translateUiText(type, "bg")}`],
  [/^Delete client profile #(.+) and its related client requests\?$/i, (_, id) => `Да се изтрие ли клиентски профил №${id} и свързаните с него заявки?`],
  [/^Delete artist profile #(.+) and its related studio\/request data\?$/i, (_, id) => `Да се изтрие ли профилът на татуист №${id} и свързаните данни за студио и заявки?`],
  [/^Permanently delete (.+) and all related InkRoute data\?$/i, (_, user) => `Да се изтрие ли необратимо ${user} и всички свързани данни в InkRoute?`],
  [/^Permanently delete tattoo request #(.+) and all sessions, consultation, review and response data attached to it\?$/i, (_, id) => `Да се изтрие ли необратимо заявка №${id} и всички свързани сесии, консултация, отзив и отговор?`],
  [/^Permanently delete AI project #(.+) and all generated versions\?$/i, (_, id) => `Да се изтрие ли необратимо проектът с изкуствен интелект №${id} и всички генерирани версии?`],
];

function translateMonthText(text) {
  let translated = text;
  let changed = false;

  Object.entries(monthNames).forEach(([english, bulgarian]) => {
    const next = translated.replace(new RegExp(`\\b${english}\\b`, "g"), bulgarian);
    if (next !== translated) changed = true;
    translated = next;
  });

  return changed ? translated : null;
}

export function translateUiText(text, language) {
  if (language === "en" || typeof text !== "string") return text;

  const normalized = text.replace(/\s+/g, " ").trim();
  if (!normalized || tattooStyles.has(normalized)) return text;

  const exact = translations[language]?.[normalized];
  if (exact) return exact;

  for (const [pattern, createTranslation] of dynamicRules) {
    const match = normalized.match(pattern);
    if (match) return createTranslation(...match);
  }

  const monthTranslation = translateMonthText(normalized);
  return monthTranslation || text;
}
