namespace MicroServiceReports.Infraestructure.Persistence
{
    using System.Collections.Generic;
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

        public async Task<SaleEventRecord?> GetBySaleIdAsync(string saleId)
        {
            return await _dbContext.SaleEventRecords.FirstOrDefaultAsync(x => x.SaleId == saleId);
        }

        public async Task<IEnumerable<SaleEventRecord>> GetAllAsync()
        {
            return await _dbContext.SaleEventRecords
                .OrderByDescending(x => x.ReceivedAt)
                .ToListAsync();
        }
    }
}
