using System.Linq.Expressions;
using System.Reflection;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Computational_Practice.Common;

namespace Computational_Practice.Extensions
{
    public static class QueryableExtensions
    {
        public static IQueryable<T> ApplyPaging<T>(this IQueryable<T> query, PagedRequest request)
        {
            return query.Skip(request.Skip).Take(request.Take);
        }

        public static IQueryable<T> ApplySorting<T>(this IQueryable<T> query, string? sortBy, string sortDirection = "asc")
        {
            if (string.IsNullOrEmpty(sortBy))
                return query;

            var property = typeof(T).GetProperty(sortBy, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");
            var propertyAccess = Expression.MakeMemberAccess(parameter, property);
            var orderByExpression = Expression.Lambda(propertyAccess, parameter);

            var methodName = sortDirection.ToLower() == "desc" ? "OrderByDescending" : "OrderBy";
            var resultExpression = Expression.Call(
                typeof(Queryable),
                methodName,
                new Type[] { typeof(T), property.PropertyType },
                query.Expression,
                Expression.Quote(orderByExpression));

            return query.Provider.CreateQuery<T>(resultExpression);
        }

        public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, string? searchTerm, params string[] searchProperties)
        {
            if (string.IsNullOrEmpty(searchTerm) || !searchProperties.Any())
                return query;

            var parameter = Expression.Parameter(typeof(T), "x");
            Expression? searchExpression = null;

            foreach (var propertyName in searchProperties)
            {
                var property = typeof(T).GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                if (property?.PropertyType == typeof(string))
                {
                    var propertyAccess = Expression.MakeMemberAccess(parameter, property);
                    var containsMethod = typeof(string).GetMethod("Contains", new[] { typeof(string) });
                    var searchValue = Expression.Constant(searchTerm, typeof(string));
                    var containsExpression = Expression.Call(propertyAccess, containsMethod!, searchValue);

                    searchExpression = searchExpression == null
                        ? containsExpression
                        : Expression.OrElse(searchExpression, containsExpression);
                }
            }

            if (searchExpression != null)
            {
                var lambda = Expression.Lambda<Func<T, bool>>(searchExpression, parameter);
                query = query.Where(lambda);
            }

            return query;
        }

        public static async Task<PagedResponse<T>> ToPagedResponseAsync<T>(this IQueryable<T> query, PagedRequest request)
        {
            var totalCount = await query.CountAsync();
            var pagedQuery = query.ApplyPaging(request);
            var data = await pagedQuery.ToListAsync();

            return new PagedResponse<T>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }

        public static async Task<PagedResponse<TDto>> ToPagedResponseAsync<TEntity, TDto>(this IQueryable<TEntity> query, PagedRequest request, IMapper mapper)
        {
            var totalCount = await query.CountAsync();
            var pagedQuery = query.ApplyPaging(request);
            var entities = await pagedQuery.ToListAsync();
            var data = mapper.Map<IEnumerable<TDto>>(entities);

            return new PagedResponse<TDto>
            {
                Data = data,
                TotalCount = totalCount,
                Page = request.Page,
                PageSize = request.PageSize
            };
        }
    }
}
