import { useEffect, useState } from 'react';
import { eventCategoriesApi } from '../api/event-categories';
import type { EventCategory } from '../api/types';

export const useEventCategories = (includeInactive = false) => {
  const [categories, setCategories] = useState<EventCategory[]>([]);

  useEffect(() => {
    let active = true;
    const request = includeInactive
      ? eventCategoriesApi.getCategoriesForAdmin()
      : eventCategoriesApi.getCategories();
    request
      .then((response) => {
        if (active && response.success && response.data) setCategories(response.data);
      })
      .catch(() => undefined);
    return () => {
      active = false;
    };
  }, [includeInactive]);

  return categories;
};

export const getEventCategoryLabel = (
  type: string | undefined,
  categories: EventCategory[],
  language: string,
) => {
  if (!type) return undefined;
  const normalized = type.trim().toLowerCase();
  const category = categories.find((item) => item.slug.toLowerCase() === normalized);
  if (!category) return type;
  return language.startsWith('en') ? category.nameEn || category.name : category.name;
};
