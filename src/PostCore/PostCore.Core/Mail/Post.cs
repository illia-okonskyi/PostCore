using PostCore.Core.Branches;
using PostCore.Core.Cars;

namespace PostCore.Core.Mail
{
    public class Post
    {
        public long Id { get; set; }

        public string PersonFrom { get; set; }
        public string PersonTo { get; set; }
        public string AddressTo { get; set; }

        public long? BranchId { get; set; }
        public Branch Branch { get; set; }
        public string BranchStockAddress { get; set; }

        public long? CarId { get; set; }
        public Car Car { get; set; }

        public long? SourceBranchId { get; set; }
        public Branch SourceBranch { get; set; }

        public long? DestinationBranchId { get; set; }
        public Branch DestinationBranch { get; set; }

        public PostState State { get; set; }
    }
}
