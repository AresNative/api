using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Glosarios : BaseController
    {
        public Glosarios(IConfiguration configuration) : base(configuration) { }
        [HttpGet("api/v1/glosarios/glosario-compras")]
        public async Task<IActionResult> ObtenerGlosarioCompras()
        {
            try
            {
                var glosario = new List<Dictionary<string, object>>();

                // Diccionario de descripciones personalizadas
                var descripciones = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    {"ID", "Identificador único de la transacción"},
                    {"Codigo", "Código único del documento de compra"},
                    {"Proveedor", "Nombre del proveedor o suministrador"},
                    {"Tipo", "Tipo de movimiento o transacción"},
                    {"Movimiento", "Clasificación del movimiento contable"},
                    {"Articulo", "Código único del artículo comprado"},
                    {"Nombre", "Nombre descriptivo del artículo"},
                    {"Categoria", "Categoría principal de clasificación"},
                    {"Grupo", "Grupo de clasificación secundaria"},
                    {"Linea", "Línea de productos asociada"},
                    {"Familia", "Familia de productos específica"},
                    {"CostoUnitario", "Valor unitario del artículo en moneda local"},
                    {"CostoTotal", "Valor total de la transacción en moneda local"},
                    {"Cantidad", "Número de unidades adquiridas"},
                    {"Almacen", "Ubicación física del inventario"},
                    {"FechaEmision", "Fecha de emisión del documento"},
                    {"Mes", "Mes de la transacción en formato numérico"},
                    {"Año", "Año de la transacción en formato numérico"}
                };

                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(@"
            SELECT TOP 1 *
            FROM [LOCAL_TC032391E].[dbo].[Temp_ComprasReport]
        ", connection);

                await using var reader = await command.ExecuteReaderAsync();
                var schemaTable = reader.GetSchemaTable();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string columnName = reader.GetName(i);
                    var columnMetadata = schemaTable.Rows[i];

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

                return Ok(glosario);
            }
            catch (Exception ex)
            {
                return HandleException(ex, "Error al generar el glosario dinámico.");
            }
        }
    }
}
