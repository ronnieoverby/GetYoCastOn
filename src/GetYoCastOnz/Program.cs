using System.Data.Common;
using CoreTechs.Common;

namespace GetYoCastOn
{
    public class Program
    {
        private static void Main()
        {
            var db = new DbConnectionStringBuilder
            {
                ConnectionString = ConnectionStrings.Default.ConnectionString
            };

            
        }
    }
}