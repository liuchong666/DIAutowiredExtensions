using System;
using System.Collections.Generic;
using System.Text;

namespace DIAutowiredExtensions
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class AutowiredAttribute : Attribute
    {
    }
}
