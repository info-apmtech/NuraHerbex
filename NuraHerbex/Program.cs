using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using Domain.Implementation;
using Domain.Interface;
using Domain.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ServiceStack.Configuration;
using System.Net.Http.Headers;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);
//var connectionString = builder.Configuration.GetConnectionString("NuraConnection")
//    ?? throw new InvalidOperationException("Connection string 'NuraConnection' not found.");

//builder.Services.AddDbContext<NuraDbContext>(options =>
//    options.UseSqlServer(connectionString));
//builder.Services.AddDatabaseDeveloperPageExceptionFilter();
//builder.Services.ConfigureApplicationCookie(options =>
//{ 
//	options.LoginPath = "/Authentication/Login";
//	options.LogoutPath = "/Authentication/Logout";
//	options.AccessDeniedPath = "/Authentication/AccessDenied";
//	options.ExpireTimeSpan = TimeSpan.FromHours(8);
//	options.SlidingExpiration = true;
//});

// Add services to the container.
builder.Services.AddControllersWithViews()
    .AddJsonOptions(options =>
    {

        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());

        options.JsonSerializerOptions.PropertyNamingPolicy = null;

        options.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });
//builder.Services.AddScoped<IAdmin, AdminService>();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();


builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
	options.LoginPath = "/Authentication/Login";
	options.LogoutPath = "/Authentication/Logout";
	options.ExpireTimeSpan = TimeSpan.FromHours(8);
	options.SlidingExpiration = true;
});
//.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
//{
//	options.RequireHttpsMetadata = false;
//	options.SaveToken = true;
//	options.TokenValidationParameters = new TokenValidationParameters
//	{
//		ValidateIssuer = true,
//		ValidateAudience = true,
//		ValidateLifetime = true,
//		ValidateIssuerSigningKey = true,
//		ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
//		ValidAudience = builder.Configuration["JWT:ValidAudience"],
//		IssuerSigningKey = new SymmetricSecurityKey(
//			Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
//	};
//});

builder.Services.AddAuthorization();
//builder.Services.AddIdentity<RegisterUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = false)
//    .AddRoles<IdentityRole>()
//    .AddDefaultTokenProviders()
//    .AddEntityFrameworkStores<NuraDbContext>();
builder.Services.AddHttpClient("NuraHerbexApi", client =>
{
	client.BaseAddress = new Uri(builder.Configuration["ApiBaseUrl"]);
	client.DefaultRequestHeaders.Accept.Add(
		new MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddNotyf(config => { config.DurationInSeconds = 10; config.IsDismissable = true; config.Position = NotyfPosition.TopRight; });
builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowAll", policy =>
	{
		policy.AllowAnyOrigin()
			  .AllowAnyHeader()
			  .AllowAnyMethod();
	});
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);
	options.Cookie.HttpOnly = true;
	options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();
var app = builder.Build();
// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
	app.UseMigrationsEndPoint();
}
else
{
	app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("AllowAll");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapStaticAssets();
app.UseNotyf();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
