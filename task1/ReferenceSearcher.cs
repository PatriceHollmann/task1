﻿using System;
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
        private List<string> _urls;
        private List<string> _errors;
        private Dictionary<int, string> _statuses;
        private Dictionary<string, int> _reports;
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
            _reports = new Dictionary<string, int>();
            _errors = new List<string>();
            _statuses = new Dictionary<int, string>();
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
            if (this._inclusion > level)
            {
                foreach (var link in urls)
                {
                   if (!_reports.ContainsKey(link))
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
                    _statuses.Add((int)response.StatusCode, response.StatusDescription);

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
                _reports.Add(url, (int)response.StatusCode);
                _statuses.Add((int)response.StatusCode, response.StatusDescription);
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
                if (!(_reports.ContainsKey(link)|| _errors.Where(x=>x.Contains(link)).ToList().Any()))
                {
                    try
                    {
                        var absoluteUrl = Adresses.ToAbsoluteUrl(link, baseUrl);
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(absoluteUrl);
                        HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                        int key = (int)response.StatusCode;
                        _reports.Add(absoluteUrl, key);

                        if (!_statuses.ContainsKey((int)response.StatusCode))
                            _statuses.Add((int)response.StatusCode, response.StatusDescription);

                        response.Close();
                    }
                    catch (WebException ex)
                    {
                        var response = ex.Response as HttpWebResponse;
                        if (response != null)
                        {
                            _reports.Add(link, (int)response.StatusCode);

                            if (!_statuses.ContainsKey((int)response.StatusCode))
                                _statuses.Add((int)response.StatusCode, response.StatusDescription);
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
                foreach (var keyValue in _reports)
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
            string linkSqlString = "SELECT * FROM Links GROUP BY StatusCode"; 
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
                        _reports.Add(LinkReportData.Name, LinkReportData.StatusCode);
                    }
                }
            }
        }
        private void HtmlCreator (CheckReportData checkReportData)
        {
            try
            {
                using (StreamWriter writer = new StreamWriter(@"report.html", false, Encoding.UTF8))
                {
                    HtmlCreator htmlCreator = new HtmlCreator();
                    //htmlContext.Add("<html>\n");
                    //htmlContext.Add("<head>\n");
                    //htmlContext.Add("      <title>HTML-Document</title>\n");
                    //htmlContext.Add("      <meta charset=\"utf-8\">\n");
                    //htmlContext.Add("</head>\n");
                    //htmlContext.Add("<body>\n");
                    //htmlContext.Add("       <h1>Отчет по странице</h1>\n");
                    //htmlContext.Add("             <table>\n");
                    //htmlContext.Add("                   <tr>\n");
                    //htmlContext.Add("                       <td>\n");
                    writer.Write(htmlCreator.htmlHeaderStrings);
                   foreach (var status in _statuses)
                    {
                            while (_reports.ContainsValue(status.Key))
                            {
                            writer.WriteLine("                   <tr>\n");
                            writer.WriteLine("                       <td>"+ status.Key + status.Value + "<\td>\n");
                            writer.WriteLine("                   <\tr>\n");
                            foreach (var item in _reports)
                                {
                                    writer.WriteLine("                   <tr>\n");
                                    writer.WriteLine("                       <td>" + item.Key + "<\td>+<td>" + item.Value + "<\td>\n");
                                    writer.WriteLine("                   <\tr>\n");
                                }
                            }
                    }
                    writer.Write(htmlCreator.htmlFooterStrings);
                }
            }
            catch (Exception e)
            {
                _errors.Add(e.Message);
            }
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
