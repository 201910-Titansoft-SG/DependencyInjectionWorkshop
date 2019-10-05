using DependencyInjectionWorkshop.Models;
using NSubstitute;
using NUnit.Framework;

namespace DependencyInjectionWorkshopTests
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        [Test]
        public void is_valid()
        {
            var profile = Substitute.For<IProfile>();
            profile.GetPasswordFromDb("joey").Returns("my hashed password");

            var hash = Substitute.For<IHash>();
            hash.ComputeHash("abc").Returns("my hashed password");

            var otpService = Substitute.For<IOtpService>();
            otpService.GetCurrentOtp("joey").Returns("123456");

            var notification = Substitute.For<INotification>();
            var failedCounter = Substitute.For<IFailedCounter>();
            var logger = Substitute.For<ILogger>();

            var authenticationService = new AuthenticationService(profile, hash, otpService, notification, failedCounter, logger);

            var isValid = authenticationService.Verify("joey","abc","123456");
            Assert.IsTrue(isValid);
        }
    }
}