using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests.Group;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class GroupRepository : BaseRepository<IPO_GroupMaster>, IGroupRepository
    {
        public GroupRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<int> CreateAsync(CreateGroupRequest request, int userId, int companyId)
        {
            var group = new IPO_GroupMaster
            {
                GroupName = request.GroupName,
                MobileNo = request.MobileNo,
                Email = request.Email,
                Address = request.Address,
                Remark = request.Remark,
                IPOId = request.IPOId,
                CompanyId = companyId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                IsActive = true
            };

            await _dbSet.AddAsync(group);
            await _context.SaveChangesAsync();
            return group.IPOGroupId;
        }

        public async Task<bool> UpdateAsync(UpdateGroupRequest request, int userId)
        {
            var group = await _dbSet.FindAsync(request.IPOGroupId);
            if (group == null || !group.IsActive)
                return false;

            group.GroupName = request.GroupName;
            group.MobileNo = request.MobileNo;
            group.Email = request.Email;
            group.Address = request.Address;
            group.Remark = request.Remark;
            group.IPOId = request.IPOId;
            group.ModifiedBy = userId;
            group.ModifiedDate = DateTime.UtcNow;

            _dbSet.Update(group);
            await _context.SaveChangesAsync();
            return true;
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

        public async Task<IPO_GroupMaster?> GetByIdAsync(int id, int companyId)
        {
            return await _dbSet
                .Include(g => g.IPOMaster)
                .FirstOrDefaultAsync(g => g.IPOGroupId == id && g.IsActive && g.CompanyId == companyId);
        }

        public async Task<PagedResult<IPO_GroupMaster>> GetGroupsWithFiltersAsync(GroupFilterRequest request, int companyId)
        {
            var query = _dbSet.AsQueryable();

            // Only active and belong to company
            query = query.Where(x => x.IsActive && x.CompanyId == companyId);

            // Filter by IPO if provided
            if (request.IPOId.HasValue)
            {
                query = query.Where(x => x.IPOId == request.IPOId.Value);
            }

            // Apply global search if provided
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                query = query.Where(x =>
                    (x.GroupName != null && x.GroupName.Contains(request.SearchValue)) ||
                    (x.MobileNo != null && x.MobileNo.Contains(request.SearchValue)) ||
                    (x.Email != null && x.Email.Contains(request.SearchValue)) ||
                    (x.Address != null && x.Address.Contains(request.SearchValue)) ||
                    (x.Remark != null && x.Remark.Contains(request.SearchValue))
                );
            }

            // Total count before pagination
            var totalCount = await query.CountAsync();

            // Apply offset-based pagination
            var groups = await query
                .Include(g => g.IPOMaster)
                .OrderByDescending(x => x.CreatedDate)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<IPO_GroupMaster>(groups, totalCount, request.Skip, request.PageSize);
        }

        public async Task<List<IPO_GroupMaster>> GetAllByCompanyAsync(int companyId)
        {
            return await _context.IPO_GroupMaster
                .Include(g => g.IPOMaster)
                .Where(g => g.IsActive && g.CompanyId == companyId)
                .OrderByDescending(g => g.CreatedDate)
                .ToListAsync();
        }
    }
}
