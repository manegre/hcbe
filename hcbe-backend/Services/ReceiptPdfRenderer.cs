using System.Globalization;
using System.Text;
using HcbeApi.Models;
using QRCoder;

namespace HcbeApi.Services;

/// <summary>
/// Produces a compact, dependency-free PDF payment receipt. The document uses
/// the PDF base fonts and WinAnsi encoding so it renders consistently in the
/// Linux production container without relying on system-installed fonts.
/// </summary>
public static class ReceiptPdfRenderer
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double Margin = 44;

    public static byte[] Render(FinancialTransaction item)
    {
        ArgumentNullException.ThrowIfNull(item);

        var content = new StringBuilder();
        DrawBackground(content);
        DrawHeader(content, item);
        DrawSummary(content, item);
        DrawDetails(content, item);
        DrawFooter(content);
        return BuildPdf(content.ToString(), item.ReceiptNumber);
    }

    public static byte[] RenderEventTickets(EventTicketOrder order)
    {
        ArgumentNullException.ThrowIfNull(order.Event);
        var tickets = order.Tickets.OrderBy(item => item.Tier?.DisplayOrder).ThenBy(item => item.TicketCode).ToList();
        if (tickets.Count == 0) throw new InvalidOperationException("The order has no issued tickets.");
        var pages = new List<string>();
        foreach (var ticket in tickets)
        {
            var content = new StringBuilder();
            Fill(content, "0.969 0.976 0.957", 0, 0, PageWidth, PageHeight);
            RoundedFill(content, "0.043 0.231 0.129", 30, 610, 535, 202, 22);
            Fill(content, "0.961 0.773 0.094", 48, 632, 5, 154);
            CircleStroke(content, "0.149 0.365 0.235", 540, 796, 70, 20);
            DrawLogo(content, 72, 772);
            Text(content, "BILLET OFFICIEL  /  OFFICIAL TICKET", 72, 722, 8.5, true, "0.788 0.851 0.808", 1.05);
            var titleLines = Wrap(order.Event.Title, 48);
            for (var index = 0; index < Math.Min(2, titleLines.Count); index++)
                Text(content, titleLines[index], 72, 682 - index * 29, 21, true, "1 1 1");
            Text(content, $"COMMANDE / ORDER  ·  {order.OrderNumber}", 72, 630, 7.2, true, "0.788 0.851 0.808", 0.45);

            RoundedFill(content, "1 1 1", 44, 222, 507, 356, 18);
            RoundedStroke(content, "0.827 0.859 0.820", 44, 222, 507, 356, 18, 0.8);
            Text(content, "PARTICIPANT·E  /  ATTENDEE", 70, 535, 7, true, "0.435 0.478 0.443", 0.55);
            Text(content, ticket.AttendeeName, 70, 505, 17, true, "0.043 0.231 0.129");
            Text(content, ticket.AttendeeEmail, 70, 484, 8, false, "0.310 0.353 0.318");
            Text(content, "CATÉGORIE  /  TICKET TYPE", 70, 446, 7, true, "0.435 0.478 0.443", 0.55);
            Text(content, ticket.Tier?.Name ?? "Billet", 70, 418, 13, true, "0.086 0.145 0.106");
            Text(content, "DATE ET HEURE  /  DATE AND TIME", 70, 378, 7, true, "0.435 0.478 0.443", 0.55);
            Text(content, order.Event.Date.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + $"  {order.Event.TimeZone}", 70, 352, 9.5, true, "0.086 0.145 0.106");
            Text(content, "LIEU  /  LOCATION", 70, 314, 7, true, "0.435 0.478 0.443", 0.55);
            var locationLines = Wrap(order.Event.Location ?? (order.Event.Format == "Online" ? "En ligne / Online" : "À confirmer / To be confirmed"), 35);
            for (var index = 0; index < Math.Min(2, locationLines.Count); index++) Text(content, locationLines[index], 70, 288 - index * 15, 9, index == 0, "0.086 0.145 0.106");

            RoundedFill(content, "0.925 0.945 0.918", 337, 300, 180, 230, 14);
            DrawQr(content, ticket.TicketCode, 357, 334, 140);
            Text(content, "PRÉSENTEZ CE CODE À L’ENTRÉE", 354, 318, 5.8, true, "0.310 0.353 0.318", 0.25);
            Text(content, "SHOW THIS CODE AT THE ENTRANCE", 350, 305, 5.8, true, "0.310 0.353 0.318", 0.25);
            Text(content, ticket.TicketCode, 359, 548, 8, true, "0.043 0.231 0.129", 0.35);

            RoundedFill(content, "0.925 0.945 0.918", 44, 82, 507, 110, 14);
            Fill(content, "0.961 0.773 0.094", 44, 82, 5, 110);
            Text(content, "INFORMATION IMPORTANTE  /  IMPORTANT INFORMATION", 70, 164, 7, true, "0.043 0.231 0.129", 0.55);
            Text(content, "Billet personnel et utilisable une seule fois. Une pièce d’identité peut être demandée.", 70, 139, 7.4, false, "0.310 0.353 0.318");
            Text(content, "Personal, single-use ticket. Identification may be requested at entry.", 70, 120, 7.4, false, "0.310 0.353 0.318");
            Text(content, "contact@hcbe.ca  |  hcbe.ca", 70, 98, 7.2, true, "0.043 0.231 0.129");
            Text(content, $"ÉMIS / ISSUED  ·  {ticket.IssuedAtUtc:yyyy-MM-dd HH:mm} UTC", 367, 48, 6.5, true, "0.435 0.478 0.443", 0.35);
            pages.Add(content.ToString());
        }
        return BuildMultiPagePdf(pages, $"HCBE tickets {order.OrderNumber}");
    }

    public static byte[] RenderCertificate(OpportunityApplication application)
    {
        ArgumentNullException.ThrowIfNull(application);
        ArgumentNullException.ThrowIfNull(application.Certificate);
        var content = new StringBuilder();
        Fill(content, "0.969 0.976 0.957", 0, 0, PageWidth, PageHeight);
        Stroke(content, "0.043 0.231 0.129", 26, 26, 543, 790, 2);
        Stroke(content, "0.961 0.773 0.094", 34, 34, 527, 774, 0.8);
        RoundedFill(content, "0.043 0.231 0.129", 48, 666, 499, 112, 18);
        Fill(content, "0.961 0.773 0.094", 48, 666, 5, 112);
        CircleStroke(content, "0.149 0.365 0.235", 520, 768, 52, 18);
        DrawLogo(content, 72, 746);

        Text(content, "ATTESTATION DE PARTICIPATION", 74, 630, 10, true, "0.043 0.231 0.129", 1.2);
        Text(content, "CERTIFICATE OF PARTICIPATION", 74, 610, 8, true, "0.435 0.478 0.443", 0.8);
        Text(content, "Le HCBE Canada atteste que / HCBE Canada certifies that", 74, 562, 9, false, "0.310 0.353 0.318");
        var memberName = $"{application.Member?.FirstName} {application.Member?.LastName}".Trim();
        Text(content, memberName, 74, 515, 28, true, "0.043 0.231 0.129");
        Line(content, "0.827 0.859 0.820", 74, 493, 521, 493, 0.9);

        Text(content, "a contribué à / contributed to", 74, 458, 8.5, false, "0.435 0.478 0.443");
        var title = application.Opportunity?.Title ?? "Initiative communautaire HCBE";
        var titleLines = Wrap(title, 48);
        for (var index = 0; index < Math.Min(2, titleLines.Count); index++)
            Text(content, titleLines[index], 74, 425 - index * 24, 17, true, "0.086 0.145 0.106");

        var certificate = application.Certificate;
        var summary = certificate!.ContributionSummary;
        if (!string.IsNullOrWhiteSpace(summary))
        {
            var summaryLines = Wrap(summary, 80);
            for (var index = 0; index < Math.Min(3, summaryLines.Count); index++)
                Text(content, summaryLines[index], 74, 354 - index * 17, 9, false, "0.310 0.353 0.318");
        }

        RoundedFill(content, "1 1 1", 74, 196, 447, 92, 12);
        RoundedStroke(content, "0.827 0.859 0.820", 74, 196, 447, 92, 12, 0.8);
        Text(content, "DATE D'ÉMISSION / ISSUE DATE", 96, 257, 7, true, "0.435 0.478 0.443", 0.65);
        Text(content, certificate.IssuedAtUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 96, 230, 11, true, "0.086 0.145 0.106");
        Line(content, "0.827 0.859 0.820", 286, 213, 286, 271, 0.7);
        Text(content, certificate.ConfirmedHours.HasValue ? "HEURES CONFIRMÉES / CONFIRMED HOURS" : "TYPE DE PARTICIPATION / PARTICIPATION TYPE", 310, 257, 7, true, "0.435 0.478 0.443", 0.5);
        Text(content, certificate.ConfirmedHours.HasValue ? $"{certificate.ConfirmedHours:0.##} h" : TypeLabel(application.Opportunity?.Type), 310, 230, 11, true, "0.086 0.145 0.106");

        Text(content, "HCBE Canada", 74, 142, 11, true, "0.043 0.231 0.129");
        Text(content, "Haut Conseil des Burkinabè du Canada", 74, 123, 8, false, "0.310 0.353 0.318");
        Text(content, "contact@hcbe.ca  |  hcbe.ca", 74, 105, 8, false, "0.310 0.353 0.318");
        Text(content, "N° / NO.", 377, 142, 6.8, true, "0.435 0.478 0.443", 0.5);
        Text(content, certificate.CertificateNumber, 377, 122, 8.2, true, "0.043 0.231 0.129");
        Text(content, "Document vérifiable émis électroniquement / Verifiable electronically issued document", 74, 65, 7, false, "0.435 0.478 0.443");
        return BuildPdf(content.ToString(), certificate.CertificateNumber);
    }

    public static byte[] RenderEventCertificate(EventRegistration registration)
    {
        ArgumentNullException.ThrowIfNull(registration.Event);
        ArgumentNullException.ThrowIfNull(registration.Member);
        var item = registration.Event;
        var number = $"HCBE-EVT-{item.Date:yyyy}-{registration.ConfirmationCode}";
        var content = new StringBuilder();
        Fill(content, "0.969 0.976 0.957", 0, 0, PageWidth, PageHeight);
        Stroke(content, "0.043 0.231 0.129", 26, 26, 543, 790, 2);
        Stroke(content, "0.961 0.773 0.094", 34, 34, 527, 774, 0.8);
        RoundedFill(content, "0.043 0.231 0.129", 48, 666, 499, 112, 18);
        Fill(content, "0.961 0.773 0.094", 48, 666, 5, 112);
        CircleStroke(content, "0.149 0.365 0.235", 520, 768, 52, 18);
        DrawLogo(content, 72, 746);
        Text(content, "ATTESTATION DE PRÉSENCE", 74, 630, 10, true, "0.043 0.231 0.129", 1.2);
        Text(content, "CERTIFICATE OF ATTENDANCE", 74, 610, 8, true, "0.435 0.478 0.443", 0.8);
        Text(content, "Le HCBE Canada atteste la participation de / HCBE Canada certifies the attendance of", 74, 562, 8.5, false, "0.310 0.353 0.318");
        Text(content, $"{registration.Member.FirstName} {registration.Member.LastName}".Trim(), 74, 515, 28, true, "0.043 0.231 0.129");
        Line(content, "0.827 0.859 0.820", 74, 493, 521, 493, 0.9);
        Text(content, "à l’événement / at the event", 74, 458, 8.5, false, "0.435 0.478 0.443");
        var titleLines = Wrap(item.Title, 48);
        for (var index = 0; index < Math.Min(2, titleLines.Count); index++)
            Text(content, titleLines[index], 74, 425 - index * 24, 17, true, "0.086 0.145 0.106");
        RoundedFill(content, "1 1 1", 74, 220, 447, 92, 12);
        RoundedStroke(content, "0.827 0.859 0.820", 74, 220, 447, 92, 12, 0.8);
        Text(content, "DATE DE L’ÉVÉNEMENT / EVENT DATE", 96, 281, 7, true, "0.435 0.478 0.443", 0.65);
        Text(content, item.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), 96, 254, 11, true, "0.086 0.145 0.106");
        Line(content, "0.827 0.859 0.820", 286, 237, 286, 295, 0.7);
        Text(content, "FORMAT / FORMAT", 310, 281, 7, true, "0.435 0.478 0.443", 0.65);
        Text(content, item.Format, 310, 254, 11, true, "0.086 0.145 0.106");
        Text(content, "HCBE Canada", 74, 142, 11, true, "0.043 0.231 0.129");
        Text(content, "Haut Conseil des Burkinabè du Canada", 74, 123, 8, false, "0.310 0.353 0.318");
        Text(content, "contact@hcbe.ca  |  hcbe.ca", 74, 105, 8, false, "0.310 0.353 0.318");
        Text(content, "N° / NO.", 377, 142, 6.8, true, "0.435 0.478 0.443", 0.5);
        Text(content, number, 377, 122, 8.2, true, "0.043 0.231 0.129");
        Text(content, "Document vérifiable émis électroniquement / Verifiable electronically issued document", 74, 65, 7, false, "0.435 0.478 0.443");
        return BuildPdf(content.ToString(), number);
    }

    public static byte[] RenderMembershipCard(MembershipCardDto card)
    {
        ArgumentNullException.ThrowIfNull(card);
        var content = new StringBuilder();
        Fill(content, "0.969 0.976 0.957", 0, 0, PageWidth, PageHeight);
        RoundedFill(content, "0.043 0.231 0.129", 44, 398, 507, 320, 24);
        CircleStroke(content, "0.149 0.365 0.235", 524, 700, 92, 26);
        CircleStroke(content, "0.149 0.365 0.235", 524, 700, 55, 18);
        Fill(content, "0.961 0.773 0.094", 44, 398, 6, 320);
        DrawLogo(content, 74, 681);
        RoundedFill(content, "0.961 0.773 0.094", 405, 657, 112, 30, 15);
        Text(content, card.Status == MembershipStatuses.GracePeriod ? "GRÂCE / GRACE" : "ACTIVE / ACTIVE", 421, 668, 7.3, true, "0.043 0.231 0.129", 0.45);

        Text(content, "CARTE DE MEMBRE  /  MEMBERSHIP CARD", 74, 619, 8, true, "0.788 0.851 0.808", 1.1);
        Text(content, card.MemberName, 74, 568, 27, true, "1 1 1");
        Text(content, card.Email, 74, 541, 10, false, "0.788 0.851 0.808");
        Line(content, "0.149 0.365 0.235", 74, 512, 521, 512, 0.8);
        Text(content, "FORMULE / PLAN", 74, 480, 7, true, "0.788 0.851 0.808", 0.65);
        Text(content, $"{card.PlanName} / {card.PlanNameEn}", 74, 454, 10.5, true, "1 1 1");
        Text(content, "VALIDE JUSQU’AU / VALID UNTIL", 350, 480, 7, true, "0.788 0.851 0.808", 0.5);
        Text(content, card.ValidUntilUtc?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "—", 350, 454, 10.5, true, "1 1 1");

        RoundedFill(content, "1 1 1", 44, 182, 507, 184, 18);
        RoundedStroke(content, "0.827 0.859 0.820", 44, 182, 507, 184, 18, 0.8);
        Text(content, "VÉRIFICATION OFFICIELLE  /  OFFICIAL VERIFICATION", 70, 329, 8, true, "0.043 0.231 0.129", 0.75);
        Text(content, "Présentez le code QR de votre carte numérique ou utilisez l’adresse sécurisée ci-dessous.", 70, 301, 8.4, false, "0.310 0.353 0.318");
        Text(content, "Show the QR code on your digital card or use the secure address below.", 70, 282, 8.4, false, "0.310 0.353 0.318");
        RoundedFill(content, "0.925 0.945 0.918", 70, 218, 455, 44, 10);
        Text(content, card.VerificationUrl, 84, 235, 7.2, true, "0.043 0.231 0.129");
        Text(content, $"CODE : {card.VerificationCode}", 70, 198, 6.8, true, "0.435 0.478 0.443", 0.35);

        Text(content, "HCBE Canada", 44, 128, 11, true, "0.043 0.231 0.129");
        Text(content, "Haut Conseil des Burkinabè du Canada", 44, 107, 8.5, false, "0.310 0.353 0.318");
        Text(content, "contact@hcbe.ca  |  hcbe.ca", 44, 88, 8.5, false, "0.310 0.353 0.318");
        Text(content, "DOCUMENT PERSONNEL — NE PAS PARTAGER PUBLIQUEMENT", 309, 112, 6.8, true, "0.435 0.478 0.443", 0.45);
        Text(content, "PERSONAL DOCUMENT — DO NOT SHARE PUBLICLY", 329, 92, 6.8, true, "0.435 0.478 0.443", 0.45);
        return BuildPdf(content.ToString(), $"HCBE membership card - {card.MemberName}");
    }

    public static byte[] RenderImpactReport(ImpactDashboardDto report)
    {
        ArgumentNullException.ThrowIfNull(report);
        var content = new StringBuilder();
        Fill(content, "0.969 0.976 0.957", 0, 0, PageWidth, PageHeight);
        RoundedFill(content, "0.043 0.231 0.129", 34, 650, 527, 158, 22);
        Fill(content, "0.961 0.773 0.094", 50, 671, 4, 116);
        CircleStroke(content, "0.149 0.365 0.235", 535, 796, 68, 22);
        DrawLogo(content, 72, 772);
        Text(content, "RAPPORT D'IMPACT ORGANISATIONNEL", 72, 721, 15, true, "1 1 1", 0.35);
        Text(content, "ORGANIZATIONAL IMPACT REPORT", 72, 699, 8, true, "0.788 0.851 0.808", 0.9);
        Text(content, $"PÉRIODE / PERIOD  ·  {report.PeriodStartUtc:yyyy-MM} — {report.GeneratedAtUtc:yyyy-MM}", 72, 674, 7.5, true, "0.788 0.851 0.808", 0.45);
        Text(content, $"GÉNÉRÉ / GENERATED  ·  {report.GeneratedAtUtc:yyyy-MM-dd HH:mm} UTC", 342, 674, 7, false, "0.788 0.851 0.808");

        Text(content, "INDICATEURS CLÉS  /  KEY INDICATORS", 44, 616, 8, true, "0.043 0.231 0.129", 0.8);
        var metricKeys = new[] { "members", "event-attendance", "service-cases", "resolution-time", "volunteer-hours", "mentorship-completed" };
        for (var index = 0; index < metricKeys.Length; index++)
        {
            var metric = report.Metrics.FirstOrDefault(item => item.Key == metricKeys[index]);
            if (metric is null) continue;
            var column = index % 3;
            var row = index / 3;
            var x = 44 + column * 173;
            var y = 515 - row * 96;
            RoundedFill(content, "1 1 1", x, y, 161, 78, 12);
            RoundedStroke(content, "0.827 0.859 0.820", x, y, 161, 78, 12, 0.7);
            Text(content, FormatImpactValue(metric), x + 14, y + 39, 20, true, "0.043 0.231 0.129");
            var label = BilingualImpactLabel(metric.Key, metric.Label);
            var lines = Wrap(label, 29);
            for (var line = 0; line < Math.Min(2, lines.Count); line++)
                Text(content, lines[line], x + 14, y + 17 - line * 10, 6.5, true, "0.310 0.353 0.318", 0.25);
        }

        Text(content, "PARCOURS D'ACTIVATION  /  MEMBER ACTIVATION", 44, 384, 8, true, "0.043 0.231 0.129", 0.8);
        for (var index = 0; index < report.ActivationFunnel.Count && index < 5; index++)
        {
            var stage = report.ActivationFunnel[index];
            var x = 44 + index * 103;
            RoundedFill(content, index == report.ActivationFunnel.Count - 1 ? "0.925 0.945 0.918" : "1 1 1", x, 286, 94, 76, 10);
            RoundedStroke(content, "0.827 0.859 0.820", x, 286, 94, 76, 10, 0.65);
            Text(content, $"0{index + 1}", x + 11, 341, 6.5, true, "0.788 0.098 0.122", 0.5);
            Text(content, $"{stage.Percentage:0.#}%", x + 11, 315, 15, true, "0.043 0.231 0.129");
            Text(content, $"{stage.Count} membres / members", x + 11, 298, 5.8, false, "0.310 0.353 0.318");
        }

        RoundedFill(content, "1 1 1", 44, 128, 248, 130, 12);
        RoundedStroke(content, "0.827 0.859 0.820", 44, 128, 248, 130, 12, 0.7);
        Text(content, "ACTIVITÉ DES MEMBRES / MEMBER ACTIVITY", 58, 235, 6.8, true, "0.043 0.231 0.129", 0.45);
        for (var index = 0; index < Math.Min(4, report.ActivitySegments.Count); index++)
        {
            var item = report.ActivitySegments[index];
            Text(content, ActivityLabel(item.Key, item.Label), 58, 208 - index * 23, 7.2, false, "0.310 0.353 0.318");
            Text(content, $"{item.Count}  ·  {item.Percentage:0.#}%", 225, 208 - index * 23, 7.2, true, "0.043 0.231 0.129");
        }

        RoundedFill(content, "1 1 1", 303, 128, 248, 130, 12);
        RoundedStroke(content, "0.827 0.859 0.820", 303, 128, 248, 130, 12, 0.7);
        Text(content, "PRÉSENCE NATIONALE / NATIONAL PRESENCE", 317, 235, 6.8, true, "0.043 0.231 0.129", 0.45);
        for (var index = 0; index < Math.Min(4, report.ProvinceBreakdown.Count); index++)
        {
            var item = report.ProvinceBreakdown[index];
            Text(content, item.Key == "other" ? "Autres régions / Other regions" : item.Label, 317, 208 - index * 23, 7.2, false, "0.310 0.353 0.318");
            Text(content, $"{item.Count}  ·  {item.Percentage:0.#}%", 484, 208 - index * 23, 7.2, true, "0.043 0.231 0.129");
        }

        Line(content, "0.961 0.773 0.094", 44, 95, 551, 95, 2);
        Text(content, "HCBE Canada  ·  Haut Conseil des Burkinabè du Canada", 44, 72, 8, true, "0.043 0.231 0.129");
        Text(content, "Données agrégées conformément aux principes de minimisation de la Loi 25.", 44, 54, 6.7, false, "0.310 0.353 0.318");
        Text(content, "Aggregated data produced in accordance with Law 25 data-minimization principles.", 303, 54, 6.7, false, "0.310 0.353 0.318");
        return BuildPdf(content.ToString(), $"HCBE impact report {report.GeneratedAtUtc:yyyyMMdd}");
    }

    private static string FormatImpactValue(ImpactMetricDto metric) => metric.Unit == "%"
        ? $"{metric.Value:0.#}%"
        : $"{metric.Value:0.#} {ImpactUnit(metric.Unit)}";

    private static string ImpactUnit(string unit) => unit switch { "heures" => "h", _ => string.Empty };
    private static string BilingualImpactLabel(string key, string fallback) => key switch
    {
        "members" => "Membres / Members",
        "event-attendance" => "Présence événements / Event attendance",
        "service-cases" => "Demandes ouvertes / Open requests",
        "resolution-time" => "Délai de résolution / Resolution time",
        "volunteer-hours" => "Bénévolat confirmé / Confirmed volunteering",
        "mentorship-completed" => "Jumelages complétés / Completed matches",
        _ => fallback
    };

    private static string ActivityLabel(string key, string fallback) => key switch
    {
        "active" => "Actifs — 30 j / Active — 30 d",
        "warm" => "À réengager / Re-engage",
        "dormant" => "Dormants / Dormant",
        "never" => "Jamais connectés / Never signed in",
        _ => fallback
    };

    private static string TypeLabel(string? type) => type switch
    {
        "Job" => "Emploi / Employment",
        "Training" => "Formation / Training",
        "Business" => "Affaires / Business",
        _ => "Participation communautaire / Community participation"
    };

    public static string DownloadFileName(FinancialTransaction item)
    {
        var safeNumber = new string((item.ReceiptNumber ?? string.Empty)
            .Where(character => char.IsLetterOrDigit(character) || character is '-' or '_')
            .ToArray());
        return $"{(string.IsNullOrWhiteSpace(safeNumber) ? "HCBE-receipt" : safeNumber)}.pdf";
    }

    private static void DrawBackground(StringBuilder content)
    {
        Fill(content, "0.969 0.976 0.957", 0, 0, PageWidth, PageHeight);
        RoundedFill(content, "0.043 0.231 0.129", 34, 592, 527, 216, 22);
        Fill(content, "0.961 0.773 0.094", 52, 616, 4, 166);

        // Quiet concentric rings echo the HCBE interface without competing with receipt data.
        CircleStroke(content, "0.149 0.365 0.235", 536, 798, 70, 22);
        CircleStroke(content, "0.149 0.365 0.235", 536, 798, 42, 16);

        RoundedFill(content, "1 1 1", Margin, 445, PageWidth - Margin * 2, 125, 16);
        RoundedStroke(content, "0.827 0.859 0.820", Margin, 445, PageWidth - Margin * 2, 125, 16, 0.75);
        RoundedFill(content, "1 1 1", Margin, 185, PageWidth - Margin * 2, 238, 16);
        RoundedStroke(content, "0.827 0.859 0.820", Margin, 185, PageWidth - Margin * 2, 238, 16, 0.75);
        RoundedFill(content, "0.925 0.945 0.918", Margin, 78, PageWidth - Margin * 2, 82, 12);
    }

    private static void DrawHeader(StringBuilder content, FinancialTransaction item)
    {
        DrawLogo(content, Margin + 24, 771);
        Text(content, "REÇU DE PAIEMENT  /  PAYMENT RECEIPT", Margin + 24, 718, 8.5, true, "0.788 0.851 0.808", 1.05);
        Text(content, item.ReceiptNumber, Margin + 24, 678, 23, true, "1 1 1");
        Text(content, "Confirmation officielle du paiement  /  Official payment confirmation", Margin + 24, 647, 9.2, false, "0.788 0.851 0.808");

        var status = Status(item.Status);
        var badgeWidth = Math.Min(190, Math.Max(94, Measure(status, 7.2, true) + 24));
        var badgeX = PageWidth - Margin - 22 - badgeWidth;
        RoundedFill(content, "0.961 0.773 0.094", badgeX, 757, badgeWidth, 30, 15);
        Text(content, status, badgeX + 12, 768, 7.2, true, "0.043 0.231 0.129", 0.35);
    }

    private static void DrawSummary(StringBuilder content, FinancialTransaction item)
    {
        var amount = Money(item.AmountCents - item.RefundedAmountCents, item.Currency);
        Text(content, "MONTANT CONFIRMÉ  /  CONFIRMED AMOUNT", Margin + 24, 539, 7.5, true, "0.435 0.478 0.443", 0.7);
        Text(content, amount, Margin + 24, 492, 30, true, "0.043 0.231 0.129");

        var purpose = Purpose(item);
        Line(content, "0.827 0.859 0.820", 298, 466, 298, 549, 0.8);
        Text(content, "OBJET DU PAIEMENT  /  PAYMENT PURPOSE", 322, 539, 7.5, true, "0.435 0.478 0.443", 0.7);
        var purposeLines = Wrap(purpose, 33);
        for (var index = 0; index < Math.Min(3, purposeLines.Count); index++)
            Text(content, purposeLines[index], 322, 510 - index * 17, 10.5, index == 0, "0.086 0.145 0.106");
    }

    private static void DrawDetails(StringBuilder content, FinancialTransaction item)
    {
        var payer = item.IsAnonymous ? "Donateur anonyme / Anonymous donor" : item.PayerName ?? item.PayerEmail;
        var paidAt = item.PaidAtUtc ?? item.CreatedAtUtc;
        Text(content, "DÉTAILS DE LA TRANSACTION  /  TRANSACTION DETAILS", Margin + 24, 394, 8, true, "0.043 0.231 0.129", 0.85);
        Line(content, "0.827 0.859 0.820", Margin + 24, 378, PageWidth - Margin - 24, 378, 0.7);

        Text(content, "REÇU DE  /  RECEIVED FROM", Margin + 24, 349, 6.8, true, "0.435 0.478 0.443", 0.55);
        var payerLines = Wrap(payer, 34);
        for (var index = 0; index < Math.Min(2, payerLines.Count); index++)
            Text(content, payerLines[index], Margin + 24, 327 - index * 14, 9.5, index == 0, "0.086 0.145 0.106");
        if (!item.IsAnonymous && !string.IsNullOrWhiteSpace(item.PayerEmail) && !string.Equals(payer, item.PayerEmail, StringComparison.OrdinalIgnoreCase))
            Text(content, item.PayerEmail, Margin + 24, 325 - Math.Min(2, payerLines.Count) * 14, 8.5, false, "0.435 0.478 0.443");
        Detail(content, "DATE DU PAIEMENT  /  PAYMENT DATE", $"{paidAt:yyyy-MM-dd HH:mm} UTC", 318, 349, 30);

        Detail(content, "TYPE", item.Kind == FinanceKinds.Membership ? "Adhésion / Membership" : "Contribution / Contribution", Margin + 24, 284, 28);
        Detail(content, "MODE DE PAIEMENT  /  PAYMENT MODE", item.IsRecurring ? "Renouvellement automatique / Recurring" : "Paiement unique / One-time", 318, 284, 30);

        Detail(content, "MONTANT INITIAL  /  ORIGINAL AMOUNT", Money(item.AmountCents, item.Currency), Margin + 24, 219, 28);
        Detail(content,
            item.RefundedAmountCents > 0 ? "REMBOURSÉ  /  REFUNDED" : "ÉTAT DU PAIEMENT  /  PAYMENT STATUS",
            item.RefundedAmountCents > 0 ? Money(item.RefundedAmountCents, item.Currency) : Status(item.Status),
            318, 219, 30);
    }

    private static void DrawFooter(StringBuilder content)
    {
        Fill(content, "0.961 0.773 0.094", Margin, 78, 4, 82);
        Text(content, "DOCUMENT DE CONFIRMATION  /  CONFIRMATION DOCUMENT", Margin + 24, 137, 7.2, true, "0.043 0.231 0.129", 0.65);
        Text(content, "Ce document confirme un paiement et son état actuel. Il ne constitue pas un reçu fiscal de don de bienfaisance.", Margin + 24, 114, 7.2, false, "0.310 0.353 0.318");
        Text(content, "This document confirms a payment and its current status. It is not a charitable tax receipt.", Margin + 24, 96, 7.2, false, "0.310 0.353 0.318");

        Text(content, "HCBE Canada", Margin, 48, 9, true, "0.043 0.231 0.129");
        Text(content, "contact@hcbe.ca  |  hcbe.ca", Margin, 32, 8, false, "0.310 0.353 0.318");
        Text(content, "ÉMIS ÉLECTRONIQUEMENT  /  ISSUED ELECTRONICALLY", 362, 40, 6.8, true, "0.435 0.478 0.443", 0.55);
    }

    private static void Detail(StringBuilder content, string label, string value, double x, double y, int wrapLimit)
    {
        Text(content, label, x, y, 6.8, true, "0.435 0.478 0.443", 0.55);
        var lines = Wrap(value, wrapLimit);
        for (var index = 0; index < Math.Min(2, lines.Count); index++)
            Text(content, lines[index], x, y - 22 - index * 14, 9.5, index == 0, "0.086 0.145 0.106");
    }

    private static void DrawLogo(StringBuilder content, double x, double y)
    {
        const double flagWidth = 25;
        const double flagHeight = 16;
        var flagBottom = y - 7;
        var flagCenterY = flagBottom + flagHeight / 2;

        // Both flags share the same official 3:2 proportions and vertical centre.
        Fill(content, "0.937 0.169 0.176", x, flagCenterY, flagWidth, flagHeight / 2);
        Fill(content, "0 0.620 0.286", x, flagBottom, flagWidth, flagHeight / 2);
        Star(content, "0.988 0.820 0.086", x + flagWidth / 2, flagCenterY, 4.4, 2);

        Text(content, "HCBE", x + 36, y - 3, 13.5, true, "1 1 1");
        Polygon(content, "0.961 0.773 0.094",
        [
            (x + 79, flagCenterY + 4), (x + 83, flagCenterY),
            (x + 79, flagCenterY - 4), (x + 75, flagCenterY)
        ]);
        Text(content, "Canada", x + 87, y - 3, 13.5, true, "1 1 1");

        // Canada flag uses 1:2:1 vertical fields and a vector maple leaf.
        const double canadaX = 141;
        Fill(content, "1 1 1", x + canadaX, flagBottom, flagWidth, flagHeight);
        Fill(content, "0.812 0.125 0.157", x + canadaX, flagBottom, flagWidth / 4, flagHeight);
        Fill(content, "0.812 0.125 0.157", x + canadaX + flagWidth * 0.75, flagBottom, flagWidth / 4, flagHeight);
        MapleLeaf(content, "0.812 0.125 0.157", x + canadaX + flagWidth / 2, flagCenterY);
    }

    private static string Purpose(FinancialTransaction item)
    {
        if (item.Kind == FinanceKinds.Membership)
            return item.MembershipPlan?.Name is { Length: > 0 } name ? $"Adhésion - {name}" : "Adhésion annuelle / Annual membership";
        return item.DonationCampaign?.Title is { Length: > 0 } title ? title : "Fonds communautaire HCBE / HCBE Community Fund";
    }

    private static string Status(string status) => status switch
    {
        FinanceStatuses.PartiallyRefunded => "PARTIELLEMENT REMBOURSÉ / PARTIALLY REFUNDED",
        FinanceStatuses.Refunded => "REMBOURSÉ / REFUNDED",
        FinanceStatuses.Disputed => "EN LITIGE / DISPUTED",
        _ => "PAYÉ / PAID"
    };

    private static string Money(long cents, string currency) =>
        $"{(cents / 100m).ToString("N2", CultureInfo.InvariantCulture)} {(string.IsNullOrWhiteSpace(currency) ? "CAD" : currency.ToUpperInvariant())}";

    private static IReadOnlyList<string> Wrap(string value, int limit)
    {
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var lines = new List<string>();
        var current = new StringBuilder();
        foreach (var word in words)
        {
            if (current.Length > 0 && current.Length + word.Length + 1 > limit)
            {
                lines.Add(current.ToString());
                current.Clear();
            }
            if (current.Length > 0) current.Append(' ');
            current.Append(word.Length > limit ? word[..limit] : word);
        }
        if (current.Length > 0) lines.Add(current.ToString());
        return lines.Count == 0 ? [string.Empty] : lines;
    }

    private static void Text(StringBuilder content, string? value, double x, double y, double size, bool bold, string color, double spacing = 0)
    {
        content.Append(color).Append(" rg BT /").Append(bold ? "F2" : "F1").Append(' ')
            .Append(Number(size)).Append(" Tf ").Append(Number(spacing)).Append(" Tc ")
            .Append(Number(x)).Append(' ').Append(Number(y)).Append(" Td (")
            .Append(Escape(value ?? string.Empty)).Append(") Tj ET\n");
    }

    private static void Fill(StringBuilder content, string color, double x, double y, double width, double height) =>
        content.Append(color).Append(" rg ").Append(Number(x)).Append(' ').Append(Number(y)).Append(' ')
            .Append(Number(width)).Append(' ').Append(Number(height)).Append(" re f\n");

    private static void RoundedFill(StringBuilder content, string color, double x, double y, double width, double height, double radius)
    {
        content.Append(color).Append(" rg ");
        RoundedPath(content, x, y, width, height, radius);
        content.Append("f\n");
    }

    private static void RoundedStroke(StringBuilder content, string color, double x, double y, double width, double height, double radius, double lineWidth)
    {
        content.Append(color).Append(" RG ").Append(Number(lineWidth)).Append(" w ");
        RoundedPath(content, x, y, width, height, radius);
        content.Append("S\n");
    }

    private static void RoundedPath(StringBuilder content, double x, double y, double width, double height, double radius)
    {
        var r = Math.Min(radius, Math.Min(width, height) / 2);
        var k = r * 0.55228475;
        content.Append(Number(x + r)).Append(' ').Append(Number(y)).Append(" m ")
            .Append(Number(x + width - r)).Append(' ').Append(Number(y)).Append(" l ")
            .Append(Number(x + width - r + k)).Append(' ').Append(Number(y)).Append(' ')
            .Append(Number(x + width)).Append(' ').Append(Number(y + r - k)).Append(' ')
            .Append(Number(x + width)).Append(' ').Append(Number(y + r)).Append(" c ")
            .Append(Number(x + width)).Append(' ').Append(Number(y + height - r)).Append(" l ")
            .Append(Number(x + width)).Append(' ').Append(Number(y + height - r + k)).Append(' ')
            .Append(Number(x + width - r + k)).Append(' ').Append(Number(y + height)).Append(' ')
            .Append(Number(x + width - r)).Append(' ').Append(Number(y + height)).Append(" c ")
            .Append(Number(x + r)).Append(' ').Append(Number(y + height)).Append(" l ")
            .Append(Number(x + r - k)).Append(' ').Append(Number(y + height)).Append(' ')
            .Append(Number(x)).Append(' ').Append(Number(y + height - r + k)).Append(' ')
            .Append(Number(x)).Append(' ').Append(Number(y + height - r)).Append(" c ")
            .Append(Number(x)).Append(' ').Append(Number(y + r)).Append(" l ")
            .Append(Number(x)).Append(' ').Append(Number(y + r - k)).Append(' ')
            .Append(Number(x + r - k)).Append(' ').Append(Number(y)).Append(' ')
            .Append(Number(x + r)).Append(' ').Append(Number(y)).Append(" c h ");
    }

    private static void CircleStroke(StringBuilder content, string color, double centerX, double centerY, double radius, double lineWidth)
    {
        var k = radius * 0.55228475;
        content.Append(color).Append(" RG ").Append(Number(lineWidth)).Append(" w ")
            .Append(Number(centerX + radius)).Append(' ').Append(Number(centerY)).Append(" m ")
            .Append(Number(centerX + radius)).Append(' ').Append(Number(centerY + k)).Append(' ')
            .Append(Number(centerX + k)).Append(' ').Append(Number(centerY + radius)).Append(' ')
            .Append(Number(centerX)).Append(' ').Append(Number(centerY + radius)).Append(" c ")
            .Append(Number(centerX - k)).Append(' ').Append(Number(centerY + radius)).Append(' ')
            .Append(Number(centerX - radius)).Append(' ').Append(Number(centerY + k)).Append(' ')
            .Append(Number(centerX - radius)).Append(' ').Append(Number(centerY)).Append(" c ")
            .Append(Number(centerX - radius)).Append(' ').Append(Number(centerY - k)).Append(' ')
            .Append(Number(centerX - k)).Append(' ').Append(Number(centerY - radius)).Append(' ')
            .Append(Number(centerX)).Append(' ').Append(Number(centerY - radius)).Append(" c ")
            .Append(Number(centerX + k)).Append(' ').Append(Number(centerY - radius)).Append(' ')
            .Append(Number(centerX + radius)).Append(' ').Append(Number(centerY - k)).Append(' ')
            .Append(Number(centerX + radius)).Append(' ').Append(Number(centerY)).Append(" c S\n");
    }

    private static void Star(StringBuilder content, string color, double centerX, double centerY, double outerRadius, double innerRadius)
    {
        var points = Enumerable.Range(0, 10)
            .Select(index =>
            {
                var angle = -Math.PI / 2 + index * Math.PI / 5;
                var radius = index % 2 == 0 ? outerRadius : innerRadius;
                return (X: centerX + Math.Cos(angle) * radius, Y: centerY + Math.Sin(angle) * radius);
            })
            .ToArray();
        Polygon(content, color, points);
    }

    private static void MapleLeaf(StringBuilder content, string color, double centerX, double centerY)
    {
        var points = new (double X, double Y)[]
        {
            (centerX, centerY + 6), (centerX + 1.3, centerY + 2.7), (centerX + 4.1, centerY + 4),
            (centerX + 3.1, centerY + 0.9), (centerX + 5.8, centerY), (centerX + 2.3, centerY - 1.1),
            (centerX + 3, centerY - 3.3), (centerX + 0.8, centerY - 2.4), (centerX + 0.6, centerY - 6),
            (centerX - 0.6, centerY - 6), (centerX - 0.8, centerY - 2.4), (centerX - 3, centerY - 3.3),
            (centerX - 2.3, centerY - 1.1), (centerX - 5.8, centerY), (centerX - 3.1, centerY + 0.9),
            (centerX - 4.1, centerY + 4), (centerX - 1.3, centerY + 2.7)
        };
        Polygon(content, color, points);
    }

    private static void DrawQr(StringBuilder content, string value, double x, double y, double size)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(value, QRCodeGenerator.ECCLevel.Q);
        var modules = data.ModuleMatrix;
        var cell = size / modules.Count;
        Fill(content, "1 1 1", x, y, size, size);
        for (var row = 0; row < modules.Count; row++)
            for (var column = 0; column < modules[row].Length; column++)
                if (modules[row][column]) Fill(content, "0.043 0.231 0.129", x + column * cell, y + (modules.Count - row - 1) * cell, cell + 0.05, cell + 0.05);
    }

    private static void Polygon(StringBuilder content, string color, IReadOnlyList<(double X, double Y)> points)
    {
        if (points.Count == 0) return;
        content.Append(color).Append(" rg ").Append(Number(points[0].X)).Append(' ').Append(Number(points[0].Y)).Append(" m ");
        foreach (var point in points.Skip(1)) content.Append(Number(point.X)).Append(' ').Append(Number(point.Y)).Append(" l ");
        content.Append("h f\n");
    }

    private static void Stroke(StringBuilder content, string color, double x, double y, double width, double height, double lineWidth) =>
        content.Append(color).Append(" RG ").Append(Number(lineWidth)).Append(" w ").Append(Number(x)).Append(' ')
            .Append(Number(y)).Append(' ').Append(Number(width)).Append(' ').Append(Number(height)).Append(" re S\n");

    private static void Line(StringBuilder content, string color, double x1, double y1, double x2, double y2, double lineWidth) =>
        content.Append(color).Append(" RG ").Append(Number(lineWidth)).Append(" w ").Append(Number(x1)).Append(' ')
            .Append(Number(y1)).Append(" m ").Append(Number(x2)).Append(' ').Append(Number(y2)).Append(" l S\n");

    private static double Measure(string value, double fontSize, bool bold) => value.Length * fontSize * (bold ? 0.59 : 0.52);
    private static string Number(double value) => value.ToString("0.###", CultureInfo.InvariantCulture);

    private static string Escape(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormC);
        var result = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            var encoded = character switch
            {
                '\u2018' or '\u2019' => (byte)'\'',
                '\u201C' or '\u201D' => (byte)'"',
                '\u2013' or '\u2014' => (byte)'-',
                '\u2022' => (byte)'*',
                >= ' ' and <= '~' => (byte)character,
                >= '\u00A0' and <= '\u00FF' => (byte)character,
                _ => (byte)'?'
            };
            if (encoded is (byte)'(' or (byte)')' or (byte)'\\') result.Append('\\').Append((char)encoded);
            else if (encoded < 32 || encoded > 126) result.Append('\\').Append(Convert.ToString(encoded, 8).PadLeft(3, '0'));
            else result.Append((char)encoded);
        }
        return result.ToString();
    }

    private static byte[] BuildPdf(string content, string receiptNumber)
    {
        var streamLength = Encoding.ASCII.GetByteCount(content);
        const string unicodeMap = """
            /CIDInit /ProcSet findresource begin
            12 dict begin
            begincmap
            /CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def
            /CMapName /HCBE-WinAnsi def
            /CMapType 2 def
            1 begincodespacerange
            <00> <FF>
            endcodespacerange
            2 beginbfrange
            <20> <7E> <0020>
            <A0> <FF> <00A0>
            endbfrange
            endcmap
            CMapName currentdict /CMap defineresource pop
            end
            end
            """;
        var objects = new[]
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 5 0 R /F2 6 0 R >> >> /Contents 4 0 R >>",
            $"<< /Length {streamLength} >>\nstream\n{content}endstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding /ToUnicode 8 0 R >>",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding /ToUnicode 8 0 R >>",
            $"<< /Title ({Escape(receiptNumber)}) /Author (HCBE Canada) /Creator (HCBE document service) >>",
            $"<< /Length {Encoding.ASCII.GetByteCount(unicodeMap)} >>\nstream\n{unicodeMap}endstream"
        };

        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n%HCBE\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Length; index++)
        {
            offsets.Add(output.Position);
            Write(output, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        var xref = output.Position;
        Write(output, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R /Info 7 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    private static byte[] BuildMultiPagePdf(IReadOnlyList<string> pages, string title)
    {
        const string unicodeMap = """
            /CIDInit /ProcSet findresource begin
            12 dict begin
            begincmap
            /CIDSystemInfo << /Registry (Adobe) /Ordering (UCS) /Supplement 0 >> def
            /CMapName /HCBE-WinAnsi def
            /CMapType 2 def
            1 begincodespacerange
            <00> <FF>
            endcodespacerange
            2 beginbfrange
            <20> <7E> <0020>
            <A0> <FF> <00A0>
            endbfrange
            endcmap
            CMapName currentdict /CMap defineresource pop
            end
            end
            """;
        var fontRegularId = 3 + pages.Count * 2;
        var fontBoldId = fontRegularId + 1;
        var infoId = fontRegularId + 2;
        var unicodeId = fontRegularId + 3;
        var kids = string.Join(' ', Enumerable.Range(0, pages.Count).Select(index => $"{3 + index * 2} 0 R"));
        var objects = new List<string>
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            $"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>"
        };
        for (var index = 0; index < pages.Count; index++)
        {
            var contentId = 4 + index * 2;
            var content = pages[index];
            objects.Add($"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 {fontRegularId} 0 R /F2 {fontBoldId} 0 R >> >> /Contents {contentId} 0 R >>");
            objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream");
        }
        objects.Add($"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding /ToUnicode {unicodeId} 0 R >>");
        objects.Add($"<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding /ToUnicode {unicodeId} 0 R >>");
        objects.Add($"<< /Title ({Escape(title)}) /Author (HCBE Canada) /Creator (HCBE ticketing service) >>");
        objects.Add($"<< /Length {Encoding.ASCII.GetByteCount(unicodeMap)} >>\nstream\n{unicodeMap}endstream");

        using var output = new MemoryStream();
        Write(output, "%PDF-1.4\n%HCBE\n");
        var offsets = new List<long> { 0 };
        for (var index = 0; index < objects.Count; index++)
        {
            offsets.Add(output.Position);
            Write(output, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }
        var xref = output.Position;
        Write(output, $"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var offset in offsets.Skip(1)) Write(output, $"{offset:0000000000} 00000 n \n");
        Write(output, $"trailer\n<< /Size {objects.Count + 1} /Root 1 0 R /Info {infoId} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    private static void Write(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
