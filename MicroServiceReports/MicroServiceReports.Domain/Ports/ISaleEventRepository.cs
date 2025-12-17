namespace MicroServiceReports.Domain.Ports
{
    using System.Threading.Tasks;
    using MicroServiceReports.Domain.Models;

    public interface ISaleEventRepository
    {
        Task SaveAsync(SaleEventRecord record);
        Task<SaleEventRecord?> GetBySaleIdAsync(long saleId);
    }
}
