using CSweet.Agent.SDK;
using CSweet.Agents.ProductManager;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateApplicationBuilder(args);

var manifest = await AgentManifestLoader.LoadAsync("csweet-plugin.json", CancellationToken.None);
if (manifest.Id != ProductManagerProfile.AgentId || manifest.Version != ProductManagerProfile.Version)
    throw new InvalidOperationException("The Product Manager implementation identity does not match csweet-plugin.json.");

builder.AddCSweetAgent<ProductManagerAgent>();
builder.Services.AddSingleton<ProductManagerOrchestrator>();

var host = builder.Build();
host.Run();
