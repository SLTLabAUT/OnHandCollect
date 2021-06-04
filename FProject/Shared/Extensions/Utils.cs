using FProject.Shared.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
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

        public static IOrderedQueryable<TSource> OrderByCustomOrder<TSource, TCompare>(this IQueryable<TSource> query, Expression<Func<TSource, TCompare>> memberSelector, IList<TCompare> orderList)
        {
            var intType = typeof(int);
            var lambdaExpression = (LambdaExpression)memberSelector;
            var member = (MemberExpression)lambdaExpression.Body;
            var parameters = lambdaExpression.Parameters;

            ConditionalExpression exp = null;
            for (int i = orderList.Count - 1; i >= 0; i--)
            {
                if (exp is null)
                {
                    exp = Expression.Condition(
                        Expression.Equal(member, Expression.Constant(orderList[i])),
                        Expression.Constant(i),
                        Expression.Constant(orderList.Count),
                        intType
                    );
                }
                else
                {
                    exp = Expression.Condition(
                        Expression.Equal(member, Expression.Constant(orderList[i])),
                        Expression.Constant(i),
                        exp,
                        intType
                    );
                }
            }

            return query.OrderBy(Expression.Lambda<Func<TSource, int>>(exp, parameters));
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
        public static string GetDisplayName<TModel>(Expression<Func<TModel, object>> expression)
        {
            var attribute = Attribute.GetCustomAttribute(((MemberExpression)expression.Body).Member, typeof(DisplayAttribute)) as DisplayAttribute;
            if (attribute == null)
            {
                throw new ArgumentException($"Expression '{expression}' doesn't have DisplayAttribute");
            }
            return attribute.Name;
        }

        public static string GetLocalTimeString(this DateTimeOffset dateTimeOffset)
        {
            var pc = new PersianCalendar();
            var dateTime = dateTimeOffset.LocalDateTime;
            return string.Format("{3:00}:{4:00} {0}/{1:00}/{2:00}",
                pc.GetYear(dateTime), pc.GetMonth(dateTime), pc.GetDayOfMonth(dateTime), pc.GetHour(dateTime), pc.GetMinute(dateTime));
        }

        public static TextType ToTextType(this WritepadType type)
        {
            return type switch
            {
                WritepadType.Text => TextType.Text,
                WritepadType.WordGroup => TextType.WordGroup,
                _ => throw new NotSupportedException()
            };
        }

        public static Hand ToHand(this Handedness handedness)
        {
            return handedness switch
            {
                Handedness.Right => Hand.Right,
                Handedness.Left => Hand.Left,
                _ => throw new NotSupportedException()
            };
        }
    }
}
