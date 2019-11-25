using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PostCore.Core;
using PostCore.Core.Cars;
using PostCore.Core.Services.Dao;
using PostCore.Core.Users;
using PostCore.MainApp.ViewModels.Cars;
using PostCore.Utils;
using PostCore.ViewUtils;

namespace PostCore.MainApp.Controllers
{
    [Authorize(Roles = Role.Authorize.Admin)]
    public class CarsController : Controller
    {
        public static readonly long PageSize = 10;

        private static readonly ListOptions DefaultListOptions = new ListOptions
        {
            Filters = new Dictionary<string, string>
            {
                {"model", ""},
                {"number", ""}
            },
            SortKey = "Id",
            SortOrder = SortOrder.Ascending
        };

        private readonly ICarsDao _carsDao;

        public CarsController(ICarsDao carsDao)
        {
            _carsDao = carsDao;
        }

        public async Task<IActionResult> Index(ListOptions options)
        {
            options = options ?? DefaultListOptions;

            var filterModel = options.Filters["model"];
            var filterNumber = options.Filters["number"];

            var cars = await _carsDao.GetAllAsync(
                filterModel,
                filterNumber,
                options.SortKey,
                options.SortOrder);

            return View(new IndexViewModel
            {
                Cars = cars.ToPaginatedList(options.Page, PageSize),
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
            var car = await _carsDao.GetByIdAsync(id);
            return View(
                new EditViewModel
                {
                    Id = car.Id,
                    EditorMode = EditorMode.Update,
                    Model = car.Model,
                    Number = car.Number,
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

            var car = new Car
            {
                Id = vm.Id,
                Model = vm.Model,
                Number = vm.Number,
            };

            try
            {
                if (vm.EditorMode == EditorMode.Create)
                {
                    await _carsDao.CreateAsync(car);
                }
                else
                {
                    await _carsDao.UpdateAsync(car);
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
                await _carsDao.DeleteAsync(id);
            }
            catch (Exception e)
            {
                ModelState.AddModelError("", e.Message);
            }

            return Redirect(returnUrl);
        }
    }
}
