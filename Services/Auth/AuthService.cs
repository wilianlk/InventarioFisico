using System;

namespace InventarioFisico.Services.Auth
{
    public class AuthService
    {
        private const string PASSWORD = "123";
        private const string DOMAIN = "@recamier.com";

        public bool LoginValido(string usuario, string password)
        {
            if (string.IsNullOrWhiteSpace(usuario))
                return false;

            if (!usuario.EndsWith(DOMAIN, StringComparison.OrdinalIgnoreCase))
                return false;

            if (string.IsNullOrWhiteSpace(password))
                return false;

            if (password != PASSWORD)
                return false;

            return true;
        }
    }
}
