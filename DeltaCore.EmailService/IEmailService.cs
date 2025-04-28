using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaCore.EmailService
{
    public interface IEmailService
    {
        Task<bool> SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
        Task<bool> SendOTPAsync(string email, cashIn cash = cashIn.MEMORY, CancellationToken cancellationToken = default);
        Task<bool> VerifyOTP(string email, string code, cashIn cash = cashIn.MEMORY);
        public enum cashIn
        {
            MEMORY,
            REDIS,
        }
    }
}
