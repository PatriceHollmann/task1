﻿using System;
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
            httpReport.Reports.Add( "http://asasasa/fgfg.com", 404);
            httpReport.Statuses.Add(200, "статус ОК");
            httpReport.Statuses.Add(404, "статус NotFound");

            for (var i = 0; i < httpReport.Statuses.Count; i++)
    {
                httpReport.Statuses.Keys.ElementAt(i);
                Console.WriteLine(httpReport.Statuses[httpReport.Statuses.Keys.ElementAt(i)]);
                    if (httpReport.Reports.TryGetValue(httpReport.Statuses.Keys.ElementAt(i), out int item)
                    {
                        Console.WriteLine(httpReport.Reports);
                        Console.WriteLine(item.Value);
                    }
               }
                       Console.ReadKey();
            return;

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
            return;
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
