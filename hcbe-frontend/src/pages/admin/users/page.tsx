import React, { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { usersApi } from '../../../lib/api/users';
import type { AdminUser } from '../../../lib/api/types';
import { useAuth } from '../../../contexts/AuthContext';
import { AdminListPage } from '../../../components/admin/AdminListPage';
import { Tag, Td } from '../../../components/ui';

const AdminUsersPage: React.FC = () => {
  const { t } = useTranslation();
  const { user: currentUser } = useAuth();
  const [users, setUsers] = useState<AdminUser[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const loadUsers = async () => {
    try {
      setLoading(true);
      const response = await usersApi.getAdminUsers();
      if (response.success && response.data) {
        setUsers(response.data);
        setError(null);
      } else {
        setError(response.message || t('admin.users.errorLoad'));
      }
    } catch (err) {
      console.error('Error loading admin users:', err);
      setError(err instanceof Error ? err.message : t('admin.users.errorLoad'));
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    loadUsers();
  }, []);

  const handleDelete = async (id: string, email: string) => {
    if (!window.confirm(t('admin.common.confirmDelete', { name: email }))) return;

    try {
      const response = await usersApi.deleteAdminUser(id);
      if (response.success) {
        loadUsers();
      } else {
        window.alert(response.message || t('admin.common.errorDelete'));
      }
    } catch (err) {
      console.error('Error deleting admin user:', err);
    }
  };

  const displayName = (user: AdminUser) =>
    [user.firstName, user.lastName].filter(Boolean).join(' ') || user.email;

  if (loading) {
    return (
      <div className="flex items-center justify-center py-24">
        <div className="h-8 w-8 animate-spin border-2 border-line border-t-green" />
      </div>
    );
  }

  return (
    <AdminListPage
      title={t('admin.users.title')}
      count={error ? undefined : users.length}
      createLabel={t('admin.users.create')}
      createPath="/admin/users/create"
      columns={[
        { key: 'user', label: t('admin.users.colUser') },
        { key: 'email', label: t('admin.common.email') },
        { key: 'date', label: t('admin.common.date') },
        { key: 'actions', label: t('admin.common.actions'), align: 'right' },
      ]}
      isEmpty={users.length === 0}
      emptyTitle={t('admin.users.emptyTitle')}
      error={error ?? undefined}
      onRetry={loadUsers}
    >
      {users.map((user) => (
        <tr key={user.id} className="transition-colors hover:bg-surface-container">
          <Td className="text-ink">
            <div className="flex items-center gap-3">
              <i className="ri-shield-user-line text-lg text-ink-variant" aria-hidden="true" />
              <div className="font-medium">
                {displayName(user)}
                {currentUser?.id === user.id && <Tag className="ml-2">{t('admin.users.you')}</Tag>}
              </div>
            </div>
          </Td>
          <Td>{user.email}</Td>
          <Td>{new Date(user.createdAt).toLocaleDateString()}</Td>
          <Td align="right">
            <div className="inline-flex items-center justify-end gap-1">
              <Link
                to={`/admin/users/${user.id}/edit`}
                aria-label={t('admin.common.edit')}
                title={t('admin.common.edit')}
                className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center text-green transition-colors hover:text-green-deep"
              >
                <i className="ri-edit-line text-lg" aria-hidden="true" />
              </Link>
              {currentUser?.id !== user.id && (
                <button
                  type="button"
                  onClick={() => handleDelete(user.id, user.email)}
                  aria-label={t('admin.common.delete')}
                  title={t('admin.common.delete')}
                  className="inline-flex min-h-[44px] min-w-[44px] items-center justify-center rounded-control text-error transition-colors hover:text-error-deep focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-error"
                >
                  <i className="ri-delete-bin-line text-lg" aria-hidden="true" />
                </button>
              )}
            </div>
          </Td>
        </tr>
      ))}
    </AdminListPage>
  );
};

export default AdminUsersPage;
