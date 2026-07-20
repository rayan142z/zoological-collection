using System.Text;
using Zoolog;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
        {
            // Verhindert den Absturz bei zyklischen Tabellen-Beziehungen
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        });
builder.Services.AddOpenApi();

var allowedOriginsConfig = builder.Configuration.GetValue<string>("allowedOrigins") ?? "http://localhost:4200";
var allowedOrigins = allowedOriginsConfig.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddDbContext<Group6DbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "Zoolog";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ZoologClient";

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT key is missing. Set it with: dotnet user-secrets set \"Jwt:Key\" \"your-long-secret-key\"");
}

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // for development
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromMinutes(2)
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .WithMethods("GET", "POST", "PUT", "DELETE")
            .AllowCredentials();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<Group6DbContext>();
    var connection = dbContext.Database.GetDbConnection();
    
    Console.WriteLine("==================================================");
    Console.WriteLine("[DB-DIAGNOSE] Backend ist gestartet!");
    Console.WriteLine($"[DB-DIAGNOSE] Provider: {dbContext.Database.ProviderName}");
    Console.WriteLine($"[DB-DIAGNOSE] Datenbank: {connection.Database}");
    Console.WriteLine($"[DB-DIAGNOSE] DataSource/Server: {connection.DataSource}");
    Console.WriteLine($"[DB-DIAGNOSE] ConnectionString: {connection.ConnectionString}");
    Console.WriteLine("==================================================");
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    //app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    //app.UseHsts();
}

if (app.Environment.IsDevelopment())
{
    // OpenAPI JSON at /openapi/v1.json, browsable docs UI at /scalar/v1.
    // No "Authorize" token field wired in here - the .NET 10 security-scheme APIs
    // for that are currently broken upstream. Paste the Authorization header
    // manually in the request panel for protected endpoints, same as with curl.
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();

app.UseCors("AllowAngular");
app.UseStaticFiles();
//app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

//app.MapStaticAssets();

// app.MapControllerRoute(
//     name: "default",
//     pattern: "{controller=Home}/{action=Index}/{id?}")
//     .WithStaticAssets(); 

app.Run();
