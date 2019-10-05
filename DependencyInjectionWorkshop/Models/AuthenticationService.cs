using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public interface IAuthentication
    {
        bool Verify(string account, string inputPassword, string otp);
    }

    public class FailedCounterDecorator : BaseAuthenticationDecorator
    {
        private readonly IFailedCounter _failedCounter;

        public FailedCounterDecorator(IAuthentication authentication, IFailedCounter failedCounter) : base(
            authentication)
        {
            _failedCounter = failedCounter;
        }

        public override bool Verify(string account, string inputPassword, string otp)
        {
            var isValid = base.Verify(account, inputPassword, otp);
            if (isValid)
            {
                ResetFailedCount(account);
            }

            return isValid;
        }

        private void ResetFailedCount(string account)
        {
            _failedCounter.ResetFailedCount(account);
        }
    }

    public class AuthenticationService : IAuthentication
    {
        private readonly IFailedCounter _failedCounter;
        private readonly FailedCounterDecorator _failedCounterDecorator;
        private readonly IHash _hash;
        private readonly ILogger _logger;
        private readonly IOtpService _otpService;
        private readonly IProfile _profile;

        public AuthenticationService()
        {
            //_failedCounterDecorator = new FailedCounterDecorator(this);
            _profile = new ProfileDao();
            _hash = new Sha256Adapter();
            _otpService = new OtpService();
            _failedCounter = new FailedCounter();
            _logger = new NLogAdapter();
        }

        public AuthenticationService(IProfile profile, IHash hash, IOtpService otpService, IFailedCounter failedCounter,
            ILogger logger)
        {
            //_failedCounterDecorator = new FailedCounterDecorator(this);
            _profile = profile;
            _hash = hash;
            _otpService = otpService;
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
                //_failedCounterDecorator.ResetFailedCount(account);

                return true;
            }
            else
            {
                _failedCounter.AddFailedCount(account);

                LogFailedCount(account);

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