namespace MicroServiceReports.Domain.Ports
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using MicroServiceReports.Domain.Models;

    public interface ISaleEventRepository
    {
        Task SaveAsync(SaleEventRecord record);
        Task<SaleEventRecord?> GetBySaleIdAsync(long saleId);
        Task<IEnumerable<SaleEventRecord>> GetAllAsync();
    }
}
