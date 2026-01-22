using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Models.Requests.IPOMaster.Response;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using static IPOClient.Repositories.Implementations.IPORepository;

namespace IPOClient.Repositories.Implementations
{
    public class IPORepository : BaseRepository<IPO_IPOMaster>, IIPORepository
    {
        public IPORepository(IPOClientDbContext context) : base(context)
        {
        }

        // CREATE
        public async Task<int> CreateAsync(CreateIPORequest request, int userId, int companyId)
        {
            var ipo = new IPO_IPOMaster
            {
                IPOName = request.IPOName!,
                IPOType = request.IPOType,
                IPO_Upper_Price_Band = request.IPO_Upper_Price_Band,
                Total_IPO_Size_Cr = request.Total_IPO_Size_Cr,
                IPO_Retail_Lot_Size = request.IPO_Retail_Lot_Size,
                IPO_SHNI_Lot_Size = request.IPO_SHNI_Lot_Size,
                IPO_BHNI_Lot_Size = request.IPO_BHNI_Lot_Size,
                Retail_Percentage = request.Retail_Percentage,
                BHNI_Percentage = request.BHNI_Percentage,
                SHNI_Percentage = request.SHNI_Percentage,
                Remark = request.Remark,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true,
                CompanyId = companyId
            };

            await _dbSet.AddAsync(ipo);
            await _context.SaveChangesAsync();
            return ipo.Id;
        }

        // UPDATE
        public async Task<bool> UpdateAsync(CreateIPORequest request, int userId)
        {
            var ipo = await _dbSet.FindAsync(request.Id);
            if (ipo == null || !ipo.IsActive)
                return false;

            ipo.IPOName = request.IPOName!;
            ipo.IPOType = request.IPOType;
            ipo.IPO_Upper_Price_Band = request.IPO_Upper_Price_Band;
            ipo.Total_IPO_Size_Cr = request.Total_IPO_Size_Cr;
            ipo.IPO_Retail_Lot_Size = request.IPO_Retail_Lot_Size;
            ipo.IPO_SHNI_Lot_Size = request.IPO_SHNI_Lot_Size;
            ipo.IPO_BHNI_Lot_Size = request.IPO_BHNI_Lot_Size;
            ipo.Retail_Percentage = request.Retail_Percentage;
            ipo.BHNI_Percentage = request.BHNI_Percentage;
            ipo.SHNI_Percentage = request.SHNI_Percentage;
            ipo.Remark = request.Remark;
            ipo.ModifiedBy = userId.ToString();
            ipo.ModifiedDate = DateTime.UtcNow;

            _dbSet.Update(ipo);
            await _context.SaveChangesAsync();
            return true;
        }

        // GET BY ID
        public async Task<IPO_IPOMaster?> GetByIdAsync(int id, int companyId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.Id == id && x.IsActive && x.CompanyId == companyId);
        }

        // DELETE (soft delete)
        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var ipo = await _dbSet.FindAsync(id);
            if (ipo == null || !ipo.IsActive)
                return false;

            ipo.IsActive = false;
            ipo.ModifiedBy = userId.ToString();
            ipo.ModifiedDate = DateTime.UtcNow;

            _dbSet.Update(ipo);
            await _context.SaveChangesAsync();
            return true;
        }

        // GET WITH FILTERS & PAGINATION (like UserRepository)
        public async Task<PagedResult<IPO_IPOMaster>> GetIPOsWithFiltersAsync(IPOFilterRequest request, int companyId)
        {
            var query = _dbSet.AsQueryable();

            // Only active and belong to company
            query = query.Where(x => x.IsActive && x.CompanyId == companyId);

            // Apply global search if provided
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                query = query.Where(x =>
                    (x.IPOName != null && x.IPOName.Contains(request.SearchValue)) ||
                    (x.Remark != null && x.Remark.Contains(request.SearchValue))
                );
            }

            // Total count before pagination
            var totalCount = await query.CountAsync();

            // Apply offset-based pagination
            var ipos = await query
                .OrderBy(x => x.IPOName)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<IPO_IPOMaster>(ipos, totalCount, request.Skip, request.PageSize);
        }
        // Return IPOs Name and Id for a company
        public async Task<List<IPO_IPOMaster>> GetIPONameIdByCompanyAsync(int companyId)
        {
            // Return lightweight projection to avoid tracking unnecessary fields.
            var list = await _dbSet
                .AsNoTracking()
                .Where(x => x.IsActive && x.CompanyId == companyId)
                .OrderBy(x => x.IPOName)
                .Select(x => new IPO_IPOMaster
                {
                    Id = x.Id,
                    IPOName = x.IPOName
                })
                .ToListAsync();

            return list;
        }

    }

}
