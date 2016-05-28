﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Logging;
using Consul;
using Nanophone.Core;
using Newtonsoft.Json;

namespace Nanophone.RegistryProvider.ConsulRegistry
{
    public class ConsulRegistryProvider : IRegistryProvider
    {
        private static readonly ILog s_log = LogManager.GetLogger<ConsulRegistryProvider>();

        private readonly ConsulRegistryProviderConfiguration _configuration;
        private readonly ConsulClient _consul;

        private void StartRemovingCriticalServices()
        {
            Task.Factory.StartNew(async () =>
            {
                await Task.Delay(_configuration.ReaperDelay);
                s_log.Info("Starting to remove services in critical state");

                while (true)
                {
                    try
                    {
                        // deregister critical services
                        var queryResult = await _consul.Health.State(CheckStatus.Critical);
                        var criticalServiceIds = queryResult.Response.Select(x => x.ServiceID);
                        foreach (var serviceId in criticalServiceIds)
                        {
                            await _consul.Agent.ServiceDeregister(serviceId);
                        }
                    }
                    catch (Exception ex)
                    {
                        s_log.Error(ex);
                    }

                    await Task.Delay(_configuration.ReaperInterval);
                }
            });
        }

        public ConsulRegistryProvider(ConsulRegistryProviderConfiguration configuration)
        {
            _configuration = configuration;

            if (_configuration.ReaperDelay == TimeSpan.MinValue)
            {
                _configuration.ReaperDelay = TimeSpan.FromSeconds(10);
            }
            if (_configuration.ReaperInterval == TimeSpan.MinValue)
            {
                _configuration.ReaperInterval = TimeSpan.FromSeconds(5);
            }

            _consul = new ConsulClient();
        }

        public async Task<RegistryInformation[]> FindServiceInstancesAsync(string name)
        {
            var queryResult = await _consul.Health.Service(name);
            var results = queryResult.Response
                .Select(serviceEntry =>
                {
                    string serviceAddress = serviceEntry.Service.Address;
                    int servicePort = serviceEntry.Service.Port;
                    // XXX tags
                    return new RegistryInformation(serviceAddress, servicePort);
                });

            return results.ToArray();
        }

        public async Task RegisterServiceAsync(string serviceName, string serviceId, string version, Uri uri)
        {
            var localIp = DnsHelper.GetLocalIpAddress();
            var check = "http://" + localIp + ":" + uri.Port + "/status";

            s_log.Info($"Registering service on {localIp} on Consul {_configuration.ConsulHost}:{_configuration.ConsulPort} with status check {check}");
            var registration = new AgentServiceRegistration
            {
                ID = serviceId,
                Name = serviceName,
                Tags = new [] { $"urlprefix-/{serviceName}" },
                Address = localIp,
                Port = uri.Port,
                Check = new AgentServiceCheck { HTTP = check, Interval = TimeSpan.FromSeconds(1) }
            };
            await _consul.Agent.ServiceRegister(registration);
            s_log.Info($"Registration succeeded");

            StartRemovingCriticalServices();
        }

        public Task BootstrapClientAsync()
        {
            StartRemovingCriticalServices();
            return Task.FromResult(0);
        }

        public async Task KeyValuePutAsync(string key, object value)
        {
            var serialized = JsonConvert.SerializeObject(value);
            var keyValuePair = new KVPair(key) { Value = Encoding.UTF8.GetBytes(serialized) };
            await _consul.KV.Put(keyValuePair);
        }

        public async Task<T> KeyValueGetAsync<T>(string key)
        {
            var queryResult = await _consul.KV.Get(key);
            var serialized = Encoding.UTF8.GetString(queryResult.Response.Value, 0, queryResult.Response.Value.Length);
            var result = JsonConvert.DeserializeObject<T>(serialized);

            return result;
        }
    }
}