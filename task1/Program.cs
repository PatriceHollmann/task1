using System.Collections.Specialized;
using System.Configuration;
using System.Security.Policy;
using System.Web;

namespace task1
{
    class Program
    {
        static void Main(string[] args)
        {
            int.TryParse(ConfigurationManager.AppSettings["inclusion"],out int inclusion);
            string fileAddress = ConfigurationManager.AppSettings["fileAddress"];
            string fileName = ConfigurationManager.AppSettings["fileName"];
            string email = ConfigurationManager.AppSettings["email"];
            string server = ConfigurationManager.AppSettings["server"];
            string urlAddress = ConfigurationManager.AppSettings["urlAddress"];

            string linkConnectionString = ConfigurationManager.ConnectionStrings["LinkConnection"].ConnectionString;
            string reportConnectionString = ConfigurationManager.ConnectionStrings["LinkConnection"].ConnectionString;

            ReferenceSearcher refereneSearcher = new ReferenceSearcher(server, email, @urlAddress, inclusion);
            refereneSearcher.Search();
            refereneSearcher.Report(@fileAddress, fileName);
            refereneSearcher.SendEmailAsync().GetAwaiter();
        }
    }

}
