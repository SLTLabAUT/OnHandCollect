using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace FProject.Shared.Extensions
{
    public static class Utils
    {
        public static bool IsNullOrEmpty(this IEnumerable source)
        {
            if (source != null)
            {
                foreach (object obj in source)
                    return false;
            }
            return true;
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            if (source != null)
            {
                foreach (T obj in source)
                    return false;
            }
            return true;
        }

        public static TAttribute GetAttribute<TAttribute>(this Enum enumValue)
            where TAttribute : Attribute
        {
            var type = enumValue.GetType();
            return type
                .GetMember(type.GetEnumName(enumValue))
                .First()
                .GetCustomAttribute<TAttribute>();
        }

        public static string GetDisplayName<TModel, TProperty>(Expression<Func<TModel, TProperty>> expression)
        {
            var attribute = Attribute.GetCustomAttribute(((MemberExpression)expression.Body).Member, typeof(DisplayAttribute)) as DisplayAttribute;
            if (attribute == null)
            {
                throw new ArgumentException($"Expression '{expression}' doesn't have DisplayAttribute");
            }
            return attribute.Name;
        }
    }
}
