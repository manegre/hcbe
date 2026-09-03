import { useEffect, useState } from 'react';
import { Outlet, Link, useLocation } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useAuth } from '../../contexts/AuthContext';
import { LanguageSwitcher } from './LanguageSwitcher';
import { HcbeLogoMark } from '../brand/HcbeLogo';
import { features } from '../../config/features';
import ThemeToggle from '../feature/ThemeToggle';

interface SubItem {
  nameKey: string;
  href: string;
  icon: string;
  disabled?: boolean;
  permission?: string;
}

interface NavItem {
  nameKey: string;
  href?: string;
  icon: string;
  disabled?: boolean;
  subItems?: SubItem[];
  permission?: string;
}

interface NavGroup {
  headingKey: string;
  items: NavItem[];
}

const navLinkClass = (active: boolean, disabled?: boolean) => {
  if (disabled) {
    return 'cursor-not-allowed text-green-dim/40';
  }
  if (active) {
    return 'bg-white/[0.11] text-white shadow-[inset_3px_0_0_#FFCD00,0_8px_22px_rgba(0,0,0,.08)]';
  }
  return 'text-green-dim hover:bg-white/[0.06] hover:text-white';
};

export const AdminLayout = () => {
  const { user, logout } = useAuth();
  const location = useLocation();
  const { t } = useTranslation();
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isSidebarCollapsed, setIsSidebarCollapsed] = useState(false);
  const isDashboard = location.pathname === '/admin/dashboard';

  const isActive = (path: string) =>
    location.pathname === path || location.pathname.startsWith(`${path}/`);

  const closeSidebar = () => setIsSidebarOpen(false);

  useEffect(() => {
    closeSidebar();
  }, [location.pathname]);

  useEffect(() => {
    if (!isSidebarOpen) {
      document.body.style.overflow = '';
      return;
    }

    document.body.style.overflow = 'hidden';
    const onKeyDown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') {
        closeSidebar();
      }
    };
    window.addEventListener('keydown', onKeyDown);

    return () => {
      document.body.style.overflow = '';
      window.removeEventListener('keydown', onKeyDown);
    };
  }, [isSidebarOpen]);

  const handleLogout = () => {
    logout();
    window.location.href = '/';
  };

  const allNavigationGroups: NavGroup[] = [
    {
      headingKey: 'admin.nav.groups.content',
      items: [
        { nameKey: 'admin.nav.dashboard', href: '/admin/dashboard', icon: 'ri-dashboard-line', permission: 'dashboard.view' },
        { nameKey: 'admin.nav.impact', href: '/admin/impact', icon: 'ri-line-chart-line', permission: 'analytics.view' },
        {
          nameKey: 'admin.nav.news',
          icon: 'ri-newspaper-line',
          subItems: [
            { nameKey: 'admin.nav.events', href: '/admin/events', icon: 'ri-calendar-event-line', permission: 'events.manage' },
            { nameKey: 'admin.nav.announcements', href: '/admin/news', icon: 'ri-article-line', permission: 'content.manage' },
          ],
        },
        { nameKey: 'admin.nav.documents', href: '/admin/documents', icon: 'ri-file-text-line', permission: 'content.manage' },
      ],
    },
    {
      headingKey: 'admin.nav.groups.community',
      items: [
        { nameKey: 'admin.nav.associations', href: '/admin/associations', icon: 'ri-building-line', permission: 'community.manage' },
        { nameKey: 'admin.nav.associationRequests', href: '/admin/association-requests', icon: 'ri-building-2-line', permission: 'community.manage' },
        { nameKey: 'admin.nav.projects', href: '/admin/projects', icon: 'ri-hammer-line', permission: 'community.manage' },
        { nameKey: 'admin.nav.opportunities', href: '/admin/opportunities', icon: 'ri-briefcase-4-line', permission: 'community.manage' },
        { nameKey: 'admin.nav.grants', href: '/admin/grants', icon: 'ri-hand-coin-line', permission: 'community.manage' },
        { nameKey: 'admin.nav.consultations', href: '/admin/consultations', icon: 'ri-chat-poll-line', permission: 'community.manage' },
      ],
    },
    {
      headingKey: 'admin.nav.groups.members',
      items: [
        {
          nameKey: 'admin.nav.members',
          icon: 'ri-group-line',
          subItems: [
            { nameKey: 'admin.nav.membersList', href: '/admin/members', icon: 'ri-user-line', permission: 'members.manage' },
            {
              nameKey: 'admin.nav.membershipApplications',
              href: '/admin/membership-applications',
              icon: 'ri-user-add-line',
              permission: 'members.manage',
            },
            {
              nameKey: 'admin.nav.newsletter',
              href: '/admin/newsletter',
              icon: 'ri-mail-send-line',
              permission: 'communications.manage',
            },
            {
              nameKey: 'admin.nav.mentorship',
              href: '/admin/mentorship',
              icon: 'ri-user-heart-line',
              permission: 'community.manage',
            },
            {
              nameKey: 'admin.nav.messageReports',
              href: '/admin/message-reports',
              icon: 'ri-shield-user-line',
              permission: 'moderation.manage',
            },
            {
              nameKey: 'admin.nav.submissions',
              href: '/admin/submissions',
              icon: 'ri-inbox-archive-line',
              permission: 'community.manage',
            },
            {
              nameKey: 'admin.nav.serviceCases',
              href: '/admin/service-cases',
              icon: 'ri-customer-service-2-line',
              permission: 'service-cases.manage',
            },
          ],
        },
      ],
    },
    {
      headingKey: 'admin.nav.groups.administration',
      items: [
        ...(features.adminTeamMembersEnabled
          ? [{ nameKey: 'admin.nav.teamMembers', href: '/admin/team-members', icon: 'ri-team-line', permission: 'content.manage' }]
          : []),
        { nameKey: 'admin.nav.users', href: '/admin/users', icon: 'ri-shield-user-line', permission: 'users.manage' },
        { nameKey: 'admin.nav.partners', href: '/admin/partners', icon: 'ri-shake-hands-line', permission: 'content.manage' },
        { nameKey: 'admin.nav.siteContent', href: '/admin/site-content', icon: 'ri-layout-4-line', permission: 'content.manage' },
      ],
    },
  ];

  const can = (permission?: string) => !permission || user?.permissions?.includes(permission) || user?.adminRole === 'super-admin';
  const navigationGroups: NavGroup[] = allNavigationGroups
    .map((group) => ({
      ...group,
      items: group.items
        .map((item) => item.subItems ? { ...item, subItems: item.subItems.filter((subItem) => can(subItem.permission)) } : item)
        .filter((item) => can(item.permission) && (!item.subItems || item.subItems.length > 0)),
    }))
    .filter((group) => group.items.length > 0);

  const navigation: NavItem[] = navigationGroups.flatMap((group) => group.items);

  const getPageTitle = () => {
    for (const item of navigation) {
      if (item.href && isActive(item.href)) {
        return t(item.nameKey);
      }
      if (item.subItems) {
        const subItem = item.subItems.find((sub) => isActive(sub.href));
        if (subItem) {
          return t(subItem.nameKey);
        }
      }
    }
    return t('admin.layout.defaultPage');
  };

  const initials = `${user?.firstName?.[0] ?? ''}${user?.lastName?.[0] ?? ''}`.toUpperCase();

  const sidebarContent = (
    <>
      <div className={`border-b border-white/10 py-4 ${isSidebarCollapsed ? 'lg:px-3' : 'px-5'}`}>
        <div className={`flex items-center gap-3 ${isSidebarCollapsed ? 'lg:justify-center' : 'justify-between'}`}>
          <Link to="/admin/dashboard" className={`min-w-0 ${isSidebarCollapsed ? 'lg:hidden' : ''}`} onClick={closeSidebar}>
            <HcbeLogoMark size="sm" tone="dark" />
          </Link>
          {isSidebarCollapsed && (
            <Link to="/admin/dashboard" className="hidden h-11 w-11 items-center justify-center rounded-xl bg-gold font-display text-sm font-bold text-green-deep lg:flex" aria-label="HCBE Canada">
              HC
            </Link>
          )}
          <button
            type="button"
            onClick={closeSidebar}
            className="flex h-11 w-11 shrink-0 items-center justify-center rounded-lg border border-white/10 text-white lg:hidden"
            aria-label={t('admin.layout.closeMenu')}
          >
            <i className="ri-close-line text-xl" aria-hidden="true"></i>
          </button>
        </div>
        <Link to="/admin/dashboard" className={`mt-4 flex items-center justify-between gap-3 rounded-xl border border-white/10 bg-white/[0.045] px-4 py-3 ${isSidebarCollapsed ? 'lg:hidden' : ''}`} onClick={closeSidebar}>
          <span>
            <span className="block text-[10px] font-bold uppercase tracking-[0.18em] text-gold">{t('admin.login.workspaceLabel')}</span>
            <span className="mt-1 block text-sm font-semibold text-white">{t('admin.layout.title')}</span>
          </span>
          <span className="flex h-9 w-9 items-center justify-center rounded-full bg-gold text-green-deep">
            <i className="ri-settings-4-line text-base" aria-hidden="true" />
          </span>
        </Link>
        <button
          type="button"
          onClick={() => setIsSidebarCollapsed((collapsed) => !collapsed)}
          className={`mt-3 hidden min-h-[40px] items-center rounded-xl border border-white/10 text-green-dim transition-colors hover:bg-white/[0.06] hover:text-white lg:flex ${isSidebarCollapsed ? 'w-full justify-center px-0' : 'w-full justify-between px-3'}`}
          aria-label={t(isSidebarCollapsed ? 'admin.layout.expandSidebar' : 'admin.layout.collapseSidebar')}
          aria-pressed={isSidebarCollapsed}
          title={t(isSidebarCollapsed ? 'admin.layout.expandSidebar' : 'admin.layout.collapseSidebar')}
        >
          {!isSidebarCollapsed && <span className="text-[10px] font-bold uppercase tracking-[0.13em]">{t('admin.layout.collapseSidebar')}</span>}
          <i className={isSidebarCollapsed ? 'ri-expand-right-line text-lg' : 'ri-contract-left-line text-lg'} aria-hidden="true" />
        </button>
      </div>

      <nav className={`admin-sidebar-scroll flex-1 overflow-y-auto py-3 ${isSidebarCollapsed ? 'lg:px-2' : 'px-3'}`}>
        {navigationGroups.map((group) => (
          <div key={group.headingKey} className={`${isSidebarCollapsed ? 'mb-2 lg:border-b lg:border-white/10 lg:pb-2' : 'mb-4'} last:mb-0 last:border-b-0`}>
            <p className={`px-3 pb-1.5 text-[9px] font-bold uppercase tracking-[0.19em] text-green-dim/50 ${isSidebarCollapsed ? 'lg:hidden' : ''}`}>{t(group.headingKey)}</p>
            <div className="space-y-0.5">
              {group.items.map((item) =>
                item.subItems ? (
                  <div key={item.nameKey}>
                    {item.subItems.map((subItem) => (
                      <Link
                        key={subItem.nameKey}
                        to={subItem.disabled ? '#' : subItem.href}
                        className={`flex min-h-[40px] items-center gap-3 rounded-xl px-3 text-[14px] font-medium transition-all ${isSidebarCollapsed ? 'lg:justify-center lg:px-0' : ''} ${navLinkClass(
                          isActive(subItem.href),
                          subItem.disabled,
                        )}`}
                        title={isSidebarCollapsed ? t(subItem.nameKey) : undefined}
                        onClick={(e) => {
                          if (subItem.disabled) {
                            e.preventDefault();
                            return;
                          }
                          closeSidebar();
                        }}
                      >
                        <i className={`${subItem.icon} text-base`} aria-hidden="true"></i>
                        <span className={`truncate ${isSidebarCollapsed ? 'lg:hidden' : ''}`}>{t(subItem.nameKey)}</span>
                        {subItem.disabled && (
                          <span className={`ml-auto text-label-md uppercase text-green-dim/70 ${isSidebarCollapsed ? 'lg:hidden' : ''}`}>
                            {t('admin.common.soon')}
                          </span>
                        )}
                      </Link>
                    ))}
                  </div>
                ) : (
                  <Link
                    key={item.nameKey}
                    to={item.disabled ? '#' : item.href!}
                    className={`flex min-h-[40px] items-center gap-3 rounded-xl px-3 text-[14px] font-medium transition-all ${isSidebarCollapsed ? 'lg:justify-center lg:px-0' : ''} ${navLinkClass(
                      isActive(item.href!),
                      item.disabled,
                    )}`}
                    title={isSidebarCollapsed ? t(item.nameKey) : undefined}
                    onClick={(e) => {
                      if (item.disabled) {
                        e.preventDefault();
                        return;
                      }
                      closeSidebar();
                    }}
                  >
                    <i className={`${item.icon} text-base`} aria-hidden="true"></i>
                    <span className={`truncate ${isSidebarCollapsed ? 'lg:hidden' : ''}`}>{t(item.nameKey)}</span>
                    {item.disabled && (
                      <span className={`ml-auto text-label-md uppercase text-green-dim/70 ${isSidebarCollapsed ? 'lg:hidden' : ''}`}>
                        {t('admin.common.soon')}
                      </span>
                    )}
                  </Link>
                ),
              )}
            </div>
          </div>
        ))}
      </nav>

      <div className="border-t border-white/10 p-3">
        <div className={`mb-2 flex items-center gap-3 rounded-xl bg-white/[0.045] p-3 ${isSidebarCollapsed ? 'lg:justify-center lg:p-2' : ''}`}>
          <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-full bg-gold text-[11px] font-bold text-green-deep">
            {initials || <i className="ri-user-line text-base" aria-hidden="true"></i>}
          </span>
          <span className={`min-w-0 ${isSidebarCollapsed ? 'lg:hidden' : ''}`}>
            <span className="block truncate text-sm font-semibold text-white">
              {[user?.firstName, user?.lastName].filter(Boolean).join(' ') || t('admin.layout.title')}
            </span>
            <span className="block truncate text-xs text-green-dim/60">{user?.email}</span>
          </span>
        </div>
        <button
          type="button"
          onClick={handleLogout}
          className={`flex min-h-[44px] w-full items-center gap-2 rounded-xl px-3 text-[10px] font-bold uppercase tracking-[0.14em] text-green-dim transition-colors hover:bg-white/[0.06] hover:text-white ${isSidebarCollapsed ? 'lg:justify-center lg:px-0' : ''}`}
          title={isSidebarCollapsed ? t('admin.layout.signOut') : undefined}
        >
          <i className="ri-logout-box-r-line text-base" aria-hidden="true"></i>
          <span className={isSidebarCollapsed ? 'lg:hidden' : ''}>{t('admin.layout.signOut')}</span>
        </button>
      </div>
    </>
  );

  return (
    <div className="admin-shell min-h-screen text-ink">
      {isSidebarOpen && (
        <button
          type="button"
          className="fixed inset-0 z-40 bg-ink/50 lg:hidden"
          aria-label={t('admin.layout.closeMenu')}
          onClick={closeSidebar}
        />
      )}

      <aside
        className={`fixed inset-y-0 left-0 z-50 flex w-[min(288px,88vw)] flex-col overflow-hidden bg-green-deep text-green-dim shadow-[20px_0_60px_rgba(0,59,27,.08)] transition-[width,transform] duration-300 ease-out lg:translate-x-0 ${isSidebarCollapsed ? 'lg:w-[88px]' : 'lg:w-[288px]'} ${
          isSidebarOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
        aria-hidden={!isSidebarOpen}
      >
        {sidebarContent}
      </aside>

      <div className={`transition-[padding] duration-300 ${isSidebarCollapsed ? 'lg:pl-[88px]' : 'lg:pl-[288px]'}`}>
        <header className="admin-topbar sticky top-0 z-30 flex h-[76px] items-center gap-3 border-b border-line/50 px-4 shadow-[0_8px_30px_rgba(0,59,27,.045)] backdrop-blur-xl sm:px-7">
          <button
            type="button"
            onClick={() => setIsSidebarOpen(true)}
            className="flex h-11 w-11 shrink-0 items-center justify-center text-green lg:hidden"
            aria-label={t('admin.layout.openMenu')}
            aria-expanded={isSidebarOpen}
          >
            <i className="ri-menu-line text-xl" aria-hidden="true"></i>
          </button>

          <div className="flex min-w-0 flex-1 items-center gap-3">
            <span className="hidden h-9 w-1 shrink-0 rounded-full bg-gold sm:block" aria-hidden="true" />
            <div className="min-w-0">
              <p className="hidden items-center gap-2 text-[9px] font-bold uppercase tracking-[0.2em] text-ink-variant/65 sm:flex">
                <span>HCBE Canada</span>
                <span className="h-1 w-1 rounded-full bg-gold" aria-hidden="true" />
                <span className="text-ink-variant/85">Administration</span>
              </p>
              <h2 className="truncate font-display text-[21px] font-bold leading-tight tracking-[-0.015em] text-green-deep sm:text-[23px]">{getPageTitle()}</h2>
            </div>
          </div>

          <div className="flex shrink-0 items-center gap-2.5 sm:gap-3">
            <ThemeToggle />
            <LanguageSwitcher />
            <div className="hidden items-center gap-2.5 rounded-full border border-line/50 bg-surface/75 py-1.5 pl-1.5 pr-4 shadow-[0_5px_18px_rgba(0,59,27,.06)] sm:flex">
              <span className="relative flex h-9 w-9 items-center justify-center rounded-full bg-green text-[11px] font-bold text-white shadow-[0_4px_12px_rgba(0,59,27,.16)]">
                {initials || <i className="ri-user-line text-base" aria-hidden="true"></i>}
                <span className="absolute -right-0.5 -top-0.5 h-2.5 w-2.5 rounded-full border-2 border-white bg-green-muted" aria-hidden="true" />
              </span>
              <span className="hidden whitespace-nowrap text-[13px] font-medium text-ink-variant lg:inline">
                {t('admin.layout.welcome', { name: user?.firstName })}
              </span>
            </div>
          </div>
        </header>

        <main className={`overflow-x-hidden p-4 sm:p-6 xl:p-7 ${isDashboard ? 'xl:h-[calc(100vh-76px)] xl:overflow-hidden' : ''}`}>
          <div className={`admin-route-enter mx-auto max-w-[1440px] ${isDashboard ? 'xl:h-full' : ''}`}><Outlet /></div>
        </main>
      </div>
    </div>
  );
};
