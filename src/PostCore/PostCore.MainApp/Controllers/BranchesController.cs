using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core.Branches;
using PostCore.Core.Dao;
using PostCore.MainApp.ViewModels.Branches;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.Controllers
{
    public class BranchesController : Controller
    {
        private static readonly ListOptions DefaultListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"name", ""},
                {"address", ""}
            },
            SortKey = "Id",
            SortOrder = SortOrder.Ascending
        };
        private static readonly long PageSize = 10;

        private readonly IBranchesDao _branchesDao;

        public BranchesController(IBranchesDao branchesDao)
        {
            _branchesDao = branchesDao;
        }

        public async Task<IActionResult> Index(ListOptions options)
        {
            options = options ?? DefaultListOptions;

            var filterName = options.Filters["name"];
            var filterAddress = options.Filters["address"];

            var branches = await _branchesDao.GetAllAsync(
                filterName,
                filterAddress,
                options.SortKey,
                options.SortOrder);

            return View(new IndexViewModel
            {
                Branches = branches.ToPaginatedList(options.Page, PageSize),
                CurrentListOptions = options,
                ReturnUrl = HttpContext.Request.PathAndQuery()
            });
        }

        public IActionResult Create(string returnUrl)
        {
            return View(
                nameof(Edit),
                new EditViewModel
                {
                    EditorMode = EditorMode.Create,
                    ReturnUrl = returnUrl
                });
        }

        public async Task<IActionResult> Edit(long id, string returnUrl)
        {
            var branch = await _branchesDao.GetByIdAsync(id);
            return View(
                new EditViewModel
                {
                    Id = branch.Id,
                    EditorMode = EditorMode.Update,
                    Name = branch.Name,
                    Address = branch.Address,
                    ReturnUrl = returnUrl
                });
        }

        [HttpPost]
        public async Task<IActionResult> Edit(EditViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var branch = new Branch
            {
                Id = vm.Id,
                Name = vm.Name,
                Address = vm.Address,
            };

            try
            {
                if (vm.EditorMode == EditorMode.Create)
                {
                    await _branchesDao.CreateAsync(branch);
                }
                else
                {
                    await _branchesDao.UpdateAsync(branch);
                }
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            if (ModelState.ErrorCount != 0)
            {
                return View(vm);
            }
            else
            {
                return Redirect(vm.ReturnUrl);
            }
        }

        [HttpPost]
        public async Task<IActionResult> Delete(long id, string returnUrl)
        {
            try
            {
                await _branchesDao.DeleteAsync(id);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Redirect(returnUrl);
        }
    }
}
