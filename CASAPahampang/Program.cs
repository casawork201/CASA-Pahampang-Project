using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CASAPahampang.Components;
using CASAPahampang.Data;
using CASAPahampang.Hubs;
using CASAPahampang.Interfaces;
using CASAPahampang.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TestWASM.AuthLib.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.Configure<Microsoft.AspNetCore.SignalR.HubOptions>(options =>
{
    options.MaximumReceiveMessageSize = 100 * 1024 * 1024;
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
        options.SerializerSettings.ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore);

// --- JWT Bearer validation (must match AuthGateway's Issuer/Audience/Key exactly) ---
// var jwt = builder.Configuration.GetSection("Jwt");
// var key = Encoding.UTF8.GetBytes(jwt["Key"]!);
// --- JWT Bearer validation ---
var jwt = builder.Configuration.GetSection("Jwt");
var rawKey = jwt["Key"]?.Trim() ?? throw new InvalidOperationException("JWT Key is missing!");
var key = Encoding.UTF8.GetBytes(rawKey);
builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = jwt["Issuer"]?.Trim(),
            ValidAudience = jwt["Audience"]?.Trim(),
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateLifetime = true,
            RoleClaimType = ClaimTypes.Role, // 🔑 Aligned with AuthGateway
            ClockSkew = TimeSpan.Zero
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                // 🔑 Updated to handle all registered SignalR hubs
                if (!string.IsNullOrEmpty(accessToken) && 
                   (path.StartsWithSegments("/teamhub") ||
                    path.StartsWithSegments("/bingohub") ||
                    path.StartsWithSegments("/volleyballhub") ||
                    path.StartsWithSegments("/basketballhub") ||
                    path.StartsWithSegments("/chathub")) ||
                    path.StartsWithSegments("/matchhub") ||
                    path.StartsWithSegments("/sportshub"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            },

            OnChallenge = context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var errorDetails = new
                {
                    Message = "Unauthorized access",
                    Error = context.Error,
                    Description = context.ErrorDescription
                };

                return context.Response.WriteAsync(JsonSerializer.Serialize(errorDetails));
            }
        };
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("is-admin", policy =>
    {
        policy.AuthenticationSchemes.Add(JwtBearerDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
        policy.RequireRole("Admin");
    });
});
builder.Services.AddHttpClient<IContentModerationService, ContentModerationService>();
builder.Services.AddAntiforgery();

var app = builder.Build();

app.UseForwardedHeaders();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
else
{
    app.UseWebAssemblyDebugging();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapControllers();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CASAPahampang.Client._Imports).Assembly);
app.MapHub<BingoHub>("/bingohub");
// app.MapHub<VolleyballHub>("/volleyballhub");
// app.MapHub<BasketballHub>("/basketballhub");
app.MapHub<SportsHub>("/sportshub");
app.MapHub<ChatHub>("/chathub");
app.MapHub<TeamHub>("/teamhub");
app.MapHub<MatchHub>("/matchhub");
app.Run();