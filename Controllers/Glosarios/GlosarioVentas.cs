using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Glosarios : BaseController
    {

        [HttpGet("api/v1/glosarios/glosario-ventas")]
        public async Task<IActionResult> ObtenerGlosarioVentas()
        {
            try
            {
                // Lista para almacenar el glosario dinámico
                var glosario = new List<Dictionary<string, object>>();
                // Diccionario de descripciones personalizadas
                var descripciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"ID", "Identificador único de la transacción"},
                    {"Codigo", "Código único del documento de compra"},
                    {"Cliente", "Nombre del tipo de cliente que realizo la compra"},
                    {"Tipo", "Tipo de movimiento o transacción"},
                    {"Movimiento", "Clasificación del movimiento contable"},
                    {"Articulo", "Código único del artículo comprado"},
                    {"Nombre", "Nombre descriptivo del artículo"},
                    {"Categoria", "Categoría principal de clasificación"},
                    {"Grupo", "Grupo de clasificación secundaria"},
                    {"Linea", "Línea de productos asociada"},
                    {"Familia", "Familia de productos específica"},
                    {"ImporteUnitario", "Valor unitario del artículo en moneda local"},
                    {"ImporteTotal", "Valor total de la transacción en moneda local"},
                    {"Cantidad", "Número de unidades adquiridas"},
                    {"Almacen", "Ubicación física del inventario"},
                    {"FechaEmision", "Fecha de emisión del documento"},
                    {"Mes", "Mes de la transacción en formato numérico"},
                    {"Año", "Año de la transacción en formato numérico"}
                };

                // Abre la conexión a la base de datos
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(@"
                    SELECT 
                        top (1) *
                    FROM 
                        Temp_VentasReport
                ", connection) // Solo un registro para obtener columnas
                {
                    CommandTimeout = 30
                };

                // Ejecutar el query y obtener los metadatos de las columnas
                await using var reader = await command.ExecuteReaderAsync();
                var schemaTable = reader.GetSchemaTable();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    // Obtener el nombre de la columna
                    string columnName = reader.GetName(i);
                    var columnMetadata = schemaTable.Rows[i];

                    // Construcción dinámica del glosario
                    var columna = new Dictionary<string, object>
                    {
                        { "Nombre", columnName },
                        { "TipoDato", reader.GetDataTypeName(i) },
                        { "Tamaño", columnMetadata["ColumnSize"] },
                        { "EsNulo", (bool)columnMetadata["AllowDBNull"] },
                        { "Descripcion", descripciones.TryGetValue(columnName, out var desc)
                        ? desc
                        : "Descripción no definida" }
                    };

                    glosario.Add(columna);
                }

                // Devuelve el glosario generado
                return Ok(glosario);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error al generar el glosario dinámico.");
            }
        }
    }
}
