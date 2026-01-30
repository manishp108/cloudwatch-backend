using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
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
            services.AddControllers();

            // ✅ ADD SWAGGER
            services.AddSwaggerGen();
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
