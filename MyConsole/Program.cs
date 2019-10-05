using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using DependencyInjectionWorkshop.Models;

namespace MyConsole
{
    class Program
    {
        private static ILogger _logger;
        private static IFailedCounter _failedCounter;
        private static INotification _notification;
        private static IOtpService _otpService;
        private static IHash _hash;
        private static IProfile _profile;
        private static IAuthentication _authenticationService;
        private static IContainer _container;

        static void Main(string[] args)
        {
            RegisterContainer();
            //_logger = new FakeLogger();
            //_failedCounter = new FakeFailedCounter();
            //_notification = new FakeSlack();
            //_otpService = new FakeOtp();
            //_hash = new FakeHash();
            //_profile = new FakeProfile();
            //_authenticationService =
            //    new AuthenticationService(_profile, _hash, _otpService);

            //_authenticationService = new FailedCounterDecorator(_authenticationService, _failedCounter);
            //_authenticationService = new LogFailedCountDecorator(_authenticationService, _logger, _failedCounter);
            //_authenticationService = new NotificationDecorator(_authenticationService, _notification);

            _authenticationService = _container.Resolve<IAuthentication>();

            var isValid = _authenticationService.Verify("joey", "abc", "wrong otp");
            Console.WriteLine($"result is {isValid}");
        }

        private static void RegisterContainer()
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<FakeProfile>().As<IProfile>();
            builder.RegisterType<FakeHash>().As<IHash>();
            builder.RegisterType<FakeOtp>().As<IOtpService>();
            builder.RegisterType<FakeFailedCounter>().As<IFailedCounter>();
            builder.RegisterType<FakeLogger>().As<ILogger>();
            builder.RegisterType<FakeSlack>().As<INotification>();
            builder.RegisterType<AuthenticationService>().As<IAuthentication>();

            builder.RegisterType<FailedCounterDecorator>();
            builder.RegisterType<LogFailedCountDecorator>();
            builder.RegisterType<NotificationDecorator>();

            builder.RegisterDecorator<FailedCounterDecorator, IAuthentication>();
            //builder.RegisterDecorator<LogFailedCountDecorator, IAuthentication>();
            //builder.RegisterDecorator<NotificationDecorator, IAuthentication>();

            //_authenticationService = new FailedCounterDecorator(_authenticationService, _failedCounter);
            //_authenticationService = new LogFailedCountDecorator(_authenticationService, _logger, _failedCounter);
            //_authenticationService = new NotificationDecorator(_authenticationService, _notification);
            _container = builder.Build();
        }
    }

    internal class FakeLogger : ILogger
    {
        public void Info(string message)
        {
            Console.WriteLine(message);
        }

        public void LogInfo(string message)
        {
            Info(message);
        }
    }

    internal class FakeSlack : INotification
    {
        public void PushMessage(string message)
        {
            Console.WriteLine(message);
        }

        public void Send(string message)
        {
            PushMessage(message);
        }

        public void Notify(string message)
        {
            PushMessage(message);
        }
    }

    internal class FakeFailedCounter : IFailedCounter
    {
        public void ResetFailedCount(string accountId)
        {
            Console.WriteLine($"{nameof(FakeFailedCounter)}.{nameof(ResetFailedCount)}({accountId})");
        }

        public void AddFailedCount(string accountId)
        {
            Console.WriteLine($"{nameof(FakeFailedCounter)}.{nameof(AddFailedCount)}({accountId})");
        }

        public bool GetAccountIsLocked(string accountId)
        {
            return IsAccountLocked(accountId);
        }

        public int GetFailedCount(string accountId)
        {
            Console.WriteLine($"{nameof(FakeFailedCounter)}.{nameof(GetFailedCount)}({accountId})");
            return 91;
        }

        public bool IsAccountLocked(string accountId)
        {
            Console.WriteLine($"{nameof(FakeFailedCounter)}.{nameof(IsAccountLocked)}({accountId})");
            return false;
        }
    }

    internal class FakeOtp : IOtpService
    {
        public string GetCurrentOtp(string accountId)
        {
            Console.WriteLine($"{nameof(FakeOtp)}.{nameof(GetCurrentOtp)}({accountId})");
            return "123456";
        }
    }

    internal class FakeHash : IHash
    {
        public string Compute(string plainText)
        {
            Console.WriteLine($"{nameof(FakeHash)}.{nameof(Compute)}({plainText})");
            return "my hashed password";
        }

        public string ComputeHash(string input)
        {
            return Compute(input);
        }
    }

    internal class FakeProfile : IProfile
    {
        public string GetPassword(string accountId)
        {
            Console.WriteLine($"{nameof(FakeProfile)}.{nameof(GetPassword)}({accountId})");
            return "my hashed password";
        }

        public string GetPasswordFromDb(string account)
        {
            return GetPassword(account);
        }
    }
}