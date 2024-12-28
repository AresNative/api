using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;
using System.Text;

namespace MyApiProject.Controllers
{
    public partial class Filtros : BaseController
    {
        [HttpGet("api/v1/filtros/autocompletar-ventas")]
        public async Task<IActionResult> AutocompletarVentas(
            [FromQuery] string? searchTerm,
            [FromQuery] string? searchField) // Campo a buscar dinámicamente
        {
            // Validar que el campo de búsqueda sea permitido
            var allowedFields = new[] { "cb.Codigo", "A.Descripcion1", "A.Proveedor", "A.Codigo", "A.Articulo", "A.Proveedor" };
            if (string.IsNullOrEmpty(searchField) || !allowedFields.Contains(searchField))
            {
                return BadRequest(new { Message = "El campo de búsqueda no es válido." });
            }

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

            if (!string.IsNullOrEmpty(searchTerm))
            {
                whereClauses.Add($"{searchField} LIKE @SearchTerm");
                parameters.Add(new SqlParameter("@SearchTerm", $"%{searchTerm}%"));
            }

            // Si hay cláusulas WHERE, agregarlas al query base
            var whereQuery = whereClauses.Any() ? $" AND {string.Join(" AND ", whereClauses)}" : "";

            // Construcción de la consulta
            var queryBuilder = new StringBuilder($@"
                USE [TC032841E]
                SELECT DISTINCT TOP 10
                    {searchField} AS SearchResult
                {baseQuery} {whereQuery}");

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
                var results = new List<string>();

                while (await reader.ReadAsync())
                {
                    results.Add(reader.GetString(0)); // Solo devuelve la columna buscada
                }

                // Crear la respuesta en el formato solicitado
                var response = new
                {
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
