using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;
using System.Text;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {

        [HttpGet("api/v1/reporteria/ventas")]
        public async Task<IActionResult> ObtenerVentas(
            [FromQuery] string? codigo,
            [FromQuery] string? articulo,
            [FromQuery] string? descripcion1,
            [FromQuery] decimal? minPrecio,
            [FromQuery] decimal? maxPrecio,
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
                    [TC032841E].[dbo].[VentaD] VTA
                INNER JOIN 
                    [TC032841E].[dbo].[Venta] VTE ON VTE.ID = VTA.ID
                LEFT JOIN 
                    [TC032841E].[dbo].[ART] A ON A.ARTICULO = VTA.Articulo
                WHERE 
                    VTE.Mov = 'NOTA' AND VTE.Estatus IN ('CONCLUIDO','PROCESAR')";

            // Construcción de la cláusula WHERE de manera dinámica
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(codigo))
            {
                whereClauses.Add("VTA.Codigo LIKE @Codigo");
                parameters.Add(new SqlParameter("@Codigo", $"{codigo}"));
            }
            if (!string.IsNullOrEmpty(articulo))
            {
                whereClauses.Add("VTA.Articulo LIKE @Articulo");
                parameters.Add(new SqlParameter("@Articulo", $"%{articulo}%"));
            }
            if (!string.IsNullOrEmpty(descripcion1))
            {
                whereClauses.Add("A.Descripcion1 LIKE @Descripcion1");
                parameters.Add(new SqlParameter("@Descripcion1", $"%{descripcion1}%"));
            }
            if (minPrecio.HasValue)
            {
                whereClauses.Add("VTA.Precio >= @MinPrecio");
                parameters.Add(new SqlParameter("@MinPrecio", minPrecio.Value));
            }
            if (maxPrecio.HasValue)
            {
                whereClauses.Add("VTA.Precio <= @MaxPrecio");
                parameters.Add(new SqlParameter("@MaxPrecio", maxPrecio.Value));
            }
            if (startDate.HasValue)
            {
                whereClauses.Add("VTE.FechaEmision >= @StartDate");
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }
            if (endDate.HasValue)
            {
                whereClauses.Add("VTE.FechaEmision <= @EndDate");
                parameters.Add(new SqlParameter("@EndDate", endDate.Value));
            }

            // Si hay cláusulas WHERE, agregarlas al query base
            var whereQuery = whereClauses.Any() ? $" AND {string.Join(" AND ", whereClauses)}" : "";

            // Construcción de la consulta para el total de registros
            var countQueryBuilder = new StringBuilder($"SELECT COUNT(1) {baseQuery} {whereQuery}");

            // Construcción de la consulta con paginación
            var queryBuilder = new StringBuilder($@"
                USE [TC032841E]
                SELECT 
                    ROW_NUMBER() OVER (ORDER BY VTA.Codigo) AS ID,
                    VTA.Codigo,
                    VTA.Articulo,
                    A.Descripcion1 AS Nombre,
                    VTA.Precio,
                    VTA.Costo,
                    VTA.Cantidad,
                    VTE.Importe,
                    VTE.Impuestos,
                    VTE.CostoTotal,
                    VTE.PrecioTotal,
                    VTA.Unidad,
                    VTA.Sucursal,
                    VTE.FechaEmision,
                    NULLIF(VTA.TipoImpuesto1, '') AS [IVA], 
                    NULLIF(VTA.TipoImpuesto2, '') AS [IEPS], 
                    NULLIF(VTA.TipoImpuesto3, '') AS  [ISR],
                    NULLIF(VTA.Impuesto1, '') AS [IVA%], 
                    NULLIF(VTA.Impuesto2, '') AS [IEPS%], 
                    NULLIF(VTA.Impuesto3, '') AS [ISR%]
                {baseQuery} {whereQuery}
                ORDER BY (SELECT NULL)
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY");

            try
            {
                int totalRecords;

                // Abre la conexión de forma segura
                await using var connection = await OpenConnectionAsync();

                // Crear una copia de los parámetros para el conteo
                var countParameters = parameters.Select(p => new SqlParameter(p.ParameterName, p.Value)).ToArray();

                // Ejecutar consulta para obtener el total de registros
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
