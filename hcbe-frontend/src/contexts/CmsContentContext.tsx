import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation } from 'react-router-dom';
import i18n from '../i18n';
import { messages } from '../i18n/local';
import { siteContentApi } from '../lib/api/site-content';
import type { CmsPublishedContentDto } from '../lib/api/types';
import { createCmsHubConnection } from '../lib/realtime/cms-hub';

interface CmsContentContextValue {
  loading: boolean;
  version: number;
  getValue: (key: string, fallback?: string) => string;
  refresh: () => Promise<void>;
}

const CmsContentContext = createContext<CmsContentContextValue | undefined>(undefined);

const pageFromPath = (pathname: string) => {
  if (pathname === '/') return 'home';
  const firstSegment = pathname.split('/').filter(Boolean)[0] || 'home';
  if (firstSegment === 'actualites') return 'news';
  if (firstSegment === 'espace-membre') return 'member';
  return firstSegment;
};

export const CmsContentProvider = ({ children }: { children: ReactNode }) => {
  const { i18n: activeI18n } = useTranslation();
  const location = useLocation();
  const [items, setItems] = useState<Record<string, CmsPublishedContentDto>>({});
  const [loading, setLoading] = useState(true);
  const [version, setVersion] = useState(0);
  const appliedTranslationKeys = useRef<Set<string>>(new Set());

  const applyBundle = useCallback((nextItems: CmsPublishedContentDto[]) => {
    const byKey = Object.fromEntries(nextItems.map((item) => [item.key, item]));

    for (const key of appliedTranslationKeys.current) {
      const next = byKey[key];
      if (!next || (next.contentType !== 'text' && next.contentType !== 'richtext')) {
        const fallbackFr = messages.fr?.translation[key];
        const fallbackEn = messages.en?.translation[key];
        if (fallbackFr !== undefined) i18n.addResource('fr', 'translation', key, fallbackFr);
        if (fallbackEn !== undefined) i18n.addResource('en', 'translation', key, fallbackEn);
      }
    }

    const applied = new Set<string>();
    for (const item of nextItems) {
      if (item.contentType !== 'text' && item.contentType !== 'richtext') continue;
      if (item.valueFr !== undefined) i18n.addResource('fr', 'translation', item.key, item.valueFr);
      if (item.valueEn !== undefined) i18n.addResource('en', 'translation', item.key, item.valueEn);
      applied.add(item.key);
    }
    appliedTranslationKeys.current = applied;
    setItems(byKey);
    void i18n.changeLanguage(i18n.language);
  }, []);

  const refresh = useCallback(async () => {
    try {
      const response = await siteContentApi.getPublishedCms();
      if (response.success && response.data) {
        applyBundle(response.data.items);
        setVersion(response.data.version);
      }
    } catch {
      // The compiled bilingual content remains a resilient fallback when the API is unavailable.
    } finally {
      setLoading(false);
    }
  }, [applyBundle]);

  useEffect(() => { void refresh(); }, [refresh]);

  useEffect(() => {
    const connection = createCmsHubConnection();
    connection.on('ContentPublished', () => {
      void refresh().then(() => window.dispatchEvent(new Event('hcbe:content-published')));
    });
    void connection.start().catch(() => undefined);
    return () => { void connection.stop(); };
  }, [refresh]);

  const getValue = useCallback((key: string, fallback = '') => {
    const item = items[key];
    if (!item) return fallback;
    const english = activeI18n.language.startsWith('en');
    return (english ? item.valueEn || item.valueFr : item.valueFr || item.valueEn) || fallback;
  }, [activeI18n.language, items]);

  useEffect(() => {
    const page = pageFromPath(location.pathname);
    const language = activeI18n.language.startsWith('en') ? 'en' : 'fr';
    const localizedValue = (key: string) => {
      const item = items[key];
      return language === 'en' ? item?.valueEn || item?.valueFr : item?.valueFr || item?.valueEn;
    };
    const title = localizedValue(`seo.${page}.title`) || localizedValue('seo.global.title');
    const description = localizedValue(`seo.${page}.description`) || localizedValue('seo.global.description');
    document.title = title || "HCBE Canada - Haut Conseil des Burkinabè de l'Extérieur au Canada";
    let meta = document.querySelector<HTMLMetaElement>('meta[name="description"]');
    if (!meta) {
      meta = document.createElement('meta');
      meta.name = 'description';
      document.head.appendChild(meta);
    }
    meta.content = description || 'Services, actualités et communauté des Burkinabè au Canada.';
  }, [activeI18n.language, items, location.pathname]);

  const value = useMemo(() => ({ loading, version, getValue, refresh }), [getValue, loading, refresh, version]);
  return <CmsContentContext.Provider value={value}>{children}</CmsContentContext.Provider>;
};

export const useCmsContent = () => {
  const context = useContext(CmsContentContext);
  if (!context) throw new Error('useCmsContent must be used within CmsContentProvider');
  return context;
};
