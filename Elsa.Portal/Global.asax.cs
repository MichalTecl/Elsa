using System;
using System.Web.Mvc;
using System.Web.Routing;

using Elsa.App.Profile;
using Elsa.Common;
using Elsa.Common.Logging;
using Elsa.Users;

using Robowire;
using Robowire.RoboApi;

namespace Elsa.Portal
{
    public class Global : System.Web.HttpApplication
    {
        private readonly Container m_container = new Container();

        protected void Application_Start(object sender, EventArgs e)
        {
            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });

            Setup.SetupContainer(m_container);

            var installer = new RoboApiInstaller();
            installer.Install(
                ControllerBuilder.Current,
                m_container,
                (context, locator) =>
                    {
                        var session = locator.Get<IWebSession>();
                        session.Initialize(context);
                    },
                typeof(ElsaControllerBase).Assembly,
                typeof(ProfileController).Assembly,
                typeof(UserController).Assembly);

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
    }
}