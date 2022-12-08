using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Serilog;
using AuctionPlatform.BidsService.Common;
using AuctionPlatform.BidsService.Infrastructure;
using AuctionPlatform.Contract.Interfaces;
using AuctionPlatform.Contract.Models.Authentication;
using AuctionPlatform.ResourceAccess.EntityFramework;

namespace AuctionPlatform.BidsService.WebApi;

internal class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(_ => Log.Logger);

        // Register data access layer and its supporting infrastructure.
        services.AddScoped<IUserSession, UserSession>();
        services.AddDbContextFactory<SaDbContext>(options => options.UseSqlServer(Configuration[GlobalConsts.SettingNames.SqlConnectionString],
                                                  providerOptions => providerOptions.EnableRetryOnFailure()));
        services.AddScoped<IDbContext, SaDbContext>();

        services.AddDomainValidators();
        services.AddDomainTransforms();

        services.AddControllers().AddNewtonsoftJson();

        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Auction Platform Bidding Service API", Version = "v1" });

            var docFilePath = Path.Combine(AppContext.BaseDirectory, "BidsService.WebApi.xml");
            c.IncludeXmlComments(docFilePath);
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment() || env.IsEnvironment(GlobalConsts.CustomDevelopmentEnvironmentName))
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "AuctionPlatform.BidsServiceApi v1"));
        }

        app.UseRouting();
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}
