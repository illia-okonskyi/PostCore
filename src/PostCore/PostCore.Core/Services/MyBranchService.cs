using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PostCore.Core.Branches;
using PostCore.Core.Services.Dao;

namespace PostCore.Core.Services
{
    public interface IMyBranchService
    {
        Task<Branch> GetMyBranchAsync();
        void SetMyBranch(long id);
    }

    public class MyBranchService : IMyBranchService
    {
        private readonly string JsonKey = "MyBranchId";

        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IBranchesDao _branchesDao;

        public MyBranchService(
            IHttpContextAccessor httpContextAccessor,
            IBranchesDao branchesDao)
        {
            _httpContextAccessor = httpContextAccessor;
            _branchesDao = branchesDao;

        }

        public async Task<Branch> GetMyBranchAsync()
        {
            var session = _httpContextAccessor.HttpContext.Session;
            var myBranchId = session.GetJson<long>(JsonKey);
            if (myBranchId == default(long))
            {
                return null;
            }

            try
            {
                return await _branchesDao.GetByIdAsync(myBranchId);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public void SetMyBranch(long id)
        {
            var session = _httpContextAccessor.HttpContext.Session;
            session.SetJson(JsonKey, id);
        }
    }
}
