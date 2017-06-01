using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using IFramework.AspNet;

[assembly: PreApplicationStartMethod(typeof(ProfileHandler), "Init")]

namespace IFramework.AspNet
{
    public class ProfileHandler : IHttpHandler
    {
        public ProfileHandler()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        public static void Init()
        {
            RouteTable.Routes.IgnoreRoute("profile");
        }

        /// <summary>
        ///     You will need to configure this handler in the Web.config file of your
        ///     web and register it with IIS before being able to use it. For more information
        ///     see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>

        #region IHttpHandler Members private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _ramCounter;

        private readonly PerformanceCounter _cpuCounter;

        public float CurrentCpuUsage => _cpuCounter.NextValue();

        public float AvailableRAM => _ramCounter.NextValue();


        public bool IsReusable => true;

        public void ProcessRequest(HttpContext context)
        {
            var availableWorkThreads = 0;
            var availableCompletionPortThreads = 0;
            var maxWorkerThreads = 0;
            var maxCompletionPortThreads = 0;

            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            ThreadPool.GetAvailableThreads(out availableWorkThreads, out availableCompletionPortThreads);

            //write your handler implementation here.
            context.Response.Write(
                                   $"CurrentCpuUsage:{CurrentCpuUsage} AvailableRAM:{AvailableRAM} work threads:{availableWorkThreads}/{maxWorkerThreads} completionPortThreads:{availableCompletionPortThreads}/{maxCompletionPortThreads} ");
            var ipv4 = Dns.GetHostEntry(Dns.GetHostName())
                          .AddressList
                          .First(x => x.AddressFamily == AddressFamily.InterNetwork);

            context.Response.Write($"host ip: {ipv4}");
        }

        #endregion
    }
}