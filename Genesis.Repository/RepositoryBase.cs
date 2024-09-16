﻿using Microsoft.EntityFrameworkCore;
using Genesis.Repository.Expressions;
using System.Linq.Expressions;
using Genesis.Models.DTO;
using System.Reflection;

namespace Genesis.Repository
{
    public class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        protected readonly DbContext _context;
        protected readonly DbSet<T> _dbSet;

        public RepositoryBase(DbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        public async Task<IEnumerable<T>> GetRecordsAsync(List<SearchParam> searchParams)
        {
            Expression<Func<T, bool>> expression = ComparisonBuilder.BuildExpression<T>(searchParams);
            return await _dbSet.Where(expression).ToListAsync();
        }

        public async Task<T> GetByPKAsync(object keyValue)
        {
            var entity = await _dbSet.FindAsync(keyValue);
            return entity ?? throw new Exception("Record Not Found");
        }

        public async Task<bool> IsEntityExistAsync(T entity)
        {
            var keyValue = GetPKValues(entity);
            return await _dbSet.FindAsync(keyValue) != null;
        }

        public async Task AddAsync(T entity)
        {
            if (await IsEntityExistAsync(entity))
            {
                throw new InvalidOperationException("Entity with the same key already exists.");
            }
            await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
        }

        public async Task SaveRecordsAsync(IEnumerable<T> records)
        {
            await _dbSet.AddRangeAsync(records);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(T entity)
        {
            if (!await IsEntityExistAsync(entity))
            {
                throw new InvalidOperationException("Entity not found to update.");
            }
            _context.Update(entity);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(T entity)
        {
            if (await IsEntityExistAsync(entity))
            {
                _dbSet.Remove(entity);
                await _context.SaveChangesAsync();
            }
        }

        public async Task DeleteByPKAsync(object keyValue)
        {
            T? obj = await _dbSet.FindAsync(keyValue);
            if (obj != null)
            {
                _dbSet.Remove(obj);
                await _context.SaveChangesAsync();
            }
        }
        private object GetPKValues(T entity)
        {
            var keyProperty = GetPKProperty();
            if (keyProperty == null)
            {
                throw new InvalidOperationException("No key property defined for the entity.");
            }

            return keyProperty.GetValue(entity) ?? throw new InvalidOperationException("Invalid property defined for the entity.");
        }

        private PropertyInfo GetPKProperty()
        {
            var keyProperties = _context.Model.FindEntityType(typeof(T))?.FindPrimaryKey()?.Properties;
            return keyProperties?.FirstOrDefault()?.PropertyInfo
                ?? throw new InvalidOperationException("No key property defined for the entity.");
        }
    }
}
