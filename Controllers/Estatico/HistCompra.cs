using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;
using System.Text;

namespace MyApiProject.Controllers
{
    public partial class Estatico : BaseController
    {
        public Estatico(IConfiguration configuration) : base(configuration) { }

        [HttpGet("api/v1/estatico/historial-compras")]
        public async Task<IActionResult> HistorialCompras(
            [FromQuery] string? articulo,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? top = 10,  // Añadido parámetro 'top' para limitar los registros
            [FromQuery] string? groupByColumn = null,  // Parámetro para la columna a agrupar
            [FromQuery] string? sumColumn = null)  // Parámetro para la columna a sumar
        {
            // Validación de 'top'
            if (top <= 0 || top > 100) top = 10;

            // Construcción del query base
            var baseQuery = @"
                FROM 
                    HistorialCompra";

            // Construcción de la cláusula WHERE de manera dinámica con LIKE y rango de fechas
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(articulo))
            {
                whereClauses.Add("DescripcionArt LIKE @articulo");
                parameters.Add(new SqlParameter("@articulo", $"%{articulo}%"));
            }

            if (startDate.HasValue)
            {
                whereClauses.Add("FechaEmision >= @StartDate");
                parameters.Add(new SqlParameter("@StartDate", startDate.Value));
            }
            if (endDate.HasValue)
            {
                whereClauses.Add("FechaEmision <= @EndDate");
                parameters.Add(new SqlParameter("@EndDate", endDate.Value));
            }

            // Si hay cláusulas WHERE, agregarlas al query base
            var whereQuery = whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "";

            // Construcción de la consulta con agrupación y suma
            var queryBuilder = new StringBuilder($@"
                SELECT TOP {top.Value}
                    {groupByColumn ?? "ProveedorNom"},
                    SUM(ROUND({sumColumn ?? "Costo"}, 2)) AS Total,
                    COUNT(*) AS NumeroCompras
                    {baseQuery} {whereQuery}
                    GROUP BY {groupByColumn ?? "ProveedorNom"}
                    order by Total desc
            ");

            try
            {
                // Abre la conexión de forma segura
                await using var connection = await OpenConnectionAsync();

                // Ejecutar la consulta
                await using var command = new SqlCommand(queryBuilder.ToString(), connection)
                {
                    CommandTimeout = 30
                };

                // Asigna los parámetros al comando
                command.Parameters.AddRange(parameters.ToArray());

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
                return Ok(new
                {
                    Data = results
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, queryBuilder.ToString());
            }
        }
    }
}
