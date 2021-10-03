using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Converters;

namespace FirmwareProviderAPI
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
            FirmwareIndexer.Init();
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers()
                .AddNewtonsoftJson(options =>
                { 
                    options.SerializerSettings.Converters.Add(new StringEnumConverter());
                });
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo {Title = "FirmwareProviderAPI", Version = "v1"});
                c.UseInlineDefinitionsForEnums();
                
                /* Hack to support enums in Swagger as query parameters properly */
                foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies())
                {
                    foreach (Type t in a.GetTypes())
                    {
                        if (t.IsEnum)
                        {
                            c.MapType(t, () => new OpenApiSchema
                            {
                                Type = "string",
                                Enum = t.GetEnumNames().Select(name => new OpenApiString(name)).Cast<IOpenApiAny>().ToList(),
                                Nullable = true
                            });
                        }
                    }
                }
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger(c => c.SerializeAsV2 = true);
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "FirmwareProviderAPI v1"));
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        }
    }
}