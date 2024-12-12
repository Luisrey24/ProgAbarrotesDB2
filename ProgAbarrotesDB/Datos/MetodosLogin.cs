using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgAbarrotesDB.Clases
{
    public class MetodosLogin
    {
        private string dataSource = "RAMSES"; // Nombre del servidor
        private string initialCatalog = "TiendaBD"; // Nombre de la BD

        /// <param name="usuario">Nombre de usuario de SQL Server.</param>
        /// <param name="contrasena">Contraseña de SQL Server.</param>
        /// <returns>Verdadero si la conexión es exitosa; de lo contrario, falso.</returns>
        public bool VerificarCredenciales(string usuario, string contrasena)
        {
            string connectionString = $"Data Source={dataSource};Initial Catalog={initialCatalog};User ID={usuario};Password={contrasena}";

            try
            {
                using (SqlConnection conexion = new SqlConnection(connectionString))
                {
                    conexion.Open(); // Intentar abrir la conexión
                    return true;
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine($"Error de conexión: {ex.Message}");
                return false;
            }
        }

        /// <param name="usuario">Nombre de usuario de SQL Server.</param>
        /// <param name="contrasena">Contraseña de SQL Server.</param>
        public string GetConnectionString(string usuario, string contrasena)
        {
            // Cadena de conexión sin Encrypt ni TrustServerCertificate.
            return $"Data Source={dataSource};Initial Catalog={initialCatalog};User ID={usuario};Password={contrasena}";
        }
    }
}
