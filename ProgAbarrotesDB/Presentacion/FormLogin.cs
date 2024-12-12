using ProgAbarrotesDB.Clases;
using ProgAbarrotesDB.Forms;
using System;
using System.Windows.Forms;

namespace ProgAbarrotesDB
{
    public partial class FormLogin : Form
    {
        private MetodosLogin metodosLogin;
        private DiseñoLoginClass disenoLogin;

        public FormLogin()
        {
            InitializeComponent();
            metodosLogin = new MetodosLogin();
            disenoLogin = new DiseñoLoginClass(this);

            disenoLogin.AsignarEventoIngresar(Ingresar_Click);
            disenoLogin.AsignarEventoCancelar(Cancelar_Click);
        }

        private void Ingresar_Click(object sender, EventArgs e)
        {
            string usuario = disenoLogin.ObtenerUsuario();
            string contrasena = disenoLogin.ObtenerContrasena();

            if (string.IsNullOrEmpty(usuario) || string.IsNullOrEmpty(contrasena))
            {
                MessageBox.Show("Por favor, ingrese usuario y contraseña.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            bool esValido = metodosLogin.VerificarCredenciales(usuario, contrasena);
            if (esValido)
            {
                // Obtener la cadena de conexión con las credenciales proporcionadas
                string connectionString = metodosLogin.GetConnectionString(usuario, contrasena);

                // Instanciar ClaseConexion con la cadena de conexión
                ClaseConexion conexion = new ClaseConexion(connectionString);

                // Pasar también la connectionString al FormPrincipal
                FormPrincipal formPrincipal = new FormPrincipal(conexion, connectionString);
                formPrincipal.Show();

                // Ocultar el formulario de login
                this.Hide();
            }
            else
            {
                MessageBox.Show("Usuario o contraseña incorrectos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                disenoLogin.LimpiarCampos();
            }
        }

        private void Cancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
