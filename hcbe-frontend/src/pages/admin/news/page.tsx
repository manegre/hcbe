import { newsApi } from '../../../lib/api/news';
import type { NewsArticle } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Field, StatusChip, Tag, Td, inputClasses } from '../../../components/ui';

type StatusFilter = 'all' | 'published' | 'draft';

const NewsAdminPage = () => {
  const { t } = useTranslation();
  const [articles, setArticles] = useState<NewsArticle[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<StatusFilter>('all');

  const loadArticles = async () => {
    try {
      setLoading(true);
      const response = await newsApi.getNewsForAdmin();
      if (response.success && response.data) {
        setArticles(response.data);
        setError(null);
      } else {
        setError(response.message || t('admin.news.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading news:', err);
      setError(err instanceof Error ? err.message : t('admin.news.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadArticles();
  }, []);

  const handleDelete = async (id: string, title: string) => {
    if (!window.confirm(t('admin.common.confirmDelete', { name: title }))) return;

    try {
      const response = await newsApi.deleteNews(id);
      if (response.success) {
        loadArticles();
      }
    } catch (err) {
      console.error('Error deleting news:', err);
    }
  };

  const filteredArticles = articles.filter((article) => {
    if (statusFilter === 'all') return true;
    return article.status === statusFilter;
  });

  const filterOptions: { value: StatusFilter; label: string }[] = [
    { value: 'all', label: t('admin.news.filterAll') },
    { value: 'published', label: t('admin.news.statusPublished') },
    { value: 'draft', label: t('admin.news.statusDraft') },
  ];

  const toolbar = (
    <Field label={t('admin.common.filterBy')} htmlFor="news-filter">
      <select
        id="news-filter"
        value={statusFilter}
        onChange={(e) => setStatusFilter(e.target.value as StatusFilter)}
        className={inputClasses}
      >
        {filterOptions.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
    </Field>
  );

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <AdminListPage
      title={t('admin.news.title')}
      count={error ? undefined : filteredArticles.length}
      createLabel={t('admin.news.create')}
      createPath="/admin/news/create"
      toolbar={toolbar}
      columns={[
        { key: 'title', label: t('admin.common.title') },
        { key: 'category', label: t('admin.news.category') },
        { key: 'status', label: t('admin.common.status') },
        { key: 'author', label: t('admin.news.author') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={filteredArticles.length === 0}
      emptyTitle={t('admin.news.emptyTitle')}
      error={error ?? undefined}
      onRetry={loadArticles}
    >
      {filteredArticles.map((article) => (
        <tr key={article.id} className="transition-colors hover:bg-surface-container">
          <Td className="text-ink">
            <div className="font-medium">{article.title}</div>
            <p className="mt-1 max-w-xs truncate text-body-md text-ink-variant">
              {article.excerpt || article.content}
            </p>
            {article.isPinned && <Tag className="mt-2">{t('admin.news.pinned')}</Tag>}
          </Td>
          <Td>{article.category || t('admin.common.na')}</Td>
          <Td>
            <StatusChip
              status={article.status === 'published' ? 'published' : 'draft'}
              label={
                article.status === 'published' ? t('admin.news.statusPublished') : t('admin.news.statusDraft')
              }
            />
          </Td>
          <Td>
            <div>{article.author || t('admin.common.na')}</div>
            {article.publishedDate && (
              <div className="text-ink-variant">{new Date(article.publishedDate).toLocaleDateString()}</div>
            )}
          </Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/news/${article.id}`}
                aria-label={t('admin.common.view')}
                title={t('admin.common.view')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-eye-line text-lg" aria-hidden="true" />
              </Link>
              <Link
                to={`/admin/news/${article.id}/edit`}
                aria-label={t('admin.common.edit')}
                title={t('admin.common.edit')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-edit-line text-lg" aria-hidden="true" />
              </Link>
              <button
                type="button"
                onClick={() => handleDelete(article.id, article.title)}
                aria-label={t('admin.common.delete')}
                title={t('admin.common.delete')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center rounded-control text-error transition-colors hover:text-error-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error"
              >
                <i className="ri-delete-bin-line text-lg" aria-hidden="true" />
              </button>
            </div>
          </Td>
        </tr>
      ))}
    </AdminListPage>
  );
};

export default NewsAdminPage;
