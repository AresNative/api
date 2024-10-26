using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;
using System.Text;

namespace MyApiProject.Controllers
{
    public partial class TestQuery : BaseController
    {
        public TestQuery(IConfiguration configuration) : base(configuration) { }

        [HttpGet("api/v1/testQuery/compras")]
        public async Task<IActionResult> ObtenerCompras(
            [FromQuery] string? codigo,
            [FromQuery] string? estatus,
            [FromQuery] string? articulo,
            [FromQuery] string? proveedor,
            [FromQuery] string? descripcion,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validación de paginación
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

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

            // Construcción de la cláusula WHERE de manera dinámica con LIKE y rango de fechas
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(codigo))
            {
                whereClauses.Add("cb.Codigo LIKE @Codigo");
                parameters.Add(new SqlParameter("@Codigo", $"{codigo}"));
            }
            if (!string.IsNullOrEmpty(estatus))
            {
                whereClauses.Add("c.Estatus LIKE @Estatus");
                parameters.Add(new SqlParameter("@Estatus", $"%{estatus}%"));
            }
            if (!string.IsNullOrEmpty(articulo))
            {
                whereClauses.Add("cd.Articulo LIKE @Articulo");
                parameters.Add(new SqlParameter("@Articulo", $"%{articulo}%"));
            }
            if (!string.IsNullOrEmpty(proveedor))
            {
                whereClauses.Add("c.Proveedor LIKE @Proveedor");
                parameters.Add(new SqlParameter("@Proveedor", $"%{proveedor}%"));
            }
            if (startDate.HasValue)
            {
                whereClauses.Add("c.FechaEmision >= @StartDate");
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }
            if (endDate.HasValue)
            {
                whereClauses.Add("c.FechaEmision <= @EndDate");
                parameters.Add(new SqlParameter("@EndDate", endDate.Value));
            }
            if (!string.IsNullOrEmpty(descripcion))
            {
                whereClauses.Add("A.Descripcion1 LIKE @Descripcion");
                parameters.Add(new SqlParameter("@Descripcion", $"%{descripcion}%"));
            }

            // Si hay cláusulas WHERE, agregarlas al query base
            var whereQuery = whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "";

            // Construcción de la consulta para el total de registros
            var countQueryBuilder = new StringBuilder($"SELECT COUNT(1) {baseQuery} {whereQuery}");

            // Construcción de la consulta con paginación
            var queryBuilder = new StringBuilder($@"
                USE [TC032841E]
                SELECT
                    cb.Codigo, 
                    cd.Articulo,
                    A.Descripcion1 AS Nombre,
                    c.Estatus,
                    cd.Unidad, 
                    U.Factor AS Equivalente, 
                    cd.ID AS CompraID, 
                    cd.CODIGO AS CompraCodigo, 
                    cd.Cantidad, 
                    cd.Costo,
                    cd.Sucursal,
                    c.MovID, 
                    c.FechaEmision, 
                    c.Proveedor,
                    p.Nombre AS ProveedorNombre,
                    CONCAT_WS(', ', 
                        NULLIF(A.TipoImpuesto1, ''), 
                        NULLIF(A.TipoImpuesto2, ''), 
                        NULLIF(A.TipoImpuesto3, '')
                    ) as Impuestos
                {baseQuery} {whereQuery}
                ORDER BY cb.Codigo
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");

            try
            {
                int totalRecords;
                // Abre la conexión de forma segura
                await using var connection = await OpenConnectionAsync();

                // Crear una copia de los parámetros para el comando de conteo
                var countParameters = parameters.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToArray();

                // Ejecutar la consulta para obtener el total de registros
                await using (var countCommand = new SqlCommand(countQueryBuilder.ToString(), connection))
                {
                    countCommand.Parameters.AddRange(countParameters);
                    totalRecords = (int)await countCommand.ExecuteScalarAsync();
                }

                // Crear una copia de los parámetros para la consulta con paginación
                var paginatedParameters = parameters.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToList();
                paginatedParameters.Add(new SqlParameter("@Offset", offset));
                paginatedParameters.Add(new SqlParameter("@PageSize", pageSize));

                // Ejecutar la consulta con paginación
                await using var command = new SqlCommand(queryBuilder.ToString(), connection)
                {
                    CommandTimeout = 30
                };

                // Asigna los parámetros al comando
                command.Parameters.AddRange(paginatedParameters.ToArray());

                // Ejecuta la consulta
                await using var reader = await command.ExecuteReaderAsync();
                var results = new List<Dictionary<string, object>>();

                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }

                // Crear la respuesta en el formato solicitado
                var response = new
                {
                    TotalRecords = totalRecords,
                    Page = page,
                    PageSize = pageSize,
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
