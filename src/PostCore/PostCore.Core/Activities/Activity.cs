using System;
using System.Collections.Generic;
using PostCore.Core.Branches;
using PostCore.Core.Cars;
using PostCore.Core.Mail;

namespace PostCore.Core.Activities
{
    public class Activity
    {
        public long Id { get; set; }

        public ActivityType Type { get; set; }
        public string Message { get; set; }
        public DateTime DateTime { get; set; }
        public string User { get; set; }

        public long PostId { get; set; }
        public Post Post { get; set; }

        public long? BranchId { get; set; }
        public Branch Branch { get; set; }

        public long? CarId { get; set; }
        public Car Car { get; set; }

        public static IEnumerable<ActivityType> AllTypes = new List<ActivityType>
        {
            ActivityType.PostCreated,
            ActivityType.PostMovedToBranchStock,
            ActivityType.PostMovedToCar,
            ActivityType.PostDelivered
        };
    }
}
