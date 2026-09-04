using System.Text;
using FluentAssertions;
using HcbeApi.Models;
using HcbeApi.Services;

namespace HcbeApi.Tests.Services;

public sealed class ReceiptPdfRendererTests
{
    [Fact]
    public void Render_CreatesSinglePagePdfWithFinancialDetails()
    {
        var transaction = new FinancialTransaction
        {
            Kind = FinanceKinds.Donation,
            Status = FinanceStatuses.PartiallyRefunded,
            AmountCents = 12550,
            RefundedAmountCents = 2550,
            Currency = "cad",
            PayerName = "Aminata Ouedraogo",
            PayerEmail = "aminata@example.com",
            ReceiptNumber = "HCBE-2026-ABC123",
            ReceiptToken = new string('a', 64),
            PaidAtUtc = new DateTime(2026, 9, 4, 14, 30, 0, DateTimeKind.Utc),
            DonationCampaign = new DonationCampaign { Title = "Fonds de solidarite communautaire" }
        };

        var pdf = ReceiptPdfRenderer.Render(transaction);
        var source = Encoding.ASCII.GetString(pdf);

        pdf.Should().StartWith(Encoding.ASCII.GetBytes("%PDF-1.4"));
        source.Should().Contain("/Type /Page");
        source.Should().Contain("HCBE-2026-ABC123");
        source.Should().Contain("100.00 CAD");
        source.Should().Contain("PARTIALLY REFUNDED");
        source.Should().Contain("(HCBE)");
        source.Should().Contain("(Canada)");
        source.Should().Contain("aminata@example.com");
        source.Should().Contain("TRANSACTION DETAILS");
        source.Should().EndWith("%%EOF\n");
        source.Should().NotContain("<!doctype html>");
    }

    [Fact]
    public void DownloadFileName_RemovesUnsafeCharactersAndUsesPdfExtension()
    {
        var transaction = new FinancialTransaction { ReceiptNumber = "HCBE/2026 (123)" };

        ReceiptPdfRenderer.DownloadFileName(transaction).Should().Be("HCBE2026123.pdf");
    }
}
