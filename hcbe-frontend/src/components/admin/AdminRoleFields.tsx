import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { usersApi } from '../../lib/api/users';
import type { AdminRole } from '../../lib/api/types';
import { getAdminPermissionLabel } from '../../lib/adminPermissions';
import { Field, inputClasses } from '../ui';

interface AdminRoleFieldsProps {
  role: string;
  permissions: string[];
  onChange: (role: string, permissions: string[]) => void;
}

export function AdminRoleFields({ role, permissions, onChange }: AdminRoleFieldsProps) {
  const { i18n } = useTranslation();
  const language = i18n.language.startsWith('en') ? 'en' : 'fr';
  const [roles, setRoles] = useState<AdminRole[]>([]);
  const [loadError, setLoadError] = useState(false);

  useEffect(() => {
    usersApi.getAdminRoles()
      .then((response) => response.data ? setRoles(response.data) : setLoadError(true))
      .catch(() => setLoadError(true));
  }, []);

  const selected = roles.find((item) => item.key === role);
  const availablePermissions = useMemo(
    () => Array.from(new Set(roles.flatMap((item) => item.permissions))).sort(),
    [roles],
  );
  const isSuperAdmin = role === 'super-admin';

  const changeRole = (nextRole: string) => {
    const definition = roles.find((item) => item.key === nextRole);
    onChange(nextRole, definition?.permissions ?? []);
  };

  const togglePermission = (permission: string) => {
    const next = permissions.includes(permission)
      ? permissions.filter((item) => item !== permission)
      : [...permissions, permission];
    onChange(role, next);
  };

  return (
    <section className="rounded-2xl border border-line bg-surface p-5 sm:p-7">
      <div className="mb-6 flex items-center gap-3 border-b border-line pb-5">
        <span className="flex h-9 w-9 items-center justify-center rounded-xl bg-green/10 text-green"><i className="ri-shield-keyhole-line" /></span>
        <div>
          <p className="text-[9px] font-bold uppercase tracking-[.16em] text-red-link">03</p>
          <h3 className="font-display text-xl font-bold text-green-deep">{language === 'fr' ? 'Rôle et accès' : 'Role and access'}</h3>
        </div>
      </div>

      <Field label={language === 'fr' ? 'Rôle principal' : 'Primary role'} htmlFor="adminRole" required>
        <select id="adminRole" value={role} onChange={(event) => changeRole(event.target.value)} className={inputClasses}>
          {roles.map((item) => <option key={item.key} value={item.key}>{language === 'en' ? item.nameEn : item.name}</option>)}
        </select>
      </Field>

      {loadError && <p className="mt-3 text-sm text-error">{language === 'fr' ? 'Impossible de charger les rôles.' : 'Unable to load roles.'}</p>}

      <div className="mt-6">
        <div className="flex items-end justify-between gap-4">
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[.14em] text-ink">{language === 'fr' ? 'Autorisations effectives' : 'Effective permissions'}</p>
            <p className="mt-1 text-sm text-ink-variant">{isSuperAdmin
              ? (language === 'fr' ? 'Le super administrateur possède tous les accès.' : 'The super administrator has full access.')
              : (language === 'fr' ? 'Personnalisez ce que cette personne peut gérer.' : 'Customize what this person can manage.')}</p>
          </div>
          {selected && <span className="rounded-full bg-green/8 px-3 py-1 text-[9px] font-bold uppercase tracking-[.12em] text-green">{language === 'en' ? selected.nameEn : selected.name}</span>}
        </div>
        <div className="mt-4 grid gap-2 sm:grid-cols-2">
          {availablePermissions.map((permission) => {
            const checked = isSuperAdmin || permissions.includes(permission);
            return (
              <label key={permission} className={`flex min-h-12 items-center gap-3 rounded-xl border px-4 py-3 text-sm transition-colors ${checked ? 'border-green/30 bg-green/[.045] text-green-deep' : 'border-line bg-surface-container/35 text-ink-variant'}`}>
                <input type="checkbox" checked={checked} disabled={isSuperAdmin} onChange={() => togglePermission(permission)} className="h-4 w-4 accent-green" />
                <span>{getAdminPermissionLabel(permission, language)}</span>
              </label>
            );
          })}
        </div>
      </div>
    </section>
  );
}
