using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PostCore.Core.Branches;

namespace PostCore.MainApp.ViewModels.MyBranch
{
    public class IndexViewModel
    {
        public Branch MyBranch { get; set; }
        public List<Branch> Branches { get; set; }
        public string ReturnUrl { get; set; }
    }
}
