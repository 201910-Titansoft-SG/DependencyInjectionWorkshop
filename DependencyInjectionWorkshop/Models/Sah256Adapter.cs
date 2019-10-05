using System.Text;

namespace DependencyInjectionWorkshop.Models
{
    public class Sah256Adapter
    {
        public Sah256Adapter()
        {
        }

        public string ComputeHash(string input)
        {
            var crypt = new System.Security.Cryptography.SHA256Managed();
            var hash = new StringBuilder();
            var crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(input));
            foreach (var theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }

            var hashedPassword = hash.ToString();
            return hashedPassword;
        }
    }
}