﻿using DependencyInjectionWorkshop.Models;
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
        private AuthenticationService _authenticationService;
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
                new AuthenticationService(_profile, _hash, _otpService, _notification, _failedCounter, _logger);
        }

        [Test]
        public void is_valid()
        {
            GivenPassword(DefaultAccount, DefaultHashedPassword); 
            GivenHash(DefaultInputPassword, DefaultHashedPassword); 
            GivenOtp(DefaultAccount, DefaultOtp);

            ShouldBeValid(DefaultAccount, DefaultInputPassword, DefaultOtp);
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