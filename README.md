![Icon](http://i.imgur.com/WnKfKOC.png?1) 
# Nanophone
Nanophone is a minimalistic library for Service Registration and Discovery, heavily inspired by Roger Johansson's [Microphone](https://github.com/rogeralsing/Microphone) library

##Features:
* Find available service instances by service name
* Find available service instances by service name and version
* Extensible service registry host - includes [Consul](https://www.consul.io/) host
* Extensible service registry tenants - includes [Nancy](https://github.com/NancyFx/Nancy) and Web Api tenants

##Usage:

* Find available service instances by service name:
~~~~
using System.Threading.Tasks;
using Nanophone.Core;
using Nanophone.RegistryHost.ConsulRegistry;

var serviceRegistry = new ServiceRegistry();
serviceRegistry.StartClient(new ConsulRegistryHost());

var instances = serviceRegistry.FindServiceInstancesAsync("my-service-name").Result;
foreach (var instance in instances)
{
    Console.WriteLine($"Address: {instance.Address}:{instance.Port}, Version: {instance.Version}");
}
~~~~

* Find available service instances by service name and version:
~~~~
using System.Threading.Tasks;
using Nanophone.Core;
using Nanophone.RegistryHost.ConsulRegistry;

var serviceRegistry = new ServiceRegistry();
serviceRegistry.StartClient(new ConsulRegistryHost());

var instances = serviceRegistry.FindServiceInstancesWithVersionAsync("my-service-name", "1.2").Result;
foreach (var instance in instances)
{
    Console.WriteLine($"Address: {instance.Address}:{instance.Port}, Version: {instance.Version}");
}
~~~~

* Start Nancy service:
~~~~
using Nanophone.Core;
using Nanophone.RegistryHost.ConsulRegistry;
using Nanophone.RegistryTenant.Nancy;

var serviceRegistry = new ServiceRegistry();
serviceRegistry.Start(new NancyRegistryTenant(new Uri("http://localhost:9001")), new ConsulRegistryHost(),
    "customers", "v1");
~~~~

* Start Web Api service:
~~~~
using Microsoft.Owin.Hosting;
using Nanophone.Core;
using Nanophone.RegistryHost.ConsulRegistry;
using Nanophone.RegistryTenant.WebApi;

string url = "http://localhost:9000/";

var serviceRegistry = new ServiceRegistry();
serviceRegistry.Start(new WebApiRegistryTenant(new Uri(url)), new ConsulRegistryHost(), 
    "date", "1.7-pre");

WebApp.Start<Startup>(url);
~~~~

##Thanks
* [SIM Card](https://thenounproject.com/term/sim-card/15160) icon by misirlou from [The Noun Project](https://thenounproject.com)
