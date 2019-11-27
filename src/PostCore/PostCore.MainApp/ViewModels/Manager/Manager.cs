using System.Collections.Generic;
using PostCore.Core.Activities;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Utils;

namespace PostCore.MainApp.ViewModels.Manager
{
    public class IndexViewModel
    {
        public IEnumerable<ActivityType> AllActivityTypes { get; set; }
        public IEnumerable<Branch> AllBranches { get; set; }
        public IEnumerable<Car> AllCars { get; set; }

        public PaginatedList<Activity> Activities { get; set; }
        public ListOptions CurrentListOptions { get; set; }
    }
}
