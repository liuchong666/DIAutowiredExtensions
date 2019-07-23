using Microsoft.AspNetCore.Mvc.Controllers;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Internal;
using Microsoft.AspNetCore.Mvc;

namespace DIAutowiredExtensions
{
    public class AutowiredControllerActivator : DefaultControllerActivator
    {
        private readonly ITypeActivatorCache _typeActivatorCache;
        //private static IDictionary<string, IEnumerable<PropertyInfo>> _publicPropertyCache = new Dictionary<string, IEnumerable<PropertyInfo>>();
        private static Dictionary<Type, Action<object, IServiceProvider>> autowiredActions = new Dictionary<Type, Action<object, IServiceProvider>>();

        public AutowiredControllerActivator(ITypeActivatorCache typeActivatorCache) : base(typeActivatorCache)
        {
            _typeActivatorCache = typeActivatorCache ?? throw new ArgumentNullException(nameof(typeActivatorCache));
        }

        public override object Create(ControllerContext controllerContext)
        {
            if (controllerContext == null)
            {
                throw new ArgumentNullException(nameof(controllerContext));
            }

            if (controllerContext.ActionDescriptor == null)
            {
                throw new ArgumentException(nameof(ControllerContext.ActionDescriptor));
            }

            var controllerTypeInfo = controllerContext.ActionDescriptor.ControllerTypeInfo;

            if (controllerTypeInfo == null)
            {
                throw new ArgumentException(nameof(controllerContext.ActionDescriptor.ControllerTypeInfo));
            }

            var serviceProvider = controllerContext.HttpContext.RequestServices;
            var instance = _typeActivatorCache.CreateInstance<object>(serviceProvider, controllerTypeInfo.AsType());
            if (instance != null)
            {
                var serviceType = controllerTypeInfo.AsType();
                if (autowiredActions.TryGetValue(serviceType, out Action<object, IServiceProvider> act))
                {
                    act(instance, serviceProvider);
                }
                else
                {
                    //参数
                    var objParam = Expression.Parameter(typeof(object), "obj");
                    var spParam = Expression.Parameter(typeof(IServiceProvider), "sp");

                    var obj = Expression.Convert(objParam, serviceType);
                    var getService = typeof(IServiceProvider).GetMethod("GetService");
                    List<Expression> setList = new List<Expression>();

                    //字段赋值
                    foreach (FieldInfo field in serviceType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var autowiredAttr = field.GetCustomAttribute<AutowiredAttribute>();
                        if (autowiredAttr != null)
                        {
                            var fieldExp = Expression.Field(obj, field);
                            var createService = Expression.Call(spParam, getService, Expression.Constant(field.FieldType));
                            //var createService=Expression.Call(getService, spParam, Expression.Constant(field.FieldType));
                            var setExp = Expression.Assign(fieldExp, Expression.Convert(createService, field.FieldType));
                            setList.Add(setExp);
                        }
                    }

                    //属性赋值
                    foreach (PropertyInfo property in serviceType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var autowiredAttr = property.GetCustomAttribute<AutowiredAttribute>();
                        if (autowiredAttr != null)
                        {
                            var propExp = Expression.Property(obj, property);
                            var createService = Expression.Call(spParam, getService, Expression.Constant(property.PropertyType));
                            var setExp = Expression.Assign(propExp, Expression.Convert(createService, property.PropertyType));
                            setList.Add(setExp);
                        }
                    }

                    var bodyExp = Expression.Block(setList);
                    var setAction = Expression.Lambda<Action<object, IServiceProvider>>(bodyExp, objParam, spParam).Compile();
                    autowiredActions[serviceType] = setAction;
                    setAction(instance, serviceProvider);
                }

                #region 反射
                //if (!_publicPropertyCache.ContainsKey(controllerTypeInfo.FullName))
                //{
                //    var ps = controllerTypeInfo.GetProperties(BindingFlags.Instance).AsEnumerable();
                //    ps = ps.Where(c => c.GetCustomAttribute<AutowiredAttribute>() != null);
                //    _publicPropertyCache[controllerTypeInfo.FullName] = ps;
                //}

                //var requireServices = _publicPropertyCache[controllerTypeInfo.FullName];
                //foreach (var item in requireServices)
                //{
                //    var service = serviceProvider.GetService(item.PropertyType);
                //    if (service == null)
                //    {
                //        throw new InvalidOperationException($"Unable to resolve service for type '{item.PropertyType.FullName}' while attempting to activate '{controllerTypeInfo.FullName}'");
                //    }
                //    item.SetValue(instance, service);
                //} 
                #endregion
            }
            return instance;
        }
    }
}
