const IMAGE_EXTENSIONS = new Set(['jpg', 'jpeg', 'png', 'webp', 'gif', 'bmp', 'svg']);

export const isImageFile = (contentType?: string | null, fileName?: string | null): boolean => {
  const mime = contentType?.trim().toLowerCase() ?? '';
  if (mime.startsWith('image/')) {
    return true;
  }

  const name = fileName?.trim().toLowerCase() ?? '';
  const extension = name.includes('.') ? name.split('.').pop() ?? '' : '';
  return IMAGE_EXTENSIONS.has(extension);
};
