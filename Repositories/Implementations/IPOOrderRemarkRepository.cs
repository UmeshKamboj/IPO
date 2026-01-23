using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class IPOOrderRemarkRepository: BaseRepository<IPO_Order_Remark>,IIPOOrderRemarkRepository
    {
        public IPOOrderRemarkRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<int> CreateAsync(CreateOrderRemarkRequest request, int userId, int companyId)
        {
            var remark = new IPO_Order_Remark
            {
                IPOId = request.IPOId,
                Remark = request.Remark,
                CompanyId = companyId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };
            await _dbSet.AddAsync(remark);
            await _context.SaveChangesAsync();
            return remark.RemarkId;


        }

        public async Task<IPO_Order_Remark?> GetByIdAsync(int id, int companyId)
        {
           return await _dbSet.FirstOrDefaultAsync(r => r.RemarkId == id && r.IsActive &&  r.CompanyId == companyId);
        }

        public async Task<List<IPO_Order_Remark>> GetRemarkByCompanyAsync(int companyId, int? ipoId)
        {
            var query = _dbSet.AsQueryable().Where(r => r.IsActive && r.CompanyId == companyId && (!ipoId.HasValue || r.IPOId == ipoId.Value)).OrderByDescending(r=>r.CreatedDate);

            return await query.ToListAsync();
        }
    }
}
