using FProject.Shared.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

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
                WritepadType.WordGroup2 => TextType.WordGroup2,
                WritepadType.WordGroup3 => TextType.WordGroup3,
                WritepadType.NumberGroup => TextType.NumberGroup,
                _ => throw new NotSupportedException()
            };
        }

        public static WritepadType ToWritepadType(this TextType type)
        {
            return type switch
            {
                TextType.Text => WritepadType.Text,
                TextType.WordGroup => WritepadType.WordGroup,
                TextType.WordGroup2 => WritepadType.WordGroup2,
                TextType.WordGroup3 => WritepadType.WordGroup3,
                TextType.NumberGroup => WritepadType.NumberGroup,
                _ => throw new NotSupportedException()
            };
        }

        public static WritepadType ToWritepadType(this WordGroupType type)
        {
            return type switch
            {
                WordGroupType.WordGroup => WritepadType.WordGroup,
                WordGroupType.WordGroup2 => WritepadType.WordGroup2,
                WordGroupType.WordGroup3 => WritepadType.WordGroup3,
                _ => throw new NotSupportedException()
            };
        }

        public static bool IsWordGroup(this WritepadType type)
        {
            return type == WritepadType.WordGroup || type == WritepadType.WordGroup2 || type == WritepadType.WordGroup3;
        }

        public static bool IsWordGroup(this TextType type)
        {
            return type == TextType.WordGroup || type == TextType.WordGroup2 || type == TextType.WordGroup3;
        }

        public static bool IsGroupedText(this TextType type)
        {
            return type == TextType.WordGroup || type == TextType.WordGroup2 || type == TextType.WordGroup3 || type == TextType.NumberGroup;
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

        private static string[] EngDigits = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        private static string[] FaDigits = new[] { "۰", "۱", "۲", "۳", "۴", "۵", "۶", "۷", "۸", "۹" };
        public static string ToPersianNumber(this object number)
        {
            var result = number.ToString();
            for (int i = 0; i <= 9; i++)
            {
                result = result.Replace(EngDigits[i], FaDigits[i]);
            }
            return result;
        }
    }
}
