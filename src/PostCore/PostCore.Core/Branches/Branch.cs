using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using PostCore.Core.Mail;

namespace PostCore.Core.Branches
{
    public class Branch
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }

        [InverseProperty(nameof(Post.Branch))]
        public ICollection<Post> Mail { get; set; }

        [InverseProperty(nameof(Post.SourceBranch))]
        public ICollection<Post> SourceMail { get; set; }

        [InverseProperty(nameof(Post.DestinationBranch))]
        public ICollection<Post> DestinationMail { get; set; }
    }
}
