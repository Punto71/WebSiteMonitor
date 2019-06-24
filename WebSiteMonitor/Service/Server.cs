using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using Microsoft.Owin.Hosting;
using WebSiteMonitor.Service.Models;
using System;
using WebSiteMonitor.Service.Auth;

namespace WebSiteMonitor.Service {
    public class Server {

        public static IDisposable Start(int port, string ip = "*") {
            SessionManager.Instance.Run();
            var baseAddress = string.Format("http://{0}:{1}", ip, port);
            Logger.AddInfo("Starting web server...");
            var server = WebApp.Start<ServerConfig>(url: baseAddress);
            Logger.AddInfo("Server started. Access url: " + baseAddress);
            Logger.AddInfo("Starting ping workers...");
            WebSiteItem.GetAll().ForEach(t => PingWorker.Instance.AddWebSite(t));
            Logger.AddInfo("All ping workers started.");
            return server;
        }
    }
}
