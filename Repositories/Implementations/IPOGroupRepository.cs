using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests.IPOMaster.Request;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class IPOGroupRepository : BaseRepository<IPO_GroupMaster>, IIPOGroupRepository
    {
        public IPOGroupRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<int> CreateAsync(CreateIPOGroupRequest request, int userId, int companyId)
        {
            var group = new IPO_GroupMaster
            {
                GroupName = request.GroupName,
                CompanyId = companyId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                IPOId= request.IPOId??0,
                IsActive = true
            };

            await _dbSet.AddAsync(group);
            await _context.SaveChangesAsync();
            return group.IPOGroupId;
        }

        public async Task<bool> UpdateAsync(CreateIPOGroupRequest request, int userId)
        {
            if (!request.Id.HasValue)
                return false;

            var group = await _dbSet.FindAsync(request.Id.Value);
            if (group == null || !group.IsActive)
                return false;

            group.GroupName = request.GroupName;
            group.ModifiedBy = userId;
            group.ModifiedDate = DateTime.UtcNow;

            _dbSet.Update(group);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IPO_GroupMaster?> GetByIdAsync(int id, int companyId)
        {
            return await _dbSet.FirstOrDefaultAsync(x => x.IPOGroupId == id && x.IsActive && x.CompanyId == companyId);
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var group = await _dbSet.FindAsync(id);
            if (group == null || !group.IsActive)
                return false;

            group.IsActive = false;
            group.ModifiedBy = userId;
            group.ModifiedDate = DateTime.UtcNow;

            _dbSet.Update(group);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<IPO_GroupMaster>> GetGroupsByCompanyAsync(int companyId, int? ipoId)
        {
            var list = await _dbSet
                .AsNoTracking()
                .Where(x => x.IsActive && x.CompanyId == companyId && (ipoId.HasValue && x.IPOId== ipoId.Value))
                .OrderByDescending(x => x.CreatedDate)
                .ToListAsync();

            return list;
        }
    }
}

