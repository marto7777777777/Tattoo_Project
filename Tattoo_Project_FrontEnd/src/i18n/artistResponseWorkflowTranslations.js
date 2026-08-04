const keys = {
  workflow: "Project workflow",
  question: "How should this project continue?",
  choose: "Choose whether the client should book a consultation or go directly to tattoo sessions.",
  consultationFirst: "Consultation first",
  consultationDescription: "The client books a consultation before the final tattoo sessions are planned.",
  direct: "Go directly to sessions",
  directDescription: "Skip the consultation and let the client book the planned tattoo sessions immediately.",
  skipped: "Consultation skipped",
  plan: "Plan the tattoo sessions",
  exact: "Set the exact price and duration of every session the client will be able to book.",
  chooseError: "Choose how the tattoo project should continue.",
  directArtist: "This project goes directly to tattoo session booking.",
  skippedByArtist: "Skipped by artist",
  totalPrice: "Planned total price",
  totalTime: "Planned total time",
  directClient: "The artist confirmed that this project can continue directly to tattoo session booking.",
  directWaiting: "Consultation skipped · waiting for tattoo session booking",
  validWorkflow: "Choose a valid workflow after the artist response.",
  needsSession: "Direct booking requires at least one tattoo session.",
  priceCount: "Session price count must match the number of sessions.",
  durationCount: "Session duration count must match the number of sessions.",
  consultationEstimate: "Indicative price and duration must be greater than zero when a consultation is required."
};

function make(values) {
  return Object.fromEntries(Object.keys(keys).map((key) => [keys[key], values[key]]));
}

export const artistResponseWorkflowTranslations = {
  bg: make({
    workflow:"Работен процес",question:"Как да продължи този проект?",choose:"Избери дали клиентът да резервира консултация или да премине директно към тату сесиите.",consultationFirst:"Първо консултация",consultationDescription:"Клиентът резервира консултация, преди да бъдат планирани окончателните тату сесии.",direct:"Директно към сесиите",directDescription:"Пропусни консултацията и позволи на клиента веднага да резервира планираните тату сесии.",skipped:"Консултацията е пропусната",plan:"Планирай тату сесиите",exact:"Задай точната цена и продължителност на всяка сесия, която клиентът ще може да резервира.",chooseError:"Избери как да продължи тату проектът.",directArtist:"Този проект преминава директно към резервиране на тату сесии.",skippedByArtist:"Пропусната от татуиста",totalPrice:"Обща планирана цена",totalTime:"Общо планирано време",directClient:"Татуистът потвърди, че проектът може да продължи директно към резервиране на тату сесии.",directWaiting:"Консултацията е пропусната · изчаква резервиране на тату сесия",validWorkflow:"Избери валиден работен процес след отговора на татуиста.",needsSession:"Директното резервиране изисква поне една тату сесия.",priceCount:"Броят на цените трябва да съвпада с броя на сесиите.",durationCount:"Броят на продължителностите трябва да съвпада с броя на сесиите.",consultationEstimate:"Ориентировъчната цена и продължителност трябва да са по-големи от нула, когато е необходима консултация."
  }),
  de: make({
    workflow:"Projektablauf",question:"Wie soll dieses Projekt fortgesetzt werden?",choose:"Wähle, ob der Kunde eine Beratung buchen oder direkt zu den Tattoo-Sitzungen gehen soll.",consultationFirst:"Zuerst Beratung",consultationDescription:"Der Kunde bucht eine Beratung, bevor die endgültigen Tattoo-Sitzungen geplant werden.",direct:"Direkt zu den Sitzungen",directDescription:"Überspringe die Beratung und erlaube dem Kunden, die geplanten Sitzungen sofort zu buchen.",skipped:"Beratung übersprungen",plan:"Tattoo-Sitzungen planen",exact:"Lege den genauen Preis und die Dauer jeder buchbaren Sitzung fest.",chooseError:"Wähle, wie das Tattoo-Projekt fortgesetzt werden soll.",directArtist:"Dieses Projekt geht direkt zur Buchung der Tattoo-Sitzungen.",skippedByArtist:"Vom Tätowierer übersprungen",totalPrice:"Geplanter Gesamtpreis",totalTime:"Geplante Gesamtzeit",directClient:"Der Tätowierer hat bestätigt, dass das Projekt direkt mit der Sitzungsbuchung fortgesetzt werden kann.",directWaiting:"Beratung übersprungen · wartet auf Sitzungsbuchung",validWorkflow:"Wähle einen gültigen Ablauf nach der Antwort.",needsSession:"Direkte Buchung erfordert mindestens eine Tattoo-Sitzung.",priceCount:"Die Anzahl der Preise muss der Sitzungsanzahl entsprechen.",durationCount:"Die Anzahl der Dauern muss der Sitzungsanzahl entsprechen.",consultationEstimate:"Preis und Dauer müssen größer als null sein, wenn eine Beratung erforderlich ist."
  }),
  fr: make({
    workflow:"Parcours du projet",question:"Comment ce projet doit-il continuer ?",choose:"Choisissez si le client doit réserver une consultation ou passer directement aux séances.",consultationFirst:"Consultation d’abord",consultationDescription:"Le client réserve une consultation avant la planification finale des séances.",direct:"Directement aux séances",directDescription:"Ignorez la consultation et permettez au client de réserver immédiatement les séances prévues.",skipped:"Consultation ignorée",plan:"Planifier les séances",exact:"Définissez le prix exact et la durée de chaque séance réservable.",chooseError:"Choisissez comment le projet doit continuer.",directArtist:"Ce projet passe directement à la réservation des séances.",skippedByArtist:"Ignorée par le tatoueur",totalPrice:"Prix total prévu",totalTime:"Durée totale prévue",directClient:"Le tatoueur a confirmé que ce projet peut passer directement à la réservation des séances.",directWaiting:"Consultation ignorée · en attente de réservation",validWorkflow:"Choisissez un parcours valide après la réponse.",needsSession:"La réservation directe exige au moins une séance.",priceCount:"Le nombre de prix doit correspondre au nombre de séances.",durationCount:"Le nombre de durées doit correspondre au nombre de séances.",consultationEstimate:"Le prix et la durée indicatifs doivent être supérieurs à zéro lorsqu’une consultation est requise."
  }),
  es: make({
    workflow:"Flujo del proyecto",question:"¿Cómo debe continuar este proyecto?",choose:"Elige si el cliente debe reservar una consulta o pasar directamente a las sesiones.",consultationFirst:"Primero consulta",consultationDescription:"El cliente reserva una consulta antes de planificar las sesiones definitivas.",direct:"Directamente a las sesiones",directDescription:"Omite la consulta y permite que el cliente reserve inmediatamente las sesiones previstas.",skipped:"Consulta omitida",plan:"Planificar las sesiones",exact:"Define el precio exacto y la duración de cada sesión disponible.",chooseError:"Elige cómo debe continuar el proyecto.",directArtist:"Este proyecto pasa directamente a la reserva de sesiones.",skippedByArtist:"Omitida por el tatuador",totalPrice:"Precio total previsto",totalTime:"Tiempo total previsto",directClient:"El tatuador confirmó que el proyecto puede continuar directamente con la reserva de sesiones.",directWaiting:"Consulta omitida · esperando reserva de sesión",validWorkflow:"Elige un flujo válido después de la respuesta.",needsSession:"La reserva directa requiere al menos una sesión.",priceCount:"La cantidad de precios debe coincidir con las sesiones.",durationCount:"La cantidad de duraciones debe coincidir con las sesiones.",consultationEstimate:"El precio y la duración indicativos deben ser mayores que cero cuando se requiere consulta."
  }),
  it: make({
    workflow:"Flusso del progetto",question:"Come deve proseguire questo progetto?",choose:"Scegli se il cliente deve prenotare una consulenza o passare direttamente alle sedute.",consultationFirst:"Prima la consulenza",consultationDescription:"Il cliente prenota una consulenza prima della pianificazione finale delle sedute.",direct:"Direttamente alle sedute",directDescription:"Salta la consulenza e consenti al cliente di prenotare subito le sedute pianificate.",skipped:"Consulenza saltata",plan:"Pianifica le sedute",exact:"Imposta il prezzo esatto e la durata di ogni seduta prenotabile.",chooseError:"Scegli come deve proseguire il progetto.",directArtist:"Questo progetto passa direttamente alla prenotazione delle sedute.",skippedByArtist:"Saltata dal tatuatore",totalPrice:"Prezzo totale pianificato",totalTime:"Durata totale pianificata",directClient:"Il tatuatore ha confermato che il progetto può passare direttamente alla prenotazione delle sedute.",directWaiting:"Consulenza saltata · in attesa di prenotazione",validWorkflow:"Scegli un flusso valido dopo la risposta.",needsSession:"La prenotazione diretta richiede almeno una seduta.",priceCount:"Il numero dei prezzi deve corrispondere alle sedute.",durationCount:"Il numero delle durate deve corrispondere alle sedute.",consultationEstimate:"Prezzo e durata indicativi devono essere maggiori di zero quando è richiesta una consulenza."
  })
};
