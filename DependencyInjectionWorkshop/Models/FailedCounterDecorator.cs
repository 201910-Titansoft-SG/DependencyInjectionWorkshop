namespace DependencyInjectionWorkshop.Models
{
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
            CheckIsAccountLocked(account);
            var isValid = base.Verify(account, inputPassword, otp);
            if (isValid)
            {
                ResetFailedCount(account);
            }
            else
            {
                AddFailedCount(account);
            }

            return isValid;
        }

        private void AddFailedCount(string account)
        {
            _failedCounter.AddFailedCount(account);
        }

        private void CheckIsAccountLocked(string account)
        {
            if (_failedCounter.IsAccountLocked(account))
            {
                throw new FailedTooManyTimesException();
            }
        }

        private void ResetFailedCount(string account)
        {
            _failedCounter.ResetFailedCount(account);
        }
    }
}