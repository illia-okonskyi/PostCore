using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;

namespace PostCore.Core.Services
{
    public interface ICurrentUserService
    {
        bool IsAuthenticated { get; }

        Task<User> GetUserAsync();
        Task<Role> GetRoleAsync();

        Task<Branch> GetBranchAsync();
        Task<bool> SetBranchAsync(long branchId);

        Task<Car> GetCarAsync();
        Task<bool> SetCarAsync(long carId);

        void Reset();
    }

    public class CurrentUserService : ICurrentUserService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IUsersDao _usersDao;
        private readonly IBranchesDao _branchesDao;
        private readonly ICarsDao _carsDao;

        private readonly string BranchIdJsonKey = "CurrentUserService.BranchId";
        private readonly string CarIdJsonKey = "CurrentUserService.CarId";

        private User _cachedUser;
        private Branch _cachedBranch;
        private Car _cachedCar;

        public CurrentUserService(
            IHttpContextAccessor httpContextAccessor,
            IUsersDao usersDao,
            IBranchesDao branchesDao,
            ICarsDao carsDao)
        {
            _httpContextAccessor = httpContextAccessor;
            _usersDao = usersDao;
            _branchesDao = branchesDao;
            _carsDao = carsDao;
        }

        public bool IsAuthenticated => _httpContextAccessor.HttpContext.User.Identity.IsAuthenticated;

        public async Task<User> GetUserAsync()
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            if (_cachedUser == null)
            {
                if (!await CacheUserAsync())
                {
                    return null;
                }
            }

            return _cachedUser;
        }

        public async Task<Role> GetRoleAsync()
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            if (_cachedUser == null)
            {
                if (!await CacheUserAsync())
                {
                    return null;
                }
            }

            return _cachedUser.Role;
        }

        public async Task<Branch> GetBranchAsync()
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            if (_cachedBranch == null)
            {
                if (!await CacheBranchAsync())
                {
                    return null;
                }
            }

            return _cachedBranch;
        }

        public async Task<bool> SetBranchAsync(long branchId)
        {
            if (!IsAuthenticated)
            {
                return false;
            }

            if (_cachedBranch != null && _cachedBranch.Id == branchId)
            {
                return true;
            }

            if (!await CacheBranchAsync(branchId))
            {
                return false;
            }

            var session = _httpContextAccessor.HttpContext.Session;
            session.SetJson(BranchIdJsonKey, branchId);
            return true;
        }

        public async Task<Car> GetCarAsync()
        {
            if (!IsAuthenticated)
            {
                return null;
            }

            if (_cachedCar == null)
            {
                if (!await CacheCarAsync())
                {
                    return null;
                }
            }

            return _cachedCar;
        }

        public async Task<bool> SetCarAsync(long carId)
        {
            if (!IsAuthenticated)
            {
                return false;
            }

            if (_cachedCar != null && _cachedCar.Id == carId)
            {
                return true;
            }

            if (!await CacheCarAsync(carId))
            {
                return false;
            }

            var session = _httpContextAccessor.HttpContext.Session;
            session.SetJson(CarIdJsonKey, carId);
            return true;
        }

        public void Reset()
        {
            _cachedUser = null;
            _cachedBranch = null;
            _cachedCar = null;

            var session = _httpContextAccessor.HttpContext.Session;
            session.SetJson(BranchIdJsonKey, default(long));
            session.SetJson(CarIdJsonKey, default(long));
        }

        private async Task<bool> CacheUserAsync()
        {
            var userName = _httpContextAccessor.HttpContext.User.Identity.Name;
            _cachedUser = await _usersDao.GetByUserNameWithRoleAsync(userName);
            return _cachedUser != null;
        }

        private async Task<bool> CacheBranchAsync(long branchId = default(long))
        {
            if (branchId == default(long))
            {
                var session = _httpContextAccessor.HttpContext.Session;
                branchId = session.GetJson<long>(BranchIdJsonKey);
                if (branchId == default(long))
                {
                    return false;
                }
            }

            _cachedBranch =  await _branchesDao.GetByIdAsync(branchId);
            return _cachedBranch != null;
        }

        private async Task<bool> CacheCarAsync(long carId = default(long))
        {
            if (carId == default(long))
            {
                var session = _httpContextAccessor.HttpContext.Session;
                carId = session.GetJson<long>(CarIdJsonKey);
                if (carId == default(long))
                {
                    return false;
                }
            }

            _cachedCar = await _carsDao.GetByIdAsync(carId);
            return _cachedCar != null;
        }
    }
}
