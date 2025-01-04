using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;
using System.Text;

namespace MyApiProject.Controllers
{
    public partial class Filtros : BaseController
    {
        [HttpGet("api/v1/filtros/compras")]
        public async Task<IActionResult> ComprasLost(
            [FromQuery] string? searchTerm,
            [FromQuery] string? searchField,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10
        )
        {
            if (page <= 0 || pageSize <= 0)
            {
                return BadRequest(new { Message = "Los valores de 'page' y 'pageSize' deben ser mayores a cero." });
            }

            // Validar que el campo de búsqueda sea permitido
            var allowedFields = new[] { "cb.Codigo", "A.Descripcion1", "A.Proveedor", "A.Codigo", "A.Articulo" };
            if (!string.IsNullOrEmpty(searchField) && !allowedFields.Contains(searchField))
            {
                return BadRequest(new { Message = "El campo de búsqueda no es válido." });
            }

            // Construcción del query base
            var baseQuery = @"
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
                    [TC032841E].[dbo].[Art] A ON cb.Cuenta = A.Articulo";

            // Construcción de la cláusula WHERE de manera dinámica
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(searchTerm) && !string.IsNullOrEmpty(searchField))
            {
                whereClauses.Add($"{searchField} LIKE @SearchTerm");
                parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            var whereQuery = whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "";

            // Paginación
            var offset = (page - 1) * pageSize;

            // Construcción de la consulta completa
            var queryBuilder = new StringBuilder($@"
                USE [TC032841E]
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY cb.Codigo) AS ID,
                    cb.Codigo, 
                    cd.Articulo,
                    A.Descripcion1 AS Nombre,
                    c.Estatus,
                    cd.Unidad, 
                    U.Factor AS Equivalente, 
                    cd.CODIGO AS CompraCodigo, 
                    cd.Cantidad, 
                    cd.Costo,
                    cd.Sucursal,
                    c.FechaEmision, 
                    p.Nombre AS ProveedorNombre,
                    NULLIF(A.Impuesto1, '') AS [IVA%], 
                    NULLIF(A.Impuesto2, '') AS [IEPS%], 
                    NULLIF(A.Impuesto3, '') AS [ISR%]
                {baseQuery}
                {whereQuery}
                ORDER BY (SELECT NULL)
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;");

            try
            {
                await using var connection = await OpenConnectionAsync();

                await using var command = new SqlCommand(queryBuilder.ToString(), connection)
                {
                    CommandTimeout = 30
                };

                // Asigna los parámetros al comando
                command.Parameters.AddRange(parameters.ToArray());
                command.Parameters.AddWithValue("@Offset", offset);
                command.Parameters.AddWithValue("@PageSize", pageSize);

                // Ejecuta la consulta
                await using var reader = await command.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (var i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    }
                    results.Add(row);
                }

                // Crear la respuesta
                var response = new
                {
                    Page = page,
                    PageSize = pageSize,
                    TotalRecords = results.Count,
                    Data = results
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, queryBuilder.ToString());
            }
        }
    }
}
