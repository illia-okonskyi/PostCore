namespace PostCore.MainApp.ViewModels.Home
{
    public class MyPostStatusViewModel
    {
        public long PostId { get; set; }
        public bool HasInformation { get; set; }
        public string PersonFrom { get; set; }
        public string PersonTo { get; set; }
        public string AddressTo { get; set; }
        public string SourceBranchName { get; set; }
        public string DestinationBranchName { get; set; }
        public string CurrentBranchName { get; set; }
        public string PostState { get; set; }
    }
}
