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
using System.Web;
using System.Web.UI.HtmlControls;

namespace task1
{
    public class ReferenceSearcher
    {
        HttpReport httpReport = new HttpReport();
        private List<string> _urls;
        private List<string> _errors;
        //private string _serverErrorMessage { get; }
        //private string _emailErrorMessage { get; }
        private string _urlAddress { get; }
        private int _inclusion { get; }
        //private string _pattern = @"((http(s)?:\/\/)?(www\.)?([.]{2}[~\.\\/])*[a-zA-Z0-9@:%_\+~#=/\\]+\.[a-z]{2,}([a-zA-Z0-9-@:%_\+.~#?&/\\=])*)";
        private string _pattern = @"[(http(s)?):\/\/(www\.)?a-zA-Z0-9@:%._\+~#=]{2,}\.[a-z]{2,3}\b([-a-zA-Z0-9@:%_\+.~#?&//=]*)";
        public ReferenceSearcher( string urlAddress, int inclusion) //string serverErrorReport, string emailErrorReport,
        {
            //_serverErrorMessage = serverErrorReport;
            //_emailErrorMessage = emailErrorReport;
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
            if (_inclusion == 0)
                _errors.Add("Не корректное значение степени вложенности");
            if (_urlAddress == null)
                _errors.Add("Не корректный адрес страницы");
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                string content = null;
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    //httpReport.Statuses.Add((int)response.StatusCode, response.StatusDescription);
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
                //httpReport.Statuses.Add((int)response.StatusCode, response.StatusDescription);
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
                        //if (!httpReport.Statuses.ContainsKey((int)response.StatusCode))
                        //    httpReport.Statuses.Add((int)response.StatusCode, response.StatusDescription);
                        response.Close();
                    }
                    catch (WebException ex)
                    {
                        var response = ex.Response as HttpWebResponse;
                        if (response != null)
                        {
                            httpReport.Reports.Add(link, (int)response.StatusCode);
                            //if (!httpReport.Statuses.ContainsKey((int)response.StatusCode))
                            //    httpReport.Statuses.Add((int)response.StatusCode, response.StatusDescription);
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

        public void ErrorsReport(string fileAddress, string fileName)
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
        public async Task SendErrorsReportAsync(string serverErrorReport, string emailErrorReport)
        {
            MailParamsCheck(serverErrorReport, emailErrorReport);
            if (_errors != null)
            {
                MailAddress from = new MailAddress("Tatiana.Grigorieva@icl-services.com");
                MailAddress to = new MailAddress(emailErrorReport);
                MailMessage message = new MailMessage(from,to);
                message.Subject = "Отчет об ошибках";
                _errors.ForEach(x => x += ";\n");
                message.Body += "Ошибки :";
                foreach (var error in _errors)
                {
                    message.Body += error + ";\n";
                }
                SmtpClient client = new SmtpClient(serverErrorReport);
                client.UseDefaultCredentials = true;
                try
                {
                    await client.SendMailAsync(message);
                }
                catch (Exception ex)
                {
                    string errorMessage= "Ошибка создания письма: "+ ex.ToString();
                    MailSendingError(errorMessage);
                }
            }
            else
                return;
        }

        private void MailParamsCheck(string server, string email)
        {
            string emailPattern = @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$";
            if (server == null || email == null)
            {
                string errorMessage = "Не полные исходные данные для отправки почты";
                MailSendingError(errorMessage);
            }
            if (!Regex.IsMatch(email, emailPattern, RegexOptions.IgnoreCase))
            {
                string errorMessage = "Не корректно введен адрес получателя почты";
                MailSendingError(errorMessage);
            }
        }
        private void MailSendingError (string errorMessage)
        {
            string path = "Errors.txt";
            _errors.Add(errorMessage);
            FileInfo fileErrors = new FileInfo(path);
            if (!fileErrors.Exists)
            {
                using (StreamWriter writer = new StreamWriter(path, false, Encoding.UTF8))
                {
                    foreach (var item in _errors)
                        writer.WriteLine(item);
                }
            }   
            else
            {
                using (StreamWriter writer = new StreamWriter(path, true, Encoding.UTF8))
                {
                        writer.WriteLine(errorMessage);
                }
            }
            Console.WriteLine(errorMessage);
            Console.ReadKey();
        }
        public void GetDataFromDatabase(string linkConnectionString)//, string checkConnnectionString)
        {
           //DbConnection connection = new SqlConnection(linkConnectionString);

            //CheckReportData checkReport;
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
        public void GetHeaders ()
        {
            string statusDescription;
            foreach (var status in httpReport.Reports.Values)
            {
                if (!httpReport.Statuses.ContainsKey(status))
                {
                    statusDescription = HttpWorkerRequest.GetStatusDescription(status);
                    httpReport.Statuses.Add(status, statusDescription);
                }
            }
        }
        public void SendCheckReport(string serverCheckReport, string emailCheckReport, string fileReportPath)
        {
            MailParamsCheck(serverCheckReport, emailCheckReport);
                MailAddress from = new MailAddress("Tatiana.Grigorieva@icl-services.com");
                MailAddress to = new MailAddress(emailCheckReport);
                MailMessage message = new MailMessage(from, to);
                message.Subject = "Отчет о работе";
                message.Attachments.Add(new Attachment(fileReportPath));
                SmtpClient client = new SmtpClient(serverCheckReport);
                client.UseDefaultCredentials = true;
                try
                {
                    client.Send(message);
                }
                catch (Exception ex)
                {
                _errors.Add(ex.ToString());
                using (StreamWriter writer = new StreamWriter("Errors.txt", false, Encoding.UTF8))
                {
                    foreach (var item in _errors)
                        writer.WriteLine(item);
                }
                Console.WriteLine("Ошибка создания письма: {0}", ex.ToString());
                Console.ReadKey();
                }
        }
    }
}
