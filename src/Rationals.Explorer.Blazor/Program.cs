using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Rationals.Explorer.Blazor;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
// needed for e.g. <PageTitle> и <HeadContent>
//builder.RootComponents.Add<HeadOutlet>("head::after");

// needed e.g. for FetchData.razor or @inject HttpClient Http
//builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
