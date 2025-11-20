using CMCSPrototype.Models;
using System.Collections.Generic;

namespace CMCSPrototype.Services
{
    public interface IPdfService
    {
        byte[] GenerateReportPdf(List<Claim> claims, Dictionary<string, decimal> summary);
    }
}
