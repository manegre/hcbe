import { useEffect, useState } from 'react';
import { AdminPageHeader } from '../../../components/admin/AdminPageHeader';
import { Button } from '../../../components/ui';
import { errorIncidentsApi, type ErrorIncident } from '../../../lib/api/error-incidents';

export default function MonitoringPage() {
  const [incidents, setIncidents] = useState<ErrorIncident[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  const load = () => {
    setLoading(true);
    errorIncidentsApi.list()
      .then((response) => response.data ? setIncidents(response.data) : setError(response.message ?? 'Impossible de charger les incidents.'))
      .catch(() => setError('Impossible de charger les incidents.'))
      .finally(() => setLoading(false));
  };

  useEffect(load, []);

  const resolve = async (id: string) => {
    await errorIncidentsApi.resolve(id);
    setIncidents((current) => current.filter((item) => item.id !== id));
  };

  return (
    <div className="space-y-6">
      <AdminPageHeader
        title="Surveillance de production"
        subtitle="Incidents applicatifs regroupés, alertes et identifiants de trace pour accélérer le diagnostic."
        icon="ri-pulse-line"
        count={incidents.length}
      />
      {error && <p className="rounded-xl border border-error/20 bg-error/5 p-4 text-error">{error}</p>}
      {loading ? <div className="h-40 animate-pulse rounded-2xl bg-surface" /> : incidents.length === 0 ? (
        <section className="rounded-[24px] border border-green/15 bg-surface px-6 py-14 text-center shadow-[0_12px_35px_rgba(0,59,27,.05)]">
          <span className="mx-auto flex h-12 w-12 items-center justify-center rounded-full bg-green/10 text-xl text-green"><i className="ri-shield-check-line" /></span>
          <h2 className="mt-4 font-display text-2xl font-bold text-green-deep">Aucun incident ouvert</h2>
          <p className="mt-2 text-sm text-ink-variant">Les erreurs non gérées apparaîtront ici automatiquement.</p>
        </section>
      ) : (
        <section className="grid gap-4">
          {incidents.map((incident) => (
            <article key={incident.id} className="rounded-2xl border border-error/20 bg-surface p-5 shadow-[0_12px_35px_rgba(0,59,27,.05)] sm:p-6">
              <div className="flex flex-col gap-5 lg:flex-row lg:items-start lg:justify-between">
                <div className="min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="rounded-full bg-error/10 px-3 py-1 text-[10px] font-bold uppercase tracking-[.12em] text-error">{incident.occurrenceCount} occurrence{incident.occurrenceCount > 1 ? 's' : ''}</span>
                    <span className="font-mono text-xs text-ink-variant">{incident.httpMethod} {incident.path}</span>
                  </div>
                  <h2 className="mt-4 break-words font-display text-xl font-bold text-green-deep">{incident.exceptionType}</h2>
                  <p className="mt-2 break-words text-sm leading-6 text-ink-variant">{incident.message}</p>
                  <p className="mt-4 font-mono text-[11px] text-ink-variant">Trace {incident.traceId} · {new Date(incident.lastOccurredAtUtc).toLocaleString('fr-CA')}</p>
                </div>
                <Button variant="secondary" type="button" onClick={() => resolve(incident.id)} className="shrink-0">Marquer comme résolu</Button>
              </div>
            </article>
          ))}
        </section>
      )}
    </div>
  );
}
