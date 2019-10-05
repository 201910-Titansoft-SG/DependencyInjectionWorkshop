using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly IFailedCounter _failedCounter;
        private readonly IHash _hash;
        private readonly ILogger _logger;
        private readonly INotification _notification;
        private readonly IOtpService _otpService;
        private readonly IProfile _profile;

        public AuthenticationService()
        {
            _profile = new ProfileDao();
            _hash = new Sha256Adapter();
            _otpService = new OtpService();
            _notification = new SlackAdapter();
            _failedCounter = new FailedCounter();
            _logger = new NLogAdapter();
        }

        public AuthenticationService(IProfile profile, IHash hash, IOtpService otpService,
            INotification notification, IFailedCounter failedCounter, ILogger logger)
        {
            _profile = profile;
            _hash = hash;
            _otpService = otpService;
            _notification = notification;
            _failedCounter = failedCounter;
            _logger = logger;
        }

        public bool Verify(string account, string inputPassword, string otp)
        {
            if (_failedCounter.IsAccountLocked(account))
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profile.GetPasswordFromDb(account);

            var hashedPassword = _hash.ComputeHash(inputPassword);

            var currentOtp = _otpService.GetCurrentOtp(account);

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                _failedCounter.ResetFailedCount(account);

                return true;
            }
            else
            {
                _failedCounter.AddFailedCount(account);

                LogFailedCount(account);

                _notification.Notify($"{account}: try to login failed");

                return false;
            }
        }

        private void LogFailedCount(string account)
        {
            var failedCount =
                _failedCounter.GetFailedCount(account);

            _logger.LogInfo($"accountId:{account} failed times:{failedCount}");
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}