using DeltaCore.CacheHelper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System;
using System.Net;
using System.Net.Mail;
using System.Runtime;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using static DeltaCore.EmailService.IEmailService;

namespace DeltaCore.EmailService
{
    public class SMTPService : IEmailService
    {
        private readonly SMTPSettings _emailSettings;
        private readonly MemoryCacheService _memoryCache;
        private readonly RedisCacheService _redisCache;
        private readonly ILogger<SMTPService> _logger;
        private readonly IAsyncPolicy _retryPolicy;

        public SMTPService(
            IOptions<SMTPSettings> emailSettings,
            MemoryCacheService memoryCache,
            RedisCacheService redisCache,
            ILogger<SMTPService> logger)
        {
            _emailSettings = emailSettings.Value;
            _memoryCache = memoryCache;
            _redisCache = redisCache;
            _logger = logger;

            _retryPolicy = Policy.Handle<Exception>()
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    (exception, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning(exception, "Retry {RetryCount} after {Delay}s due to {ExceptionMessage}", retryCount, timeSpan.TotalSeconds, exception.Message);
                    });
        }

        public async Task<bool> SendEmailAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var success = await _retryPolicy.ExecuteAsync(async () =>
                {
                    await SendMailAsync(email, subject, body, cancellationToken);
                    return true;
                });

                if (success)
                {
                    _logger.LogInformation("Email sent successfully to {Email}", email);
                    return true;
                }

                return false;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Sending email cancelled for {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending email to {Email}", email);
                return false;
            }
        }

        private async Task SendMailAsync(string email, string subject, string body, CancellationToken cancellationToken)
        {
            using var smtpClient = new SmtpClient(_emailSettings.SMTPHost)
            {
                Port = _emailSettings.SMTPPort,
                Credentials = new NetworkCredential(_emailSettings.SMTPUser, _emailSettings.SMTPPassword),
                EnableSsl = true,
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_emailSettings.FromEmail, _emailSettings.OrganizationName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true,
            };
            mailMessage.To.Add(email);

            try
            {
                await smtpClient.SendMailAsync(mailMessage, cancellationToken);
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error while sending email to {Email}", email);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "General error while sending email to {Email}", email);
                throw;
            }
        }


        public async Task<bool> SendOTPAsync(string email, cashIn cash = cashIn.MEMORY, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var throttleKey = $"{email}:otp-last-sent";
                var lastSent = await _redisCache.GetDataAsync<DateTime?>(throttleKey);

                if (lastSent.HasValue && DateTime.UtcNow - lastSent.Value < TimeSpan.FromSeconds(60))
                {
                    _logger.LogWarning("OTP request throttled for {Email}", email);
                    return false;
                }

                var verificationCode = GenerateVerificationCode();
                var otpCacheKey = GetCacheKey(email);

                if (cash == cashIn.REDIS)
                    await _redisCache.SetDataAsync(otpCacheKey, verificationCode, cancellationToken);
                else
                    _memoryCache.SetData(otpCacheKey, verificationCode);
                var path = Path.Combine(Directory.GetCurrentDirectory(), _emailSettings.OTPHtmlBodyTemplatePath);
                if (File.Exists(path))
                {
                    _emailSettings.OTPHtmlBodyTemplate = File.ReadAllText(path);
                }

                var body = string.IsNullOrEmpty(_emailSettings.OTPHtmlBodyTemplate)
                    ? $"<h3>Your verification code is: <b>{verificationCode}</b></h3>"
                    : _emailSettings.OTPHtmlBodyTemplate
                        .Replace("{code}", verificationCode)
                        .Replace("{organization}", _emailSettings.OrganizationName);
               

                await _retryPolicy.ExecuteAsync(() => SendMailAsync(email, "Your Verification Code", body, cancellationToken));

                await _redisCache.SetDataAsync(throttleKey, DateTime.UtcNow, cancellationToken);

                _logger.LogInformation("OTP sent successfully to {Email}", email);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Sending OTP cancelled for {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to {Email}", email);
                return false;
            }
        }

        public async Task<bool> VerifyOTP(string email, string code, cashIn cash = cashIn.MEMORY)
        {
            var cacheKey = GetCacheKey(email);
            string storedCode = cash == cashIn.REDIS
                ? await _redisCache.GetDataAsync<string>(cacheKey)
                : _memoryCache.GetData<string>(cacheKey);

            if (string.IsNullOrEmpty(storedCode))
            {
                _logger.LogWarning("No OTP found for {Email}", email);
                return false;
            }

            if (storedCode == code)
            {
                if (cash == cashIn.REDIS)
                    await _redisCache.DelDataAsync(cacheKey);
                else
                    _memoryCache.DelData(cacheKey);

                _logger.LogInformation("OTP verified successfully for {Email}", email);
                return true;
            }

            _logger.LogWarning("OTP mismatch for {Email}", email);
            return false;
        }



        private string GenerateVerificationCode()
        {
            return RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString(); // 6 digits secure random
        }

        private string GetCacheKey(string email) => $"{email}:otp-code";

        public async Task<bool> SendOTPAsync(string email, string key, cashIn cash = cashIn.MEMORY, CancellationToken cancellationToken = default)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var throttleKey = $"{email}:otp-last-sent";
                var lastSent = await _redisCache.GetDataAsync<DateTime?>(throttleKey);

                if (lastSent.HasValue && DateTime.UtcNow - lastSent.Value < TimeSpan.FromSeconds(60))
                {
                    _logger.LogWarning("OTP request throttled for {Email}", email);
                    return false;
                }

                var verificationCode = GenerateVerificationCode();
                var otpCacheKey = GetCacheKey(key);

                if (cash == cashIn.REDIS)
                    await _redisCache.SetDataAsync(otpCacheKey, verificationCode, cancellationToken);
                else
                    _memoryCache.SetData(otpCacheKey, verificationCode);

                var body = string.IsNullOrEmpty(_emailSettings.OTPHtmlBodyTemplate)
                    ? $"<h3>Your verification code is: <b>{verificationCode}</b></h3>"
                    : _emailSettings.OTPHtmlBodyTemplate.Replace("{code}", verificationCode).Replace("{organization}", _emailSettings.OrganizationName);

                await _retryPolicy.ExecuteAsync(() => SendMailAsync(email, "Your Verification Code", body, cancellationToken));

                await _redisCache.SetDataAsync(throttleKey, DateTime.UtcNow, cancellationToken);

                _logger.LogInformation("OTP sent successfully to {Email}", email);
                return true;
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Sending OTP cancelled for {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP to {Email}", email);
                return false;
            }
        }
    }
}
