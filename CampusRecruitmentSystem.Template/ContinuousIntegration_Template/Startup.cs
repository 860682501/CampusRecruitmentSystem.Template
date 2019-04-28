using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Consul;
using ContinuousIntegration_Template.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace ContinuousIntegration_Template
{
    public class Startup
    {
        public IConfiguration Configuration { get; }
        public Startup(IConfiguration configuration)  // 【+】0 注入RestTemplate[这个是写的，在Service里面]
        {
            Configuration = configuration;
            RestTemplate rr = new RestTemplate();
        }




        public void ConfigureServices(IServiceCollection services)
        {
            // 【+】1  AddMvc -> AddMvcCore
            services.AddMvcCore()
                .SetCompatibilityVersion(CompatibilityVersion.Version_2_1)
                .AddAuthorization()  // 【+】2 IdentityServer4授权
                .AddJsonFormatters();  // 【+】3 Json
            

            //  【+】4 向权限中心注册这个服务，这样在不同用户访问你的API时就可以通过在Controller上添加[注解]轻松限制不同角色的访问
            services.AddAuthentication("Bearer")
                .AddJwtBearer("Bearer", options =>
                {
                    options.Authority = $"http://{GetIdentityServer()}";  // 请求IS4服务器地址
                    options.RequireHttpsMetadata = false;
                    options.Audience = "YourApiResourceName";  // 这里填写你服务的名称，
                    //  名称一定要是IS4服务中包含的"ApiResource"（具体名称可查看文档）
                });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime lifetime)  //【+】 6 注入软件生命周期用于服务结束时反注册
        {
            app.UseAuthentication();  // 【+】5 Use授权保护器
            app.UseMvc();
            RegisterInConsul(Program.PORT, lifetime);  //【+】8 向服务发现器Consul注册你的服务
        }

        //【+】7 consul 注册发现
        private void RegisterInConsul(int port, IApplicationLifetime lifetime)
        {
            // host 部署到不同服务器的时候不能写成127.0.0.1或者0.0.0.0，因为这是让服务消费者调用的地址
            var serviceName = "Your_Service_Name";
            var clientID = serviceName + Guid.NewGuid();  // 生成Guid随机数

            var client = new ConsulClient(ConfigurationOverview);   // using consul;
            var result = client.Agent.ServiceRegister(new AgentServiceRegistration()
            {
                ID = clientID,  //服务编号，不能重复
                Name = serviceName,  //服务的名字
                Address = "127.0.0.1",  //我的ip地址(可以被其他应用访问的地址，本地测试可以用127.0.0.1，部署环境中一定要写自己的外网ip地址)
                Port = port,  //我的端口号
                Check = new AgentServiceCheck
                {
                    DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(5),  //服务停止多久后反注册
                    Interval = TimeSpan.FromSeconds(10),  //健康检查时间间隔，或者称为心跳间隔
                    HTTP = $"http://39.106.206.130:{port}/api/health",  //健康检查地址
                    Timeout = TimeSpan.FromSeconds(5)
                }
            });
            lifetime.ApplicationStopping.Register(() =>
            {
                client.Agent.ServiceDeregister(clientID).Wait();  // 服务停止时反注册
            });
        }
        private static void ConfigurationOverview(ConsulClientConfiguration obj)
        {
            obj.Address = new Uri("http://39.106.206.130:8500");  // 服务注册与发现服务器Consul地址
            obj.Datacenter = "dc1";  // 名称
        }

        //【+】4 获取IS4地址
        public string GetIdentityServer()  // 负载均衡 获取IS4地址 使用VPN响应会很慢
        {
            var st = new RestTemplate().ResolveRootUrlAsync("IdentityServer4");
            st.Wait();
            var aat = st.Result;
            return aat;
        }
    }
}
