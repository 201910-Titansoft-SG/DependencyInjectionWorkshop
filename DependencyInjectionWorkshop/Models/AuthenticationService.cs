using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http;
using System.Text;
using Dapper;
using SlackAPI;

namespace DependencyInjectionWorkshop.Models
{
    public class AuthenticationService
    {
        public bool Verify(string account, string inputPassword, string otp)
        {
            var httpClient = new HttpClient() {BaseAddress = new Uri("http://joey.com/")};
            if (GetIsAccountLocked(account, httpClient))
            {
                throw new FailedTooManyTimesException();
            }

            var passwordFromDb = GetPasswordFromDb(account);

            var hashedPassword = HashedPassword(inputPassword);

            var currentOtp = GetCurrentOtp(account, httpClient);

            if (passwordFromDb == hashedPassword && otp == currentOtp)
            {
                ResetFailedCount(account, httpClient);

                return true;
            }
            else
            {
                AddFailedCount(account, httpClient);

                LogFailedCount(account, httpClient);

                Notify(account);

                return false;
            }
        }

        private static void LogFailedCount(string account, HttpClient httpClient)
        {
            var failedCount = GetFailedCount(account, httpClient);
            LogInfo(account, failedCount);
        }

        private static void LogInfo(string account, int failedCount)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info($"accountId:{account} failed times:{failedCount}");
        }

        private static int GetFailedCount(string account, HttpClient httpClient)
        {
            var failedCountResponse =
                httpClient.PostAsJsonAsync("api/failedCounter/GetFailedCount", account).Result;

            failedCountResponse.EnsureSuccessStatusCode();

            var failedCount = failedCountResponse.Content.ReadAsAsync<int>().Result;
            return failedCount;
        }

        private static bool GetIsAccountLocked(string account, HttpClient httpClient)
        {
            var isLockedResponse = httpClient.PostAsJsonAsync("api/failedCounter/IsLocked", account).Result;

            isLockedResponse.EnsureSuccessStatusCode();
            var isAccountLocked = isLockedResponse.Content.ReadAsAsync<bool>().Result;
            return isAccountLocked;
        }

        private static void ResetFailedCount(string account, HttpClient httpClient)
        {
            var resetResponse = httpClient.PostAsJsonAsync("api/failedCounter/Reset", account).Result;
            resetResponse.EnsureSuccessStatusCode();
        }

        private static void AddFailedCount(string account, HttpClient httpClient)
        {
            var addFailedCountResponse = httpClient.PostAsJsonAsync("api/failedCounter/Add", account).Result;
            addFailedCountResponse.EnsureSuccessStatusCode();
        }

        private static void Notify(string account)
        {
            var slackClient = new SlackClient("my api token");
            slackClient.PostMessage(response1 => { }, "my channel", $"{account}: try to login failed",
                                    "my bot name");
        }

        private static string GetCurrentOtp(string account, HttpClient httpClient)
        {
            var response = httpClient.PostAsJsonAsync("api/otps", account).Result;
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception($"web api error, accountId:{account}");
            }

            var currentOtp = response.Content.ReadAsAsync<string>().Result;
            return currentOtp;
        }

        private static string HashedPassword(string inputPassword)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(inputPassword));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            var hashedPassword = hash.ToString();
            return hashedPassword;
        }

        private static string GetPasswordFromDb(string account)
        {
            string passwordFromDb;
            using (var connection = new SqlConnection("my connection string"))
            {
                var password = connection.Query<string>("spGetUserPassword", new {Id = account},
                                                        commandType: CommandType.StoredProcedure).SingleOrDefault();

                passwordFromDb = password;
            }

            return passwordFromDb;
        }
    }

    public class FailedTooManyTimesException : Exception
    {
    }
}