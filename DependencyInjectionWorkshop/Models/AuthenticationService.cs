using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public class LogFailedCountDecorator : BaseAuthenticationDecorator
    {
        private readonly IFailedCounter _failedCounter;
        private readonly ILogger _logger;

        public LogFailedCountDecorator(IAuthentication authenticationService, ILogger logger,
            IFailedCounter failedCounter) : base(authenticationService)
        {
            _logger = logger;
            _failedCounter = failedCounter;
        }

        public override bool Verify(string account, string inputPassword, string otp)
        {
            var isValid = base.Verify(account, inputPassword, otp);
            if (!isValid)
            {
                LogFailedCount(account);
            }

            return isValid;
        }

        private void LogFailedCount(string account)
        {
            var failedCount = _failedCounter.GetFailedCount(account);

            _logger.LogInfo($"accountId:{account} failed times:{failedCount}");
        }
    }

    public class AuthenticationService : IAuthentication
    {
        private readonly IFailedCounter _failedCounter;
        private readonly IHash _hash;
        private readonly LogFailedCountDecorator _logFailedCountDecorator;
        private readonly ILogger _logger;
        private readonly IOtpService _otpService;
        private readonly IProfile _profile;

        public AuthenticationService()
        {
            //_logFailedCountDecorator = new LogFailedCountDecorator(this);
            _profile = new ProfileDao();
            _hash = new Sha256Adapter();
            _otpService = new OtpService();
            _failedCounter = new FailedCounter();
            _logger = new NLogAdapter();
        }

        public AuthenticationService(IProfile profile, IHash hash, IOtpService otpService, IFailedCounter failedCounter,
            ILogger logger)
        {
            //_logFailedCountDecorator = new LogFailedCountDecorator(this);
            _profile = profile;
            _hash = hash;
            _otpService = otpService;
            _failedCounter = failedCounter;
            _logger = logger;
        }

        public bool Verify(string account, string inputPassword, string otp)
        {
            var passwordFromDb = _profile.GetPasswordFromDb(account);

            var hashedPassword = _hash.ComputeHash(inputPassword);

            var currentOtp = _otpService.GetCurrentOtp(account);

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                return true;
            }
            else
            {
                //_logFailedCountDecorator.LogFailedCount(account);

                return false;
            }
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}