using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace PostCore.Core.Exceptions
{
    public class IdentityException : PostCoreException
    {
        public IEnumerable<string> Errors { get; set; }

        public IdentityException(IdentityResult identityResult)
               : base(string.Join(
                   "\n",
                   identityResult.Errors.Select(e => $"Code: {e.Code}; Description: {e.Description}")))
        {
            Errors = identityResult.Errors.Select(e => $"Code: {e.Code}; Description: {e.Description}");
        }
    }
}
