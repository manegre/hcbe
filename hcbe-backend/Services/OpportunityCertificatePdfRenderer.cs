using HcbeApi.Models;

namespace HcbeApi.Services;

public static class OpportunityCertificatePdfRenderer
{
    public static byte[] Render(OpportunityApplication application) => ReceiptPdfRenderer.RenderCertificate(application);
}
