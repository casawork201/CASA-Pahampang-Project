using System.Security.Claims;
using System.Text;
using System.Text.Json;
using CASAPahampang.Components;
using CASAPahampang.Data;
using CASAPahampang.Hubs;
using CASAPahampang.Interfaces;
using CASAPahampang.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TestWASM.AuthLib.Models;
using TestWASM.AuthLib.Services;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddSignalR();
builder.Services.AddHttpClient();

builder.Services.AddRazorComponents()
    .AddInteractiveWebAssemblyComponents();

// 🔑 Bind the Jwt section to your options class so IOptions<JwtAuthOptionsDto> works correctly
builder.Services.Configure<JwtAuthOptionsDto>(builder.Configuration.GetSection("Jwt"));

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

var jwt = builder.Configuration.GetSection("Jwt");
var rawKey = jwt["Key"]?.Trim() ?? throw new InvalidOperationException("JWT Key is missing!");
var key = Encoding.UTF8.GetBytes(rawKey);

builder.Services.AddScoped<AuthService>();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/";
})
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
        RoleClaimType = ClaimTypes.Role,
        ClockSkew = TimeSpan.Zero
    };

    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;

            if (!string.IsNullOrEmpty(accessToken) && 
            (
                path.StartsWithSegments("/teamhub") ||
                path.StartsWithSegments("/bingohub") ||
                path.StartsWithSegments("/volleyballhub") ||
                path.StartsWithSegments("/basketballhub") ||
                path.StartsWithSegments("/chathub") ||
                path.StartsWithSegments("/matchhub") ||
                path.StartsWithSegments("/sportshub")))
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
app.MapGet("/blazor/signin", async (HttpContext context, IOptions<JwtAuthOptionsDto> jwtOptions) =>
{
    // Read parameters from the Query string since the browser navigation is a GET request
    var token = context.Request.Query["Token"].ToString();
    var refreshToken = context.Request.Query["RefreshToken"].ToString();
    var email = context.Request.Query["Email"].ToString();

    if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(email))
        return Results.BadRequest("Missing token or username.");
    
    var validation = JwtValidator.ValidateJwt(
        token,
        jwtOptions.Value.Issuer,
        jwtOptions.Value.Audience,
        jwtOptions.Value.Key
    );

    if (!validation.IsValid) 
        return Results.BadRequest($"Invalid token: {validation.Error}");

    var jwtToken = validation.Token!;
    var roles = jwtToken.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
    var uid = jwtToken.Claims.FirstOrDefault(c => c.Type == "uid")?.Value ?? "";

    var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, email),
        new Claim("uid", uid),
        new Claim("JWT", token),
        new Claim("RefreshToken", refreshToken)
    };
    claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
    var principal = new ClaimsPrincipal(identity);

    await context.SignInAsync(
        CookieAuthenticationDefaults.AuthenticationScheme,
        principal,
        new AuthenticationProperties
        {
            IsPersistent = true,
            AllowRefresh = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        }
    );

    context.Session.SetString("AccessToken", token);
    context.Session.SetString("RefreshToken", refreshToken);
    context.Session.SetString("UserRoles", System.Text.Json.JsonSerializer.Serialize(roles));
    context.Session.SetString("Email", email);

    return Results.Redirect("/");
});

app.MapRazorComponents<App>()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(CASAPahampang.Client._Imports).Assembly);

app.MapHub<BingoHub>("/bingohub");
app.MapHub<SportsHub>("/sportshub");
app.MapHub<ChatHub>("/chathub");
app.MapHub<TeamHub>("/teamhub");
app.MapHub<MatchHub>("/matchhub");

app.Run();