using log4net;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.IO;
using WhatSearch.Core;
using WhatSearch.Middlewares;
using WhatSearch.Utility;

namespace WhatSearch
{
    public class Startup
    {
        private ILog logger = LogManager.GetLogger(typeof(Startup));

        public Startup(IWebHostEnvironment env)
        {
            logger.Info("Startup");
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddCors(cfg =>
            //{
            //    cfg.AddPolicy("AllowMyOrigin",
            //        builder => builder.WithOrigins("http://localhost:7777", "http://uni2.tw:7777"));
            //});
            services.AddMvc()
                .AddNewtonsoftJson(options =>
                    options.SerializerSettings.ContractResolver = new
                    CamelCasePropertyNamesContractResolver())
                .AddJsonOptions(options => options.JsonSerializerOptions.PropertyNameCaseInsensitive = true);
            //services.AddMvc((options) => { options.SerializerOptions.WriteIndented = true; })
            //    .AddJsonOptions(options =>
            //    {
            //        options.SerializerSettings.Formatting = Formatting.Indented;
            //        options.SerializerSettings.ContractResolver
            //            = new CamelCasePropertyNamesContractResolver();
            //    });
            services.AddHttpContextAccessor();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime appLifetime)
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


            if (Directory.Exists(config.ContentsFolder) == false)
            {
                throw new Exception("ContentsFolder not fonnd, value=" + config.ContentsFolder);
            }
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = new PhysicalFileProvider(config.ContentsFolder),
                RequestPath = new PathString("/contents"),
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

            app.UseRouting();
            app.UseEndpoints(config =>
            {
                config.MapControllers();
            });


        }
    }
}
