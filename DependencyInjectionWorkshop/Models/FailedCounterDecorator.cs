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

        private void ResetFailedCount(string account)
        {
            _failedCounter.ResetFailedCount(account);
        }
    }
}