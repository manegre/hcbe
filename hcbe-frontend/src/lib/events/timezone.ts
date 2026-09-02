export const EVENT_TIME_ZONES = [
  { value: 'America/Toronto', label: 'Toronto / Montréal / Ottawa (ET)' },
  { value: 'America/Winnipeg', label: 'Winnipeg (CT)' },
  { value: 'America/Edmonton', label: 'Calgary / Edmonton (MT)' },
  { value: 'America/Vancouver', label: 'Vancouver (PT)' },
  { value: 'America/Halifax', label: 'Halifax (AT)' },
  { value: 'America/St_Johns', label: "St. John’s (NT)" },
  { value: 'Africa/Ouagadougou', label: 'Ouagadougou (GMT)' },
  { value: 'UTC', label: 'UTC' },
] as const;

const dateParts = (value: Date, timeZone: string) => {
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hourCycle: 'h23',
  }).formatToParts(value);
  return Object.fromEntries(parts.map((part) => [part.type, part.value]));
};

export const isoToZonedInput = (iso: string | undefined, timeZone: string): string => {
  if (!iso) return '';
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return '';
  const parts = dateParts(date, timeZone);
  return `${parts.year}-${parts.month}-${parts.day}T${parts.hour}:${parts.minute}`;
};

export const zonedInputToIso = (value: string, timeZone: string): string => {
  const match = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})$/.exec(value);
  if (!match) throw new Error('Invalid local date and time');

  const [, year, month, day, hour, minute] = match;
  const desiredWallTime = Date.UTC(+year, +month - 1, +day, +hour, +minute, 0);
  let instant = desiredWallTime;

  for (let iteration = 0; iteration < 3; iteration += 1) {
    const parts = dateParts(new Date(instant), timeZone);
    const renderedWallTime = Date.UTC(
      +parts.year,
      +parts.month - 1,
      +parts.day,
      +parts.hour,
      +parts.minute,
      +parts.second,
    );
    instant += desiredWallTime - renderedWallTime;
  }

  return new Date(instant).toISOString();
};

export const formatEventDateTime = (
  value: string,
  locale: string,
  timeZone?: string,
  options: Intl.DateTimeFormatOptions = {},
) =>
  new Intl.DateTimeFormat(locale, {
    timeZone: timeZone || 'America/Toronto',
    ...options,
  }).format(new Date(value));

export const getEventMonthKey = (value: string, timeZone?: string) => {
  const parts = dateParts(new Date(value), timeZone || 'America/Toronto');
  return `${parts.year}-${parts.month}`;
};
