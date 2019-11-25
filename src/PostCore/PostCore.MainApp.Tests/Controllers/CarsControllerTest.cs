using System;
using System.Collections.Generic;
using System.Linq;
using PostCore.Core.Services.Dao;
using Xunit;
using Moq;
using PostCore.Utils;
using PostCore.MainApp.Controllers;
using PostCore.MainApp.ViewModels.Cars;
using PostCore.ViewUtils;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Http;
using PostCore.Core.Cars;

namespace PostCore.MainApp.Tests.Controllers
{
    public class CarsControllerTest
    {
        class Context
        {
            public ICarsDao CarsDao { get; set; }
            public ITempDataDictionary TempDataDictionary { get; set; }
            public ControllerContext ControllerContext { get; set; }
            public List<Car> Cars { get; set; } = new List<Car>();
            public Dictionary<string, object> TempData { get; set; } = new Dictionary<string, object>();
        }

        Context MakeContext(string path = "/", string query = "?query=query")
        {
            var context = new Context();

            var carsDaoMock = new Mock<ICarsDao>();
            carsDaoMock.Setup(m => m.GetAllAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<SortOrder>()))
                .ReturnsAsync((
                    string filterModel,
                    string filterNumber,
                    string sortKey,
                    SortOrder sortOrder) =>
                {
                    return context.Cars
                        .Where(c => c.Model.Contains(filterModel))
                        .Where(c => c.Number.Contains(filterNumber))
                        .Order(sortKey, sortOrder);
                });
            carsDaoMock.Setup(m => m.GetByIdAsync(It.IsAny<long>()))
                .ReturnsAsync((long id) => context.Cars.First(b => b.Id == id));
            carsDaoMock.Setup(m => m.CreateAsync(It.IsAny<Car>()))
                .Callback((Car car) =>
                {
                    context.Cars.Add(car);
                });
            carsDaoMock.Setup(m => m.UpdateAsync(It.IsAny<Car>(), It.IsAny<Car>()))
                .Callback((Car car, Car oldCar) =>
                {
                    var contextCar = context.Cars.First(c => c.Id == car.Id);
                    contextCar.Model = car.Model;
                    contextCar.Number = car.Number;
                });
            carsDaoMock.Setup(m => m.DeleteAsync(It.IsAny<long>()))
                .Callback((long id) =>
                {
                    var car = context.Cars.First(c => c.Id == id);
                    context.Cars.Remove(car);
                });
            context.CarsDao = carsDaoMock.Object;

            var httpContextMock = new Mock<HttpContext>();
            httpContextMock.Setup(m => m.Request.Path)
                .Returns(path);
            httpContextMock.Setup(m => m.Request.QueryString)
                .Returns(new QueryString(query));
            var controllerContext = new ControllerContext
            {
                HttpContext = httpContextMock.Object
            };
            context.ControllerContext = controllerContext;

            var tempDataDictionaryMock = new Mock<ITempDataDictionary>();
            tempDataDictionaryMock.Setup(m => m[It.IsAny<string>()])
                .Returns((string key) => context.TempData[key]);
            tempDataDictionaryMock.SetupSet(m => m[It.IsAny<string>()] = It.IsAny<object>())
                .Callback((string key, object o) => context.TempData[key] = o);
            context.TempDataDictionary = tempDataDictionaryMock.Object;

            return context;
        }

        [Fact]
        public async Task Index_GetAsync()
        {
            var options = new ListOptions
            {
                Filters = new Dictionary<string, string>
                {
                    { "model", "1"},
                    { "number", "2"}
                },
                SortKey = "number",
                SortOrder = SortOrder.Descending
            };
            var returnUrlPath = "/index";
            var returnUrlQuery = "?indexquery=indexquery";
            var returnUrl = returnUrlPath + returnUrlQuery;

            var context = MakeContext(returnUrlPath, returnUrlQuery);
            var random = new Random();
            for (int i = 0; i < 100; ++i)
            {
                await context.CarsDao.CreateAsync(new Car
                {
                    Id = i,
                    Model = "model" + random.Next(),
                    Number = "number" + random.Next()
                });
            }

            var controller = new CarsController(context.CarsDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Index(options) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as IndexViewModel;
            Assert.NotNull(vm);
            var expectedCars = context.Cars
                .Where(c => c.Model.Contains(options.Filters["model"]))
                .Where(c => c.Number.Contains(options.Filters["number"]))
                .Order(options.SortKey, options.SortOrder)
                .ToPaginatedList(options.Page, BranchesController.PageSize);
            Assert.Equal(expectedCars, vm.Cars);
            Assert.Same(options, vm.CurrentListOptions);
            Assert.Equal(returnUrl, vm.ReturnUrl);
        }

        [Fact]
        public void Create_Get()
        {
            var returnUrl = "/";
            var expectedVm = new EditViewModel
            {
                EditorMode = EditorMode.Create,
                ReturnUrl = returnUrl
            };

            var context = MakeContext();

            var controller = new CarsController(context.CarsDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = controller.Create(returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Equal(nameof(controller.Edit), r.ViewName);
            var vm = r.Model as EditViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.EditorMode, vm.EditorMode);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Edit_Get()
        {
            var car = new Car
            {
                Id = 1,
                Model = "model",
                Number = "number"
            };
            var returnUrl = "/";
            var expectedVm = new EditViewModel
            {
                EditorMode = EditorMode.Update,
                Id = car.Id,
                Model = car.Model,
                Number = car.Number,
                ReturnUrl = returnUrl
            };

            var context = MakeContext();
            await context.CarsDao.CreateAsync(car);

            var controller = new CarsController(context.CarsDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Edit(car.Id, returnUrl) as ViewResult;
            Assert.NotNull(r);
            Assert.Null(r.ViewName);
            var vm = r.Model as EditViewModel;
            Assert.NotNull(vm);
            Assert.Equal(expectedVm.EditorMode, vm.EditorMode);
            Assert.Equal(expectedVm.Id, vm.Id);
            Assert.Equal(expectedVm.Model, vm.Model);
            Assert.Equal(expectedVm.Number, vm.Number);
            Assert.Equal(expectedVm.ReturnUrl, vm.ReturnUrl);
        }

        [Fact]
        public async Task Edit_Post_CreateCar()
        {
            var context = MakeContext();

            var controller = new CarsController(context.CarsDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };
            var vm = new EditViewModel
            {
                EditorMode = EditorMode.Create,
                Id = 1,
                Model = "model",
                Number = "number",
                ReturnUrl = "/"
            };

            var r = await controller.Edit(vm) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(vm.ReturnUrl, r.Url);
            Assert.Single(context.Cars);
            var car = context.Cars.First();
            Assert.Equal(vm.Model, car.Model);
            Assert.Equal(vm.Number, car.Number);
        }

        [Fact]
        public async Task Edit_Post_UpdateCar()
        {
            var car = new Car
            {
                Id = 1,
                Model = "model",
                Number = "number"
            };

            var context = MakeContext();
            await context.CarsDao.CreateAsync(car);

            var controller = new CarsController(context.CarsDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };
            var vm = new EditViewModel
            {
                EditorMode = EditorMode.Update,
                Id = 1,
                Model = "model1",
                Number = "number1",
                ReturnUrl = "/"
            };

            var r = await controller.Edit(vm) as RedirectResult;
            Assert.NotNull(r);
            Assert.Equal(vm.ReturnUrl, r.Url);
            Assert.Single(context.Cars);
            var updatedCar = context.Cars.First();
            Assert.Equal(vm.Model, updatedCar.Model);
            Assert.Equal(vm.Number, updatedCar.Number);
        }

        [Fact]
        public async Task Delete_Post()
        {
            var car = new Car
            {
                Id = 1,
                Model = "model",
                Number = "number"
            };
            var returnUrl = "/";

            var context = MakeContext();
            await context.CarsDao.CreateAsync(car);

            var controller = new CarsController(context.CarsDao)
            {
                TempData = context.TempDataDictionary,
                ControllerContext = context.ControllerContext
            };

            var r = await controller.Delete(car.Id, returnUrl) as RedirectResult;
            Assert.NotNull(r);
            Assert.Same(returnUrl, r.Url);
            Assert.Empty(context.Cars);
        }
    }
}
