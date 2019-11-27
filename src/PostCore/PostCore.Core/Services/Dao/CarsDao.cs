using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using PostCore.Core.Cars;
using PostCore.Core.DbContext;
using PostCore.Utils;

namespace PostCore.Core.Services.Dao
{
    public interface ICarsDao
    {
        Task<IEnumerable<Car>> GetAllAsync(
            string filterModel = null,
            string filterNumber = null,
            string sortKey = null,
            SortOrder sortOrder = SortOrder.Ascending);
        Task<Car> GetByIdAsync(long id);
        Task CreateAsync(Car car);
        Task UpdateAsync(Car car, Car oldCar = null);
        Task DeleteAsync(long id);
    }

    public class CarsDao : ICarsDao
    {
        public static List<string> AcceptableSortKeys { get; private set; } = new List<string>
        {
            nameof(Car.Id),
            nameof(Car.Model),
            nameof(Car.Number)
        };

        private readonly ApplicationDbContext _dbContext;
        public CarsDao(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Car>> GetAllAsync(
            string filterModel = null,
            string filterNumber = null,
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
            var cars = _dbContext.Car.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(filterModel))
            {
                cars = cars.Where(c => c.Model.Contains(filterModel));
            }
            if (!string.IsNullOrEmpty(filterNumber))
            {
                cars = cars.Where(c => c.Number.Contains(filterNumber));
            }

            // 3) Sort
            cars = cars.Order(sortKey, sortOrder);

            return await cars.ToListAsync();
        }

        public async Task<Car> GetByIdAsync(long id)
        {
            return await _dbContext.Car.FindAsync(id);
        }

        public async Task CreateAsync(Car car)
        {
            await _dbContext.Car.AddAsync(car);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateAsync(Car car, Car oldCar = null)
        {
            var dbCar = oldCar;
            if (oldCar == null)
            {
                dbCar = await GetByIdAsync(car.Id);
                if (dbCar == null)
                {
                    throw new ArgumentException("Car with such id not found", nameof(car));
                }
            }
            else
            {
                _dbContext.Car.Attach(dbCar);
            }


            dbCar.Model = car.Model;
            dbCar.Number = car.Number;
            await _dbContext.SaveChangesAsync();
        }

        public async Task DeleteAsync(long id)
        {
            _dbContext.Car.Remove(new Car { Id = id });
            await _dbContext.SaveChangesAsync();
        }
    }
}
