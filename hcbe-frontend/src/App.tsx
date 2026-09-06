import { BrowserRouter } from "react-router-dom";
import { AppRoutes } from "./router";
import { I18nextProvider } from "react-i18next";
import i18n from "./i18n";
import { AuthProvider } from "./contexts/AuthContext";
import DocumentLanguageSync from "./components/DocumentLanguageSync";
import ScrollToTop from "./components/ScrollToTop";
import BackToTopButton from "./components/feature/BackToTopButton";
import { ThemeProvider } from "./contexts/ThemeContext";
import CookieConsent from "./components/feature/CookieConsent";
import { CmsContentProvider } from "./contexts/CmsContentContext";
import AccessibilityNavigation from "./components/AccessibilityNavigation";
import PwaExperience from "./components/feature/PwaExperience";
import PublicPageHelp from "./components/feature/PublicPageHelp";


function App() {
  return (
    <ThemeProvider>
      <I18nextProvider i18n={i18n}>
        <DocumentLanguageSync />
        <AuthProvider>
          <BrowserRouter basename={__BASE_PATH__}>
            <CmsContentProvider>
              <AccessibilityNavigation />
              <ScrollToTop />
              <AppRoutes />
              <PublicPageHelp />
              <BackToTopButton />
              <PwaExperience />
              <CookieConsent />
            </CmsContentProvider>
          </BrowserRouter>
        </AuthProvider>
      </I18nextProvider>
    </ThemeProvider>
  );
}

export default App;
