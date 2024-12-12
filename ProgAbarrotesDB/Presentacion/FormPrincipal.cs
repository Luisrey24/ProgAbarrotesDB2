using ProgAbarrotesDB.Clases; 
using ProgAbarrotesDB.Datos;
using ProgAbarrotesDB.Forms;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ProgAbarrotesDB
{
    public partial class FormPrincipal : Form
    {
        private DiseñoMenu diseñoMenu;
        private ClaseConexion conexion;
        private string _connectionString;

        public FormPrincipal(ClaseConexion conexionDb, string connectionString)
        {
            InitializeComponent();
            InitializeForm();
            conexion = conexionDb;
            _connectionString = connectionString;
        }

        private void InitializeForm()
        {
            this.Text = "PUNTO DE VENTA Ramses_comerciable";
            this.WindowState = FormWindowState.Maximized;
            this.StartPosition = FormStartPosition.CenterScreen;

            diseñoMenu = new DiseñoMenu(this);
            AsignarEventosMenu();

            Panel panelContenido = new Panel
            {
                Name = "panelContenido",
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(60, 60, 60)
            };
            this.Controls.Add(panelContenido);
        }

        private void AsignarEventosMenu()
        {
            diseñoMenu.BtnClientes.Click += BtnClientes_Click;
            diseñoMenu.BtnProductos.Click += BtnProductos_Click;
            diseñoMenu.BtnProveedor.Click += BtnProveedor_Click;
            diseñoMenu.BtnVentas.Click += BtnVentas_Click;
            diseñoMenu.BtnInventario.Click += BtnInventario_Click;
            diseñoMenu.BtnReportesCPP.Click += BtnReportesCPP_Click;
            diseñoMenu.BtnReportesVentas.Click += BtnReportesVentas_Click;
            diseñoMenu.BtnReportesInventario.Click += BtnReportesInventario_Click;
            diseñoMenu.BtnAnalisisVentas.Click += BtnAnalisisVentas_Click;
            diseñoMenu.BtnSalir.Click += BtnSalir_Click;
        }

        private void BtnClientes_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormClientes(conexion, _connectionString));
        }

        private void BtnProductos_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormProductos(conexion, _connectionString));
        }

        private void BtnProveedor_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormProveedor(conexion, _connectionString));
        }

        private void BtnVentas_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormVentas(conexion, _connectionString));
        }

        private void BtnInventario_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormInventario(conexion, _connectionString));
        }

        private void BtnReportesCPP_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormReportesCPP(conexion, _connectionString));
        }

        private void BtnReportesVentas_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormReportesVentas(conexion, _connectionString));
        }

        private void BtnReportesInventario_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormReportesInventario(conexion, _connectionString));
        }

        private void BtnAnalisisVentas_Click(object sender, EventArgs e)
        {
            AbrirFormulario(new FormAnalisisVentas(conexion, _connectionString));
        }

        private void BtnSalir_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("¿Está seguro que desea salir?", "Confirmar Salida", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                Application.Exit();
            }
        }

        private void AbrirFormulario(Form formulario)
        {
            Panel panelContenido = this.Controls["panelContenido"] as Panel;
            if (panelContenido != null)
            {
                panelContenido.Controls.Clear();
                formulario.TopLevel = false;
                formulario.FormBorderStyle = FormBorderStyle.None;
                formulario.Dock = DockStyle.Fill;
                panelContenido.Controls.Add(formulario);
                formulario.Show();
            }
        }
    }
}
