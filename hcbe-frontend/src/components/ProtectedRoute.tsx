import React from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { Button } from './ui';
import { useTranslation } from 'react-i18next';

interface ProtectedRouteProps {
  children: React.ReactNode;
  requireAdmin?: boolean;
}

export const ProtectedRoute: React.FC<ProtectedRouteProps> = ({ children, requireAdmin = true }) => {
  const { isAuthenticated, isAdmin, isLoading } = useAuth();
  const { t } = useTranslation();
  const location = useLocation();

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="h-12 w-12 animate-spin rounded-full border-b-2 border-green"></div>
      </div>
    );
  }

  if (!isAuthenticated) {
    return <Navigate to="/admin/login" state={{ from: location }} replace />;
  }

  if (requireAdmin && !isAdmin) {
    return (
      <div className="flex min-h-screen items-center justify-center bg-background">
        <div className="w-full max-w-md rounded-2xl border border-line border-l-4 border-l-gold bg-surface p-8 text-center shadow-[0_24px_70px_rgba(0,59,27,.12)]">
          <div className="mx-auto mb-5 flex h-14 w-14 items-center justify-center rounded-full border border-gold/35 bg-gold/10 text-2xl text-green">
            <i className="ri-shield-keyhole-line" aria-hidden="true" />
          </div>
          <h2 className="font-display text-headline-md text-green">{t('admin.accessDenied.title')}</h2>
          <p className="mt-2 mb-6 text-body-md text-ink-variant">
            {t('admin.accessDenied.description')}
          </p>
          <Button variant="primary" onClick={() => window.history.back()}>
            {t('admin.accessDenied.back')}
          </Button>
        </div>
      </div>
    );
  }

  return <>{children}</>;
};
