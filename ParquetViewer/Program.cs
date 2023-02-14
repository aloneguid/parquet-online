using KristofferStrube.Blazor.FileSystemAccess;
using LP.Domain;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ParquetViewer;

Tracker.Instance = new Tracker("parquetdbg", Parquet.Globals.Version);
await Tracker.Instance.Track("start");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddFileSystemAccessService();

await builder.Build().RunAsync();
