/**
 * Event status has two layers:
 * 1. Publication status (manual in admin): Draft | Active | Cancelled | Completed
 * 2. Lifecycle (derived from date for published events): upcoming | ongoing | past
 *
 * Legacy French values ("À venir", "Brouillon", …) are still accepted from the API.
 */

export type EventPublicationStatus = 'draft' | 'active' | 'cancelled' | 'completed';
export type EventLifecycle = 'upcoming' | 'ongoing' | 'past' | 'draft' | 'cancelled';

type EventLike = {
  date: string;
  status: string;
};

const normalize = (value: string) =>
  value
    .trim()
    .toLowerCase()
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '');

export const getPublicationStatus = (status: string): EventPublicationStatus => {
  const value = normalize(status);

  if (value === 'draft' || value === 'brouillon') return 'draft';
  if (value === 'cancelled' || value === 'canceled' || value === 'annule') return 'cancelled';
  if (value === 'completed' || value === 'termine' || value === 'past') return 'completed';

  // Active, Upcoming, "À venir", and unknown published values
  return 'active';
};

const toDate = (value: Date | string | undefined): Date => {
  if (value instanceof Date) {
    return value;
  }

  if (typeof value === 'string') {
    return new Date(value);
  }

  return new Date();
};

const startOfLocalDay = (value: Date | string | undefined) => {
  const date = toDate(value);
  return new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
};

/**
 * Derives the public-facing lifecycle from publication status + event date.
 * Past dates are never shown as "upcoming", even if the DB still says "À venir".
 */
export const getEventLifecycle = (event: EventLike, now?: Date | string): EventLifecycle => {
  const publication = getPublicationStatus(event.status);

  if (publication === 'draft') return 'draft';
  if (publication === 'cancelled') return 'cancelled';
  if (publication === 'completed') return 'past';

  const eventDate = new Date(event.date);
  if (Number.isNaN(eventDate.getTime())) return 'past';

  const todayStart = startOfLocalDay(now);
  const eventDayStart = startOfLocalDay(eventDate);
  const tomorrowStart = todayStart + 24 * 60 * 60 * 1000;

  if (eventDayStart < todayStart) return 'past';
  if (eventDayStart >= tomorrowStart) return 'upcoming';

  // Same calendar day: treat as ongoing for the public agenda
  return 'ongoing';
};

export const isPublicAgendaEvent = (event: EventLike, now?: Date | string): boolean => {
  const lifecycle = getEventLifecycle(event, now);
  return lifecycle === 'upcoming' || lifecycle === 'ongoing' || lifecycle === 'past';
};

export const isCurrentOrUpcomingEvent = (event: EventLike, now?: Date | string): boolean => {
  const lifecycle = getEventLifecycle(event, now);
  return lifecycle === 'upcoming' || lifecycle === 'ongoing';
};

export const sortEventsForPublic = <T extends EventLike>(events: T[], now?: Date | string): T[] => {
  return [...events].sort((a, b) => {
    const lifeA = getEventLifecycle(a, now);
    const lifeB = getEventLifecycle(b, now);
    const rank = (life: EventLifecycle) => {
      if (life === 'ongoing') return 0;
      if (life === 'upcoming') return 1;
      return 2;
    };

    const rankDiff = rank(lifeA) - rank(lifeB);
    if (rankDiff !== 0) return rankDiff;

    const timeA = new Date(a.date).getTime();
    const timeB = new Date(b.date).getTime();

    // Upcoming/ongoing: soonest first. Past: most recent first.
    if (lifeA === 'past') return timeB - timeA;
    return timeA - timeB;
  });
};
