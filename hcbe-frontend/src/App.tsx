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


function App() {
  return (
    <ThemeProvider>
      <I18nextProvider i18n={i18n}>
        <DocumentLanguageSync />
        <AuthProvider>
          <BrowserRouter basename={__BASE_PATH__}>
            <ScrollToTop />
            <AppRoutes />
            <BackToTopButton />
            <CookieConsent />
          </BrowserRouter>
        </AuthProvider>
      </I18nextProvider>
    </ThemeProvider>
  );
}

export default App;
