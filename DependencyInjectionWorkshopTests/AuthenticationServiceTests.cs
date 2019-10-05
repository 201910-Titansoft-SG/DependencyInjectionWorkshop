using System;
using DependencyInjectionWorkshop.Models;
using NSubstitute;
using NUnit.Framework;

namespace DependencyInjectionWorkshopTests
{
    [TestFixture]
    public class AuthenticationServiceTests
    {
        private const string DefaultAccount = "joey";
        private const string DefaultHashedPassword = "my hashed password";
        private const string DefaultInputPassword = "abc";
        private const string DefaultOtp = "123456";
        private const int DefaultFailedCount = 91;
        private IAuthentication _authenticationService;
        private IFailedCounter _failedCounter;
        private IHash _hash;
        private ILogger _logger;
        private INotification _notification;
        private IOtpService _otpService;
        private IProfile _profile;

        [SetUp]
        public void SetUp()
        {
            _logger = Substitute.For<ILogger>();
            _failedCounter = Substitute.For<IFailedCounter>();
            _notification = Substitute.For<INotification>();
            _otpService = Substitute.For<IOtpService>();
            _hash = Substitute.For<IHash>();
            _profile = Substitute.For<IProfile>();
            _authenticationService =
                new AuthenticationService(_profile, _hash, _otpService);

            _authenticationService = new FailedCounterDecorator(_authenticationService, _failedCounter);
            _authenticationService = new LogFailedCountDecorator(_authenticationService, _logger, _failedCounter);
            _authenticationService = new NotificationDecorator(_authenticationService, _notification);
        }

        [Test]
        public void is_valid()
        {
            GivenPassword(DefaultAccount, DefaultHashedPassword);
            GivenHash(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccount, DefaultOtp);

            ShouldBeValid(DefaultAccount, DefaultInputPassword, DefaultOtp);
        }

        [Test]
        public void is_invalid()
        {
            GivenPassword(DefaultAccount, DefaultHashedPassword);
            GivenHash(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccount, DefaultOtp);

            ShouldBeInvalid(DefaultAccount, DefaultInputPassword, "wrong otp");
        }

        [Test]
        public void should_notify_user_when_invalid()
        {
            WhenInvalid();
            ShouldNotify(DefaultAccount);
        }

        [Test]
        public void reset_failed_count_when_valid()
        {
            WhenValid();
            ShouldResetFailedCount(DefaultAccount);
        }

        [Test]
        public void add_failed_count_when_invalid()
        {
            WhenInvalid();
            ShouldAddFailedCount(DefaultAccount);
        }

        [Test]
        public void account_is_locked()
        {
            GivenAccountIsLocked(true);
            ShouldThrow<FailedTooManyTimesException>();
        }

        [Test]
        public void log_failed_count_when_invalid()
        {
            GivenFailedCount(DefaultAccount, DefaultFailedCount);
            WhenInvalid();
            LogShouldContains(DefaultAccount, DefaultFailedCount);
        }

        private void LogShouldContains(string account, int failedCount)
        {
            _logger.Received(1).LogInfo(
                Arg.Is<string>(m => m.Contains(account) && m.Contains(failedCount.ToString())));
        }

        private void GivenFailedCount(string account, int failedCount)
        {
            _failedCounter.GetFailedCount(account).Returns(failedCount);
        }

        private void ShouldThrow<TException>() where TException : Exception
        {
            TestDelegate action = () => _authenticationService.Verify(DefaultAccount, DefaultInputPassword, DefaultOtp);
            Assert.Throws<TException>(action);
        }

        private void GivenAccountIsLocked(bool isLocked)
        {
            _failedCounter.IsAccountLocked(DefaultAccount).Returns(isLocked);
        }

        private void ShouldAddFailedCount(string account)
        {
            _failedCounter.Received().AddFailedCount(account);
        }

        private void ShouldResetFailedCount(string account)
        {
            _failedCounter.Received(1).ResetFailedCount(account);
        }

        private bool WhenValid()
        {
            GivenPassword(DefaultAccount, DefaultHashedPassword);
            GivenHash(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccount, DefaultOtp);

            var isValid = _authenticationService.Verify(DefaultAccount, DefaultInputPassword, DefaultOtp);
            return isValid;
        }

        private void ShouldNotify(string account)
        {
            _notification.Received(1).Notify(Arg.Is<string>(m => m.Contains(account)));
        }

        private bool WhenInvalid()
        {
            GivenPassword(DefaultAccount, DefaultHashedPassword);
            GivenHash(DefaultInputPassword, DefaultHashedPassword);
            GivenOtp(DefaultAccount, DefaultOtp);

            var isValid = _authenticationService.Verify(DefaultAccount, DefaultInputPassword, "wrong otp");
            return isValid;
        }

        private void ShouldBeInvalid(string account, string inputPassword, string otp)
        {
            var isValid = _authenticationService.Verify(account, inputPassword, otp);
            Assert.IsFalse(isValid);
        }

        private void ShouldBeValid(string account, string inputPassword, string otp)
        {
            var isValid = _authenticationService.Verify(account, inputPassword, otp);
            Assert.IsTrue(isValid);
        }

        private void GivenOtp(string account, string otp)
        {
            _otpService.GetCurrentOtp(account).Returns(otp);
        }

        private void GivenHash(string inputPassword, string hashedPassword)
        {
            _hash.ComputeHash(inputPassword).Returns(hashedPassword);
        }

        private void GivenPassword(string account, string hashedPassword)
        {
            _profile.GetPasswordFromDb(account).Returns(hashedPassword);
        }
    }
}