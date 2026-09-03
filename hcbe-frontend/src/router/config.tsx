import type { RouteObject } from 'react-router-dom';
import { lazy } from 'react';
import { ProtectedRoute } from '../components/ProtectedRoute';
import { AdminLayout } from '../components/admin/Layout';

const HomePage = lazy(() => import('../pages/home/page'));
const ServicesPage = lazy(() => import('../pages/services/page'));
const ActualitesPage = lazy(() => import('../pages/actualites/page'));
const EngagementPage = lazy(() => import('../pages/engagement/page'));
const EspaceMembrePage = lazy(() => import('../pages/espace-membre/page'));
const ContactPage = lazy(() => import('../pages/contact/page'));
const PrivacyPage = lazy(() => import('../pages/confidentialite/page'));
const NotFound = lazy(() => import('../pages/NotFound'));

// Engagement sub-pages
const AnnuairePage = lazy(() => import('../pages/engagement/annuaire/page'));
const ProjetsPage = lazy(() => import('../pages/engagement/projets/page'));
const ConsultationsPage = lazy(() => import('../pages/engagement/consultations/page'));
const ProjectDetailPage = lazy(() => import('../pages/projet/page'));

// Services sub-pages
const DocumentsOfficielsPage = lazy(() => import('../pages/services/documents-officiels/page'));
const ComitesPage = lazy(() => import('../pages/services/comites/page'));
const BoursesPage = lazy(() => import('../pages/services/bourses/page'));

// Actualités sub-pages
const EvenementsPage = lazy(() => import('../pages/actualites/evenements/page'));
const EventDetailPage = lazy(() => import('../pages/actualites/evenements/[id]/page'));
const AnnoncesPage = lazy(() => import('../pages/actualites/annonces/page'));
const AnnonceDetailPage = lazy(() => import('../pages/actualites/annonces/[id]/page'));
const SouvenirsPage = lazy(() => import('../pages/actualites/souvenirs/page'));

// Admin Pages
const AdminLoginPage = lazy(() => import('../pages/admin/login/page').then(module => ({ default: module.AdminLoginPage })));
const RequiredPasswordChangePage = lazy(() => import('../pages/admin/change-password/page'));
const AdminDashboard = lazy(() => import('../pages/admin/dashboard/page').then(module => ({ default: module.AdminDashboard })));
const AdminEventsList = lazy(() => import('../pages/admin/events/page').then(module => ({ default: module.AdminEventsList })));
const CreateEventPage = lazy(() => import('../pages/admin/events/create/page').then(module => ({ default: module.CreateEventPage })));
const EditEventPage = lazy(() => import('../pages/admin/events/edit/page').then(module => ({ default: module.EditEventPage })));
const ViewEventPage = lazy(() => import('../pages/admin/events/view/page').then(module => ({ default: module.ViewEventPage })));
const EventCategoriesPage = lazy(() => import('../pages/admin/events/categories/page'));

// Admin Association Pages
const AdminAssociationsList = lazy(() => import('../pages/admin/associations/page').then(module => ({ default: module.AdminAssociationsList })));
const CreateAssociationPage = lazy(() => import('../pages/admin/associations/create/page').then(module => ({ default: module.CreateAssociationPage })));
const EditAssociationPage = lazy(() => import('../pages/admin/associations/edit/page').then(module => ({ default: module.EditAssociationPage })));
const ViewAssociationPage = lazy(() => import('../pages/admin/associations/view/page').then(module => ({ default: module.ViewAssociationPage })));

// Admin Project Pages
const AdminProjectsList = lazy(() => import('../pages/admin/projects/page').then(module => ({ default: module.default })));
const CreateProjectPage = lazy(() => import('../pages/admin/projects/create/page').then(module => ({ default: module.default })));
const EditProjectPage = lazy(() => import('../pages/admin/projects/edit/page').then(module => ({ default: module.default })));
const ViewProjectPage = lazy(() => import('../pages/admin/projects/view/page').then(module => ({ default: module.default })));

// Admin Team Members Pages
const AdminTeamMembersList = lazy(() => import('../pages/admin/team-members/page').then(module => ({ default: module.default })));
const CreateTeamMemberPage = lazy(() => import('../pages/admin/team-members/create/page').then(module => ({ default: module.default })));
const ViewTeamMemberPage = lazy(() => import('../pages/admin/team-members/[id]/page').then(module => ({ default: module.default })));
const EditTeamMemberPage = lazy(() => import('../pages/admin/team-members/[id]/edit/page').then(module => ({ default: module.default })));

// Admin Members Pages
const AdminMembersList = lazy(() => import('../pages/admin/members/page').then(module => ({ default: module.default })));
const CreateMemberPage = lazy(() => import('../pages/admin/members/create/page').then(module => ({ default: module.default })));
const ViewMemberPage = lazy(() => import('../pages/admin/members/[id]/page').then(module => ({ default: module.default })));
const EditMemberPage = lazy(() => import('../pages/admin/members/[id]/edit/page').then(module => ({ default: module.default })));

// Admin Membership Applications Pages
const AdminMembershipApplicationsList = lazy(() => import('../pages/admin/membership-applications/page').then(module => ({ default: module.default })));
const ViewMembershipApplicationPage = lazy(() => import('../pages/admin/membership-applications/[id]/page').then(module => ({ default: module.default })));
const AdminNewsletterPage = lazy(() => import('../pages/admin/newsletter/page').then(module => ({ default: module.default })));
const AdminSubmissionsPage = lazy(() => import('../pages/admin/submissions/page').then(module => ({ default: module.default })));
const AdminSiteContentPage = lazy(() => import('../pages/admin/site-content/page').then(module => ({ default: module.default })));
const AdminPartnersPage = lazy(() => import('../pages/admin/partners/page').then(module => ({ default: module.default })));
const AdminMentorshipPage = lazy(() => import('../pages/admin/mentorship/page').then(module => ({ default: module.default })));
const AdminMessageReportsPage = lazy(() => import('../pages/admin/message-reports/page').then(module => ({ default: module.default })));
const AdminServiceCasesPage = lazy(() => import('../pages/admin/service-cases/page'));
const AdminAssociationRequestsPage = lazy(() => import('../pages/admin/association-requests/page'));
const AdminOpportunitiesPage = lazy(() => import('../pages/admin/opportunities/page'));
const AdminImpactPage = lazy(() => import('../pages/admin/impact/page'));
const AdminMonitoringPage = lazy(() => import('../pages/admin/monitoring/page'));

// Admin Grants Pages
const AdminGrantsList = lazy(() => import('../pages/admin/grants/page').then(module => ({ default: module.default })));
const CreateGrantPage = lazy(() => import('../pages/admin/grants/create/page').then(module => ({ default: module.default })));
const ViewGrantPage = lazy(() => import('../pages/admin/grants/[id]/page').then(module => ({ default: module.default })));
const EditGrantPage = lazy(() => import('../pages/admin/grants/[id]/edit/page').then(module => ({ default: module.default })));

// Admin Users Pages
const AdminUsersList = lazy(() => import('../pages/admin/users/page').then(module => ({ default: module.default })));
const CreateAdminUserPage = lazy(() => import('../pages/admin/users/create/page').then(module => ({ default: module.default })));
const EditAdminUserPage = lazy(() => import('../pages/admin/users/[id]/edit/page').then(module => ({ default: module.default })));

// Admin Consultations Pages
const AdminConsultationsList = lazy(() => import('../pages/admin/consultations/page').then(module => ({ default: module.default })));
const CreateConsultationPage = lazy(() => import('../pages/admin/consultations/create/page').then(module => ({ default: module.default })));
const ViewConsultationPage = lazy(() => import('../pages/admin/consultations/[id]/page').then(module => ({ default: module.default })));
const EditConsultationPage = lazy(() => import('../pages/admin/consultations/[id]/edit/page').then(module => ({ default: module.default })));

// Admin News Pages
const AdminNewsList = lazy(() => import('../pages/admin/news/page').then(module => ({ default: module.default })));
const CreateNewsPage = lazy(() => import('../pages/admin/news/create/page').then(module => ({ default: module.default })));
const ViewNewsPage = lazy(() => import('../pages/admin/news/[id]/page').then(module => ({ default: module.default })));
const EditNewsPage = lazy(() => import('../pages/admin/news/[id]/edit/page').then(module => ({ default: module.default })));

// Admin Documents Pages
const AdminDocumentsList = lazy(() => import('../pages/admin/documents/page').then(module => ({ default: module.default })));
const CreateDocumentPage = lazy(() => import('../pages/admin/documents/create/page').then(module => ({ default: module.default })));
const ViewDocumentPage = lazy(() => import('../pages/admin/documents/view/page').then(module => ({ default: module.default })));
const EditDocumentPage = lazy(() => import('../pages/admin/documents/edit/page').then(module => ({ default: module.default })));

const routes: RouteObject[] = [
  {
    path: '/',
    element: <HomePage />,
  },
  {
    path: '/services',
    element: <ServicesPage />,
  },
  {
    path: '/services/documents-officiels',
    element: <DocumentsOfficielsPage />,
  },
  {
    path: '/services/comites',
    element: <ComitesPage />,
  },
  {
    path: '/services/bourses',
    element: <BoursesPage />,
  },
  {
    path: '/actualites',
    element: <ActualitesPage />,
  },
  {
    path: '/actualites/evenements',
    element: <EvenementsPage />,
  },
  {
    path: '/actualites/evenements/:id',
    element: <EventDetailPage />,
  },
  {
    path: '/actualites/annonces',
    element: <AnnoncesPage />,
  },
  {
    path: '/actualites/annonces/:id',
    element: <AnnonceDetailPage />,
  },
  {
    path: '/actualites/souvenirs',
    element: <SouvenirsPage />,
  },
  {
    path: '/engagement',
    element: <EngagementPage />,
  },
  {
    path: '/engagement/annuaire',
    element: <AnnuairePage />,
  },
  {
    path: '/engagement/projets',
    element: <ProjetsPage />,
  },
  {
    path: '/engagement/consultations',
    element: <ConsultationsPage />,
  },
  {
    path: '/projet/:id',
    element: <ProjectDetailPage />,
  },
  {
    path: '/espace-membre',
    element: <EspaceMembrePage />,
  },
  {
    path: '/contact',
    element: <ContactPage />,
  },
  {
    path: '/confidentialite',
    element: <PrivacyPage />,
  },
  {
    path: '/admin/login',
    element: <AdminLoginPage />,
  },
  {
    path: '/admin/change-password',
    element: <ProtectedRoute requireAdmin={false}><RequiredPasswordChangePage /></ProtectedRoute>,
  },
  {
    path: '/admin',
    element: <ProtectedRoute><AdminLayout /></ProtectedRoute>,
    children: [
      {
        index: true,
        element: <AdminDashboard />,
      },
      {
        path: 'dashboard',
        element: <AdminDashboard />,
      },
      {
        path: 'events',
        element: <AdminEventsList />,
      },
      {
        path: 'events/create',
        element: <CreateEventPage />,
      },
      {
        path: 'events/categories',
        element: <EventCategoriesPage />,
      },
      {
        path: 'events/:id',
        element: <ViewEventPage />,
      },
      {
        path: 'events/:id/edit',
        element: <EditEventPage />,
      },
      {
        path: 'news',
        element: <AdminNewsList />,
      },
      {
        path: 'news/create',
        element: <CreateNewsPage />,
      },
      {
        path: 'news/:id',
        element: <ViewNewsPage />,
      },
      {
        path: 'news/:id/edit',
        element: <EditNewsPage />,
      },
      {
        path: 'associations',
        element: <AdminAssociationsList />,
      },
      {
        path: 'associations/create',
        element: <CreateAssociationPage />,
      },
      {
        path: 'associations/:id',
        element: <ViewAssociationPage />,
      },
      {
        path: 'associations/:id/edit',
        element: <EditAssociationPage />,
      },
      {
        path: 'projects',
        element: <AdminProjectsList />,
      },
      {
        path: 'projects/create',
        element: <CreateProjectPage />,
      },
      {
        path: 'projects/:id',
        element: <ViewProjectPage />,
      },
      {
        path: 'projects/:id/edit',
        element: <EditProjectPage />,
      },
      {
        path: 'team-members',
        element: <AdminTeamMembersList />,
      },
      {
        path: 'team-members/create',
        element: <CreateTeamMemberPage />,
      },
      {
        path: 'team-members/:id',
        element: <ViewTeamMemberPage />,
      },
      {
        path: 'team-members/:id/edit',
        element: <EditTeamMemberPage />,
      },
      {
        path: 'members',
        element: <AdminMembersList />,
      },
      {
        path: 'members/create',
        element: <CreateMemberPage />,
      },
      {
        path: 'members/:id',
        element: <ViewMemberPage />,
      },
      {
        path: 'members/:id/edit',
        element: <EditMemberPage />,
      },
      {
        path: 'membership-applications',
        element: <AdminMembershipApplicationsList />,
      },
      {
        path: 'membership-applications/:id',
        element: <ViewMembershipApplicationPage />,
      },
      {
        path: 'newsletter',
        element: <AdminNewsletterPage />,
      },
      {
        path: 'mentorship',
        element: <AdminMentorshipPage />,
      },
      {
        path: 'message-reports',
        element: <AdminMessageReportsPage />,
      },
      {
        path: 'submissions',
        element: <AdminSubmissionsPage />,
      },
      {
        path: 'service-cases',
        element: <AdminServiceCasesPage />,
      },
      {
        path: 'association-requests',
        element: <AdminAssociationRequestsPage />,
      },
      {
        path: 'opportunities',
        element: <AdminOpportunitiesPage />,
      },
      {
        path: 'impact',
        element: <AdminImpactPage />,
      },
      {
        path: 'monitoring',
        element: <AdminMonitoringPage />,
      },
      {
        path: 'site-content',
        element: <AdminSiteContentPage />,
      },
      {
        path: 'partners',
        element: <AdminPartnersPage />,
      },
      {
        path: 'grants',
        element: <AdminGrantsList />,
      },
      {
        path: 'grants/create',
        element: <CreateGrantPage />,
      },
      {
        path: 'grants/:id',
        element: <ViewGrantPage />,
      },
      {
        path: 'grants/:id/edit',
        element: <EditGrantPage />,
      },
      {
        path: 'consultations',
        element: <AdminConsultationsList />,
      },
      {
        path: 'consultations/create',
        element: <CreateConsultationPage />,
      },
      {
        path: 'consultations/:id',
        element: <ViewConsultationPage />,
      },
      {
        path: 'consultations/:id/edit',
        element: <EditConsultationPage />,
      },
      {
        path: 'users',
        element: <AdminUsersList />,
      },
      {
        path: 'users/create',
        element: <CreateAdminUserPage />,
      },
      {
        path: 'users/:id/edit',
        element: <EditAdminUserPage />,
      },
      {
        path: 'documents',
        element: <AdminDocumentsList />,
      },
      {
        path: 'documents/create',
        element: <CreateDocumentPage />,
      },
      {
        path: 'documents/:id',
        element: <ViewDocumentPage />,
      },
      {
        path: 'documents/:id/edit',
        element: <EditDocumentPage />,
      },
    ],
  },
  {
    path: '*',
    element: <NotFound />,
  },
];

export default routes;
