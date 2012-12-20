using System;
using System.Configuration;
using System.Linq;
using System.Web;
using AutoWebTranslator;
using AutoWebTranslator.Translators;

namespace Example.Webforms
{
    public class Global : HttpApplication
    {
        private static string _lang = null;

        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
        }

        void Application_End(object sender, EventArgs e)
        {
            //  Code that runs on application shutdown
        }

        void Application_Error(object sender, EventArgs e)
        {
            // Code that runs when an unhandled error occurs
        }

        public void Application_PostReleaseRequestState(object sender, EventArgs e)
        {
            if (Response.ContentType == "text/html")
            {
                string langParam = Request.QueryString["lang"];
                if (!string.IsNullOrWhiteSpace(langParam))
                    _lang = langParam;
                if (_lang != null)
                    Response.Filter = new TranslationFilter(Response.Filter,
                        new GoogleTranslationProvider(ConfigurationManager.AppSettings["googleTranslateApiKey"]), "en", _lang);
            }
        }

    }
}
