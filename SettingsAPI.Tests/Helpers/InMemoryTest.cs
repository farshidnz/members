using Microsoft.EntityFrameworkCore;
using SettingsAPI.Data;
using System;

namespace SettingsAPI.Tests.Helpers
{
    public class InMemoryTest : IDisposable
    {
        private bool disposedValue;

        public ShopGoContext Context { get; }
        public ReadOnlyShopGoContext ReadOnlyContext { get; }

        public InMemoryTest()
        {
            var options = new DbContextOptionsBuilder<ShopGoContext>()
                .UseInMemoryDatabase(databaseName: $"ShopGo{Guid.NewGuid()}")
                .Options;

            Context = new ShopGoContext(options);

            var optionsReadOnly = new DbContextOptionsBuilder<ReadOnlyShopGoContext>()
                .UseInMemoryDatabase(databaseName: $"ShopGo{Guid.NewGuid()}")
                .Options;

            ReadOnlyContext = new ReadOnlyShopGoContext(optionsReadOnly);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Context.Dispose();
                    ReadOnlyContext.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
