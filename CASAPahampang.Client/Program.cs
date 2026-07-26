using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using TestWASM.AuthLib.Extensions;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddClientAuth(
    hostBaseAddress: builder.HostEnvironment.BaseAddress,
    authApiBaseUrl: "https://casa-authgateway-service.taila207b7.ts.net/"
);

await builder.Build().RunAsync();