using Domain.Implementation;
using Domain.Interface;
using Domain.Models;
using Domain.ServiceAPI;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NuGet.Configuration;
using System.Text;
using System.Text.Json.Serialization;

//var builder = WebApplication.CreateBuilder(args);
var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
	Args = args,
	WebRootPath = "wwwroot"
});
// Add services to the container.

builder.Services.AddCors(options =>
{
	options.AddPolicy("AllowSpecificOrigin",
		builder => builder.WithOrigins("https://localhost:44316") // Update this URL
						  .AllowAnyMethod()
						  .AllowAnyHeader()
						  .AllowCredentials());
});

// Add HTTP client for API consumption
builder.Services.AddHttpClient("NURAAPI", client =>
{
	client.BaseAddress = new Uri("https://localhost:44316/api/");
});

// Register YourService with the HTTP client
//builder.Services.AddHttpClient<YourService>();

var connectionString = builder.Configuration.GetConnectionString("NuraConnection")
	?? throw new InvalidOperationException("Connection string 'NuraConnection' not found.");

builder.Services.AddDbContext<NuraDbContext>(options =>
	options.UseSqlServer(connectionString));
// Configure Identity

builder.Services.AddIdentityCore<RegisterUser>(options => { })
	.AddRoles<IdentityRole>()
	.AddEntityFrameworkStores<NuraDbContext>()
	.AddSignInManager()
	.AddDefaultTokenProviders();

// Configure JWT authentication
builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["JWT:ValidIssuer"],
		ValidAudience = builder.Configuration["JWT:ValidAudience"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JWT:Secret"]))
	};
});
builder.Services.AddDistributedMemoryCache();  // Required for session state
builder.Services.AddSession(options =>
{
	options.IdleTimeout = TimeSpan.FromMinutes(30);  // Set session timeout as needed
	options.Cookie.HttpOnly = true;  // Ensure cookies are HTTP-only for security
	options.Cookie.IsEssential = true;  // Set session cookie to be essential
});
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
// Add services to the container
builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.PropertyNamingPolicy = null; // Disable camel casing if needed
		options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // For enum types if any
	});
//builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
//builder.Services.AddScoped<VodafoneParser>();
//builder.Services.AddScoped<AirtelParser>();
//builder.Services.AddScoped<BSNLParser>();
//builder.Services.AddScoped<JioParser>();
//builder.Services.AddScoped<NetworkParserFactory>();
// Register the Message Service
builder.Services.AddScoped<IAdmin, AdminService>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IEmailService, EmailService>();
// Configure Swagger
builder.Services.AddSwaggerGen(c =>
{
	c.SwaggerDoc("v1", new OpenApiInfo { Title = "Nura Herbex API", Version = "v1" });

	// Security Definition for JWT
	c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
	{
		Name = "Authorization",
		Type = SecuritySchemeType.ApiKey,
		Scheme = "Bearer",
		BearerFormat = "JWT",
		In = ParameterLocation.Header,
		Description = "Enter 'Bearer' [space] and then your token.\r\n\r\nExample: 'Bearer 12345abcdef'"
	});

	// Security Requirement
	c.AddSecurityRequirement(new OpenApiSecurityRequirement
	{
		{
			new OpenApiSecurityScheme
			{
				Reference = new OpenApiReference
				{
					Type = ReferenceType.SecurityScheme,
					Id = "Bearer"
				}
			},
			new string[] {}
		}
	});
});
builder.Services.AddAuthorization();
var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI(c =>
{
	c.SwaggerEndpoint("/swagger/v1/swagger.json", "Nura Herbex API v1");
	c.RoutePrefix = string.Empty; // Makes Swagger UI available at root
	c.ConfigObject.AdditionalItems["persistAuthorization"] = "true"; // Persists JWT token
});
//}

// Middleware for CORS
app.UseCors("AllowSpecificOrigin");
//app.UseStaticFiles();
app.UseSession();
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
