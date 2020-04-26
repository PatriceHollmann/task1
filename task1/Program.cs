using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
            // Dictionary<string, string> report=new Dictionary<string, string>();
            ReferenceSearcher refereneSearcher = new ReferenceSearcher(server, email, @urlAddress, inclusion);
            refereneSearcher.Search();
            refereneSearcher.Report(@fileAddress, fileName);
        }
    }
    public class ReferenceSearcher
    {
        private List<string> Urls;
        private List<string> Errors;
        private Dictionary<string, int> Reports;
        private string Server { get; }
        private string Email { get; }
        private string UrlAddress { get; }
        private int Inclusion { get; }
        private string pattern = @"((http(s)?:\/\/)?(www\.)?([.]{2}[~\.\\/])*[a-zA-Z0-9@:%_\+~#=/\\]+\.[a-z]{2,}([a-zA-Z0-9-@:%_\+.~#?&/\\=])*)";
        public ReferenceSearcher(string _server, string _email, string _address, int _inclusion)
        {
            Server = _server;
            Email = _email;
            UrlAddress = _address;
            Inclusion = _inclusion;
            Urls = new List<string>();
            Reports = new Dictionary<string, int>();
            Errors = new List<string>();
        }
        
        public void Search()
        {
            Search(UrlAddress, 0);
        }
        private void Search(string url,int level)
        {
            string content = PageContent(url);
            var urls = GetLinks(content);
            CheckLinks(urls);
            if (this.Inclusion>level)
            {
                foreach(var link in urls)
                {
                    if (!Reports.ContainsKey(link))
                    {
                        Search(link, level + 1);
                    }
                }
            }
        }
        private string PageContent (string url)
        {
                try
                {
                    HttpWebRequest request =(HttpWebRequest) WebRequest.Create(url);
                    HttpWebResponse response =(HttpWebResponse) request.GetResponse();
                string content = null;
                    if (response.StatusCode== HttpStatusCode.OK)
                    { 
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
                    //else
                    //{
                    //    throw new WebException();
                    //}
                 }
                catch (WebException ex)
                {
                var response = ex.Response as HttpWebResponse;
                Reports.Add(url, (int)response.StatusCode);
                }
                catch (Exception e)
                {
                Errors.Add(e.Message.ToString());
                }
            return null;
            }
        private List<string> GetLinks(string content)
        {
            Regex regex = new Regex(pattern);
            MatchCollection matches = regex.Matches(content);
            if (matches.Count>0)
            {
                foreach (Match match in matches)
                {
                    Urls.Add(match.Value);
                }
            }
            else
            {
                Errors.Add("No links on page");
            }
            return Urls;
        }
        private void CheckLinks (List<string>urls)
        {
            //report.Add("URL", "Status Code");
            foreach(String link in Urls)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(link);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                int key =(int)response.StatusCode;
                Reports.Add(link, key);
            }
           // return report;
        }

        public void Report( string FileAddress, string FileName)
        {
            if (FileAddress==null|| FileName==null)
            {
                Errors.Add("Не полные исходные данные для файла отчета");
            }
            using (StreamWriter writer = new StreamWriter(FileAddress+"/"+FileName, false, Encoding.UTF8))
            {
                writer.WriteLine("URL,Status Code");
                foreach (var keyValue in Reports)
                {
                    writer.WriteLine(keyValue.Key + "," + keyValue.Value);
                }
            }
        }
        public async Task SendEmailAsync()
        {
            string emailPattern = @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$";
            if (Server == null || Email == null)
            {
                Errors.Add("Не полные исходные данные для отправки почты");
            }
            if(UrlAddress == null || Inclusion == 0)
            {
                Errors.Add("Не полные исходные данные для проверки страницы");
            }
            if(!Regex.IsMatch(Email, emailPattern, RegexOptions.IgnoreCase))
            {
                Errors.Add("Не корректно введен адрес получателя почты");
            }
            MailAddress from = new MailAddress("Tatiana.Grigorieva@icl-services.com");
            MailAddress to = new MailAddress(Email);
            MailMessage message = new MailMessage();
            message.Subject = "Отчет об ошибках";
            Errors.ForEach(x => x += ";\n");
            message.Body = "<p>Возникли ошибки :" + Errors + "</p>";
            SmtpClient client = new SmtpClient(Server);
            client.UseDefaultCredentials = true;
            try
            {
                await client.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка создания письма: {0}",
                    ex.ToString());
            }
        }
        private string ToAbsoluteUrl(string relativeUrl)
        {
            if (HttpContext.Current == null)
                return relativeUrl;

            if (relativeUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || relativeUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                return relativeUrl;

            if (relativeUrl.StartsWith("/"))
                relativeUrl = relativeUrl.Insert(0, "~");
            if (!relativeUrl.StartsWith("~/"))
                relativeUrl = relativeUrl.Insert(0, "~/");

            var url = HttpContext.Current.Request.Url;
            var port = url.Port != 80 ? (":" + url.Port) : String.Empty;

            return String.Format("{0}://{1}{2}{3}",
                url.Scheme, url.Host, port, VirtualPathUtility.ToAbsolute(relativeUrl));
        }
    }
}
