import React, { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { associationsApi } from '../../../lib/api/associations';
import type { Association } from '../../../lib/api/types';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Field, StatusChip, Tag, Td, inputClasses } from '../../../components/ui';

export const AdminAssociationsList: React.FC = () => {
  const [associations, setAssociations] = useState<Association[]>([]);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedProvince, setSelectedProvince] = useState('all');
  const [selectedStatus, setSelectedStatus] = useState('all');
  const { t } = useTranslation();

  useEffect(() => {
    loadAssociations();
  }, []);

  const loadAssociations = async () => {
    try {
      setIsLoading(true);
      setError('');
      const response = await associationsApi.getAssociationsForAdmin();
      if (response.success && response.data) {
        setAssociations(response.data);
      } else {
        setError(t('admin.associations.errorLoad'));
      }
    } catch (error) {
      console.error('Error loading associations:', error);
      setError(t('admin.associations.errorLoad'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleDelete = async (id: string, name: string) => {
    if (!confirm(t('admin.common.confirmDelete', { name }))) {
      return;
    }

    try {
      const response = await associationsApi.deleteAssociation(id);
      if (response.success) {
        await loadAssociations(); // Reload the list
      } else {
        setError(t('admin.associations.errorDelete'));
      }
    } catch (error) {
      console.error('Error deleting association:', error);
      setError(t('admin.associations.errorDelete'));
    }
  };

  // Generate provinces dynamically
  const getUniqueProvinces = () => {
    const provinces = new Set<string>();
    associations.forEach(association => provinces.add(association.province));
    return ['all', ...Array.from(provinces).sort()];
  };

  const provinces = getUniqueProvinces();

  const filteredAssociations = associations.filter((association) => {
    const matchesSearch =
      association.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
      association.city.toLowerCase().includes(searchTerm.toLowerCase()) ||
      association.domains.some((d) => d.toLowerCase().includes(searchTerm.toLowerCase()));
    const matchesProvince =
      selectedProvince === 'all' || association.province === selectedProvince;
    const matchesStatus =
      selectedStatus === 'all' ||
      (selectedStatus === 'active' && association.isActive) ||
      (selectedStatus === 'inactive' && !association.isActive);
    return matchesSearch && matchesProvince && matchesStatus;
  });

  const toolbar = (
    <>
      <Field label={t('admin.common.search')} htmlFor="association-search">
        <input
          id="association-search"
          type="text"
          placeholder={t('admin.associations.searchPlaceholder')}
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className={inputClasses}
        />
      </Field>
      <Field label={t('admin.common.location')} htmlFor="association-province">
        <select
          id="association-province"
          value={selectedProvince}
          onChange={(e) => setSelectedProvince(e.target.value)}
          className={inputClasses}
        >
          {provinces.map((province) => (
            <option key={province} value={province}>
              {province === 'all' ? t('admin.associations.filterAllProvinces') : province}
            </option>
          ))}
        </select>
      </Field>
      <Field label={t('admin.common.status')} htmlFor="association-status">
        <select
          id="association-status"
          value={selectedStatus}
          onChange={(e) => setSelectedStatus(e.target.value)}
          className={inputClasses}
        >
          <option value="all">{t('admin.associations.filterAllStatuses')}</option>
          <option value="active">{t('admin.common.active')}</option>
          <option value="inactive">{t('admin.common.inactive')}</option>
        </select>
      </Field>
    </>
  );

  if (isLoading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <>
      <AdminListPage
        title={t('admin.associations.title')}
        count={error ? undefined : filteredAssociations.length}
        createLabel={t('admin.associations.create')}
        createPath="/admin/associations/create"
        toolbar={toolbar}
        columns={[
          { key: 'association', label: t('admin.associations.colAssociation') },
          { key: 'location', label: t('admin.associations.colLocation') },
          { key: 'domains', label: t('admin.associations.colDomains') },
          { key: 'members', label: t('admin.associations.colMembers') },
          { key: 'status', label: t('admin.common.status') },
          { key: 'actions', label: t('admin.common.actions'), align: 'right' },
        ]}
        isEmpty={filteredAssociations.length === 0}
        emptyTitle={t('admin.associations.emptyTitle')}
        emptyDescription={
          associations.length === 0
            ? t('admin.associations.emptyAll')
            : t('admin.associations.emptySearch')
        }
        error={error || undefined}
        onRetry={loadAssociations}
      >
        {filteredAssociations.map((association) => (
          <tr
            key={association.id}
            className={`transition-colors hover:bg-surface-container ${!association.isActive ? 'opacity-60' : ''}`}
          >
            <Td className="text-ink">
              <div className="font-medium">{association.name}</div>
              <div className="text-ink-variant">
                {association.president || t('admin.associations.presidentTba')}
              </div>
            </Td>
            <Td>
              <div>{association.city}</div>
              <div className="text-ink-variant">{association.province}</div>
            </Td>
            <Td>
              <div className="flex flex-wrap gap-1">
                {association.domains.slice(0, 2).map((domain) => (
                  <Tag key={domain}>{domain}</Tag>
                ))}
                {association.domains.length > 2 && (
                  <span className="text-body-md text-ink-variant">
                    {t('admin.associations.more', { count: association.domains.length - 2 })}
                  </span>
                )}
              </div>
            </Td>
            <Td>{association.memberCount || t('admin.associations.tba')}</Td>
            <Td>
              <StatusChip
                status={association.isActive ? 'published' : 'draft'}
                label={association.isActive ? t('admin.common.active') : t('admin.common.inactive')}
              />
            </Td>
            <Td align="right">
              <div className="inline-flex items-center justify-end gap-1">
                <Link
                  to={`/admin/associations/${association.id}`}
                  aria-label={t('admin.common.view')}
                  title={t('admin.common.view')}
                  className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
                >
                  <i className="ri-eye-line text-lg" aria-hidden="true" />
                </Link>
                <Link
                  to={`/admin/associations/${association.id}/edit`}
                  aria-label={t('admin.common.edit')}
                  title={t('admin.common.edit')}
                  className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
                >
                  <i className="ri-edit-line text-lg" aria-hidden="true" />
                </Link>
                <button
                  type="button"
                  onClick={() => handleDelete(association.id, association.name)}
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

      {!error && associations.length > 0 && (
        <div className="grid grid-cols-1 gap-gutter sm:grid-cols-2 xl:grid-cols-4">
          <div className="border border-line bg-surface p-6">
            <p className="font-display text-headline-xl tabular-nums text-green">{associations.length}</p>
            <p className="mt-2 text-label-md uppercase text-ink-variant">{t('admin.associations.statsTotal')}</p>
          </div>
          <div className="border border-line bg-surface p-6">
            <p className="font-display text-headline-xl tabular-nums text-green">
              {associations.filter((a) => a.isActive).length}
            </p>
            <p className="mt-2 text-label-md uppercase text-ink-variant">{t('admin.associations.statsActive')}</p>
          </div>
          <div className="border border-line bg-surface p-6">
            <p className="font-display text-headline-xl tabular-nums text-green">{provinces.length - 1}</p>
            <p className="mt-2 text-label-md uppercase text-ink-variant">{t('admin.associations.statsProvinces')}</p>
          </div>
          <div className="border border-line bg-surface p-6">
            <p className="font-display text-headline-xl tabular-nums text-green">{filteredAssociations.length}</p>
            <p className="mt-2 text-label-md uppercase text-ink-variant">{t('admin.associations.statsShowing')}</p>
          </div>
        </div>
      )}
    </>
  );
};
