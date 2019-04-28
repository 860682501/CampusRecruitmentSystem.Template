using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using ContinuousIntegration_Template.Services;
using Microsoft.Extensions.Logging;

namespace ContinuousIntegration_Template
{
    public class Program
    {
        public static int PORT;  // 【+】 1 端口参数变量-从Terminal(命令行)获取，传给Startup [dotnet YourProgramName.dll --port PortNumber]


        public static void Main(string[] args)
        {
            Console.Title = "YourProgramName";   // 【+】 0 设置你的Terminal标题（可以不写）
            CreateWebHostBuilder(args).Build().Run();
        }



        /* public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>  // 这是箭头函数
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();  */
        // 【+】 👇 2 修改CreateWebHostBuilder方法
        public static IWebHostBuilder CreateWebHostBuilder(string[] args) 
        {
            //  // 【+】 3 获取Terminal键入的数据
            var config = new ConfigurationBuilder().AddCommandLine(args).Build();
            PORT = Convert.ToInt32(config["port"]);  //  [dotnet YourProgramName.dll --port PortNumber]
            if (PORT == 0)  // 如果未输入端口号则随机分配一个
            {
                PORT = GetRandAvailablePort();
            }

            return WebHost.CreateDefaultBuilder(args)
                   .UseUrls($"http://*:{PORT}")    // 【+】 4 项目启动时设定你系统的ip和端口号
                   .UseStartup<Startup>();
        }



        // 【+】 5 如果未输入端口号则随机分配一个  #注意# 实际部署一定要写你的端口号
        public static int GetRandAvailablePort(int minPort = 1024, int maxPort = 65535)   // 产生一个介于minPort-maxPort之间的随机可用端口
        {
            Random rand = new Random();
            while (true) { int port = rand.Next(minPort, maxPort); if (!IsPortInUsed(port)) { return port; } }
        }
        public static bool IsPortInUsed(int port)  // 判断port端口是否在使用中
        {
            // using System.Net.NetworkInformation; 
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            IPEndPoint[] ipsTCP = ipGlobalProperties.GetActiveTcpListeners();
            if (ipsTCP.Any(p => p.Port == port)) { return true; }
            IPEndPoint[] ipsUDP = ipGlobalProperties.GetActiveUdpListeners();
            if (ipsUDP.Any(p => p.Port == port)) { return true; }
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();
            if (tcpConnInfoArray.Any(conn => conn.LocalEndPoint.Port == port)) { return true; }
            return false;
        }
    }
}
