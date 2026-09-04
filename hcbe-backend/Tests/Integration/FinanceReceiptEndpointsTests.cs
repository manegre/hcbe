using System.Net;
using FluentAssertions;
using HcbeApi.Data;
using HcbeApi.Models;
using Microsoft.Extensions.DependencyInjection;

namespace HcbeApi.Tests.Integration;

public sealed class FinanceReceiptEndpointsTests : IClassFixture<CustomWebApplicationFactory>, IDisposable
{
    private readonly CustomWebApplicationFactory factory;
    private readonly HttpClient client;

    public FinanceReceiptEndpointsTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
        client = factory.CreateClient();
    }

    [Fact]
    public async Task DownloadReceipt_ReturnsPdfAttachment()
    {
        var token = Convert.ToHexString(Guid.NewGuid().ToByteArray()).ToLowerInvariant().PadRight(64, 'a');
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            context.FinancialTransactions.Add(new FinancialTransaction
            {
                Kind = FinanceKinds.Membership,
                Status = FinanceStatuses.Paid,
                AmountCents = 5000,
                Currency = "cad",
                PayerName = "Membre Test",
                PayerEmail = "membre-recu@example.com",
                ReceiptNumber = "HCBE-2026-PDFTEST",
                ReceiptToken = token,
                PaidAtUtc = new DateTime(2026, 9, 4, 15, 0, 0, DateTimeKind.Utc)
            });
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/finance/receipts/{token}");
        var bytes = await response.Content.ReadAsByteArrayAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        response.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
        response.Headers.CacheControl!.NoStore.Should().BeTrue();
        response.Headers.GetValues("X-Content-Type-Options").Should().ContainSingle("nosniff");
        (response.Content.Headers.ContentDisposition.FileNameStar ?? response.Content.Headers.ContentDisposition.FileName)
            .Should().Contain("HCBE-2026-PDFTEST.pdf");
        bytes.Should().StartWith("%PDF-"u8.ToArray());
    }

    [Fact]
    public async Task DownloadReceipt_WithMalformedToken_ReturnsNotFound()
    {
        var response = await client.GetAsync("/api/finance/receipts/not-a-valid-token");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    public void Dispose() => client.Dispose();
}
