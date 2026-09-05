using System.Net;

namespace HcbeApi.Services;

public sealed class EmailTemplateRenderer(IConfiguration configuration) : IEmailTemplateRenderer
{
    private const string Green = "#123f25";
    private const string GreenDark = "#0b2d1a";
    private const string Gold = "#f5c518";
    private const string Red = "#a72b1c";
    private const string Canvas = "#f3f5f0";
    private const string Ink = "#18221b";
    private const string Muted = "#5f6b62";
    private const string Line = "#dbe1d8";

    private string ContactEmail => configuration["Email:ContactAddress"] ?? "contact@hcbe.ca";

    public RenderedEmail MemberOnboarding(string? firstName, string actionUrl)
    {
        var name = GreetingName(firstName);
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, votre compte Google est maintenant associé à HCBE Canada.</p>
            {Callout("Une dernière étape", "Complétez votre profil afin que nous puissions mieux vous orienter vers les services, événements et occasions de réseautage qui vous correspondent.")}
            {Steps(("01", "Coordonnées", "Ajoutez votre téléphone, votre ville et votre province."), ("02", "Parcours", "Précisez votre profession et votre domaine d’expertise."), ("03", "Communauté", "Dites-nous ce que vous souhaitez trouver ou apporter à la communauté."))}
            <p style="margin:22px 0 0;font-size:13px;line-height:1.65;color:{Muted};">Your Google account is ready. Complete your member profile to unlock your HCBE Canada member space.</p>
            """;
        return new RenderedEmail(
            "[HCBE Canada] Complétez votre profil / Complete your profile",
            Layout("Votre compte HCBE Canada est créé. Il ne reste qu’à compléter votre profil.", "Bienvenue à bord", "Votre espace membre prend forme.", body, "Compléter mon profil", actionUrl));
    }

    public RenderedEmail MemberWelcome(string? firstName, string memberSpaceUrl)
    {
        var name = GreetingName(firstName);
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bienvenue {name}. Votre profil est complet et votre espace membre est maintenant prêt.</p>
            {Callout("Vous faites maintenant partie du réseau", "Découvrez les événements, échangez avec d’autres membres et participez aux initiatives qui rapprochent la diaspora burkinabè au Canada.")}
            {FeatureGrid(("ÉVÉNEMENTS", "Suivez les rendez-vous de la communauté."), ("RÉSEAUTAGE", "Trouvez des profils et créez des liens utiles."), ("MENTORAT", "Partagez votre expérience ou trouvez un accompagnement."))}
            <p style="margin:22px 0 0;font-size:13px;line-height:1.65;color:{Muted};">Welcome to HCBE Canada. Your completed profile gives you access to the member community and its services.</p>
            """;
        return new RenderedEmail(
            "[HCBE Canada] Bienvenue dans la communauté / Welcome",
            Layout("Bienvenue dans la communauté HCBE Canada.", "Adhésion confirmée", "Ensemble, faisons vivre la communauté.", body, "Accéder à mon espace", memberSpaceUrl));
    }

    public RenderedEmail AdminWelcome(string? firstName, string email, string temporaryPassword, string adminLoginUrl)
    {
        var name = GreetingName(firstName);
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, un compte administrateur HCBE Canada vient d’être créé pour vous.</p>
            {Callout("Accès temporaire", "Connectez-vous avec les identifiants ci-dessous. Pour protéger le site, vous devrez choisir un nouveau mot de passe avant d’accéder au centre de gestion.")}
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin:22px 0;background:#f5f7f3;border:1px solid {Line};">
              <tr><td style="padding:18px 20px;border-bottom:1px solid {Line};font-family:Verdana,Arial,sans-serif;font-size:11px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:{Muted};">Courriel</td><td style="padding:18px 20px;border-bottom:1px solid {Line};font-family:Verdana,Arial,sans-serif;font-size:14px;font-weight:700;color:{GreenDark};">{Encode(email)}</td></tr>
              <tr><td style="padding:18px 20px;font-family:Verdana,Arial,sans-serif;font-size:11px;font-weight:700;letter-spacing:.08em;text-transform:uppercase;color:{Muted};">Mot de passe temporaire</td><td style="padding:18px 20px;font-family:Consolas,'Courier New',monospace;font-size:16px;font-weight:700;letter-spacing:.04em;color:{GreenDark};">{Encode(temporaryPassword)}</td></tr>
            </table>
            {Steps(("01", "Connectez-vous", "Utilisez le courriel et le mot de passe temporaire ci-dessus."), ("02", "Sécurisez votre compte", "Choisissez immédiatement un nouveau mot de passe personnel."), ("03", "Explorez vos espaces", "Accédez au centre de gestion et à l’espace membre avec le même compte."))}
            <p style="margin:22px 0 0;font-size:13px;line-height:1.65;color:{Muted};">An HCBE Canada administrator account has been created for you. Sign in with the temporary credentials above, then choose a permanent password.</p>
            """;
        return new RenderedEmail(
            "[HCBE Canada] Votre accès administrateur / Administrator access",
            Layout("Votre accès administrateur HCBE Canada est prêt.", "Bienvenue dans l’équipe", "Votre accès au centre de gestion", body, "Activer mon compte", adminLoginUrl, securityNotice: true));
    }

    public RenderedEmail AdminPromotion(string? firstName, string adminLoginUrl)
    {
        var name = GreetingName(firstName);
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, votre compte membre HCBE Canada dispose maintenant d’un accès administrateur.</p>
            {Callout("Nouvelles responsabilités", "Vous pouvez désormais accéder au centre de gestion pour administrer les contenus et les activités de la plateforme. Votre accès membre reste disponible avec le même compte.")}
            {Steps(("01", "Reconnectez-vous", "Déconnectez-vous de votre session actuelle, puis reconnectez-vous afin d’activer vos nouvelles permissions."), ("02", "Ouvrez l’administration", "Utilisez votre compte Google ou vos identifiants habituels sur la page de connexion administrateur."), ("03", "Protégez les données", "N’accordez un accès administrateur qu’aux personnes autorisées par le HCBE Canada."))}
            <p style="margin:22px 0 0;font-size:13px;line-height:1.65;color:{Muted};">Your member account now has administrator access. Sign in again with your usual account to activate the new permissions.</p>
            """;
        return new RenderedEmail(
            "[HCBE Canada] Accès administrateur accordé / Administrator access granted",
            Layout("Votre compte dispose maintenant d’un accès administrateur.", "Accès accordé", "Bienvenue dans l’équipe d’administration", body, "Accéder à l’administration", adminLoginUrl, securityNotice: true));
    }

    public RenderedEmail PasswordReset(string? firstName, string resetUrl, int expiresInMinutes)
    {
        var name = GreetingName(firstName);
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, nous avons reçu une demande de réinitialisation du mot de passe de votre compte.</p>
            {Callout("Lien temporaire", $"Ce lien est valide pendant {expiresInMinutes} minutes et ne peut être utilisé qu’une seule fois.")}
            <p style="margin:22px 0 0;font-size:14px;line-height:1.7;color:{Muted};">Si vous n’êtes pas à l’origine de cette demande, ignorez ce message. Votre mot de passe actuel restera inchangé.</p>
            <p style="margin:10px 0 0;font-size:13px;line-height:1.65;color:{Muted};">If you did not request a password reset, you can safely ignore this email.</p>
            """;
        return new RenderedEmail(
            "[HCBE Canada] Réinitialisation du mot de passe / Password reset",
            Layout("Utilisez ce lien sécurisé pour choisir un nouveau mot de passe.", "Sécurité du compte", "Choisissez un nouveau mot de passe.", body, "Réinitialiser mon mot de passe", resetUrl, securityNotice: true));
    }

    public RenderedEmail PasswordChanged(string? firstName, string memberSpaceUrl)
    {
        var name = GreetingName(firstName);
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, le mot de passe de votre compte HCBE Canada vient d’être modifié avec succès.</p>
            {Callout("Modification confirmée", "Vous pouvez maintenant vous connecter avec votre nouveau mot de passe. Tous les anciens liens de réinitialisation sont invalides.")}
            <p style="margin:22px 0 0;font-size:14px;line-height:1.7;color:{Muted};">Vous n’avez pas effectué cette modification? Communiquez immédiatement avec nous à <a href="mailto:{Encode(ContactEmail)}" style="color:{Green};font-weight:700;">{Encode(ContactEmail)}</a>.</p>
            <p style="margin:10px 0 0;font-size:13px;line-height:1.65;color:{Muted};">Your password was changed successfully. Contact us immediately if this was not you.</p>
            """;
        return new RenderedEmail(
            "[HCBE Canada] Mot de passe modifié / Password changed",
            Layout("Votre mot de passe a été modifié.", "Confirmation de sécurité", "Votre compte est à jour.", body, "Ouvrir mon espace membre", memberSpaceUrl, securityNotice: true));
    }

    public RenderedEmail MembershipDecision(string? firstName, bool approved, string actionUrl)
    {
        var name = GreetingName(firstName);
        if (approved)
        {
            var approvedBody = $"""
                <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, votre demande d’adhésion a été approuvée.</p>
                {Callout("Adhésion confirmée", "Votre espace membre est actif. Vous pouvez dès maintenant découvrir les services et participer à la vie de la communauté.")}
                <p style="margin:22px 0 0;font-size:13px;line-height:1.65;color:{Muted};">Your membership has been approved and your member space is now active.</p>
                """;
            return new RenderedEmail("[HCBE Canada] Adhésion confirmée / Membership approved", Layout("Votre adhésion HCBE Canada est confirmée.", "Bienvenue", "Votre adhésion est confirmée.", approvedBody, "Accéder à mon espace", actionUrl));
        }

        var rejectedBody = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, nous avons terminé l’examen de votre demande d’adhésion.</p>
            {Callout("Décision concernant votre demande", "Nous ne sommes pas en mesure de donner suite à votre demande pour le moment. Vous pouvez communiquer avec notre équipe si vous souhaitez obtenir plus de renseignements.")}
            <p style="margin:22px 0 0;font-size:13px;line-height:1.65;color:{Muted};">We are unable to approve your membership request at this time. Contact our team if you need more information.</p>
            """;
        return new RenderedEmail("[HCBE Canada] Suivi de votre demande / Application update", Layout("Une mise à jour concernant votre demande d’adhésion.", "Suivi de demande", "Mise à jour de votre demande.", rejectedBody, "Communiquer avec le HCBE", actionUrl));
    }

    public RenderedEmail Newsletter(string subject, string body, string unsubscribeUrl, bool useEnglish)
    {
        var safeBody = Encode(body).Replace("\r\n", "\n").Replace("\n", "<br>");
        var content = $"<div style=\"font-size:16px;line-height:1.8;color:{Ink};\">{safeBody}</div>";
        var unsubscribe = useEnglish ? "Unsubscribe" : "Se désabonner";
        return new RenderedEmail(subject, Layout(
            useEnglish ? "News and opportunities from HCBE Canada." : "Actualités et occasions de la communauté HCBE Canada.",
            useEnglish ? "Community update" : "Nouvelles de la communauté",
            subject,
            content,
            null,
            null,
            $"<a href=\"{SafeUrl(unsubscribeUrl)}\" style=\"color:{Muted};text-decoration:underline;\">{unsubscribe}</a>"));
    }

    public RenderedEmail EventRegistrationUpdate(
        string? firstName,
        string eventTitle,
        DateTime eventDate,
        string status,
        string confirmationCode,
        string eventUrl)
    {
        var name = GreetingName(firstName);
        var isWaitlisted = status == "Waitlisted";
        var isCancelled = status == "Cancelled";
        var eyebrow = isCancelled ? "Inscription annulée" : isWaitlisted ? "Liste d’attente" : "Inscription confirmée";
        var title = isCancelled
            ? "Votre inscription a été annulée."
            : isWaitlisted
                ? "Vous êtes sur la liste d’attente."
                : "Votre place est confirmée.";
        var explanation = isCancelled
            ? "Votre place a été libérée. Vous pouvez vous inscrire de nouveau tant que les inscriptions sont ouvertes."
            : isWaitlisted
                ? "L’événement est complet. Nous vous préviendrons automatiquement dès qu’une place se libère."
                : "Votre inscription est enregistrée. Retrouvez les informations pratiques et ajoutez le rendez-vous à votre calendrier depuis la page de l’événement.";
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, voici la mise à jour de votre participation à <strong>{Encode(eventTitle)}</strong>.</p>
            {Callout(eyebrow, explanation)}
            <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin:22px 0;background:#f5f7f3;border:1px solid {Line};">
              <tr><td style="padding:16px 20px;border-bottom:1px solid {Line};font-size:12px;color:{Muted};">Date</td><td style="padding:16px 20px;border-bottom:1px solid {Line};font-weight:700;color:{GreenDark};">{eventDate.ToUniversalTime():yyyy-MM-dd HH:mm} UTC</td></tr>
              <tr><td style="padding:16px 20px;font-size:12px;color:{Muted};">Confirmation</td><td style="padding:16px 20px;font-family:Consolas,'Courier New',monospace;font-weight:700;color:{GreenDark};">{Encode(confirmationCode)}</td></tr>
            </table>
            <p style="margin:18px 0 0;font-size:13px;line-height:1.65;color:{Muted};">Registration update for your HCBE Canada community event.</p>
            """;
        return new RenderedEmail(
            $"[HCBE Canada] {eyebrow} — {eventTitle}",
            Layout(explanation, eyebrow, title, body, "Voir l’événement", eventUrl));
    }

    public RenderedEmail EventMessage(string? firstName, string eventTitle, string subject, string body, string eventUrl)
    {
        var content = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {GreetingName(firstName)},</p>
            {Callout(eventTitle, body)}
            <p style="margin:20px 0 0;font-size:13px;line-height:1.65;color:{Muted};">This message concerns your participation in an HCBE Canada community event.</p>
            """;
        return new RenderedEmail($"[HCBE Canada] {subject}", Layout(subject, "Événement · Event", subject, content, "Voir l’événement", eventUrl));
    }

    public RenderedEmail ServiceCaseUpdate(string? firstName, string ticketNumber, string subject, string status, string? message, string caseUrl)
    {
        var name = GreetingName(firstName);
        var safeMessage = string.IsNullOrWhiteSpace(message) ? string.Empty : $"<p style=\"margin:18px 0 0;padding:16px;background:#f5f7f3;border-left:3px solid {Gold};font-size:14px;line-height:1.65;color:{Muted};\">{Encode(message)}</p>";
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {name}, votre demande <strong>{Encode(ticketNumber)}</strong> a été mise à jour.</p>
            {Callout(subject, $"Statut actuel : {status}")}
            {safeMessage}
            <p style="margin:20px 0 0;font-size:13px;line-height:1.65;color:{Muted};">You can review the request and reply securely from your HCBE Canada member space.</p>
            """;
        return new RenderedEmail($"[HCBE Canada] {ticketNumber} — mise à jour", Layout("Mise à jour de votre demande de service.", "Services aux membres", "Votre demande évolue.", body, "Consulter ma demande", caseUrl));
    }

    public RenderedEmail PaymentReceipt(string? name, string kind, long amountCents, string currency, string receiptNumber, string receiptUrl)
    {
        var contribution = kind == "Membership" ? "adhésion" : "contribution";
        var amount = $"{amountCents / 100m:0.00} {currency.ToUpperInvariant()}";
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {GreetingName(name)}, nous avons bien reçu votre {contribution}.</p>
            {Callout("Paiement confirmé", $"Montant : {amount} · Reçu : {receiptNumber}")}
            <p style="margin:22px 0 0;font-size:13px;line-height:1.65;color:{Muted};">Ce document confirme un paiement reçu par HCBE Canada. Il ne constitue pas un reçu fiscal.</p>
            <p style="margin:8px 0 0;font-size:13px;line-height:1.65;color:{Muted};">This document confirms a payment received by HCBE Canada. It is not a charitable tax receipt.</p>
            """;
        return new RenderedEmail($"[HCBE Canada] Reçu {receiptNumber}", Layout("Votre paiement a été confirmé.", "Merci pour votre engagement", "Votre reçu HCBE Canada", body, "Télécharger mon reçu", receiptUrl));
    }

    public RenderedEmail MembershipReminder(string? firstName, DateTime expiresAtUtc, string renewalUrl, bool expired)
    {
        var date = expiresAtUtc.ToString("yyyy-MM-dd");
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {GreetingName(firstName)}, { (expired ? "votre période d’adhésion est arrivée à échéance" : "votre adhésion arrive bientôt à échéance") }.</p>
            {Callout(expired ? "Renouvellement requis" : "Échéance à venir", $"Date d’échéance : {date}. Votre compte communautaire reste accessible; renouvelez votre adhésion pour conserver votre statut de membre en règle.")}
            <p style="margin:22px 0 0;font-size:13px;line-height:1.65;color:{Muted};">Manage your membership and billing securely from your HCBE Canada member space.</p>
            """;
        return new RenderedEmail("[HCBE Canada] Renouvellement de votre adhésion", Layout("Votre adhésion HCBE Canada doit être renouvelée.", "Adhésion", expired ? "Renouvelez votre adhésion" : "Votre échéance approche", body, "Gérer mon adhésion", renewalUrl));
    }

    public RenderedEmail MfaVerificationCode(string? firstName, string code, int expiresInMinutes)
    {
        var body = $"""
            <p style="margin:0 0 18px;font-size:16px;line-height:1.75;color:{Ink};">Bonjour {GreetingName(firstName)}, utilisez ce code pour confirmer votre identité sur HCBE Canada.</p>
            <div style="margin:24px 0;padding:24px;border:1px solid {Line};border-left:5px solid {Gold};background:#f5f7f3;text-align:center;">
              <p style="margin:0 0 9px;font-family:Verdana,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:.14em;text-transform:uppercase;color:{Muted};">Code de sécurité · Security code</p>
              <strong style="display:block;font-family:Consolas,'Courier New',monospace;font-size:34px;line-height:1.2;letter-spacing:.2em;color:{GreenDark};">{Encode(code)}</strong>
            </div>
            {Callout("Code temporaire", $"Ce code expire dans {expiresInMinutes} minutes et ne peut être utilisé qu’une seule fois.")}
            <p style="margin:20px 0 0;font-size:14px;line-height:1.7;color:{Muted};">Vous n’avez pas demandé ce code? Ne le partagez pas et modifiez votre mot de passe si vous soupçonnez une tentative d’accès.</p>
            <p style="margin:9px 0 0;font-size:13px;line-height:1.65;color:{Muted};">This code expires in {expiresInMinutes} minutes. Never share it with anyone.</p>
            """;
        return new RenderedEmail(
            "[HCBE Canada] Votre code de sécurité / Security code",
            Layout("Votre code de vérification HCBE Canada.", "Sécurité du compte", "Confirmez votre identité", body, null, null, securityNotice: true));
    }

    private string Layout(
        string preheader,
        string eyebrow,
        string title,
        string bodyHtml,
        string? ctaLabel,
        string? ctaUrl,
        string? footerExtra = null,
        bool securityNotice = false)
    {
        const string responsiveCss = "@media only screen and (max-width:620px){.shell{width:100%!important}.pad{padding-left:22px!important;padding-right:22px!important}.headline{font-size:31px!important;line-height:1.12!important}.feature{display:block!important;width:100%!important;padding:0 0 12px!important}}";
        var action = string.IsNullOrWhiteSpace(ctaLabel) || string.IsNullOrWhiteSpace(ctaUrl)
            ? string.Empty
            : $"""
                <table role="presentation" cellspacing="0" cellpadding="0" border="0" style="margin:28px 0 4px;"><tr><td bgcolor="{Gold}" style="border-radius:4px;"><a href="{SafeUrl(ctaUrl)}" style="display:inline-block;padding:15px 24px;font-family:Verdana,Arial,sans-serif;font-size:13px;font-weight:700;letter-spacing:.06em;color:{GreenDark};text-decoration:none;text-transform:uppercase;">{Encode(ctaLabel)} &nbsp;→</a></td></tr></table>
                """;
        var security = securityNotice
            ? $"<tr><td style=\"padding:0 34px 24px;\"><div style=\"border-left:3px solid {Red};padding:10px 14px;background:#fff7f5;font-family:Verdana,Arial,sans-serif;font-size:12px;line-height:1.6;color:{Muted};\">HCBE Canada ne vous demandera jamais votre mot de passe par courriel.<br>HCBE Canada will never ask for your password by email.</div></td></tr>"
            : string.Empty;
        var extra = string.IsNullOrWhiteSpace(footerExtra) ? string.Empty : $"<p style=\"margin:12px 0 0;\">{footerExtra}</p>";

        return $"""
            <!doctype html>
            <html lang="fr">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width,initial-scale=1">
              <meta name="color-scheme" content="light">
              <title>{Encode(title)}</title>
              <style>{responsiveCss}</style>
            </head>
            <body style="margin:0;padding:0;background:{Canvas};">
              <div style="display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;">{Encode(preheader)}&nbsp;‌&nbsp;‌&nbsp;‌&nbsp;‌</div>
              <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="width:100%;background:{Canvas};">
                <tr><td align="center" style="padding:28px 12px;">
                  <table role="presentation" class="shell" width="600" cellspacing="0" cellpadding="0" border="0" style="width:600px;max-width:600px;background:#ffffff;border:1px solid {Line};border-radius:12px;overflow:hidden;box-shadow:0 14px 40px rgba(18,63,37,.08);">
                    <tr><td class="pad" style="padding:22px 34px;background:#ffffff;border-bottom:1px solid {Line};">
                      <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0"><tr>
                        <td style="font-family:Georgia,'Times New Roman',serif;font-size:20px;font-weight:700;color:{GreenDark};">🇧🇫&nbsp; HCBE <span style="color:{Red};">Canada</span>&nbsp; 🇨🇦</td>
                        <td align="right" style="font-family:Verdana,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:.18em;color:{Muted};">BURKINA FASO · CANADA</td>
                      </tr></table>
                    </td></tr>
                    <tr><td class="pad" style="padding:44px 34px 38px;background:{GreenDark};border-bottom:5px solid {Gold};">
                      <p style="margin:0 0 14px;font-family:Verdana,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:.2em;text-transform:uppercase;color:{Gold};">{Encode(eyebrow)}</p>
                      <h1 class="headline" style="margin:0;max-width:510px;font-family:Georgia,'Times New Roman',serif;font-size:39px;line-height:1.1;font-weight:700;color:#ffffff;">{Encode(title)}</h1>
                    </td></tr>
                    <tr><td class="pad" style="padding:34px;">{bodyHtml}{action}</td></tr>
                    {security}
                    <tr><td class="pad" style="padding:24px 34px;background:#eef2ec;border-top:1px solid {Line};font-family:Verdana,Arial,sans-serif;font-size:11px;line-height:1.65;color:{Muted};">
                      <p style="margin:0;font-weight:700;color:{Green};">HCBE Canada — Ensemble pour le Burkina</p>
                      <p style="margin:7px 0 0;">Ce message a été envoyé par HCBE Canada. Besoin d’aide? <a href="mailto:{Encode(ContactEmail)}" style="color:{Green};">{Encode(ContactEmail)}</a></p>
                      {extra}
                    </td></tr>
                  </table>
                  <p style="margin:16px 0 0;font-family:Verdana,Arial,sans-serif;font-size:10px;color:#7c867e;">© {DateTime.UtcNow.Year} HCBE Canada</p>
                </td></tr>
              </table>
            </body>
            </html>
            """;
    }

    private static string Callout(string title, string body) => $"""
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin:20px 0;background:#f5f7f3;border:1px solid {Line};border-left:4px solid {Gold};"><tr><td style="padding:18px 20px;">
          <p style="margin:0 0 5px;font-family:Verdana,Arial,sans-serif;font-size:11px;font-weight:700;letter-spacing:.09em;text-transform:uppercase;color:{Green};">{Encode(title)}</p>
          <p style="margin:0;font-family:Verdana,Arial,sans-serif;font-size:14px;line-height:1.65;color:{Muted};">{Encode(body)}</p>
        </td></tr></table>
        """;

    private static string Steps(params (string Number, string Title, string Body)[] items) => string.Join(string.Empty, items.Select(item => $"""
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="border-bottom:1px solid {Line};"><tr>
          <td width="50" valign="top" style="padding:13px 0;font-family:Georgia,'Times New Roman',serif;font-size:18px;font-weight:700;color:{Red};">{Encode(item.Number)}</td>
          <td style="padding:13px 0;"><p style="margin:0 0 3px;font-family:Verdana,Arial,sans-serif;font-size:13px;font-weight:700;color:{GreenDark};">{Encode(item.Title)}</p><p style="margin:0;font-family:Verdana,Arial,sans-serif;font-size:12px;line-height:1.55;color:{Muted};">{Encode(item.Body)}</p></td>
        </tr></table>
        """));

    private static string FeatureGrid(params (string Title, string Body)[] items) => $"""
        <table role="presentation" width="100%" cellspacing="0" cellpadding="0" border="0" style="margin:22px 0 0;"><tr>
          {string.Join(string.Empty, items.Select(item => $"<td class=\"feature\" width=\"33.33%\" valign=\"top\" style=\"padding-right:12px;\"><p style=\"margin:0 0 6px;font-family:Verdana,Arial,sans-serif;font-size:10px;font-weight:700;letter-spacing:.1em;color:{Red};\">{Encode(item.Title)}</p><p style=\"margin:0;font-family:Verdana,Arial,sans-serif;font-size:12px;line-height:1.55;color:{Muted};\">{Encode(item.Body)}</p></td>"))}
        </tr></table>
        """;

    private static string GreetingName(string? firstName) => string.IsNullOrWhiteSpace(firstName) ? "membre" : Encode(firstName.Trim());
    private static string Encode(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string SafeUrl(string value) => Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.Scheme is "https" or "http" ? Encode(uri.AbsoluteUri) : "#";
}
