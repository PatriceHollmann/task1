using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace task1
{
    public class HtmlCreator//:ConfigurationSection
    {
        public List<string> htmlHeaderStrings;
        public List<string> htmlFooterStrings;
        public HtmlCreator()
        {
            htmlHeaderStrings = new List<string>();
            htmlFooterStrings = new List<string>();
        }
        private void CreateHeaderHtml()
        {
            NameValueCollection headerStrings = ConfigurationManager.GetSection("HtmlHeader") as NameValueCollection;
            if (headerStrings!=null)
            {
                foreach (var key in headerStrings.AllKeys)
                {
                    htmlHeaderStrings.Add(headerStrings[key]);
                }
            }
               // return htmlHeaderStrings;
        }
        private void CreateFooterHtml()
        {
            NameValueCollection footerStrings = ConfigurationManager.GetSection("HtmlFooter") as NameValueCollection;
            if (footerStrings != null)
            {
                foreach (var key in footerStrings.AllKeys)
                {
                    htmlHeaderStrings.Add(footerStrings[key]);
                }
            }
           // return htmlFooterStrings;
        }
    }
    
}
