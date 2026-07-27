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
  [/^Session (\d+) price$/i, (_, number) => `Сесия ${number} цена`],
  [/^Extra session (\d+) price$/i, (_, number) => `Допълнителна сесия ${number} цена`],
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
  [/^(.+) reference$/i, (_, value) => `${translateUiText(value, "bg")} — референтно изображение`],
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

const germanDynamicRules = [
  [/^Session (\d+) price$/i, (_, number) => `Preis für Sitzung ${number}`],
  [/^Extra session (\d+) price$/i, (_, number) => `Preis für zusätzliche Sitzung ${number}`],
  [/^Session (\d+)$/i, (_, number) => `Sitzung ${number}`],
  [/^Version (\d+)$/i, (_, number) => `Version ${number}`],
  [/^Step (\d+) of (\d+)$/i, (_, current, total) => `Schritt ${current} von ${total}`],
  [/^(\d+) hours?$/i, (_, number) => `${number} Std.`],
  [/^(\d+) minutes?$/i, (_, number) => `${number} Min.`],
  [/^(\d+) artists?$/i, (_, number) => `${number} ${number === "1" ? "Tätowierer" : "Tätowierer"}`],
  [/^(\d+) versions?$/i, (_, number) => `${number} ${number === "1" ? "Version" : "Versionen"}`],
  [/^(\d+) sessions?$/i, (_, number) => `${number} ${number === "1" ? "Sitzung" : "Sitzungen"}`],
  [/^(\d+) days unavailable$/i, (_, number) => `${number} ${number === "1" ? "nicht verfügbarer Tag" : "nicht verfügbare Tage"}`],
  [/^Starts at (.+)$/i, (_, value) => `Beginnt um ${value}`],
  [/^(\d+) remaining$/i, (_, number) => `${number} verbleibend`],
  [/^Remaining: (\d+)$/i, (_, number) => `Verbleibend: ${number}`],
  [/^Created (.+)$/i, (_, value) => `Erstellt ${value}`],
  [/^Sent (.+)$/i, (_, value) => `Gesendet ${value}`],
  [/^Price: (.+)$/i, (_, value) => `Preis: ${value}`],
  [/^Duration: (.+)$/i, (_, value) => `Dauer: ${value}`],
  [/^Studio: (.+)$/i, (_, value) => `Studio: ${value}`],
  [/^Artist: (.+)$/i, (_, value) => `Tätowierer: ${value}`],
  [/^Client: (.+)$/i, (_, value) => `Kunde: ${value}`],
  [/^Placement: (.+)$/i, (_, value) => `Körperstelle: ${translateUiText(value, "de")}`],
  [/^Style: (.+)$/i, (_, value) => `Stil: ${value}`],
  [/^From (.+)$/i, (_, value) => `Von ${value}`],
  [/^To (.+)$/i, (_, value) => `Bis ${value}`],
  [/^(.+) - Full day$/i, (_, value) => `${value} — ganzer Tag`],
  [/^(.+) - Hourly break$/i, (_, value) => `${value} — stundenweise Pause`],
  [/^(\d+) free slots?$/i, (_, number) => `${number} ${number === "1" ? "freier Termin" : "freie Termine"}`],
  [/^(\d+) improvements? remaining$/i, (_, number) => `${number} ${number === "1" ? "Verbesserung verbleibend" : "Verbesserungen verbleibend"}`],
  [/^Tattoo sessions \((\d+) booked(?:, (\d+) remaining)?\)$/i, (_, booked, remaining) => `Tattoo-Sitzungen (${booked} gebucht${remaining == null ? "" : `, ${remaining} verbleibend`})`],
  [/^Open portfolio image (\d+)$/i, (_, number) => `Portfoliobild ${number} öffnen`],
  [/^Show image (\d+)$/i, (_, number) => `Bild ${number} anzeigen`],
  [/^Tattoo reference (\d+)$/i, (_, number) => `Tattoo-Referenz ${number}`],
  [/^Reference (\d+)$/i, (_, number) => `Referenz ${number}`],
  [/^(.+) reference$/i, (_, value) => `${translateUiText(value, "de")} — Referenzbild`],
  [/^Requirement (\d+)$/i, (_, number) => `Anforderung ${number}`],
  [/^Request: (.+)$/i, (_, request) => `Anfrage: ${request}`],
];

const frenchDynamicRules = [
  [/^Session (\d+) price$/i, (_, number) => `Prix de la séance ${number}`],
  [/^Extra session (\d+) price$/i, (_, number) => `Prix de la séance supplémentaire ${number}`],
  [/^Session (\d+)$/i, (_, number) => `Séance ${number}`],
  [/^Version (\d+)$/i, (_, number) => `Version ${number}`],
  [/^Step (\d+) of (\d+)$/i, (_, current, total) => `Étape ${current} sur ${total}`],
  [/^(\d+) hours?$/i, (_, number) => `${number} h`],
  [/^(\d+) minutes?$/i, (_, number) => `${number} min`],
  [/^(\d+) artists?$/i, (_, number) => `${number} ${number === "1" ? "tatoueur" : "tatoueurs"}`],
  [/^(\d+) versions?$/i, (_, number) => `${number} version${number === "1" ? "" : "s"}`],
  [/^(\d+) sessions?$/i, (_, number) => `${number} séance${number === "1" ? "" : "s"}`],
  [/^(\d+) days unavailable$/i, (_, number) => `${number} jour${number === "1" ? "" : "s"} indisponible${number === "1" ? "" : "s"}`],
  [/^Starts at (.+)$/i, (_, value) => `Commence à ${value}`],
  [/^(\d+) remaining$/i, (_, number) => `${number} restant${number === "1" ? "" : "s"}`],
  [/^Remaining: (\d+)$/i, (_, number) => `Restant : ${number}`],
  [/^Created (.+)$/i, (_, value) => `Créé ${value}`],
  [/^Sent (.+)$/i, (_, value) => `Envoyé ${value}`],
  [/^Price: (.+)$/i, (_, value) => `Prix : ${value}`],
  [/^Duration: (.+)$/i, (_, value) => `Durée : ${value}`],
  [/^Studio: (.+)$/i, (_, value) => `Studio : ${value}`],
  [/^Artist: (.+)$/i, (_, value) => `Tatoueur : ${value}`],
  [/^Client: (.+)$/i, (_, value) => `Client : ${value}`],
  [/^Placement: (.+)$/i, (_, value) => `Emplacement : ${translateUiText(value, "fr")}`],
  [/^Style: (.+)$/i, (_, value) => `Style : ${value}`],
  [/^From (.+)$/i, (_, value) => `De ${value}`],
  [/^To (.+)$/i, (_, value) => `À ${value}`],
  [/^(.+) - Full day$/i, (_, value) => `${value} — journée entière`],
  [/^(.+) - Hourly break$/i, (_, value) => `${value} — pause horaire`],
  [/^(\d+) free slots?$/i, (_, number) => `${number} créneau${number === "1" ? " libre" : "x libres"}`],
  [/^(\d+) improvements? remaining$/i, (_, number) => `${number} amélioration${number === "1" ? "" : "s"} restante${number === "1" ? "" : "s"}`],
  [/^Tattoo sessions \((\d+) booked(?:, (\d+) remaining)?\)$/i, (_, booked, remaining) => `Séances (${booked} réservées${remaining == null ? "" : `, ${remaining} restantes`})`],
  [/^Open portfolio image (\d+)$/i, (_, number) => `Ouvrir l’image ${number} du portfolio`],
  [/^Show image (\d+)$/i, (_, number) => `Afficher l’image ${number}`],
  [/^Tattoo reference (\d+)$/i, (_, number) => `Référence de tatouage ${number}`],
  [/^Reference (\d+)$/i, (_, number) => `Référence ${number}`],
  [/^(.+) reference$/i, (_, value) => `${translateUiText(value, "fr")} — image de référence`],
  [/^Requirement (\d+)$/i, (_, number) => `Condition ${number}`],
  [/^Request: (.+)$/i, (_, request) => `Demande : ${request}`],
];

const spanishDynamicRules = [
  [/^Session (\d+) price$/i, (_, number) => `Precio de la sesión ${number}`],
  [/^Extra session (\d+) price$/i, (_, number) => `Precio de la sesión adicional ${number}`],
  [/^Session (\d+)$/i, (_, number) => `Sesión ${number}`],
  [/^Version (\d+)$/i, (_, number) => `Versión ${number}`],
  [/^Step (\d+) of (\d+)$/i, (_, current, total) => `Paso ${current} de ${total}`],
  [/^(\d+) hours?$/i, (_, number) => `${number} h`],
  [/^(\d+) minutes?$/i, (_, number) => `${number} min`],
  [/^(\d+) artists?$/i, (_, number) => `${number} ${number === "1" ? "tatuador" : "tatuadores"}`],
  [/^(\d+) versions?$/i, (_, number) => `${number} ${number === "1" ? "versión" : "versiones"}`],
  [/^(\d+) sessions?$/i, (_, number) => `${number} ${number === "1" ? "sesión" : "sesiones"}`],
  [/^(\d+) days unavailable$/i, (_, number) => `${number} ${number === "1" ? "día no disponible" : "días no disponibles"}`],
  [/^Starts at (.+)$/i, (_, value) => `Empieza a las ${value}`],
  [/^(\d+) remaining$/i, (_, number) => `${number} restantes`],
  [/^Remaining: (\d+)$/i, (_, number) => `Restantes: ${number}`],
  [/^Created (.+)$/i, (_, value) => `Creado ${value}`],
  [/^Sent (.+)$/i, (_, value) => `Enviado ${value}`],
  [/^Price: (.+)$/i, (_, value) => `Precio: ${value}`],
  [/^Duration: (.+)$/i, (_, value) => `Duración: ${value}`],
  [/^Studio: (.+)$/i, (_, value) => `Estudio: ${value}`],
  [/^Artist: (.+)$/i, (_, value) => `Tatuador: ${value}`],
  [/^Client: (.+)$/i, (_, value) => `Cliente: ${value}`],
  [/^Placement: (.+)$/i, (_, value) => `Ubicación: ${translateUiText(value, "es")}`],
  [/^Style: (.+)$/i, (_, value) => `Estilo: ${value}`],
  [/^From (.+)$/i, (_, value) => `Desde ${value}`],
  [/^To (.+)$/i, (_, value) => `Hasta ${value}`],
  [/^(.+) - Full day$/i, (_, value) => `${value} — día completo`],
  [/^(.+) - Hourly break$/i, (_, value) => `${value} — descanso por horas`],
  [/^(\d+) free slots?$/i, (_, number) => `${number} ${number === "1" ? "horario libre" : "horarios libres"}`],
  [/^(\d+) improvements? remaining$/i, (_, number) => `${number} ${number === "1" ? "mejora restante" : "mejoras restantes"}`],
  [/^Tattoo sessions \((\d+) booked(?:, (\d+) remaining)?\)$/i, (_, booked, remaining) => `Sesiones (${booked} reservadas${remaining == null ? "" : `, ${remaining} restantes`})`],
  [/^Open portfolio image (\d+)$/i, (_, number) => `Abrir imagen ${number} del portafolio`],
  [/^Show image (\d+)$/i, (_, number) => `Mostrar imagen ${number}`],
  [/^Tattoo reference (\d+)$/i, (_, number) => `Referencia del tatuaje ${number}`],
  [/^Reference (\d+)$/i, (_, number) => `Referencia ${number}`],
  [/^(.+) reference$/i, (_, value) => `${translateUiText(value, "es")} — imagen de referencia`],
  [/^Requirement (\d+)$/i, (_, number) => `Requisito ${number}`],
  [/^Request: (.+)$/i, (_, request) => `Solicitud: ${request}`],
];

const italianDynamicRules = [
  [/^Session (\d+) price$/i, (_, number) => `Prezzo della seduta ${number}`],
  [/^Extra session (\d+) price$/i, (_, number) => `Prezzo della seduta aggiuntiva ${number}`],
  [/^Session (\d+)$/i, (_, number) => `Seduta ${number}`],
  [/^Version (\d+)$/i, (_, number) => `Versione ${number}`],
  [/^Step (\d+) of (\d+)$/i, (_, current, total) => `Passaggio ${current} di ${total}`],
  [/^(\d+) hours?$/i, (_, number) => `${number} h`],
  [/^(\d+) minutes?$/i, (_, number) => `${number} min`],
  [/^(\d+) artists?$/i, (_, number) => `${number} ${number === "1" ? "tatuatore" : "tatuatori"}`],
  [/^(\d+) versions?$/i, (_, number) => `${number} ${number === "1" ? "versione" : "versioni"}`],
  [/^(\d+) sessions?$/i, (_, number) => `${number} ${number === "1" ? "seduta" : "sedute"}`],
  [/^(\d+) days unavailable$/i, (_, number) => `${number} ${number === "1" ? "giorno non disponibile" : "giorni non disponibili"}`],
  [/^Starts at (.+)$/i, (_, value) => `Inizia alle ${value}`],
  [/^(\d+) remaining$/i, (_, number) => `${number} rimanenti`],
  [/^Remaining: (\d+)$/i, (_, number) => `Rimanenti: ${number}`],
  [/^Created (.+)$/i, (_, value) => `Creato ${value}`],
  [/^Sent (.+)$/i, (_, value) => `Inviato ${value}`],
  [/^Price: (.+)$/i, (_, value) => `Prezzo: ${value}`],
  [/^Duration: (.+)$/i, (_, value) => `Durata: ${value}`],
  [/^Studio: (.+)$/i, (_, value) => `Studio: ${value}`],
  [/^Artist: (.+)$/i, (_, value) => `Tatuatore: ${value}`],
  [/^Client: (.+)$/i, (_, value) => `Cliente: ${value}`],
  [/^Placement: (.+)$/i, (_, value) => `Zona: ${translateUiText(value, "it")}`],
  [/^Style: (.+)$/i, (_, value) => `Stile: ${value}`],
  [/^From (.+)$/i, (_, value) => `Da ${value}`],
  [/^To (.+)$/i, (_, value) => `A ${value}`],
  [/^(.+) - Full day$/i, (_, value) => `${value} — giornata intera`],
  [/^(.+) - Hourly break$/i, (_, value) => `${value} — pausa oraria`],
  [/^(\d+) free slots?$/i, (_, number) => `${number} ${number === "1" ? "orario libero" : "orari liberi"}`],
  [/^(\d+) improvements? remaining$/i, (_, number) => `${number} ${number === "1" ? "miglioramento rimanente" : "miglioramenti rimanenti"}`],
  [/^Tattoo sessions \((\d+) booked(?:, (\d+) remaining)?\)$/i, (_, booked, remaining) => `Sedute (${booked} prenotate${remaining == null ? "" : `, ${remaining} rimanenti`})`],
  [/^Open portfolio image (\d+)$/i, (_, number) => `Apri immagine ${number} del portfolio`],
  [/^Show image (\d+)$/i, (_, number) => `Mostra immagine ${number}`],
  [/^Tattoo reference (\d+)$/i, (_, number) => `Riferimento del tatuaggio ${number}`],
  [/^Reference (\d+)$/i, (_, number) => `Riferimento ${number}`],
  [/^(.+) reference$/i, (_, value) => `${translateUiText(value, "it")} — immagine di riferimento`],
  [/^Requirement (\d+)$/i, (_, number) => `Requisito ${number}`],
  [/^Request: (.+)$/i, (_, request) => `Richiesta: ${request}`],
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

  const rules = language === "de"
    ? germanDynamicRules
    : language === "fr"
      ? frenchDynamicRules
      : language === "es"
        ? spanishDynamicRules
        : language === "it"
          ? italianDynamicRules
          : dynamicRules;
  for (const [pattern, createTranslation] of rules) {
    const match = normalized.match(pattern);
    if (match) return createTranslation(...match);
  }

  if (language === "bg") {
    const monthTranslation = translateMonthText(normalized);
    return monthTranslation || text;
  }

  return text;
}
