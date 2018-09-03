using log4net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using WhatSearch.Core;
using WhatSearch.Middlewares;
using WhatSearch.Utility;

namespace WhatSearch
{
    public class Startup
    {
        private ILog logger = LogManager.GetLogger(typeof(Startup));

        public Startup(IHostingEnvironment env)
        {
            logger.Info("Startup");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.ContractResolver
                        = new CamelCasePropertyNamesContractResolver();
                });
            
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            var config = Ioc.GetConfig();
            
            if (config.IsDebug)
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler(new ExceptionHandlerOptions()
                {
                    ExceptionHandler = async context =>
                    {
                        context.Response.Redirect("/error");
                    }
                });
            }


            app.UseDeveloperExceptionPage();
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(Helper.GetRelativePath("contents")),
                RequestPath = new PathString(""),
                ServeUnknownFileTypes = true
            });
            //app.UseHsts()

            app.UseMiddleware<SecurityHeadersMiddleware>();

            app.UseMvc();
        }
    }
}
