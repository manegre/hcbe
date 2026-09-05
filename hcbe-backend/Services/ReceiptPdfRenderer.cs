using System.Globalization;
using System.Text;
using HcbeApi.Models;

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

    private static void Write(Stream stream, string value)
    {
        var bytes = Encoding.ASCII.GetBytes(value);
        stream.Write(bytes, 0, bytes.Length);
    }
}
