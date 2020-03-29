using System;
using System.Diagnostics;
using System.Web.Mvc;
using System.Web.Routing;

using Elsa.App.Profile;
using Elsa.Assembly;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Users;
//using Elsa.Users.Controllers;
using Robowire;
using Robowire.RoboApi;

namespace Elsa.Portal
{
    public class Global : System.Web.HttpApplication
    {
        private readonly Container m_container = new Container();

        protected void Application_Start(object sender, EventArgs e)
        {
            Debug.WriteLine("Starting routes registration");

            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });

            Debug.WriteLine("Routes registration done");

            Debug.WriteLine("Setting up the container");
            DiSetup.SetupContainer(m_container, new FileLogWriter("Frontend"));
            Debug.WriteLine("Container set up");

            Debug.WriteLine("Initializing RoboApi");
            var installer = new RoboApiInstaller();
            installer.Install(
                ControllerBuilder.Current,
                m_container,
                (context, locator) =>
                    {
                        var session = locator.Get<IWebSession>();
                        session.Initialize(context);
                    }/*,
                typeof(ElsaControllerBase).Assembly,
                typeof(ProfileController).Assembly,
                typeof(UserController).Assembly*/);
            Debug.WriteLine("RoboApi initialized");

            Debug.WriteLine("Loading startup jobs");
            using (var startupJobsLocator = m_container.GetLocator())
            {
                var jobs = startupJobsLocator.GetCollection<IStartupJob>();
                var logger = startupJobsLocator.Get<ILog>();

                logger.Info("Starting startup jobs");
                foreach (var job in jobs)
                {
                    try
                    {
                        logger.Info($"Starting job {job.GetType().Name}");

                        job.Execute();

                        logger.Info($"Job {job.GetType().Name} complete");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"StartupJob {job.GetType().Name} failed", ex);
                        if (job.IsExceptionFatal)
                        {
                            throw;
                        }
                    }
                }
                logger.Info("Startup jobs done");
            }

            Debug.WriteLine("Application startup complete");
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {
            if (Request.Path == "/")
            {
                Context.RewritePath("/Home.html");
            }
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
    }
}