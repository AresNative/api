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
            [FromQuery] Dictionary<string, string?> filters,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validación de entrada
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

            // Query base
            var baseQuery = @"
        FROM 
            [TC032841E].[dbo].[VentaD] VTA
        INNER JOIN 
            [TC032841E].[dbo].[Venta] VTE ON VTE.ID = VTA.ID
        LEFT JOIN 
            [TC032841E].[dbo].[ART] A ON A.ARTICULO = VTA.Articulo
        WHERE 
            VTE.Mov = 'NOTA' AND VTE.Estatus IN ('CONCLUIDO','PROCESAR')";

            // Construcción de cláusulas WHERE dinámicas
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var filter in filters)
            {
                if (!string.IsNullOrWhiteSpace(filter.Value))
                {
                    var columnName = filter.Key;
                    var parameterName = $"@{filter.Key.Replace(".", "_")}";
                    whereClauses.Add($"{columnName} LIKE {parameterName}");
                    parameters.Add(new SqlParameter(parameterName, $"%{filter.Value}%"));
                }
            }

            var whereQuery = whereClauses.Any() ? $" AND {string.Join(" AND ", whereClauses)}" : "";

            // Query para conteo total
            var countQuery = $"SELECT COUNT(1) {baseQuery} {whereQuery}";

            // Query con paginación
            var paginatedQuery = $@"
        USE [TC032841E];
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
            ISNULL(NULLIF(VTA.TipoImpuesto1, ''), 0) AS [IVA], 
            ISNULL(NULLIF(VTA.TipoImpuesto2, ''), 0) AS [IEPS], 
            ISNULL(NULLIF(VTA.TipoImpuesto3, ''), 0) AS [ISR],
            ISNULL(NULLIF(VTA.Impuesto1, ''), 0) AS [IVA%], 
            ISNULL(NULLIF(VTA.Impuesto2, ''), 0) AS [IEPS%], 
            ISNULL(NULLIF(VTA.Impuesto3, ''), 0) AS [ISR%]
        {baseQuery} {whereQuery}
        ORDER BY VTA.Codigo
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Conteo total
                await using var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(parameters.ToArray());
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Consulta con paginación
                var paginatedParameters = new List<SqlParameter>(parameters)
        {
            new SqlParameter("@Offset", offset),
            new SqlParameter("@PageSize", pageSize)
        };

                await using var command = new SqlCommand(paginatedQuery, connection);
                command.Parameters.AddRange(paginatedParameters.ToArray());
                var results = new List<Dictionary<string, object>>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        row[reader.GetName(i)] = reader.GetValue(i);
                    }
                    results.Add(row);
                }

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
                return HandleException(ex, paginatedQuery);
            }
        }
    }
}
