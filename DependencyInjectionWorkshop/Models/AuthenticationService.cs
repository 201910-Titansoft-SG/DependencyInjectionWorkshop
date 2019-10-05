using System;
using System.Net.Http;
using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        private readonly ProfileDao _profileDao;
        private readonly Sah256Adapter _sah256Adapter;

        public AuthenticationService()
        {
            _profileDao = new ProfileDao();
            _sah256Adapter = new Sah256Adapter();
        }

        public bool Verify(string account, string inputPassword, string otp)
        {
            if (GetIsAccountLocked(account))
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = _profileDao.GetPasswordFromDb(account);

            var hashedPassword = _sah256Adapter.ComputeHash(inputPassword);

            var currentOtp = GetCurrentOtp(account);

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                ResetFailedCount(account);

                return true;
            }
            else
            {
                AddFailedCount(account);

                LogFailedCount(account);

                Notify(account);

                return false;
            }
        }

        private static void AddFailedCount(string account)
        {
            var addFailedCountResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                                         .PostAsJsonAsync("api/failedCounter/Add", account).Result;
            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        private static string GetCurrentOtp(string account)
        {
            var response = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                           .PostAsJsonAsync("api/otps", account).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"web api error, accountId:{account}");
            }

            var currentOtp = response.Content.ReadAsAsync<string>().Result;
            return currentOtp;
        }

        private static int GetFailedCount(string account, HttpClient httpClient)
        {
            var failedCountResponse =
                httpClient.PostAsJsonAsync("api/failedCounter/GetFailedCount", account).Result;

            failedCountResponse.EnsureSuccessStatusCode();

            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        private static bool GetIsAccountLocked(string account)
        {
            var isLockedResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                                   .PostAsJsonAsync("api/failedCounter/IsLocked", account).Result;

            isLockedResponse.EnsureSuccessStatusCode();
            var isAccountLocked = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isAccountLocked;
        }

        private static void LogFailedCount(string account)
        {
            var failedCount = GetFailedCount(account, new HttpClient() {BaseAddress = new Uri("http://joey.com/")});
            LogInfo(account, failedCount);
        }

        private static void LogInfo(string account, int failedCount)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info($"accountId:{account} failed times:{failedCount}");
        }

        private static void Notify(string account)
        {
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { }, "my channel", $"{account}: try to login failed",
                                    "my bot name");
        }

        private static void ResetFailedCount(string account)
        {
            var resetResponse = new HttpClient() {BaseAddress = new Uri("http://joey.com/")}
                                .PostAsJsonAsync("api/failedCounter/Reset", account).Result;
            resetResponse.EnsureSuccessStatusCode();
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}