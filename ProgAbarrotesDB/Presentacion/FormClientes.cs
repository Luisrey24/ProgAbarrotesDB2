using System;
using System.Data.SqlClient;
using System.Linq;
using System.Transactions;
using System.Windows.Forms;
using ProgAbarrotesDB.Components;

namespace ProgAbarrotesDB
{
    public partial class FormClientes : Form
    {
        private DiseñoClientes diseñoClientes;
        private ClaseConexion conexion;
        private string _connectionString;

        public FormClientes(ClaseConexion conexionDb, string connectionString)
        {
            InitializeComponent();
            conexion = conexionDb;
            _connectionString = connectionString;
            InitializeForm();
            CargarDatos();
        }

        private void InitializeForm()
        {
            diseñoClientes = new DiseñoClientes(this);
            AsignarEventos();
        }

        private void AsignarEventos()
        {
            diseñoClientes.BtnAlta.Click += BtnAlta_Click;
            diseñoClientes.BtnConsulta.Click += BtnConsulta_Click;
            diseñoClientes.BtnModificar.Click += BtnModificar_Click;
            diseñoClientes.BtnEliminar.Click += BtnEliminar_Click;
        }

        private void BtnAlta_Click(object sender, EventArgs e)
        {
            string nombre = diseñoClientes.TxtNombre.Text.Trim();
            string telefono = diseñoClientes.TxtTelefono.Text.Trim();
            string direccion = diseñoClientes.TxtDireccion.Text.Trim();

            if (string.IsNullOrEmpty(nombre) || string.IsNullOrEmpty(telefono) || string.IsNullOrEmpty(direccion))
            {
                MessageBox.Show("Por favor, complete todos los campos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (TransactionScope scope = new TransactionScope())
                {
                    var db = conexion.GetDataContext();

                    Cliente nuevoCliente = new Cliente
                    {
                        Nombre = nombre,
                        Telefono = telefono,
                        Direccion = direccion
                    };

                    db.Cliente.InsertOnSubmit(nuevoCliente);
                    db.SubmitChanges();

                    scope.Complete();
                }

                MessageBox.Show("Cliente agregado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);

                diseñoClientes.TxtNombre.Clear();
                diseñoClientes.TxtTelefono.Clear();
                diseñoClientes.TxtDireccion.Clear();
                CargarDatos();
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al agregar el cliente: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void HandleSqlException(SqlException ex)
        {
            switch (ex.Number)
            {
                case 2627:
                    MessageBox.Show("Ya existe un cliente con los mismos datos únicos.", "Error de Clave Duplicada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 547:
                    MessageBox.Show("No se puede realizar la acción debido a restricciones de integridad referencial.", "Restricción de Integridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 2601:
                    MessageBox.Show("Un valor único ya existe y está duplicado en el sistema.", "Duplicación de Índice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 229:
                    MessageBox.Show("No tienes permisos para realizar esta operación.", "Permisos insuficientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                default:
                    MessageBox.Show($"Ocurrió un error de base de datos: {ex.Message}", "Error en la Base de Datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void BtnConsulta_Click(object sender, EventArgs e)
        {
            string nombre = diseñoClientes.TxtNombre.Text.Trim();
            string telefono = diseñoClientes.TxtTelefono.Text.Trim();

            if (string.IsNullOrEmpty(nombre) && string.IsNullOrEmpty(telefono))
            {
                MessageBox.Show("Por favor, ingrese al menos un criterio de búsqueda (Nombre o Teléfono).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                var db = conexion.GetDataContext();

                var clientes = from c in db.Cliente
                               where (string.IsNullOrEmpty(nombre) || c.Nombre.Contains(nombre)) &&
                                     (string.IsNullOrEmpty(telefono) || c.Telefono.Contains(telefono))
                               select new
                               {
                                   c.ID_Cliente,
                                   c.Nombre,
                                   c.Telefono,
                                   c.Direccion
                               };

                diseñoClientes.DgvClientes.DataSource = clientes.ToList();

                if (clientes.Any())
                {
                    MessageBox.Show("Consulta realizada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("No se encontraron clientes que coincidan con los criterios ingresados.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al realizar la consulta: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnModificar_Click(object sender, EventArgs e)
        {
            if (diseñoClientes.DgvClientes.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione un cliente para modificar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataGridViewRow filaSeleccionada = diseñoClientes.DgvClientes.SelectedRows[0];
            int idCliente = Convert.ToInt32(filaSeleccionada.Cells["ID_Cliente"].Value);

            string nombreActual = filaSeleccionada.Cells["Nombre"].Value.ToString();
            string telefonoActual = filaSeleccionada.Cells["Telefono"].Value.ToString();
            string direccionActual = filaSeleccionada.Cells["Direccion"].Value.ToString();

            FormEditarCliente formEditar = new FormEditarCliente(idCliente, nombreActual, telefonoActual, direccionActual, conexion, _connectionString);
            var resultado = formEditar.ShowDialog();

            if (resultado == DialogResult.OK)
            {
                try
                {
                    CargarDatos();
                    MessageBox.Show("Cliente modificado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (SqlException ex)
                {
                    HandleSqlException(ex);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al modificar el cliente: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (diseñoClientes.DgvClientes.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione un cliente para eliminar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataGridViewRow filaSeleccionada = diseñoClientes.DgvClientes.SelectedRows[0];
            int idCliente = Convert.ToInt32(filaSeleccionada.Cells["ID_Cliente"].Value);

            DialogResult resultado = MessageBox.Show("¿Está seguro de que desea eliminar este cliente?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    using (TransactionScope scope = new TransactionScope())
                    {
                        var db = conexion.GetDataContext();
                        Cliente cliente = db.Cliente.SingleOrDefault(c => c.ID_Cliente == idCliente);

                        if (cliente != null)
                        {
                            db.Cliente.DeleteOnSubmit(cliente);
                            db.SubmitChanges();
                            scope.Complete();

                            MessageBox.Show("Cliente eliminado exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            CargarDatos();
                        }
                        else
                        {
                            MessageBox.Show("Cliente no encontrado.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
                catch (SqlException ex)
                {
                    HandleSqlException(ex);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar el cliente: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CargarDatos()
        {
            try
            {
                var db = conexion.GetDataContext();

                var clientes = from c in db.Cliente
                               select new
                               {
                                   c.ID_Cliente,
                                   c.Nombre,
                                   c.Telefono,
                                   c.Direccion
                               };

                diseñoClientes.DgvClientes.DataSource = clientes.ToList();
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los datos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
