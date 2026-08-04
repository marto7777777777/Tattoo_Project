const sharedKeys = {
  cancelConsultation: "Cancel consultation",
  cancelSession: "Cancel session",
  cancelConsultationQuestion: "Cancel consultation?",
  cancelSessionQuestion: "Cancel tattoo session?",
  clientLimit: "Clients cannot cancel within 24 hours of the appointment.",
  limit: "Cannot be cancelled within 24 hours.",
  clientRebook: "The booked time will be removed. You will be able to choose and book a new available time for this project.",
  artistRebook: "Only this booked time will be removed. The tattoo request will remain active and the client will be able to book a new available time.",
  confirm: "Confirm cancellation",
  keep: "Keep appointment",
  back: "Go back",
  cancelling: "Cancelling...",
  cancelAppointment: "Cancel appointment",
  danger: "Danger zone",
  rejectTitle: "Reject this tattoo request",
  rejectDescription: "This closes the entire project, cancels every consultation and session, and prevents the client from booking again.",
  rejectEntire: "Reject entire request",
  rejectQuestion: "Reject the entire tattoo request?",
  rejectWarning: "The request will become Rejected. Every booked consultation and tattoo session will be removed, and the client will lose all booking rights for this project. This cannot be undone.",
  reject: "Reject request"
};

function dictionary(values) {
  return Object.fromEntries(Object.keys(sharedKeys).map((key) => [sharedKeys[key], values[key]]));
}

export const cancellationTranslations = {
  bg: dictionary({
    cancelConsultation: "Откажи консултацията", cancelSession: "Откажи сесията", cancelConsultationQuestion: "Да се откаже ли консултацията?", cancelSessionQuestion: "Да се откаже ли тату сесията?", clientLimit: "Клиентът не може да откаже часа по-малко от 24 часа преди началото.", limit: "Не може да бъде отказано по-малко от 24 часа преди началото.", clientRebook: "Резервираният час ще бъде премахнат. Ще можеш да резервираш нов свободен час за този проект.", artistRebook: "Ще бъде премахнат само този час. Заявката остава активна и клиентът ще може да резервира нов свободен час.", confirm: "Потвърди отказването", keep: "Запази часа", back: "Назад", cancelling: "Отказване...", cancelAppointment: "Откажи часа", danger: "Опасна зона", rejectTitle: "Отхвърли тази заявка", rejectDescription: "Това затваря целия проект, отменя всички консултации и сесии и не позволява на клиента да резервира отново.", rejectEntire: "Отхвърли цялата заявка", rejectQuestion: "Да се отхвърли ли цялата заявка?", rejectWarning: "Заявката ще стане Отхвърлена. Всички консултации и тату сесии ще бъдат премахнати, а клиентът ще загуби всички права за резервация по проекта. Това действие е необратимо.", reject: "Отхвърли заявката"
  }),
  de: dictionary({
    cancelConsultation: "Beratung absagen", cancelSession: "Sitzung absagen", cancelConsultationQuestion: "Beratung absagen?", cancelSessionQuestion: "Tattoo-Sitzung absagen?", clientLimit: "Kunden können innerhalb von 24 Stunden vor dem Termin nicht absagen.", limit: "Innerhalb von 24 Stunden nicht stornierbar.", clientRebook: "Der gebuchte Termin wird entfernt. Du kannst einen neuen verfügbaren Termin buchen.", artistRebook: "Nur dieser Termin wird entfernt. Die Anfrage bleibt aktiv und der Kunde kann neu buchen.", confirm: "Absage bestätigen", keep: "Termin behalten", back: "Zurück", cancelling: "Wird abgesagt...", cancelAppointment: "Termin absagen", danger: "Gefahrenbereich", rejectTitle: "Tattoo-Anfrage ablehnen", rejectDescription: "Das gesamte Projekt und alle Termine werden geschlossen; der Kunde kann nicht erneut buchen.", rejectEntire: "Gesamte Anfrage ablehnen", rejectQuestion: "Die gesamte Tattoo-Anfrage ablehnen?", rejectWarning: "Die Anfrage wird abgelehnt. Alle Termine werden entfernt und der Kunde verliert alle Buchungsrechte. Dies ist endgültig.", reject: "Anfrage ablehnen"
  }),
  fr: dictionary({
    cancelConsultation: "Annuler la consultation", cancelSession: "Annuler la séance", cancelConsultationQuestion: "Annuler la consultation ?", cancelSessionQuestion: "Annuler la séance de tatouage ?", clientLimit: "Le client ne peut pas annuler moins de 24 heures avant le rendez-vous.", limit: "Impossible d’annuler moins de 24 heures avant.", clientRebook: "Le créneau sera supprimé. Vous pourrez réserver un nouveau créneau disponible.", artistRebook: "Seul ce créneau sera supprimé. La demande restera active et le client pourra réserver à nouveau.", confirm: "Confirmer l’annulation", keep: "Garder le rendez-vous", back: "Retour", cancelling: "Annulation...", cancelAppointment: "Annuler le rendez-vous", danger: "Zone dangereuse", rejectTitle: "Refuser cette demande", rejectDescription: "Cela ferme tout le projet, annule tous les rendez-vous et empêche une nouvelle réservation.", rejectEntire: "Refuser toute la demande", rejectQuestion: "Refuser toute la demande de tatouage ?", rejectWarning: "La demande sera refusée. Tous les rendez-vous seront supprimés et le client perdra ses droits de réservation. Cette action est irréversible.", reject: "Refuser la demande"
  }),
  es: dictionary({
    cancelConsultation: "Cancelar consulta", cancelSession: "Cancelar sesión", cancelConsultationQuestion: "¿Cancelar la consulta?", cancelSessionQuestion: "¿Cancelar la sesión de tatuaje?", clientLimit: "El cliente no puede cancelar dentro de las 24 horas anteriores.", limit: "No se puede cancelar con menos de 24 horas.", clientRebook: "Se eliminará la cita. Podrás reservar una nueva hora disponible.", artistRebook: "Solo se eliminará esta cita. La solicitud seguirá activa y el cliente podrá reservar de nuevo.", confirm: "Confirmar cancelación", keep: "Mantener cita", back: "Volver", cancelling: "Cancelando...", cancelAppointment: "Cancelar cita", danger: "Zona de peligro", rejectTitle: "Rechazar esta solicitud", rejectDescription: "Esto cierra el proyecto, cancela todas las citas e impide volver a reservar.", rejectEntire: "Rechazar toda la solicitud", rejectQuestion: "¿Rechazar toda la solicitud de tatuaje?", rejectWarning: "La solicitud quedará rechazada. Se eliminarán todas las citas y el cliente perderá sus derechos de reserva. Esta acción es irreversible.", reject: "Rechazar solicitud"
  }),
  it: dictionary({
    cancelConsultation: "Annulla consulenza", cancelSession: "Annulla seduta", cancelConsultationQuestion: "Annullare la consulenza?", cancelSessionQuestion: "Annullare la seduta di tatuaggio?", clientLimit: "Il cliente non può annullare nelle 24 ore precedenti.", limit: "Non annullabile nelle 24 ore precedenti.", clientRebook: "L’orario prenotato verrà rimosso. Potrai prenotarne uno nuovo.", artistRebook: "Verrà rimosso solo questo appuntamento. La richiesta resterà attiva e il cliente potrà prenotare di nuovo.", confirm: "Conferma annullamento", keep: "Mantieni appuntamento", back: "Indietro", cancelling: "Annullamento...", cancelAppointment: "Annulla appuntamento", danger: "Zona pericolosa", rejectTitle: "Rifiuta questa richiesta", rejectDescription: "Questo chiude il progetto, annulla tutti gli appuntamenti e impedisce nuove prenotazioni.", rejectEntire: "Rifiuta l’intera richiesta", rejectQuestion: "Rifiutare l’intera richiesta di tatuaggio?", rejectWarning: "La richiesta verrà rifiutata. Tutti gli appuntamenti saranno rimossi e il cliente perderà i diritti di prenotazione. L’azione è irreversibile.", reject: "Rifiuta richiesta"
  })
};
