import type { Event } from '../api/types';
import { buildApiUrl } from '../api/base-url';

const compactUtc = (value: string) => new Date(value).toISOString().replace(/[-:]/g, '').replace(/\.\d{3}/, '');

export const eventCalendarLinks = (event: Event, english: boolean) => {
  const start = compactUtc(event.date);
  const end = compactUtc(event.endDate ?? new Date(new Date(event.date).getTime() + 60 * 60 * 1000).toISOString());
  const title = english && event.titleEn ? event.titleEn : event.title;
  const description = english && event.descriptionEn ? event.descriptionEn : event.description ?? '';
  const location = english && event.locationEn ? event.locationEn : event.location ?? '';
  const details = `${description}\n\n${window.location.href}`;
  return {
    google: `https://calendar.google.com/calendar/render?action=TEMPLATE&text=${encodeURIComponent(title)}&dates=${start}/${end}&details=${encodeURIComponent(details)}&location=${encodeURIComponent(location)}`,
    outlook: `https://outlook.live.com/calendar/0/deeplink/compose?subject=${encodeURIComponent(title)}&startdt=${encodeURIComponent(new Date(event.date).toISOString())}&enddt=${encodeURIComponent(new Date(event.endDate ?? new Date(new Date(event.date).getTime() + 3600000)).toISOString())}&body=${encodeURIComponent(details)}&location=${encodeURIComponent(location)}`,
    apple: buildApiUrl(`/api/events/${event.id}/calendar.ics`),
  };
};
