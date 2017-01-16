
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Net;
using System.Linq;
using System.Net.Sockets;
using IFramework.AspNet;
using System.Diagnostics;

[assembly: PreApplicationStartMethod(typeof(ProfileHandler), "Init")]

namespace IFramework.AspNet
{


    public class ProfileHandler : IHttpHandler
    {
        public static void Init()
        {
            RouteTable.Routes.IgnoreRoute("profile");
        }

        public ProfileHandler()
        {
            _cpuCounter = new PerformanceCounter();
            _cpuCounter.CategoryName = "Processor";
            _cpuCounter.CounterName = "% Processor Time";
            _cpuCounter.InstanceName = "_Total";
            _ramCounter = new PerformanceCounter("Memory", "Available MBytes");
        }

        /// <summary>
        /// You will need to configure this handler in the Web.config file of your 
        /// web and register it with IIS before being able to use it. For more information
        /// see the following link: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        #region IHttpHandler Members

        PerformanceCounter _cpuCounter;
        PerformanceCounter _ramCounter;
     
        public float CurrentCpuUsage
        {
            get
            {
                return _cpuCounter.NextValue(); 
            }
        }

        public float AvailableRAM
        {
            get
            {
                return _ramCounter.NextValue();
            }
        }




        public bool IsReusable
        {
            // Return false in case your Managed Handler cannot be reused for another request.
            // Usually this would be false in case you have some state information preserved per request.
            get { return true; }
        }

        public void ProcessRequest(HttpContext context)
        {
            var availableWorkThreads = 0;
            var availableCompletionPortThreads = 0;
            var maxWorkerThreads = 0;
            var maxCompletionPortThreads = 0;

            ThreadPool.GetMaxThreads(out maxWorkerThreads, out maxCompletionPortThreads);
            ThreadPool.GetAvailableThreads(out availableWorkThreads, out availableCompletionPortThreads);

            //write your handler implementation here.
            context.Response.Write($"CurrentCpuUsage:{CurrentCpuUsage} AvailableRAM:{AvailableRAM} work threads:{availableWorkThreads}/{maxWorkerThreads} completionPortThreads:{availableCompletionPortThreads}/{maxCompletionPortThreads} ");
            var ipv4 = Dns.GetHostEntry(Dns.GetHostName()).AddressList.First(x => x.AddressFamily == AddressFamily.InterNetwork);

            context.Response.Write($"host ip: {ipv4}");
        }

        #endregion
    }
}
