using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Filtros : BaseController
    {
        [HttpGet("api/v1/filtros/glosario-compras")]
        public async Task<IActionResult> ObtenerGlosarioCompras()
        {
            try
            {
                // Lista para almacenar el glosario dinámico
                var glosario = new List<Dictionary<string, object>>();

                // Abre la conexión a la base de datos
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(@"
                USE [TC032841E]
                SELECT
                    cb.Codigo, 
                    cd.Articulo,
                    A.Descripcion1 AS Descripcion,
                    c.Estatus,
                    cd.Unidad, 
                    cd.Sucursal,
                    p.Nombre AS ProveedorNombre
                FROM 
                    [TC032841E].[dbo].[CB] cb
                JOIN 
                    [TC032841E].[dbo].[CompraD] cd ON cb.Cuenta = cd.Articulo 
                LEFT JOIN 
                    [TC032841E].[dbo].[Compra] c ON cd.ID = c.ID
                LEFT JOIN 
                    [TC032841E].[dbo].[Prov] p ON c.Proveedor = p.Proveedor
                LEFT JOIN
                    [TC032841E].[dbo].[ArtUnidad] U ON cb.Cuenta = U.Articulo
                LEFT JOIN
                    [TC032841E].[dbo].[Art] A ON cb.Cuenta = A.Articulo
                ORDER BY (SELECT NULL)
                OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY", connection) // Solo un registro para obtener columnas
                {
                    CommandTimeout = 30
                };

                // Ejecutar el query y obtener los metadatos de las columnas
                await using var reader = await command.ExecuteReaderAsync();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    // Obtener el nombre de la columna
                    string columnName = reader.GetName(i);

                    // El alias puede estar precedido de un prefijo en el query (e.g., cb.Codigo, A.Descripcion1)
                    string columnValue = columnName;

                    // Detectar el alias de la columna, si existe
                    if (columnValue.Contains("."))
                    {
                        var parts = columnValue.Split('.');
                        columnValue = parts[0]; // Obtiene el alias de la tabla
                    }

                    // Construcción dinámica del glosario
                    var columna = new Dictionary<string, object>
                    {
                        { "Nombre", columnName },
                        { "Valor", columnValue }, // El valor ahora tiene el alias
                        { "TipoDato", reader.GetDataTypeName(i) },
                        { "Tamaño", reader.GetFieldType(i).ToString() }
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
