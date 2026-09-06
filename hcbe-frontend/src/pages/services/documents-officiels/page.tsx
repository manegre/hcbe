import Navbar from '../../../components/feature/Navbar';
import Footer from '../../../components/feature/Footer';
import { buildApiUrl } from '../../../lib/api/base-url';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import {
  Button,
  ArrowLink,
  PageHeader,
  DataTable,
  Td,
  EmptyState,
  inputClasses,
} from '../../../components/ui';
import { ServiceNav } from '../components/ServiceNav';

interface Document {
  id: string;
  name: string;
  nameEn?: string;
  description?: string;
  descriptionEn?: string;
  icon?: string;
  type?: string;
  size?: string;
  pages?: string;
  pagesEn?: string;
  category?: string;
  categoryEn?: string;
  url?: string;
  downloads: number;
  isActive: boolean;
  displayOrder: number;
  createdAt: string;
}

const rememberItems = [
  {
    titleKey: 'public.services.documents.remember.validity.title',
    descriptionKey: 'public.services.documents.remember.validity.description',
  },
  {
    titleKey: 'public.services.documents.remember.access.title',
    descriptionKey: 'public.services.documents.remember.access.description',
  },
  {
    titleKey: 'public.services.documents.remember.support.title',
    descriptionKey: 'public.services.documents.remember.support.description',
  },
] as const;

const formatDocumentDate = (value: string, language: string) => {
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return '';
  return new Intl.DateTimeFormat(language.startsWith('en') ? 'en-CA' : 'fr-CA', {
    day: '2-digit',
    month: 'short',
    year: 'numeric',
  }).format(date);
};

const DocumentsOfficielsPage = () => {
  const { t, i18n } = useTranslation();
  const [documents, setDocuments] = useState<Document[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [loadError, setLoadError] = useState<string | null>(null);
  const [downloadError, setDownloadError] = useState<string | null>(null);
  const [searchQuery, setSearchQuery] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('all');

  useEffect(() => {
    loadDocuments();
  }, []);

  const loadDocuments = async () => {
    try {
      setIsLoading(true);
      setLoadError(null);
      const response = await fetch(buildApiUrl('/api/documents'));
      const data = await response.json();
      if (data.success && data.data) {
        setDocuments(data.data);
      } else {
        setLoadError(t('public.services.documents.errorUnavailable'));
      }
    } catch (err) {
      console.error('Error loading documents:', err);
      setLoadError(t('public.services.documents.errorLoad'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleDownload = async (doc: Document) => {
    try {
      const response = await fetch(buildApiUrl(`/api/documents/${doc.id}/download`));
      if (response.ok) {
        const blob = await response.blob();
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        const contentDisposition = response.headers.get('content-disposition');
        const filename = contentDisposition
          ? contentDisposition.split('filename=')[1].replace(/"/g, '')
          : 'document.pdf';
        a.download = filename;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      } else {
        throw new Error(`Download failed with status ${response.status}`);
      }
    } catch (err) {
      console.error('Error downloading document:', err);
      setDownloadError(t('public.services.documents.errorDownload'));
    }
  };

  const documentsWithMeta = useMemo(
    () =>
      documents.map((doc) => ({
        doc,
        name: localized(doc.name, doc.nameEn, i18n.language),
        category:
          localizedOptional(doc.category, doc.categoryEn, i18n.language) ||
          t('public.services.documents.defaultCategory'),
        format: doc.type || 'PDF',
        size: doc.size || t('public.services.documents.sizeUnknown'),
        date: formatDocumentDate(doc.createdAt, i18n.language),
      })),
    [documents, i18n.language, t],
  );

  const categories = useMemo(
    () => Array.from(new Set(documentsWithMeta.map((item) => item.category))).sort(),
    [documentsWithMeta],
  );

  const filteredDocuments = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return documentsWithMeta.filter((item) => {
      const matchesSearch = !query || item.name.toLowerCase().includes(query);
      const matchesCategory = categoryFilter === 'all' || item.category === categoryFilter;
      return matchesSearch && matchesCategory;
    });
  }, [documentsWithMeta, searchQuery, categoryFilter]);

  const columns = [
    { key: 'name', label: t('public.services.documents.columnName') },
    { key: 'category', label: t('public.services.documents.columnCategory') },
    { key: 'date', label: t('public.services.documents.columnDate') },
    { key: 'format', label: t('public.services.documents.format') },
    { key: 'action', label: t('public.services.documents.columnAction'), align: 'right' as const },
  ];

  return (
    <div className="min-h-screen bg-background">
      <Navbar />
      <main>

      <PageHeader
        variant="hero"
        title={t('public.services.documents.title')}
        description={t('public.services.documents.subtitle')}
        actions={
          <>
            <Button href="#documents" variant="primary">
              {t('public.services.documents.cta.view')}
            </Button>
            <ArrowLink to="/contact" tone="gold">
              {t('public.services.documents.cta.ask')}
            </ArrowLink>
          </>
        }
        aside={
          <div className="overflow-hidden rounded-[16px] border border-white/15 bg-white/[0.06] backdrop-blur-sm">
            {rememberItems.map((item, index) => (
              <div key={item.titleKey} className={`p-5 ${index > 0 ? 'border-t border-white/10' : ''}`}>
                <p className="text-label-md uppercase text-gold">{t(item.titleKey)}</p>
                <p className="mt-2 text-body-md text-green-dim">{t(item.descriptionKey)}</p>
              </div>
            ))}
          </div>
        }
      />
      <ServiceNav />

      <section id="documents" className="bg-background pb-24 pt-12">
        <div className="container-page">
          <div className="public-surface flex flex-wrap items-center justify-between gap-4 p-4 sm:p-5">
            <div className="relative w-full max-w-md">
              <i
                className="ri-search-line pointer-events-none absolute left-4 top-1/2 -translate-y-1/2 text-ink-variant"
                aria-hidden="true"
              ></i>
              <input
                type="search"
                value={searchQuery}
                onChange={(event) => setSearchQuery(event.target.value)}
                placeholder={t('public.services.documents.searchPlaceholder')}
                aria-label={t('public.services.documents.searchPlaceholder')}
                className={`${inputClasses} pl-11`}
              />
            </div>
            <select
              value={categoryFilter}
              onChange={(event) => setCategoryFilter(event.target.value)}
              aria-label={t('public.services.documents.filterCategoryLabel')}
              className={`${inputClasses} w-auto`}
            >
              <option value="all">{t('public.services.documents.filterAllCategories')}</option>
              {categories.map((category) => (
                <option key={category} value={category}>
                  {category}
                </option>
              ))}
            </select>
          </div>

          {downloadError && (
            <div className="mt-6 border border-error p-4 text-body-md text-error">{downloadError}</div>
          )}

          <div className="mt-8">
            {isLoading ? (
              <div className="space-y-4 border border-line bg-surface p-6">
                {[1, 2, 3].map((item) => (
                  <div key={item} className="h-12 w-full animate-pulse bg-surface-container" />
                ))}
              </div>
            ) : loadError ? (
              <EmptyState tone="error" icon="ri-error-warning-line" title={loadError} />
            ) : filteredDocuments.length === 0 ? (
              <EmptyState
                icon="ri-folder-open-line"
                title={t('public.services.documents.emptyTitle')}
                description={t('public.services.documents.emptyText')}
              />
            ) : (
              <DataTable columns={columns}>
                {filteredDocuments.map(({ doc, name, category, date, format, size }) => (
                  <tr key={doc.id} className="transition-colors duration-200 hover:bg-surface-container">
                    <Td className="text-ink">
                      <span className="flex items-center gap-3">
                        <i className="ri-file-text-line text-green" aria-hidden="true"></i>
                        <span className="font-semibold text-green">{name}</span>
                      </span>
                    </Td>
                    <Td>{category}</Td>
                    <Td>{date}</Td>
                    <Td>{`${format} (${size})`}</Td>
                    <Td align="right">
                      <button
                        type="button"
                        onClick={() => handleDownload(doc)}
                        className="inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link hover:text-green"
                      >
                        {t('public.services.documents.download')}
                        <i className="ri-download-line" aria-hidden="true"></i>
                      </button>
                    </Td>
                  </tr>
                ))}
              </DataTable>
            )}
          </div>

          <div className="public-grid-pattern mt-16 overflow-hidden rounded-[20px] bg-green-deep p-8 shadow-[0_18px_45px_rgba(0,59,27,.14)] md:p-10">
            <h3 className="font-display text-headline-md text-white">
              {t('public.services.documents.helpTitle')}
            </h3>
            <p className="mt-4 max-w-3xl text-body-md text-white/70">
              {t('public.services.documents.helpText')}
            </p>
            <div className="mt-6 flex flex-wrap gap-4">
              <Button to="/contact" variant="primary">
                {t('public.services.documents.helpContact')}
              </Button>
              <Button to="/services" variant="secondary" className="border-white text-white hover:bg-white hover:text-green">
                {t('public.services.documents.helpBack')}
              </Button>
            </div>
          </div>
        </div>
      </section>

      </main>
      <Footer />
    </div>
  );
};

export default DocumentsOfficielsPage;
