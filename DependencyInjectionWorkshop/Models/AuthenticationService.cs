using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly ProfileDao _profileDao;
        private readonly Sha256Adapter _sha256Adapter;
        private readonly OtpService _otpService;
        private readonly SlackAdapter _slackAdapter;
        private readonly FailedCounter _failedCounter;
        private readonly NLogAdapter _nLogAdapter;

        public AuthenticationService()
        {
            _profileDao = new ProfileDao();
            _sha256Adapter = new Sha256Adapter();
            _otpService = new OtpService();
            _slackAdapter = new SlackAdapter();
            _failedCounter = new FailedCounter();
            _nLogAdapter = new NLogAdapter();
        }

        public AuthenticationService(ProfileDao profileDao, Sha256Adapter sha256Adapter, OtpService otpService, SlackAdapter slackAdapter, FailedCounter failedCounter, NLogAdapter nLogAdapter)
        {
            _profileDao = profileDao;
            _sha256Adapter = sha256Adapter;
            _otpService = otpService;
            _slackAdapter = slackAdapter;
            _failedCounter = failedCounter;
            _nLogAdapter = nLogAdapter;
        }

        public bool Verify(string account, string inputPassword, string otp)
        {
            if (_failedCounter.IsAccountLocked(account))
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profileDao.GetPasswordFromDb(account);

            var hashedPassword = _sha256Adapter.ComputeHash(inputPassword);

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

                _slackAdapter.Notify($"{account}: try to login failed");

                return false;
            }
        }

        private void LogFailedCount(string account)
        {
            var failedCount =
                _failedCounter.GetFailedCount(account, new HttpClient() { BaseAddress = new Uri("http://joey.com/") });

            _nLogAdapter.LogInfo(account, failedCount);
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}