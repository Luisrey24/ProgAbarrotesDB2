using ProgAbarrotesDB.Clases;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;

namespace ProgAbarrotesDB.Forms
{
    public partial class FormEditarProducto : Form
    {
        private int idProducto;
        private ClaseConexion conexion;
        private string _connectionString;

        private Label lblTitulo;
        private Label lblNombre;
        private TextBox txtNombre;
        private Label lblPrecio;
        private TextBox txtPrecio;
        private Label lblDescripcion;
        private TextBox txtDescripcion;
        private Label lblStock;
        private TextBox txtStock;
        private Button btnGuardar;
        private Button btnCancelar;
        private TableLayoutPanel tableLayoutPanel;

        public FormEditarProducto(int id, string nombre, string precio, string descripcion, string stock, ClaseConexion conexionDb, string connectionString)
        {
            idProducto = id;
            conexion = conexionDb;
            _connectionString = connectionString;

            InitializeComponent();

            txtNombre.Text = nombre;
            txtPrecio.Text = precio;
            txtDescripcion.Text = descripcion;
            txtStock.Text = stock;
        }

        private void InitializeComponent()
        {
            this.Text = "Editar Producto";
            this.Size = new Size(500, 450);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            tableLayoutPanel = new TableLayoutPanel();
            tableLayoutPanel.RowCount = 6;
            tableLayoutPanel.ColumnCount = 2;
            tableLayoutPanel.Dock = DockStyle.Fill;
            tableLayoutPanel.Padding = new Padding(20);
            tableLayoutPanel.AutoSize = true;
            tableLayoutPanel.AutoScroll = true;

            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30F));
            tableLayoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70F));

            for (int i = 0; i < 6; i++)
            {
                tableLayoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40F));
            }

            lblTitulo = new Label();
            lblTitulo.Text = "Editar Información del Producto";
            lblTitulo.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblTitulo.ForeColor = Color.FromArgb(0, 122, 204);
            lblTitulo.AutoSize = true;
            lblTitulo.TextAlign = ContentAlignment.MiddleCenter;
            tableLayoutPanel.SetColumnSpan(lblTitulo, 2);
            tableLayoutPanel.Controls.Add(lblTitulo, 0, 0);

            lblNombre = new Label();
            lblNombre.Text = "Nombre:";
            lblNombre.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblNombre.ForeColor = Color.Black;
            lblNombre.TextAlign = ContentAlignment.MiddleRight;
            lblNombre.Dock = DockStyle.Fill;
            tableLayoutPanel.Controls.Add(lblNombre, 0, 1);

            txtNombre = new TextBox();
            txtNombre.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtNombre.Dock = DockStyle.Fill;
            txtNombre.BackColor = Color.White;
            tableLayoutPanel.Controls.Add(txtNombre, 1, 1);

            lblPrecio = new Label();
            lblPrecio.Text = "Precio:";
            lblPrecio.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblPrecio.ForeColor = Color.Black;
            lblPrecio.TextAlign = ContentAlignment.MiddleRight;
            lblPrecio.Dock = DockStyle.Fill;
            tableLayoutPanel.Controls.Add(lblPrecio, 0, 2);

            txtPrecio = new TextBox();
            txtPrecio.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtPrecio.Dock = DockStyle.Fill;
            txtPrecio.BackColor = Color.White;
            tableLayoutPanel.Controls.Add(txtPrecio, 1, 2);

            lblDescripcion = new Label();
            lblDescripcion.Text = "Descripción:";
            lblDescripcion.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblDescripcion.ForeColor = Color.Black;
            lblDescripcion.TextAlign = ContentAlignment.MiddleRight;
            lblDescripcion.Dock = DockStyle.Fill;
            tableLayoutPanel.Controls.Add(lblDescripcion, 0, 3);

            txtDescripcion = new TextBox();
            txtDescripcion.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtDescripcion.Dock = DockStyle.Fill;
            txtDescripcion.BackColor = Color.White;
            txtDescripcion.Multiline = true;
            txtDescripcion.Height = 60;
            tableLayoutPanel.Controls.Add(txtDescripcion, 1, 3);

            lblStock = new Label();
            lblStock.Text = "Stock:";
            lblStock.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            lblStock.ForeColor = Color.Black;
            lblStock.TextAlign = ContentAlignment.MiddleRight;
            lblStock.Dock = DockStyle.Fill;
            tableLayoutPanel.Controls.Add(lblStock, 0, 4);

            txtStock = new TextBox();
            txtStock.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            txtStock.Dock = DockStyle.Fill;
            txtStock.BackColor = Color.White;
            tableLayoutPanel.Controls.Add(txtStock, 1, 4);

            btnGuardar = new Button();
            btnGuardar.Text = "Guardar";
            btnGuardar.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnGuardar.BackColor = Color.FromArgb(0, 122, 204);
            btnGuardar.ForeColor = Color.White;
            btnGuardar.FlatStyle = FlatStyle.Flat;
            btnGuardar.FlatAppearance.BorderSize = 0;
            btnGuardar.Height = 35;
            btnGuardar.Cursor = Cursors.Hand;
            btnGuardar.Dock = DockStyle.Fill;
            btnGuardar.Click += btnGuardar_Click;
            tableLayoutPanel.Controls.Add(btnGuardar, 0, 5);

            btnCancelar = new Button();
            btnCancelar.Text = "Cancelar";
            btnCancelar.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            btnCancelar.BackColor = Color.Gray;
            btnCancelar.ForeColor = Color.White;
            btnCancelar.FlatStyle = FlatStyle.Flat;
            btnCancelar.FlatAppearance.BorderSize = 0;
            btnCancelar.Height = 35;
            btnCancelar.Cursor = Cursors.Hand;
            btnCancelar.Dock = DockStyle.Fill;
            btnCancelar.Click += btnCancelar_Click;
            tableLayoutPanel.Controls.Add(btnCancelar, 1, 5);

            this.Controls.Add(tableLayoutPanel);
        }

        private void btnGuardar_Click(object sender, EventArgs e)
        {
            string nuevoNombre = txtNombre.Text.Trim();
            string precioText = txtPrecio.Text.Trim();
            string nuevaDescripcion = txtDescripcion.Text.Trim();
            string stockText = txtStock.Text.Trim();

            if (string.IsNullOrEmpty(nuevoNombre) || string.IsNullOrEmpty(precioText) ||
                string.IsNullOrEmpty(nuevaDescripcion) || string.IsNullOrEmpty(stockText))
            {
                MessageBox.Show("Por favor, complete todos los campos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!decimal.TryParse(precioText, out decimal nuevoPrecio) || nuevoPrecio <= 0)
            {
                MessageBox.Show("Por favor, ingrese un precio válido y mayor a 0.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(stockText, out int nuevoStock) || nuevoStock < 0)
            {
                MessageBox.Show("Por favor, ingrese un stock válido (entero y no negativo).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand("sp_UpdateProducto", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ID_Producto", idProducto);
                    cmd.Parameters.AddWithValue("@Nombre", nuevoNombre);
                    cmd.Parameters.AddWithValue("@Descripcion", nuevaDescripcion);
                    cmd.Parameters.AddWithValue("@Precio", nuevoPrecio);
                    cmd.Parameters.AddWithValue("@Stock", nuevoStock);

                    conn.Open();
                    cmd.ExecuteNonQuery();
                }

                MessageBox.Show("Producto actualizado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar el producto: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancelar_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void HandleSqlException(SqlException ex)
        {
            switch (ex.Number)
            {
                case 2627:
                    MessageBox.Show("Error: Registro duplicado detectado.", "Error de Clave Duplicada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 547:
                    MessageBox.Show("Error: Violación de integridad referencial.", "Restricción de Integridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 2601:
                    MessageBox.Show("Error: Índice único duplicado.", "Error de Índice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 229:
                    MessageBox.Show("No tienes permisos para realizar esta operación.", "Permisos Insuficientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                default:
                    MessageBox.Show($"Error de base de datos: {ex.Message}", "Error en la Base de Datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }
    }
}
