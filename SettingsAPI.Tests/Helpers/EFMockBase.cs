using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using SettingsAPI.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace SettingsAPI.Tests.Helpers
{
    public class EFMockBase
    {
        public Mock<ShopGoContext> ContextMock { get; }
        public ShopGoContext Context { get; }
        public Mock<ReadOnlyShopGoContext> ReadOnlyContextMock { get; }
        public ReadOnlyShopGoContext ReadOnlyContext { get; }

        public EFMockBase()
        {
            var options = new DbContextOptionsBuilder<ShopGoContext>()
                .UseInMemoryDatabase(databaseName: $"ShopGo{Guid.NewGuid()}")
                .Options;

            ContextMock = new Mock<ShopGoContext>(options);
            Context = ContextMock.Object;

            var optionsReadOnly = new DbContextOptionsBuilder<ReadOnlyShopGoContext>()
                .UseInMemoryDatabase(databaseName: $"ShopGo{Guid.NewGuid()}")
                .Options;
            ReadOnlyContextMock = new Mock<ReadOnlyShopGoContext>(optionsReadOnly);
            ReadOnlyContext = ReadOnlyContextMock.Object;
        }

        public void SetUpDbContext<TEntity>(Expression<Func<ShopGoContext, DbSet<TEntity>>> expression, List<TEntity> set) where TEntity : class
        {
            ContextMock.Setup(expression).ReturnsDbSet(set);
            ContextMock.Setup(x => x.Set<TEntity>()).ReturnsDbSet(set);
        }

        public void SetUpReadOnlyDbContext<TEntity>(Expression<Func<ReadOnlyShopGoContext, DbSet<TEntity>>> expression, List<TEntity> set) where TEntity : class
        {
            ReadOnlyContextMock.Setup(expression).ReturnsDbSet(set);
            ReadOnlyContextMock.Setup(x => x.Set<TEntity>()).ReturnsDbSet(set);
        }
    }
}
