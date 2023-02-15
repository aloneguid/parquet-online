using KristofferStrube.Blazor.FileSystemAccess;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using ParquetViewer;

#if DEBUG
Tracker.Instance = new Tracker("parquetdbg", "dev");
#else
Tracker.Instance = new Tracker("parquetdbg", Config.Version);
#endif
Tracker.Instance.Constants["parquet.net"] = Parquet.Globals.Version;

await Tracker.Instance.Track("start");

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddFileSystemAccessService();

await builder.Build().RunAsync();
