namespace DependencyInjectionWorkshop.Models
{
    public interface ILogger
    {
        void LogInfo(string account, int failedCount);
    }

    public class NLogAdapter : ILogger
    {
        public NLogAdapter()
        {
        }

        public void LogInfo(string account, int failedCount)
        {
            var logger = NLog.LogManager.GetCurrentClassLogger();
            logger.Info($"accountId:{account} failed times:{failedCount}");
        }
    }
}