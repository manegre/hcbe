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
    private const double Margin = 48;

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
        Fill(content, "0.043 0.231 0.129", 0, 628, PageWidth, 214);
        Fill(content, "0.961 0.773 0.094", Margin, 628, 7, 214);
        Fill(content, "1 1 1", Margin, 82, PageWidth - Margin * 2, 520);
        Stroke(content, "0.827 0.859 0.820", Margin, 82, PageWidth - Margin * 2, 520, 0.8);
    }

    private static void DrawHeader(StringBuilder content, FinancialTransaction item)
    {
        Text(content, "HCBE CANADA", Margin + 22, 795, 11, true, "0.961 0.773 0.094", 1.4);
        Text(content, "REÇU DE PAIEMENT  /  PAYMENT RECEIPT", Margin + 22, 753, 9, true, "0.788 0.851 0.808", 1.1);
        Text(content, item.ReceiptNumber, Margin + 22, 710, 25, true, "1 1 1");
        Text(content, "Confirmation officielle du paiement", Margin + 22, 684, 10, false, "0.788 0.851 0.808");
        Text(content, "Official payment confirmation", Margin + 22, 669, 10, false, "0.788 0.851 0.808");

        var status = Status(item.Status);
        var badgeWidth = Math.Max(104, Measure(status, 9, true) + 30);
        Fill(content, "0.961 0.773 0.094", PageWidth - Margin - badgeWidth, 779, badgeWidth, 31);
        Text(content, status, PageWidth - Margin - badgeWidth + 15, 790, 9, true, "0.043 0.231 0.129");
    }

    private static void DrawSummary(StringBuilder content, FinancialTransaction item)
    {
        var amount = Money(item.AmountCents - item.RefundedAmountCents, item.Currency);
        Text(content, "MONTANT NET  /  NET AMOUNT", Margin + 24, 564, 8, true, "0.435 0.478 0.443", 0.9);
        Text(content, amount, Margin + 24, 521, 29, true, "0.043 0.231 0.129");

        var purpose = Purpose(item);
        Text(content, "OBJET  /  PURPOSE", 330, 564, 8, true, "0.435 0.478 0.443", 0.9);
        var purposeLines = Wrap(purpose, 34);
        for (var index = 0; index < Math.Min(2, purposeLines.Count); index++)
            Text(content, purposeLines[index], 330, 538 - index * 17, 11, index == 0, "0.086 0.145 0.106");

        Line(content, "0.827 0.859 0.820", Margin + 24, 490, PageWidth - Margin - 24, 490, 0.8);
    }

    private static void DrawDetails(StringBuilder content, FinancialTransaction item)
    {
        var payer = item.IsAnonymous ? "Donateur anonyme / Anonymous donor" : item.PayerName ?? item.PayerEmail;
        var paidAt = item.PaidAtUtc ?? item.CreatedAtUtc;
        var rows = new List<(string Label, string Value)>
        {
            ("REÇU DE  /  RECEIVED FROM", payer),
            ("DATE DU PAIEMENT  /  PAYMENT DATE", $"{paidAt:yyyy-MM-dd HH:mm} UTC"),
            ("TYPE", item.Kind == FinanceKinds.Membership ? "Adhésion / Membership" : "Contribution / Contribution"),
            ("MODE", item.IsRecurring ? "Renouvellement automatique / Recurring" : "Paiement unique / One-time"),
            ("MONTANT INITIAL  /  ORIGINAL AMOUNT", Money(item.AmountCents, item.Currency))
        };
        if (item.RefundedAmountCents > 0)
            rows.Add(("REMBOURSÉ  /  REFUNDED", Money(item.RefundedAmountCents, item.Currency)));

        var y = 457d;
        foreach (var (label, value) in rows)
        {
            Text(content, label, Margin + 24, y, 7.5, true, "0.435 0.478 0.443", 0.6);
            var lines = Wrap(value, 48);
            for (var index = 0; index < Math.Min(2, lines.Count); index++)
                Text(content, lines[index], 274, y - index * 14, 10.5, false, "0.086 0.145 0.106");
            y -= lines.Count > 1 ? 56 : 47;
        }
    }

    private static void DrawFooter(StringBuilder content)
    {
        Line(content, "0.961 0.773 0.094", Margin + 24, 155, PageWidth - Margin - 24, 155, 2);
        Text(content, "HCBE Canada", Margin + 24, 132, 10, true, "0.043 0.231 0.129");
        Text(content, "contact@hcbe.ca  |  hcbe.ca", Margin + 24, 114, 9, false, "0.310 0.353 0.318");
        Text(content, "Ce document confirme un paiement et son état actuel. Il ne constitue pas un reçu fiscal de don de bienfaisance.", Margin, 54, 7.5, false, "0.310 0.353 0.318");
        Text(content, "This document confirms a payment and its current status. It is not a charitable tax receipt.", Margin, 41, 7.5, false, "0.310 0.353 0.318");
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
            $"<< /Title ({Escape(receiptNumber)}) /Author (HCBE Canada) /Creator (HCBE payment service) >>",
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
