using System;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Security.Policy;
using System.Web;

namespace task1
{
    class Program
    {
        static void Main(string[] args)
        {
            var template = File.ReadAllText("Template.cshtml");
            HttpReport httpReport = new HttpReport();

            httpReport.Reports.Add("http://dfdfsdds/fgfg.com", 200);
            httpReport.Reports.Add("http://dfdfgfdgfgsdds/fgfg.com", 200);
            httpReport.Reports.Add("http://asasasa/fgfg.com", 404);
            //httpReport.Statuses.Add(200, "статус ОК");
            //httpReport.Statuses.Add(404, "статус NotFound");
            try
            {
               var html= RazorEngine.Razor.Parse(template, httpReport);
                File.WriteAllText("Index.html", html);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                Console.ReadKey();
            }
            //return;
            int.TryParse(ConfigurationManager.AppSettings["inclusion"],out int inclusion);
            string fileAddress = ConfigurationManager.AppSettings["fileAddress"];
            string fileName = ConfigurationManager.AppSettings["fileName"];
            string emailErrorReport = ConfigurationManager.AppSettings["emailErrorReport"];
            string serverErrorReport = ConfigurationManager.AppSettings["serverErrorReport"];
            string emailCheckReport = ConfigurationManager.AppSettings["emailCheckReport"];
            string serverCheckReport = ConfigurationManager.AppSettings["serverCheckReport"];
            string urlAddress = ConfigurationManager.AppSettings["urlAddress"];
            string connectionString = ConfigurationManager.ConnectionStrings["LinkConnection"].ConnectionString;

            ReferenceSearcher refereneSearcher = new ReferenceSearcher(@urlAddress, inclusion);
            refereneSearcher.Search();

            refereneSearcher.GetDataFromDatabase(connectionString);
            refereneSearcher.GetHeaders();
            refereneSearcher.SendCheckReport(serverCheckReport, emailCheckReport, "Index.html");

            refereneSearcher.ErrorsReport(@fileAddress, fileName);
            refereneSearcher.SendErrorsReportAsync(serverErrorReport, emailErrorReport).GetAwaiter();
        }
    }

}
