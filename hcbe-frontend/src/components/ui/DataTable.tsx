import { Children, cloneElement, isValidElement, type ReactElement, type ReactNode, type TdHTMLAttributes } from 'react';
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

  const enhanceRows = (nodes: ReactNode): ReactNode => Children.map(nodes, (node) => {
    if (!isValidElement(node)) return node;
    const element = node as ReactElement<{ children?: ReactNode; className?: string }>;
    if (element.type !== 'tr') {
      if (element.props.children === undefined) return element;
      return cloneElement(element, {}, enhanceRows(element.props.children));
    }

    let columnIndex = 0;
    const cells = Children.map(element.props.children, (cell) => {
      if (!isValidElement(cell)) return cell;
      const cellElement = cell as ReactElement<{ className?: string }>;
      const label = columns[columnIndex]?.label ?? '';
      columnIndex += 1;
      return cloneElement(cellElement, {
        'data-label': label,
        className: `${cellElement.props.className ?? ''} admin-data-cell`,
      } as { className: string });
    });

    return cloneElement(element, {
      className: `${element.props.className ?? ''} admin-data-row`,
    }, cells);
  });

  return (
    <div className="admin-data-table admin-panel overflow-hidden">
      <div className="flex min-h-[48px] items-center justify-between gap-4 border-b border-line/50 px-5">
        <div className="flex items-center gap-2.5">
          <span className="flex h-8 w-8 items-center justify-center rounded-lg bg-gold/14 text-gold-ink">
            <i className="ri-table-2" aria-hidden="true" />
          </span>
          <div>
            <p className="text-[10px] font-bold uppercase tracking-[0.14em] text-green-deep">{t('admin.list.tableTitle')}</p>
            <p className="hidden text-xs text-ink-variant sm:block">{t('admin.list.tableHint')}</p>
          </div>
        </div>
        <span className="inline-flex items-center gap-1 text-[9px] font-bold uppercase tracking-[0.12em] text-ink-variant/60 sm:hidden">
          {t('admin.list.mobileCards')}
          <i className="ri-layout-row-line" aria-hidden="true" />
        </span>
      </div>
      <div className="sm:overflow-x-auto">
        <table className="block w-full border-collapse text-left sm:table sm:min-w-[720px]">
          <thead className="hidden sm:table-header-group">
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
          <tbody className="block space-y-3 bg-canvas/35 p-3 sm:table-row-group sm:space-y-0 sm:bg-transparent sm:p-0 sm:divide-y sm:divide-line/50 [&>tr]:transition-colors [&>tr:hover]:bg-background">{enhanceRows(children)}</tbody>
        </table>
      </div>
    </div>
  );
};

export const Td = ({
  children,
  align = 'left',
  className = '',
  ...props
}: {
  children: ReactNode;
  align?: 'left' | 'right';
  className?: string;
} & Omit<TdHTMLAttributes<HTMLTableCellElement>, 'align'>) => (
  <td {...props} className={`grid min-w-0 grid-cols-[minmax(92px,.72fr)_minmax(0,1.28fr)] items-start gap-3 overflow-hidden break-words border-b border-line/40 px-1 py-3 text-right text-[14px] leading-5 text-ink-variant before:text-left before:text-[9px] before:font-bold before:uppercase before:tracking-[.12em] before:text-ink-muted before:content-[attr(data-label)] last:border-b-0 [&>*]:min-w-0 sm:table-cell sm:overflow-visible sm:border-b-0 sm:px-5 sm:py-4 sm:text-left sm:before:hidden ${align === 'right' ? 'sm:text-right' : ''} ${className}`}>
    {children}
  </td>
);
