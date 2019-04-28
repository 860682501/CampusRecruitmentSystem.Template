using Consul;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContinuousIntegration_Template.Services
{
    public class RestTemplate
    {
        private String consulServerUrl;
        public RestTemplate(String consulServerUrl = "http://39.106.206.130:8500")  // 服务发现Consul地址
        {
            this.consulServerUrl = consulServerUrl;
        }

        // 获取服务地址 serviceName-服务名称
        public async Task<String> ResolveRootUrlAsync(String serviceName)
        {
            using (var consulClient = new ConsulClient(c => c.Address = new Uri(consulServerUrl)))
            {
                var services = (await consulClient.Agent.Services()).Response;
                var agentServices = services.Where(s => s.Value.Service.Equals(serviceName, StringComparison.CurrentCultureIgnoreCase))
                .Select(s => s.Value);
                //TODO:注入负载均衡策略
                try
                {
                    var agentService = agentServices.ElementAt(Environment.TickCount % agentServices.Count());
                    return agentService.Address + ":" + agentService.Port;
                } catch
                {
                    throw new Exception("此Service不存在");
                }
                //根据当前TickCount对服务器个数取模，“随机”取一个机器出来，避免“轮询”的负载均衡策略需要计数加锁问题
            }
        }
    }
}
