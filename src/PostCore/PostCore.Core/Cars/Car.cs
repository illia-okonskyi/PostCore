using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using PostCore.Core.Mail;

namespace PostCore.Core.Cars
{
    public class Car
    {
        public long Id { get; set; }
        public string Model { get; set; }
        public string Number { get; set; }

        [InverseProperty(nameof(Post.Car))]
        public ICollection<Post> Mail { get; set; }
    }
}
