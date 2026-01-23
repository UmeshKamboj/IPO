using IPOClient.Data;
using IPOClient.Models.Entities;
using IPOClient.Models.Requests.ClientSetup;
using IPOClient.Models.Responses;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class ClientSetupRepository : BaseRepository<IPO_ClientSetup>, IClientSetupRepository
    {
        public ClientSetupRepository(IPOClientDbContext context) : base(context)
        {
        }

        public async Task<int> CreateAsync(CreateClientSetupRequest request, int userId, int companyId)
        {
            var client = new IPO_ClientSetup
            {
                PANNumber = request.PANNumber,
                Name = request.Name,
                GroupId = request.GroupId,
                ClientDPId = request.ClientDPId,
                CompanyId = companyId,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow,
                IsDeleted = false
            };

            await _context.Set<IPO_ClientSetup>().AddAsync(client);
            await _context.SaveChangesAsync();

            return client.ClientId;
        }

        public async Task<bool> UpdateAsync(UpdateClientSetupRequest request, int userId)
        {
            var client = await _context.Set<IPO_ClientSetup>()
                .FirstOrDefaultAsync(c => c.ClientId == request.ClientId && !c.IsDeleted);

            if (client == null)
                return false;

            client.PANNumber = request.PANNumber;
            client.Name = request.Name;
            client.GroupId = request.GroupId;
            client.ClientDPId = request.ClientDPId;
            client.ModifiedBy = userId;
            client.ModifiedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int id, int userId)
        {
            var client = await _context.Set<IPO_ClientSetup>()
                .FirstOrDefaultAsync(c => c.ClientId == id && !c.IsDeleted);

            if (client == null)
                return false;

            client.IsDeleted = true;
            client.DeletedBy = userId;
            client.DeletedDate = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IPO_ClientSetup?> GetByIdAsync(int id, int companyId, bool includeDeleted = false)
        {
            var query = _context.Set<IPO_ClientSetup>()
                .Include(c => c.Group)
                .Where(c => c.ClientId == id && c.CompanyId == companyId);

            if (!includeDeleted)
                query = query.Where(c => !c.IsDeleted);

            return await query.FirstOrDefaultAsync();
        }

        public async Task<PagedResult<IPO_ClientSetup>> GetClientsWithFiltersAsync(ClientSetupFilterRequest request, int companyId)
        {
            var query = _context.Set<IPO_ClientSetup>()
                .Include(c => c.Group)
                .Where(c => c.CompanyId == companyId);

            // Filter by deleted status
            if (!request.IncludeDeleted)
                query = query.Where(c => !c.IsDeleted);

            // Filter by GroupId
            if (request.GroupId.HasValue)
                query = query.Where(c => c.GroupId == request.GroupId.Value);

            // Global search
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var searchLower = request.SearchValue.ToLower();
                query = query.Where(c =>
                    c.PANNumber.ToLower().Contains(searchLower) ||
                    c.Name.ToLower().Contains(searchLower) ||
                    (c.ClientDPId != null && c.ClientDPId.ToLower().Contains(searchLower)) ||
                    (c.Group != null && c.Group.GroupName != null && c.Group.GroupName.ToLower().Contains(searchLower))
                );
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(c => c.CreatedDate)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<IPO_ClientSetup>(items, totalCount, request.Skip, request.PageSize);
        }

        public async Task<int> DeleteAllAsync(DeleteAllClientsRequest request, int userId, int companyId)
        {
            var clients = await _context.Set<IPO_ClientSetup>()
                .Where(c => c.CompanyId == companyId && !c.IsDeleted)
                .ToListAsync();

            if (clients.Count == 0)
                return 0;

            var deletedDate = DateTime.UtcNow;

            // Create delete history
            var history = new IPO_ClientDeleteHistory
            {
                DeletedDate = deletedDate,
                DeletedBy = userId,
                CompanyId = companyId,
                TotalClientsDeleted = clients.Count,
                Remark = request.Remark
            };

            await _context.Set<IPO_ClientDeleteHistory>().AddAsync(history);
            await _context.SaveChangesAsync();

            // Create history details
            var historyDetails = clients.Select(c => new IPO_ClientDeleteHistoryDetail
            {
                HistoryId = history.HistoryId,
                ClientId = c.ClientId,
                PANNumber = c.PANNumber,
                Name = c.Name,
                GroupId = c.GroupId,
                ClientDPId = c.ClientDPId
            }).ToList();

            await _context.Set<IPO_ClientDeleteHistoryDetail>().AddRangeAsync(historyDetails);

            // Mark all clients as deleted
            foreach (var client in clients)
            {
                client.IsDeleted = true;
                client.DeletedBy = userId;
                client.DeletedDate = deletedDate;
            }

            await _context.SaveChangesAsync();

            return history.HistoryId;
        }

        public async Task<PagedResult<IPO_ClientDeleteHistory>> GetDeleteHistoryAsync(ClientDeleteHistoryFilterRequest request, int companyId)
        {
            var query = _context.Set<IPO_ClientDeleteHistory>()
                .Where(h => h.CompanyId == companyId);

            // Filter by date range
            if (request.FromDate.HasValue)
                query = query.Where(h => h.DeletedDate >= request.FromDate.Value);

            if (request.ToDate.HasValue)
                query = query.Where(h => h.DeletedDate <= request.ToDate.Value);

            // Global search on remark
            if (!string.IsNullOrWhiteSpace(request.SearchValue))
            {
                var searchLower = request.SearchValue.ToLower();
                query = query.Where(h => h.Remark != null && h.Remark.ToLower().Contains(searchLower));
            }

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(h => h.DeletedDate)
                .Skip(request.Skip)
                .Take(request.PageSize)
                .ToListAsync();

            return new PagedResult<IPO_ClientDeleteHistory>(items, totalCount, request.Skip, request.PageSize);
        }

        public async Task<IPO_ClientDeleteHistory?> GetDeleteHistoryByIdAsync(int historyId, int companyId)
        {
            return await _context.Set<IPO_ClientDeleteHistory>()
                .Include(h => h.Details)
                .FirstOrDefaultAsync(h => h.HistoryId == historyId && h.CompanyId == companyId);
        }
    }
}
