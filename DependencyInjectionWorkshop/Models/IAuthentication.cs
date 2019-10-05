namespace DependencyInjectionWorkshop.Models
{
    public interface IAuthentication
    {
        bool Verify(string account, string inputPassword, string otp);
    }
}