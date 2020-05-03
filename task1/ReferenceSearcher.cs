using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.UI.HtmlControls;

namespace task1
{
    public class ReferenceSearcher
    {
        HttpReport httpReport = new HttpReport();
        private List<string> _urls;
        private List<string> _errors;

        private string _server { get; }
        private string _email { get; }
        private string _urlAddress { get; }
        private int _inclusion { get; }
        //private string _pattern = @"((http(s)?:\/\/)?(www\.)?([.]{2}[~\.\\/])*[a-zA-Z0-9@:%_\+~#=/\\]+\.[a-z]{2,}([a-zA-Z0-9-@:%_\+.~#?&/\\=])*)";
        private string _pattern = @"[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,}\.[a-z]{2,3}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)";
        public ReferenceSearcher(string server, string email, string urlAddress, int inclusion)
        {
            _server = server;
            _email = email;
            _urlAddress = urlAddress;
            _inclusion = inclusion;
            _urls = new List<string>();
            _errors = new List<string>();
        }

        public void Search()
        {
            Search(_urlAddress, 0);
        }
        private void Search(string url, int level)
        {
            string content = PageContent(url);
            var urls = _getLinks(content);
            CheckLinks(urls, url);
            if (_inclusion > level)
            {
                foreach (var link in urls)
                {
                   if (!httpReport.Reports.ContainsKey(link))
                        Search(link, level + 1);
                }
            }
        }
        private string PageContent(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string content = null;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    httpReport.Statuses.Add((int)response.StatusCode, response.StatusDescription);

                    using (Stream stream = response.GetResponseStream())
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            content = reader.ReadToEnd();
                        }
                    }
                    response.Close();
                    return content;
                }
            }
            catch (WebException ex)
            {
                var response = ex.Response as HttpWebResponse;
                httpReport.Reports.Add(url, (int)response.StatusCode);
                httpReport.Statuses.Add((int)response.StatusCode, response.StatusDescription);
            }
            catch (Exception e)
            {
                if (!_errors.Where(x => x.Contains(url)).ToList().Any())
                    _errors.Add(url + " " + e.Message.ToString());
            }
            return null;
        }
        private List<string> _getLinks(string content)
        {
            if (content != null)
            {
                Regex regex = new Regex(_pattern);
                MatchCollection matches = regex.Matches(content);
                if (matches.Count > 0)
                {
                    foreach (Match match in matches)
                    {
                        if(!(_urls.Contains(match.Value)||match.Value.TrimEnd()==".css"|| match.Value.TrimEnd() == ".js"))
                        _urls.Add(match.Value);
                    }
                }
                else
                    _errors.Add("No links on page");
            }
            return _urls;
        }
        private void CheckLinks(List<string> urls, string baseUrl)
        {
            foreach (String link in urls)
            {
                if (!(httpReport.Reports.ContainsKey(link)|| _errors.Where(x=>x.Contains(link)).ToList().Any()))
                {
                    try
                    {
                        var absoluteUrl = Adresses.ToAbsoluteUrl(link, baseUrl);
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(absoluteUrl);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        int key = (int)response.StatusCode;
                        httpReport.Reports.Add(absoluteUrl, key);

                        if (!httpReport.Statuses.ContainsKey((int)response.StatusCode))
                            httpReport.Statuses.Add((int)response.StatusCode, response.StatusDescription);

                        response.Close();
                    }
                    catch (WebException ex)
                    {
                        var response = ex.Response as HttpWebResponse;
                        if (response != null)
                        {
                            httpReport.Reports.Add(link, (int)response.StatusCode);

                            if (!httpReport.Statuses.ContainsKey((int)response.StatusCode))
                                httpReport.Statuses.Add((int)response.StatusCode, response.StatusDescription);
                        }
                        else
                            _errors.Add(link + " " + ex.Message.ToString());
                    }
                    catch (Exception e)
                    {
                        _errors.Add(link+" "+e.Message.ToString());
                    }
                }
            }
        }
        private void ListReportToSql (List<string> urls, string baseUrl)
        {

        }

        public void Report(string fileAddress, string fileName)
        {
            if (fileAddress == null || fileName == null)
                _errors.Add("Не полные исходные данные для файла отчета");
            using (StreamWriter writer = new StreamWriter(fileAddress + "/" + fileName, false, Encoding.UTF8))
            {
                writer.WriteLine("URL,Status Code");
                foreach (var keyValue in httpReport.Reports)
                {
                    writer.WriteLine(keyValue.Key + "," + keyValue.Value);
                }
            }
        }
        public async Task SendEmailAsync()
        {
            MailParamsCheck();
            if (_errors != null)
            {
                MailAddress from = new MailAddress("Tatiana.Grigorieva@icl-services.com");
                MailAddress to = new MailAddress(_email);
                MailMessage message = new MailMessage(from,to);
                message.Subject = "Отчет об ошибках";
                _errors.ForEach(x => x += ";\n");
                message.Body += "Ошибки :";
                foreach (var error in _errors)
                {
                    message.Body += error + ";\n";
                }
                SmtpClient client = new SmtpClient(_server);
                client.UseDefaultCredentials = true;
                try
                {
                    await client.SendMailAsync(message);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Ошибка создания письма: {0}", ex.ToString());
                }
            }
            else
                return;
        }

        private void MailParamsCheck()
        {
            string emailPattern = @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$";
            if (_server == null || _email == null)
                _errors.Add("Не полные исходные данные для отправки почты");
            if (_urlAddress == null || _inclusion == 0)
                _errors.Add("Не полные исходные данные для проверки страницы");
            if (!Regex.IsMatch(_email, emailPattern, RegexOptions.IgnoreCase))
                _errors.Add("Не корректно введен адрес получателя почты");
        }

        public CheckReportData GetDataFromDatabase(string linkConnectionString)//, string checkConnnectionString)
        {
           //DbConnection connection = new SqlConnection(linkConnectionString);

            CheckReportData checkReport=null;
            string linkSqlString = "SELECT * FROM Links"; 
                using (SqlConnection linkDbConnection = new SqlConnection(linkConnectionString))
                {
                    try
                    {
                        LinkDbConnectionOpen(linkSqlString, linkDbConnection);
                    }
                     catch (SqlException ex)
                    {
                        _errors.Add(ex.Message);
                    }
                    catch (Exception e)
                    {
                        _errors.Add(e.Message);
                    }
                }
            return checkReport;
        }
         private void LinkDbConnectionOpen(string linkSqlString, SqlConnection linkDbConnection)
        {
            linkDbConnection.Open();
            SqlCommand command = new SqlCommand(linkSqlString, linkDbConnection);
            using (SqlDataReader reader = command.ExecuteReader())
            {
                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        LinkReportData.Name = reader.GetString(0);
                        LinkReportData.StatusCode = reader.GetInt32(1);
                        httpReport.Reports.Add(LinkReportData.Name, LinkReportData.StatusCode);
                    }
                }
            }
        }
  


    }
    public class HttpReport
    {
        public Dictionary<int, string> Statuses { get; set; }
        public Dictionary<string, int> Reports { get; set; }
        public HttpReport()
        {
            Statuses = new Dictionary<int, string>();
            Reports = new Dictionary<string, int>();
        }
    }
    public  class LinkReportData
    {
        public static string Name { get; set; }
        public static int StatusCode { get; set; }
        public static string Type { get; set; }
    }
    public class CheckReportData
    {
        public static string Login { get; set; }
        public static DateTime StartTime { get; set; }
        public static DateTime EndTime { get; set; }
        public static int Number { get; set; }
    }
}
