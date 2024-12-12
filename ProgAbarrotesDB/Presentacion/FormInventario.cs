using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Windows.Forms;
using ProgAbarrotesDB.Components;

namespace ProgAbarrotesDB
{
    public partial class FormInventario : Form
    {
        private DiseñoInventario diseñoInventario;
        private ClaseConexion conexion;
        private DataClasses1DataContext db;
        private string _connectionString;

        public FormInventario(ClaseConexion conexionDb, string connectionString)
        {
            InitializeComponent();
            this.conexion = conexionDb;
            this._connectionString = connectionString;
            this.db = conexion.GetDataContext();
            InitializeForm();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db?.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeForm()
        {
            diseñoInventario = new DiseñoInventario(this);
            CargarProductos();
            CargarProveedores();
            AsignarEventos();
            ConfigurarDataGridView();
            ActualizarTotales();
            CargarDatos(); // Para cargar datos existentes, si lo necesitas
        }

        private void AsignarEventos()
        {
            diseñoInventario.BtnAgregar.Click += BtnAgregar_Click;
            diseñoInventario.BtnAñadirInventario.Click += BtnAñadirInventario_Click;
            diseñoInventario.BtnActualizar.Click += BtnActualizar_Click;
            diseñoInventario.BtnEliminar.Click += BtnEliminar_Click;
            diseñoInventario.CmbProducto.SelectedIndexChanged += CmbProducto_SelectedIndexChanged;
            diseñoInventario.BtnLimpiar.Click += BtnLimpiar_Click;
            diseñoInventario.DgvInventario.RowsRemoved += DgvInventario_RowsRemoved;
        }

        private void BtnAgregar_Click(object sender, EventArgs e)
        {
            string producto = diseñoInventario.CmbProducto.SelectedItem?.ToString();
            string proveedor = diseñoInventario.CmbProveedor.SelectedItem?.ToString();
            string observaciones = diseñoInventario.TxtObservaciones.Text.Trim();
            string cantidadEntradaText = diseñoInventario.TxtCantidadEntrada.Text.Trim();

            if (string.IsNullOrEmpty(producto) || string.IsNullOrEmpty(proveedor) || string.IsNullOrEmpty(cantidadEntradaText))
            {
                MessageBox.Show("Por favor, complete los campos de Producto, Proveedor y Cantidad Entrada.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (!int.TryParse(cantidadEntradaText, out int cantidadEntrada) || cantidadEntrada <= 0)
            {
                MessageBox.Show("Por favor, ingrese una cantidad válida (entero positivo).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal precioPorUnidad = ObtenerCostoPorProducto(producto);

            if (precioPorUnidad <= 0)
            {
                MessageBox.Show("El costo por unidad no es válido.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            decimal total = precioPorUnidad * cantidadEntrada;

            // ID_Inventario no se conoce aún, se dejará en 0 o null cuando aún no se ha insertado en la BD.
            diseñoInventario.DgvInventario.Rows.Add(
                0, // ID_Inventario inicialmente 0
                producto,
                proveedor,
                DateTime.Now.ToShortDateString(),
                cantidadEntrada,
                precioPorUnidad.ToString("C2"),
                total.ToString("C2"),
                observaciones
            );

            ActualizarTotales();
            LimpiarCampos();
        }

        private void BtnAñadirInventario_Click(object sender, EventArgs e)
        {
            if (diseñoInventario.DgvInventario.Rows.Count == 0)
            {
                MessageBox.Show("No hay entradas para añadir al inventario.", "Información", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirmacion = MessageBox.Show("¿Está seguro de que desea añadir estas entradas al inventario?", "Confirmar Añadir", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirmacion != DialogResult.Yes)
            {
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();

                    foreach (DataGridViewRow fila in diseñoInventario.DgvInventario.Rows)
                    {
                        if (fila.IsNewRow) continue;

                        string productoNombre = fila.Cells["Producto"].Value.ToString();
                        string proveedorNombre = fila.Cells["Proveedor"].Value.ToString();
                        int cantidadEntrada = Convert.ToInt32(fila.Cells["CantidadEntrada"].Value);
                        decimal precioPorUnidad = decimal.Parse(fila.Cells["PrecioUnitario"].Value.ToString(), System.Globalization.NumberStyles.Currency);
                        string observaciones = fila.Cells["Observaciones"].Value.ToString();

                        int idProducto = ObtenerIDProducto(productoNombre);
                        int idProveedor = ObtenerIDProveedor(proveedorNombre);

                        if (idProducto == 0)
                        {
                            MessageBox.Show($"Producto '{productoNombre}' no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            throw new Exception($"Producto '{productoNombre}' no existe.");
                        }

                        if (idProveedor == 0)
                        {
                            MessageBox.Show($"Proveedor '{proveedorNombre}' no existe.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            throw new Exception($"Proveedor '{proveedorNombre}' no existe.");
                        }

                        using (SqlCommand cmd = new SqlCommand("sp_AltaInventario", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@ID_Producto", SqlDbType.Int).Value = idProducto;
                            cmd.Parameters.Add("@CantidadEntrada", SqlDbType.Int).Value = cantidadEntrada;
                            cmd.Parameters.Add("@PrecioPorUnidad", SqlDbType.Decimal).Value = precioPorUnidad;
                            cmd.Parameters.Add("@ID_Proveedor", SqlDbType.Int).Value = idProveedor;
                            cmd.Parameters.Add("@Observaciones", SqlDbType.NVarChar, 255).Value = observaciones;

                            cmd.ExecuteNonQuery();
                        }
                    }

                    conn.Close();
                }

                MessageBox.Show("Entradas al inventario añadidas exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                diseñoInventario.DgvInventario.Rows.Clear();
                ActualizarTotales();
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al añadir entradas al inventario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnActualizar_Click(object sender, EventArgs e)
        {
            if (diseñoInventario.DgvInventario.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione una entrada de inventario para actualizar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataGridViewRow filaSeleccionada = diseñoInventario.DgvInventario.SelectedRows[0];
            int idInventario = Convert.ToInt32(filaSeleccionada.Cells["ID_Inventario"].Value);
            if (idInventario == 0)
            {
                MessageBox.Show("Esta entrada aún no está guardada en la base de datos, no se puede actualizar. Primero añádela al inventario.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            string productoNombre = filaSeleccionada.Cells["Producto"].Value.ToString();
            string proveedorNombre = filaSeleccionada.Cells["Proveedor"].Value.ToString();
            int cantidadEntrada = Convert.ToInt32(filaSeleccionada.Cells["CantidadEntrada"].Value);
            decimal precioPorUnidad = decimal.Parse(filaSeleccionada.Cells["PrecioUnitario"].Value.ToString(), System.Globalization.NumberStyles.Currency);
            string observaciones = filaSeleccionada.Cells["Observaciones"].Value.ToString();

            int idProducto = ObtenerIDProducto(productoNombre);
            int idProveedor = ObtenerIDProveedor(proveedorNombre);

            if (idProducto == 0 || idProveedor == 0)
            {
                MessageBox.Show("Producto o Proveedor no válidos.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    conn.Open();
                    using (SqlCommand cmd = new SqlCommand("sp_UpdateInventario", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.Add("@ID_Inventario", SqlDbType.Int).Value = idInventario;
                        cmd.Parameters.Add("@ID_Producto", SqlDbType.Int).Value = idProducto;
                        cmd.Parameters.Add("@CantidadEntrada", SqlDbType.Int).Value = cantidadEntrada;
                        cmd.Parameters.Add("@PrecioPorUnidad", SqlDbType.Decimal).Value = precioPorUnidad;
                        cmd.Parameters.Add("@ID_Proveedor", SqlDbType.Int).Value = idProveedor;
                        cmd.Parameters.Add("@Observaciones", SqlDbType.NVarChar, 255).Value = observaciones;

                        cmd.ExecuteNonQuery();
                    }
                    conn.Close();
                }

                MessageBox.Show("Entrada de inventario actualizada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                CargarDatos();
            }
            catch (SqlException ex)
            {
                HandleSqlException(ex);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al actualizar el inventario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnEliminar_Click(object sender, EventArgs e)
        {
            if (diseñoInventario.DgvInventario.SelectedRows.Count == 0)
            {
                MessageBox.Show("Por favor, seleccione una entrada de inventario para eliminar.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DataGridViewRow filaSeleccionada = diseñoInventario.DgvInventario.SelectedRows[0];
            int idInventario = Convert.ToInt32(filaSeleccionada.Cells["ID_Inventario"].Value);
            if (idInventario == 0)
            {
                MessageBox.Show("Esta entrada aún no está guardada en la base de datos, no se puede eliminar. Primero añádela al inventario.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            DialogResult resultado = MessageBox.Show("¿Está seguro de que desea eliminar esta entrada de inventario?", "Confirmar Eliminación", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (resultado == DialogResult.Yes)
            {
                try
                {
                    using (SqlConnection conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        using (SqlCommand cmd = new SqlCommand("sp_BajaInventario", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.Add("@ID_Inventario", SqlDbType.Int).Value = idInventario;
                            cmd.ExecuteNonQuery();
                        }
                        conn.Close();
                    }

                    MessageBox.Show("Entrada de inventario eliminada exitosamente.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    CargarDatos();
                }
                catch (SqlException ex)
                {
                    HandleSqlException(ex);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error al eliminar el inventario: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void CargarProductos()
        {
            try
            {
                var productos = db.Producto.Select(p => p.Nombre).ToList();
                diseñoInventario.CmbProducto.Items.Clear();
                diseñoInventario.CmbProducto.Items.AddRange(productos.ToArray());
                diseñoInventario.CmbProducto.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los productos: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CargarProveedores()
        {
            try
            {
                var proveedores = db.Proveedor.Select(p => p.Nombre).ToList();
                diseñoInventario.CmbProveedor.Items.Clear();
                diseñoInventario.CmbProveedor.Items.AddRange(proveedores.ToArray());
                diseñoInventario.CmbProveedor.SelectedIndex = -1;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar los proveedores: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ConfigurarDataGridView()
        {
            // Ya se configura en DiseñoInventario.ConfigurarColumnasDataGridView()
            // Aquí puedes cargar datos si lo deseas
        }

        private void CargarDatos()
        {
            try
            {
                var inventarios = from i in db.Inventarios
                                  join p in db.Producto on i.ID_Producto equals p.ID_Producto
                                  join pr in db.Proveedor on i.ID_Proveedor equals pr.ID_Proveedor
                                  select new
                                  {
                                      i.ID_Inventario,
                                      Producto = p.Nombre,
                                      Proveedor = pr.Nombre,
                                      Fecha = i.FechaEntrada,
                                      CantidadEntrada = i.CantidadEntrada,
                                      PrecioPorUnidad = i.PrecioPorUnidad, // Asegúrate que sea decimal no nulable
                                      Total = i.CantidadEntrada * i.PrecioPorUnidad,
                                      Observaciones = i.Observaciones
                                  };

                diseñoInventario.DgvInventario.Rows.Clear();

                foreach (var item in inventarios)
                {
                    string fechaStr = item.Fecha.HasValue ? item.Fecha.Value.ToShortDateString() : "";
                    // Asumiendo PrecioPorUnidad y Total son decimal (no nulables)
                    string precioStr = (item.PrecioPorUnidad ?? 0m).ToString("C");
                    string totalStr = (item.Total ?? 0m).ToString("C");


                    diseñoInventario.DgvInventario.Rows.Add(
                        item.ID_Inventario,
                        item.Producto,
                        item.Proveedor,
                        fechaStr,
                        item.CantidadEntrada,
                        precioStr,
                        totalStr,
                        item.Observaciones
                    );
                }

                ActualizarTotales();
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


        private decimal ObtenerCostoPorProducto(string productoNombre)
        {
            try
            {
                var producto = db.Producto.SingleOrDefault(p => p.Nombre == productoNombre);
                return producto?.Precio ?? 0.00m;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener el costo del producto: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0.00m;
            }
        }

        private void CmbProducto_SelectedIndexChanged(object sender, EventArgs e)
        {
            string productoSeleccionado = diseñoInventario.CmbProducto.SelectedItem?.ToString();
            if (!string.IsNullOrEmpty(productoSeleccionado))
            {
                decimal stockDisponible = ObtenerStockDisponible(productoSeleccionado);
                diseñoInventario.LblStockDisponible.Text = $"Stock Disponible: {stockDisponible}";
            }
            else
            {
                diseñoInventario.LblStockDisponible.Text = "Stock Disponible: N/A";
            }
        }

        private decimal ObtenerStockDisponible(string productoNombre)
        {
            try
            {
                var producto = db.Producto.SingleOrDefault(p => p.Nombre == productoNombre);
                return producto?.Stock ?? 0.00m;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al obtener el stock del producto: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return 0.00m;
            }
        }

        private int ObtenerIDProducto(string productoNombre)
        {
            var producto = db.Producto.SingleOrDefault(p => p.Nombre == productoNombre);
            return producto?.ID_Producto ?? 0;
        }

        private int ObtenerIDProveedor(string proveedorNombre)
        {
            var proveedor = db.Proveedor.SingleOrDefault(p => p.Nombre == proveedorNombre);
            return proveedor?.ID_Proveedor ?? 0;
        }

        private void HandleSqlException(SqlException ex)
        {
            switch (ex.Number)
            {
                case 2627:
                    MessageBox.Show("Error: Un registro duplicado está causando un conflicto.", "Error de Clave Duplicada", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 547:
                    MessageBox.Show("Error: Restricción de integridad referencial violada.", "Restricción de Integridad", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 2601:
                    MessageBox.Show("Error: Índice único duplicado detectado.", "Duplicación de Índice", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                case 229:
                    MessageBox.Show("No tienes permisos suficientes para ejecutar esta operación.", "Permisos insuficientes", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    break;
                default:
                    MessageBox.Show($"Ocurrió un error de base de datos: {ex.Message}", "Error en la Base de Datos", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }
        }

        private void LimpiarCampos()
        {
            diseñoInventario.CmbProducto.SelectedIndex = -1;
            diseñoInventario.CmbProveedor.SelectedIndex = -1;
            diseñoInventario.DtpFecha.Value = DateTime.Now;
            diseñoInventario.TxtObservaciones.Clear();
            diseñoInventario.TxtCantidadEntrada.Clear();
            diseñoInventario.LblStockDisponible.Text = "Stock Disponible: N/A";
        }

        private void BtnLimpiar_Click(object sender, EventArgs e)
        {
            diseñoInventario.DgvInventario.Rows.Clear();
            ActualizarTotales();
        }

        private void DgvInventario_RowsRemoved(object sender, DataGridViewRowsRemovedEventArgs e)
        {
            ActualizarTotales();
        }

        private void ActualizarTotales()
        {
            decimal subtotal = CalcularSubtotal();
            decimal iva = CalcularIVA();
            decimal total = CalcularTotal();

            diseñoInventario.LblSubtotal.Text = $"Subtotal: {subtotal:C2}";
            diseñoInventario.LblIVA.Text = $"IVA (16%): {iva:C2}";
            diseñoInventario.LblTotal.Text = $"Total: {total:C2}";
        }

        private decimal CalcularSubtotal()
        {
            decimal subtotal = 0m;
            foreach (DataGridViewRow row in diseñoInventario.DgvInventario.Rows)
            {
                if (row.IsNewRow) continue;
                decimal totalProducto = decimal.Parse(row.Cells["Total"].Value.ToString(), System.Globalization.NumberStyles.Currency);
                subtotal += totalProducto;
            }
            return subtotal;
        }

        private decimal CalcularIVA()
        {
            decimal subtotal = CalcularSubtotal();
            decimal iva = subtotal * 0.16m;
            return iva;
        }

        private decimal CalcularTotal()
        {
            decimal subtotal = CalcularSubtotal();
            decimal iva = CalcularIVA();
            return subtotal + iva;
        }
    }
}
