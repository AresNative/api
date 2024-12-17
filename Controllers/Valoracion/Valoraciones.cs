using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class ValoracionesController : BaseController
    {
        [HttpGet("api/v1/valoracion")]
        public async Task<IActionResult> ObtenerValoracion(
            [FromQuery] string? comment,
            [FromQuery] string? valor)
        {
            // Construcción del query base
            var baseQuery = @"SELECT [comment]
                                    ,[valor]
                                FROM [LOCAL_TC032391E].[dbo].[Website_Valoracion]";

            // Construcción de la cláusula WHERE de manera dinámica
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(comment))
            {
                whereClauses.Add("[comment] LIKE @Comment");
                parameters.Add(new SqlParameter("@Comment", $"%{comment}%"));
            }
            if (!string.IsNullOrEmpty(valor))
            {
                whereClauses.Add("[valor] LIKE @CorreoElectronico");
                parameters.Add(new SqlParameter("@CorreoElectronico", $"%{valor}%"));
            }


            // Si hay cláusulas WHERE, agregarlas al query base
            var whereQuery = whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "";
            var finalQuery = $"{baseQuery}{whereQuery}";

            try
            {
                // Abre la conexión de forma segura
                await using var connection = await OpenConnectionAsync();

                // Ejecutar la consulta
                await using var command = new SqlCommand(finalQuery, connection)
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
                return Ok(new { Data = results });
            }
            catch (Exception ex)
            {
                return HandleException(ex, finalQuery);
            }
        }
    }
}
