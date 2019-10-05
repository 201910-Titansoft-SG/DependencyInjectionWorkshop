using System;
using System.Net.Http;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly IFailedCounter _failedCounter;
        private readonly ILogger _nLogAdapter;
        private readonly IOtpService _otpService;
        private readonly IProfile _profileDao;
        private readonly IHash _sha256Adapter;
        private readonly INotification _slackAdapter;

        public AuthenticationService()
        {
            _profileDao = new ProfileDao();
            _sha256Adapter = new Sha256Adapter();
            _otpService = new OtpService();
            _slackAdapter = new SlackAdapter();
            _failedCounter = new FailedCounter();
            _nLogAdapter = new NLogAdapter();
        }

        public AuthenticationService(IProfile profileDao, IHash sha256Adapter, IOtpService otpService,
            INotification slackAdapter, IFailedCounter failedCounter, ILogger nLogAdapter)
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
                _failedCounter.GetFailedCount(account, new HttpClient() {BaseAddress = new Uri("http://joey.com/")});

            _nLogAdapter.LogInfo(account, failedCount);
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}