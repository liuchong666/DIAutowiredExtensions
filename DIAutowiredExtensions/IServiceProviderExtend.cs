using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DIAutowiredExtensions
{
    public static class IServiceProviderExtend
    {
        /// <summary>
        /// 使用.net core 自带DI框架 注册服务入口
        /// </summary>
        /// <param name="services"></param>
        public static void DependencyInjection(this IServiceCollection services)
        {
            services.AddSingleton<IControllerActivator, AutowiredControllerActivator>();
        }

        public static void AddAutowiredService(this IServiceCollection services)
        {
            services.AddSingleton<AutowiredService>();
        }
    }
}
