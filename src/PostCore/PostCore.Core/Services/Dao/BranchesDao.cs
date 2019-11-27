using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.Branches;
using PostCore.Core.DbContext;
using PostCore.Utils;

namespace PostCore.Core.Services.Dao
{
    public interface IBranchesDao
    {
        Task<IEnumerable<Branch>> GetAllAsync(
            string filterName = null,
            string filterAddress = null,
            string sortKey = null,
            SortOrder sortOrder = SortOrder.Ascending);
        Task<Branch> GetByIdAsync(long id);
        Task CreateAsync(Branch branch);
        Task UpdateAsync(Branch branch, Branch oldBranch = null);
        Task DeleteAsync(long id);
    }

    public class BranchesDao : IBranchesDao
    {
        public static List<string> AcceptableSortKeys { get; private set; } = new List<string>
        {
            nameof(Branch.Id),
            nameof(Branch.Name),
            nameof(Branch.Address)
        };

        private readonly ApplicationDbContext _dbContext;
        public BranchesDao(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Branch>> GetAllAsync(
            string filterName = null,
            string filterAddress = null,
            string sortKey = null,
            SortOrder sortOrder = SortOrder.Ascending)
        {
            // 1) Check sortKey
            if (string.IsNullOrEmpty(sortKey))
            {
                sortKey = AcceptableSortKeys.First();
            }
            if (!AcceptableSortKeys.Contains(sortKey))
            {
                throw new ArgumentException("Must be one of AcceptableSortKeys", nameof(sortKey));
            }

            // 2) Filter
            var branches = _dbContext.Branch.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(filterName))
            {
                branches = branches.Where(b => b.Name.Contains(filterName));
            }
            if (!string.IsNullOrEmpty(filterAddress))
            {
                branches = branches.Where(b => b.Address.Contains(filterAddress));
            }

            // 3) Sort
            branches = branches.Order(sortKey, sortOrder);

            return await branches.ToListAsync();
        }

        public async Task<Branch> GetByIdAsync(long id)
        {
            return await _dbContext.Branch.FindAsync(id);
        }

        public async Task CreateAsync(Branch branch)
        {
            await _dbContext.Branch.AddAsync(branch);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Branch branch, Branch oldBranch = null)
        {
            var dbBranch = oldBranch;
            if (oldBranch == null)
            {
                dbBranch = await GetByIdAsync(branch.Id);
                if (dbBranch == null)
                {
                    throw new ArgumentException("Branch with such id not found", nameof(branch));
                }
            }
            else
            {
                _dbContext.Branch.Attach(dbBranch);
            }


            dbBranch.Name = branch.Name;
            dbBranch.Address = branch.Address;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            // Force load mail for ClientSetNull delete behavior
            await _dbContext.Post.Where(p => p.SourceBranchId == id || p.DestinationBranchId == id)
                .LoadAsync();

            _dbContext.Branch.Remove(new Branch { Id = id });
            await _dbContext.SaveChangesAsync();
        }
    }
}
