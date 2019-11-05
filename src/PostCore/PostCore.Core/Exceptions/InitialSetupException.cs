using System;
using System.Linq;
using Microsoft.AspNetCore.Identity;

namespace PostCore.Core.Exceptions
{
    public class InitialSetupException : PostCoreException
    {
        public InitialSetupException(string message)
            : base(message)
        { }

        public InitialSetupException(string message, Exception innerException)
            : base(message, innerException)
        { }

        public static InitialSetupException FromIdentityResult(IdentityResult result)
        {
            var message = string.Join(
                "\n",
                result.Errors.Select(e => $"Code: {e.Code}; Description: {e.Description}"));
            return new InitialSetupException(message);
        }
    }
}
