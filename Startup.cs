using Azure.Identity;
using Azure.Storage.Blobs;
using BackEnd.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BackEnd
{
    public class Startup 
    {
        private readonly IConfiguration _configuration;          // Stores application configuration for access throughout Startup

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;              // Assign injected configuration instance
            Console.WriteLine("Startup constructor called.");
        }

        public IConfiguration Configuration { get; }

        // Add services to the container
        public void ConfigureServices(IServiceCollection services)          // Add services to the container
        {
            try
            {
                Console.WriteLine("Starting ConfigureServices...");

                var credentialOptions = new DefaultAzureCredentialOptions
                {
                    ExcludeVisualStudioCredential = true,
                    ExcludeAzureCliCredential = false,
                    ExcludeEnvironmentCredential = true,
                    ExcludeManagedIdentityCredential = true,
                    ExcludeSharedTokenCacheCredential = true,
                    ExcludeInteractiveBrowserCredential = true,
                    TenantId = "f6d006d0-5280-44ab-9fa1-85c211e2ab03"              // Specify Azure AD tenant for authentication
                };

               
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ConfigureServices: {ex}");
                throw;
            }
        }

        // Configure the HTTP request pipeline
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();

                // ✅ ENABLE SWAGGER
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "BackEnd API v1");
                });
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
