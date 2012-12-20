using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using AutoWebTranslator;
using AutoWebTranslator.Translators;

namespace Example.MVC3
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801

    public class MvcApplication : System.Web.HttpApplication
    {
        private static string _lang = null;

        public static void RegisterGlobalFilters(GlobalFilterCollection filters)
        {
            filters.Add(new HandleErrorAttribute());
        }

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute(
                "Default", // Route name
                "{controller}/{action}/{id}", // URL with parameters
                new { controller = "Home", action = "Index", id = UrlParameter.Optional } // Parameter defaults
            );

        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            RegisterGlobalFilters(GlobalFilters.Filters);
            RegisterRoutes(RouteTable.Routes);
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