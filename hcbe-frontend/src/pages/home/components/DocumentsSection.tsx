import { buildApiUrl } from '../../../lib/api/base-url';
import { localized, localizedOptional } from '../../../lib/i18n/localized';
import { Button, EmptyState, SectionHeading } from '../../../components/ui';

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

const DocumentsSection = () => {
  const { t, i18n } = useTranslation();
  const [documents, setDocuments] = useState<Document[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    loadDocuments();
  }, []);

  const loadDocuments = async () => {
    try {
      setIsLoading(true);
      setError(null);
      const response = await fetch(buildApiUrl('/api/documents'));
      const data = await response.json();
      if (data.success && data.data) {
        setDocuments(data.data);
      } else {
        setError(t('public.services.documents.errorUnavailable'));
      }
    } catch (err) {
      console.error('Error loading documents:', err);
      setError(t('public.services.documents.errorLoad'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleDownload = async (docId: string) => {
    try {
      const response = await fetch(buildApiUrl(`/api/documents/${docId}/download`));
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
      setError(t('public.services.documents.errorDownload'));
    }
  };

  return (
    <section id="documents" className="bg-paper py-24">
      <div className="container-page">
        <SectionHeading
          title={t('public.services.documents.sectionTitle')}
          description={t('public.services.documents.sectionSubtitle')}
        />

        {isLoading ? (
          <div className="border-b border-line">
            {[1, 2].map((item) => (
              <div key={item} className="grid grid-cols-1 gap-6 border-t border-line py-8 md:grid-cols-[120px_1fr]">
                <div className="h-4 w-24 animate-pulse bg-surface-container" />
                <div className="space-y-3">
                  <div className="h-6 w-2/3 animate-pulse bg-surface-container" />
                  <div className="h-4 w-full animate-pulse bg-surface-container" />
                  <div className="h-4 w-40 animate-pulse bg-surface-container" />
                </div>
              </div>
            ))}
          </div>
        ) : (
          <>
            {error && (
              <div className="mb-8">
                <EmptyState tone="error" icon="ri-error-warning-line" title={error} />
              </div>
            )}

            {documents.length > 0 ? (
              <div className="border-b border-line">
                {documents.map((doc) => {
                  const name = localized(doc.name, doc.nameEn, i18n.language);
                  const description = localizedOptional(doc.description, doc.descriptionEn, i18n.language);
                  const category =
                    localizedOptional(doc.category, doc.categoryEn, i18n.language) ||
                    t('public.services.documents.defaultCategory');
                  const pages =
                    localizedOptional(doc.pages, doc.pagesEn, i18n.language) ||
                    t('public.services.documents.pagesUnknown');
                  const size = doc.size || t('public.services.documents.sizeUnknown');

                  return (
                    <article
                      key={doc.id}
                      className="grid grid-cols-1 gap-6 border-t border-line py-8 md:grid-cols-[120px_1fr]"
                    >
                      <p className="text-label-md uppercase text-red-link">{category}</p>
                      <div>
                        <h3 className="font-display text-headline-md text-ink">{name}</h3>
                        {description && (
                          <p className="mt-2 line-clamp-3 text-body-md text-ink-variant">{description}</p>
                        )}
                        <p className="mt-4 text-label-md uppercase text-ink-variant">
                          {[doc.type || 'PDF', size, pages].join(' · ')}
                        </p>
                        <div className="mt-4 flex flex-wrap items-center gap-6">
                          <button
                            type="button"
                            onClick={() => handleDownload(doc.id)}
                            className="inline-flex min-h-[44px] items-center gap-2 text-label-md uppercase text-red-link transition-colors hover:text-green"
                          >
                            {t('public.services.documents.download')}
                            <i className="ri-download-line text-base" aria-hidden="true"></i>
                          </button>
                          <span className="text-body-md text-ink-variant">
                            {t('public.services.documents.downloads', { count: doc.downloads })}
                          </span>
                        </div>
                      </div>
                    </article>
                  );
                })}
              </div>
            ) : (
              <EmptyState
                icon="ri-folder-open-line"
                title={t('public.services.documents.emptyTitle')}
                description={t('public.services.documents.emptyText')}
              />
            )}
          </>
        )}

        <div className="mt-16 border border-line bg-surface-container p-8 md:p-10">
          <h3 className="font-display text-headline-md text-green">
            {t('public.services.documents.helpTitle')}
          </h3>
          <p className="mt-4 max-w-3xl text-body-md text-ink-variant">
            {t('public.services.documents.helpText')}
          </p>
          <div className="mt-6 flex flex-wrap gap-4">
            <Button to="/contact" variant="primary">
              {t('public.services.documents.helpContact')}
            </Button>
            <Button to="/services" variant="secondary">
              {t('public.services.documents.helpBack')}
            </Button>
          </div>
        </div>
      </div>
    </section>
  );
};

export default DocumentsSection;
