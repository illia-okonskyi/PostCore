using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.Activities;
using PostCore.Core.DbContext;

namespace PostCore.Core.Services.Dao
{
    public interface IActivitiesDao
    {
        Task<IEnumerable<Activity>> GetAllAsync(
            ActivityType? filterType = null,
            string filterMessage = null,
            DateTime? filterFrom = null,
            DateTime? filterTo = null,
            string filterUser = null,
            long? filterPostId = null,
            long? filterBranchId = null,
            long? filterCarId = null);
        Task CreateAsync(Activity activity);
        Task RemoveToDateAsync(DateTime date);
    }

    public class ActivitiesDao : IActivitiesDao
    {
        private readonly ApplicationDbContext _dbContext;
        public ActivitiesDao(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Activity>> GetAllAsync(
            ActivityType? filterType = null,
            string filterMessage = null,
            DateTime? filterFrom = null,
            DateTime? filterTo = null,
            string filterUser = null,
            long? filterPostId = null,
            long? filterBranchId = null,
            long? filterCarId = null)
        {
            // 1) Filter
            var activities = _dbContext.Activity.AsNoTracking().AsQueryable();
            if (filterType.HasValue)
            {
                activities = activities.Where(a => a.Type == filterType.Value);
            }
            if (!string.IsNullOrEmpty(filterMessage))
            {
                activities = activities.Where(a => a.Message.Contains(filterMessage));
            }
            if (filterFrom.HasValue)
            {
                activities = activities.Where(a => a.DateTime >= filterFrom.Value);
            }
            if (filterTo.HasValue)
            {
                activities = activities.Where(a => a.DateTime <= filterTo.Value);
            }
            if (!string.IsNullOrEmpty(filterUser))
            {
                activities = activities.Where(a => a.User.Contains(filterUser));
            }
            if (filterPostId.HasValue)
            {
                activities = activities.Where(a => a.PostId == filterPostId.Value);
            }
            if (filterBranchId.HasValue)
            {
                activities = activities.Where(a => a.BranchId == filterBranchId.Value);
            }
            if (filterCarId.HasValue)
            {
                activities = activities.Where(a => a.CarId == filterCarId.Value);
            }

            // 3) Sort
            activities = activities.OrderByDescending((a) => a.DateTime);

            return await activities.ToListAsync();
        }

        public async Task CreateAsync(Activity activity)
        {
            await _dbContext.Activity.AddAsync(activity);
            await _dbContext.SaveChangesAsync();
        }

        public async Task RemoveToDateAsync(DateTime date)
        {
            var activities = _dbContext.Activity.Where(a => a.DateTime < date);
            _dbContext.Activity.RemoveRange(activities);
            await _dbContext.SaveChangesAsync();
        }
    }
}
