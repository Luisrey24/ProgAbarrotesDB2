using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Windows.Forms;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.VisualBasic; // Necesario para InputBox

namespace ProgAbarrotesDB
{
    public partial class FormImprimirTicket : Form
    {
        private int _idVenta;
        private DataTable _datosVenta;
        private readonly int[] _denominaciones = new int[] { 1000, 500, 200, 100, 50, 20, 10, 5, 2, 1 };
        private string _connectionString = "Data Source=RAMSES;Initial Catalog=TiendaBD;Integrated Security=True;";

        public FormImprimirTicket(int idVenta)
        {
            InitializeComponent();
            _idVenta = idVenta;
            this.Load += FormImprimirTicket_Load;
        }

        private void FormImprimirTicket_Load(object sender, EventArgs e)
        {
            try
            {
                // Obtener los datos de la venta
                ObtenerDatosVenta();

                // Calcular el total de la venta sumando todas las filas
                decimal total = Convert.ToDecimal(_datosVenta.Compute("SUM(Total)", string.Empty));

                decimal montoRecibido = total;
                decimal cambio = 0;
                Dictionary<int, int> desgloseBilletes = null;

                // Verificar si el total excede 1000 para solicitar el monto recibido
                if (total > 1000)
                {
                    // Solicitar el monto recibido al usuario
                    montoRecibido = SolicitarMontoRecibido(total);

                    // Calcular el cambio
                    cambio = montoRecibido - total;

                    // Calcular el desglose de billetes para el cambio
                    desgloseBilletes = CalcularDesgloseBilletes(cambio);
                }

                // Generar el PDF y obtener la ruta del archivo
                string rutaCompleta = GenerarPDF(total, montoRecibido, cambio, desgloseBilletes);

                // Abrir el PDF generado
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = rutaCompleta,
                        UseShellExecute = true
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"No se pudo abrir el archivo PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                MessageBox.Show("Ticket generado exitosamente como PDF.", "Éxito", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Operación cancelada por el usuario.", "Cancelado", MessageBoxButtons.OK, MessageBoxIcon.Information);
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al generar el ticket: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        /// <summary>
        /// Obtiene los datos de la venta desde la vista vw_TicketVenta.
        /// </summary>
        private void ObtenerDatosVenta()
        {
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string query = "SELECT * FROM vw_TicketVenta WHERE ID_Venta = @ID_Venta";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@ID_Venta", _idVenta);
                    SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                    _datosVenta = new DataTable();
                    adapter.Fill(_datosVenta);
                }
            }

            if (_datosVenta.Rows.Count == 0)
            {
                throw new Exception("No se encontraron datos para la venta especificada.");
            }
        }

        /// <summary>
        /// Solicita al usuario el monto recibido mediante un InputBox.
        /// </summary>
        /// <param name="total">El total de la venta.</param>
        /// <returns>El monto recibido como decimal.</returns>
        private decimal SolicitarMontoRecibido(decimal total)
        {
            decimal monto = 0;
            bool montoValido = false;

            while (!montoValido)
            {
                string input = Interaction.InputBox($"El total de la venta es {total:C}. Por favor, ingrese el monto recibido:", "Monto Recibido");

                if (string.IsNullOrWhiteSpace(input))
                {
                    // Si el usuario cancela o no ingresa nada
                    throw new OperationCanceledException("El usuario canceló la operación.");
                }

                if (decimal.TryParse(input, out monto))
                {
                    if (monto >= total)
                    {
                        montoValido = true;
                    }
                    else
                    {
                        MessageBox.Show("El monto recibido es menor al total de la venta. Por favor, ingrese un monto válido.", "Monto Inválido", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    }
                }
                else
                {
                    MessageBox.Show("Entrada inválida. Por favor, ingrese un número válido.", "Error de Formato", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            return monto;
        }

        /// <summary>
        /// Calcula el desglose de billetes para un monto dado.
        /// </summary>
        /// <param name="monto">El monto para desglosar en billetes.</param>
        /// <returns>Un diccionario con la denominación y la cantidad de cada billete.</returns>
        private Dictionary<int, int> CalcularDesgloseBilletes(decimal monto)
        {
            var desglose = new Dictionary<int, int>();

            int montoInt = (int)Math.Round(monto, 0); // Redondear para evitar centavos

            foreach (int billete in _denominaciones)
            {
                if (montoInt >= billete)
                {
                    int cantidad = montoInt / billete;
                    desglose[billete] = cantidad;
                    montoInt %= billete;
                }
                else
                {
                    desglose[billete] = 0;
                }
            }

            return desglose;
        }

        /// <summary>
        /// Genera el ticket PDF y devuelve la ruta completa del archivo generado.
        /// </summary>
        /// <param name="total">El total de la venta.</param>
        /// <param name="montoRecibido">El monto recibido del cliente.</param>
        /// <param name="cambio">El cambio a devolver al cliente.</param>
        /// <param name="desgloseBilletes">El desglose de billetes para el cambio.</param>
        /// <returns>La ruta completa del archivo PDF generado.</returns>
        private string GenerarPDF(decimal total, decimal montoRecibido, decimal cambio, Dictionary<int, int> desgloseBilletes)
        {
            // Crear el documento PDF
            Document doc = new Document(PageSize.A7, 10, 10, 10, 10); // Tamaño para ticket
            string nombreArchivo = $"TicketVenta_{_idVenta}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf";
            string rutaCompleta = Path.Combine(Application.StartupPath, nombreArchivo);

            using (FileStream fs = new FileStream(rutaCompleta, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                PdfWriter writer = PdfWriter.GetInstance(doc, fs);
                doc.Open();

                // Crear las fuentes
                var fontTitulo = FontFactory.GetFont(FontFactory.COURIER_BOLD, 10);
                var fontSubtitulo = FontFactory.GetFont(FontFactory.COURIER, 8);
                var fontEncabezado = FontFactory.GetFont(FontFactory.COURIER_BOLD, 8);
                var fontTexto = FontFactory.GetFont(FontFactory.COURIER, 8);

                // Datos del encabezado
                Paragraph encabezadoRestaurante = new Paragraph("RESTAURANTE MODELO, SA DE CV", fontTitulo) { Alignment = Element.ALIGN_CENTER };
                doc.Add(encabezadoRestaurante);

                Paragraph direccion = new Paragraph("Los Morales Num 456, Col. Naucalpan\nMonterrey, N.L.\nRFC RMO-980304-MN9", fontSubtitulo) { Alignment = Element.ALIGN_CENTER };
                direccion.SpacingAfter = 5f; // Espacio después del párrafo de dirección
                doc.Add(direccion);

                // Información de la venta
                Paragraph infoVenta = new Paragraph(
                    $"Folio: {Convert.ToInt32(_datosVenta.Rows[0]["ID_Venta"]):D8}\n" +
                    $"Fecha de Ticket: {DateTime.Now:dd/MM/yyyy}\n" +
                    $"Hora de Impresión: {DateTime.Now:hh:mm tt}\n" +
                    $"Número de Mesa: 005",
                    fontSubtitulo
                );
                infoVenta.Alignment = Element.ALIGN_LEFT;
                infoVenta.SpacingAfter = 5f; // Espacio después de la información de venta
                doc.Add(infoVenta);

                // Detalle de productos
                PdfPTable table = new PdfPTable(3);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 2, 1, 1 }); // Proporciones de columnas
                table.SpacingBefore = 5f; // Espacio antes de la tabla
                table.SpacingAfter = 5f;  // Espacio después de la tabla

                // Encabezados de la tabla
                PdfPCell cellProducto = new PdfPCell(new Phrase("Producto", fontEncabezado)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 2 };
                PdfPCell cellCant = new PdfPCell(new Phrase("Cant", fontEncabezado)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 2 };
                PdfPCell cellImporte = new PdfPCell(new Phrase("Importe", fontEncabezado)) { HorizontalAlignment = Element.ALIGN_CENTER, Padding = 2 };

                // Bordes para encabezados
                cellProducto.Border = Rectangle.BOTTOM_BORDER;
                cellCant.Border = Rectangle.BOTTOM_BORDER;
                cellImporte.Border = Rectangle.BOTTOM_BORDER;

                table.AddCell(cellProducto);
                table.AddCell(cellCant);
                table.AddCell(cellImporte);

                // Iterar sobre cada fila de la venta para extraer productos y cantidades
                foreach (DataRow detalle in _datosVenta.Rows)
                {
                    string nombreProducto = detalle["NombreProducto"].ToString().Trim();
                    string cantidad = detalle["Cantidad"].ToString().Trim();
                    string importe = detalle["Total"].ToString().Trim(); // Asumiendo que "Total" es el importe por producto

                    // Validación adicional (opcional)
                    if (string.IsNullOrEmpty(nombreProducto) || string.IsNullOrEmpty(cantidad) || string.IsNullOrEmpty(importe))
                    {
                        continue; // Saltar filas incompletas
                    }

                    // Añadir celdas a la tabla
                    PdfPCell cellDesc = new PdfPCell(new Phrase(nombreProducto, fontTexto)) { HorizontalAlignment = Element.ALIGN_LEFT, Padding = 2 };
                    PdfPCell cellQty = new PdfPCell(new Phrase(cantidad, fontTexto)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 2 };
                    PdfPCell cellImp = new PdfPCell(new Phrase(Convert.ToDecimal(importe).ToString("C"), fontTexto)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 2 };

                    // Bordes opcionales para las filas de datos
                    cellDesc.Border = Rectangle.NO_BORDER;
                    cellQty.Border = Rectangle.NO_BORDER;
                    cellImp.Border = Rectangle.NO_BORDER;

                    table.AddCell(cellDesc);
                    table.AddCell(cellQty);
                    table.AddCell(cellImp);
                }

                doc.Add(table);

                // Cálculo de Totales
                PdfPTable tableTotales = new PdfPTable(2);
                tableTotales.WidthPercentage = 100;
                tableTotales.SetWidths(new float[] { 1, 1 });
                tableTotales.SpacingBefore = 5f; // Espacio antes de la tabla de totales
                tableTotales.SpacingAfter = 5f;  // Espacio después de la tabla de totales

                // Total
                PdfPCell cellTotalDesc = new PdfPCell(new Phrase("Total:", fontEncabezado)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_LEFT, Padding = 2 };
                PdfPCell cellTotal = new PdfPCell(new Phrase(total.ToString("C"), fontEncabezado)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 2 };
                tableTotales.AddCell(cellTotalDesc);
                tableTotales.AddCell(cellTotal);

                if (total > 1000)
                {
                    // Monto Recibido
                    PdfPCell cellMontoDesc = new PdfPCell(new Phrase("Monto Recibido:", fontEncabezado)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_LEFT, Padding = 2 };
                    PdfPCell cellMonto = new PdfPCell(new Phrase(montoRecibido.ToString("C"), fontEncabezado)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 2 };
                    tableTotales.AddCell(cellMontoDesc);
                    tableTotales.AddCell(cellMonto);

                    // Cambio
                    PdfPCell cellCambioDesc = new PdfPCell(new Phrase("Cambio:", fontEncabezado)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_LEFT, Padding = 2 };
                    PdfPCell cellCambio = new PdfPCell(new Phrase(cambio.ToString("C"), fontEncabezado)) { Border = Rectangle.NO_BORDER, HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 2 };
                    tableTotales.AddCell(cellCambioDesc);
                    tableTotales.AddCell(cellCambio);
                }

                doc.Add(tableTotales);

                if (total > 1000 && desgloseBilletes != null)
                {
                    // Desglose de Billetes
                    Paragraph tituloDesglose = new Paragraph("\nDesglose de Cambio:", fontEncabezado) { Alignment = Element.ALIGN_LEFT };
                    tituloDesglose.SpacingAfter = 3f; // Espacio después del título
                    doc.Add(tituloDesglose);

                    PdfPTable tableBilletes = new PdfPTable(2);
                    tableBilletes.WidthPercentage = 100;
                    tableBilletes.SetWidths(new float[] { 1, 1 });
                    tableBilletes.SpacingBefore = 2f; // Espacio antes de la tabla de billetes
                    tableBilletes.SpacingAfter = 5f;  // Espacio después de la tabla de billetes

                    PdfPCell cellDenominacion = new PdfPCell(new Phrase("Denominación", fontTexto)) { HorizontalAlignment = Element.ALIGN_LEFT, Padding = 2 };
                    PdfPCell cellCantidad = new PdfPCell(new Phrase("Cantidad", fontTexto)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 2 };

                    // Bordes para encabezados de billetes
                    cellDenominacion.Border = Rectangle.BOTTOM_BORDER;
                    cellCantidad.Border = Rectangle.BOTTOM_BORDER;

                    tableBilletes.AddCell(cellDenominacion);
                    tableBilletes.AddCell(cellCantidad);

                    foreach (var billete in desgloseBilletes)
                    {
                        if (billete.Value > 0)
                        {
                            PdfPCell cellDen = new PdfPCell(new Phrase($"${billete.Key}", fontTexto)) { HorizontalAlignment = Element.ALIGN_LEFT, Padding = 2 };
                            PdfPCell cellQty = new PdfPCell(new Phrase(billete.Value.ToString(), fontTexto)) { HorizontalAlignment = Element.ALIGN_RIGHT, Padding = 2 };

                            // Bordes opcionales para las filas de billetes
                            cellDen.Border = Rectangle.NO_BORDER;
                            cellQty.Border = Rectangle.NO_BORDER;

                            tableBilletes.AddCell(cellDen);
                            tableBilletes.AddCell(cellQty);
                        }
                    }

                    doc.Add(tableBilletes);
                }

                // Pie del ticket
                Paragraph pie = new Paragraph("\nGracias por su compra\nVuelva pronto", fontTexto) { Alignment = Element.ALIGN_CENTER };
                doc.Add(pie);

                doc.Close();
            }

            return rutaCompleta; // Devolver la ruta completa del PDF generado
        }
    }
}
