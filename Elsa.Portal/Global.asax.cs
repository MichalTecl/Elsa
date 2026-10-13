using System;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

using Elsa.App.Profile;
using Elsa.Assembly;
using Elsa.Common;
using Elsa.Common.Interfaces;
using Elsa.Common.Logging;
using Elsa.Users;
using Robowire;
using Robowire.Diagnostics;
using Robowire.RoboApi;

[assembly: PreApplicationStartMethod(typeof(Elsa.Portal.Global), nameof(Elsa.Portal.Global.PreApplicationStart))]

namespace Elsa.Portal
{
    public class Global : System.Web.HttpApplication
    {
        public static readonly Container Container = new Container();

        /// <summary>
        /// Called by ASP.NET when the application domain starts, before Global.asax is compiled and before Application_Start.
        /// </summary>
        public static void PreApplicationStart()
        {
            // TODO temporary diagnostics - remove
            try
            {
                System.IO.File.AppendAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(), "Elsa_PreApplicationStart.txt"), $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\tPreApplicationStart called\r\n");
            }
            catch (Exception)
            {
            }

            StartupLog.SetApplicationName(typeof(Global).Assembly.GetName().Name);
            StartupLog.Write("PreApplicationStart (app domain started)");
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            StartupLog.SetApplicationName(typeof(Global).Assembly.GetName().Name);
            StartupLog.Write("Application_Start");

            Debug.WriteLine("Starting routes registration");

            RouteTable.Routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            RouteTable.Routes.MapRoute(
                name: "Default",
                url: "{controller}/{action}/{id}",
                defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });

            Debug.WriteLine("Routes registration done");

            // RoboApi is registered before DiSetup: DiSetup runs the DB migration, which compiles the container,
            // and any later container.Setup() would force a second compilation.
            Debug.WriteLine("Initializing RoboApi");
            var installer = new RoboApiInstaller();
            installer.Install(
                ControllerBuilder.Current,
                Container,
                (context, locator) =>
                    {
                        var session = locator.Get<IWebSession>();
                        session.Initialize(context.HttpContext);
                    });
            Debug.WriteLine("RoboApi initialized");
            StartupLog.Write("RoboApi initialized");

            Debug.WriteLine("Setting up the container");
            DiSetup.SetupContainer(Container, new FileLogWriter("Frontend"));
            Debug.WriteLine("Container set up");
            StartupLog.Write("Container set up");

            Debug.WriteLine("Loading startup jobs");
            StartupLog.Write("Loading startup jobs");
            using (var startupJobsLocator = Container.GetLocator())
            {
                var jobs = startupJobsLocator.GetCollection<IStartupJob>();
                var logger = startupJobsLocator.Get<ILog>();

                logger.Info("Starting startup jobs");
                foreach (var job in jobs)
                {
                    try
                    {
                        logger.Info($"Starting job {job.GetType().Name}");
                        StartupLog.Write($"Startup job {job.GetType().Name} started");

                        job.Execute();

                        logger.Info($"Job {job.GetType().Name} complete");
                        StartupLog.Write($"Startup job {job.GetType().Name} complete");
                    }
                    catch (Exception ex)
                    {
                        logger.Error($"StartupJob {job.GetType().Name} failed", ex);
                        StartupLog.Write($"Startup job {job.GetType().Name} failed");
                        if (job.IsExceptionFatal)
                        {
                            throw;
                        }
                    }
                }
                logger.Info("Startup jobs done");
            }

            Debug.WriteLine("Application startup complete");
            StartupLog.Write("Application startup complete");
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