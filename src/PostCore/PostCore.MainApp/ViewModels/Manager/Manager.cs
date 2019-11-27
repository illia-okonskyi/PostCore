using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
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

        public string ReturnUrl { get; set; }
    }

    public class RemoveActivitiesViewModel
    {
        [Required]
        [Display(Name = "Remove to date")]
        [DisplayFormat(DataFormatString = "g")]
        public DateTime ToDate { get; set; }

        [UIHint("HiddenInput")]
        public string ReturnUrl { get; set; }
    }
}
