using Microsoft.EntityFrameworkCore.Storage;
using System.Linq.Expressions;

namespace UserManagement.DAL.Interfaces
{
    public interface IRepository<T>
    {
        IQueryable<T> GetAllByCondition(Expression<Func<T, bool>> condition);
        Task<ICollection<T>> GetAllByConditionAsync(Expression<Func<T, bool>> condition);

        IQueryable<T> GetAll();
        Task<ICollection<T>> GetAllAsync();

        Task<ICollection<TResult>> GetSelectedColumnAsync<TResult>(Expression<Func<T, TResult>> selectExpression);
        Task<ICollection<TResult>> GetSelectedColumnByConditionAsync<TResult>(Expression<Func<T, bool>> filterExpression, Expression<Func<T, TResult>> selectExpression);
        Task<(long TotalCount, ICollection<TResult> Data)> GetSelectedColumnByConditionAsyncWithOffsetLimitAndTotalCount<TResult>(
                Expression<Func<T, bool>> filterExpression,
                Expression<Func<T, TResult>> selectExpression,
                int? limit = null,
                int? offset = null);
        public Task<TResult> GetSingleSelectedColumnByConditionAsync<TResult>(Expression<Func<T, bool>> filterExpression, Expression<Func<T, TResult>> selectExpression);

        Task<Dictionary<TKey, List<TResult>>> GetSelectedColumnGroupByConditionAsync<TKey, TResult>(Expression<Func<T, bool>> filterExpression, Expression<Func<T, TKey>> groupByKeySelector, Expression<Func<T, TResult>> selectExpression);
        Task<ICollection<TResult>> GetSelectedColumnByConditionAsyncWithOffsetLimit<TResult>(
                Expression<Func<T, bool>> filterExpression,
                Expression<Func<T, TResult>> selectExpression,
                int? limit = null,
                int? offset = null);
        T GetSingle(Expression<Func<T, bool>> condition);

        Task<T> GetSingleAysnc(Expression<Func<T, bool>> condition);

        bool Add(T entity);
        bool Update(T entity);
        bool Delete(T entity);
        bool DeleteRange(IEnumerable<T> entities);

        void SaveChangesManaged();
        public IExecutionStrategy GetExecutionStrategy();

        public Task<object> ExecuteQuery(string sqlQuery, object parameters);
        public IQueryable<T> GetFiltered(Expression<Func<T, bool>> predicate);
        IExecutionStrategy CreateExecutionStrategy();
        Task<IDbContextTransaction> BeginTransactionAsync();
        Task CommitTransactionAsync(IDbContextTransaction transaction);
        Task RollbackTransactionAsync(IDbContextTransaction transaction);
    }
}
