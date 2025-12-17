namespace MicroServiceReports.Infraestructure.Persistence
{
    using System.Threading.Tasks;
    using Microsoft.EntityFrameworkCore;
    using MicroServiceReports.Domain.Models;
    using MicroServiceReports.Domain.Ports;

    public class SaleEventRepositoryEf : ISaleEventRepository
    {
        private readonly MicroServiceReportsDbContext _dbContext;

        public SaleEventRepositoryEf(MicroServiceReportsDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task SaveAsync(SaleEventRecord record)
        {
            _dbContext.SaleEventRecords.Add(record);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<SaleEventRecord?> GetBySaleIdAsync(long saleId)
        {
            return await _dbContext.SaleEventRecords.FirstOrDefaultAsync(x => x.SaleId == saleId);
        }
    }
}
