using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Services;
using System.Linq;
using System.ServiceModel.Activation;
using System.Web;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using Mongo.Context;
using Mongo.DataService;

namespace Mongo.DataService
{
    public class Global : System.Web.HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            RegisterRoutes();
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {

        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }

        private void RegisterRoutes()
        {
            DataServiceHostFactory factory = new DataServiceHostFactory();
            var databaseNames = MongoContext.GetDatabaseNames(ConfigurationManager.ConnectionStrings["MongoDB"].ConnectionString);
            foreach (var databaseName in databaseNames)
            {
                RouteTable.Routes.Add(new ServiceRoute(databaseName, factory, typeof(MongOData)));
            }
        }
    }
}