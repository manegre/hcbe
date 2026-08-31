export type NewsImagePosition = 'top' | 'center' | 'bottom';

export const NEWS_IMAGE_POSITIONS: NewsImagePosition[] = ['top', 'center', 'bottom'];

export function resolveNewsImagePosition(value?: string | null): NewsImagePosition {
  if (value === 'top' || value === 'bottom') return value;
  return 'center';
}

export function newsImageObjectPositionClass(value?: string | null): string {
  switch (resolveNewsImagePosition(value)) {
    case 'top':
      return 'object-top';
    case 'bottom':
      return 'object-bottom';
    default:
      return 'object-center';
  }
}
