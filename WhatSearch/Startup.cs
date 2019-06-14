using log4net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
            services.AddMvc()
                .AddJsonOptions(options =>
                {
                    options.SerializerSettings.Formatting = Formatting.Indented;
                    options.SerializerSettings.ContractResolver
                        = new CamelCasePropertyNamesContractResolver();
                });
            services.AddHttpContextAccessor();
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
                        var errorFeature = context.Features.Get<IExceptionHandlerFeature>();
                        var exception = errorFeature.Error;

                        dynamic problemDetails = new
                        {
                            Title = "An unexpected error occurred!",
                            Status = 500,
                            Detail = exception.ToString()
                        };

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

            app.UseMiddleware<UserTrackingMiddleware>();

            app.UseMiddleware<CustomSecurityHeadersMiddleware>();
            app.UseMiddleware<UserAuthenticationMiddleware>();
            //app.MapWhen(context => context.Request.Path.ToString().EndsWith(".md"),
            //    appBuilder =>
            //    {
            //        appBuilder.UseCustomHanlderMiddleware();
            //    });
            app.MapWhen(context => context.Request.Path.ToString() == "/about",
                appBuilder =>
                {
                    appBuilder.UseCustomHanlderMiddleware();
                });
            
            WebContext.Configure(app.ApplicationServices
                                  .GetRequiredService<IHttpContextAccessor>());

            app.UseMvc();

            
        }
    }
}
