import type { ReactNode } from 'react';
import { useTranslation } from 'react-i18next';

interface Column {
  key: string;
  label: string;
  align?: 'left' | 'right';
}

interface DataTableProps {
  columns: Column[];
  children: ReactNode;
}

export const DataTable = ({ columns, children }: DataTableProps) => {
  const { t } = useTranslation();

  return (
    <div className="admin-data-table admin-panel overflow-hidden">
      <div className="flex min-h-[48px] items-center justify-between gap-4 border-b border-line/50 px-5">
        <div className="flex items-center gap-2.5">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-gold/14 text-gold-ink">
            <i className="ri-table-2" aria-hidden="true" />
          </span>
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-green-deep">{t('admin.list.tableTitle')}</p>
            <p className="hidden text-xs text-ink-variant/65 sm:block">{t('admin.list.tableHint')}</p>
          </div>
        </div>
        <span className="inline-flex items-center gap-1 text-[9px] font-bold uppercase tracking-[0.12em] text-ink-variant/60 sm:hidden">
          {t('admin.list.scroll')}
          <i className="ri-arrow-left-right-line" aria-hidden="true" />
        </span>
      </div>
      <div className="overflow-x-auto">
        <table className="w-full min-w-[720px] border-collapse text-left">
          <thead>
            <tr className="border-b border-line/60 bg-surface-container/75 text-green-deep">
              {columns.map((column) => (
                <th
                  key={column.key}
                  scope="col"
                  className={`px-5 py-3 text-[9px] font-bold uppercase tracking-[0.15em] ${column.align === 'right' ? 'text-right' : 'text-left'}`}
                >
                  {column.label}
                </th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-line/50 [&>tr]:transition-colors [&>tr:hover]:bg-background">{children}</tbody>
        </table>
      </div>
    </div>
  );
};

export const Td = ({
  children,
  align = 'left',
  className = '',
}: {
  children: ReactNode;
  align?: 'left' | 'right';
  className?: string;
}) => (
  <td className={`px-5 py-4 text-[14px] leading-5 text-ink-variant ${align === 'right' ? 'text-right' : ''} ${className}`}>
    {children}
  </td>
);
