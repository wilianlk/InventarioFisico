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

            // Preferimos una sola clave para todos los entornos.
            // Fallback: mantener compatibilidad con el nombre antiguo de producción si existe.
            var preferredKey = "InformixConnection";
            var legacyProdKey = "InformixConnectionProduction";

            var csPreferred = config.GetConnectionString(preferredKey);
            var csLegacyProd = config.GetConnectionString(legacyProdKey);

            ConnectionKey = !string.IsNullOrWhiteSpace(csPreferred)
                ? preferredKey
                : (!IsDevelopment && !string.IsNullOrWhiteSpace(csLegacyProd) ? legacyProdKey : preferredKey);

            _connectionString = config.GetConnectionString(ConnectionKey)
                ?? throw new InvalidOperationException(
                    $"ConnectionString '{preferredKey}' no configurada. " +
                    $"En producción también se acepta '{legacyProdKey}' por compatibilidad."
                );
        }

        public string Get() => _connectionString;
    }
}
