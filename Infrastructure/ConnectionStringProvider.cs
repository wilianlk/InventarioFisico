using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

namespace InventarioFisico.Infrastructure
{
    public class ConnectionStringProvider : IConnectionStringProvider
    {
        private readonly string _connectionString;
        public string ConnectionKey { get; }
        public bool IsDevelopment { get; }

        public ConnectionStringProvider(IConfiguration config, IWebHostEnvironment env)
        {
            IsDevelopment = env.IsDevelopment();
            ConnectionKey = IsDevelopment ? "InformixConnection" : "InformixConnectionProduction";

            _connectionString = config.GetConnectionString(ConnectionKey)
                ?? throw new InvalidOperationException($"ConnectionString '{ConnectionKey}' no configurada en appsettings.json.");
        }

        public string Get() => _connectionString;
    }
}
