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
     [FromQuery] string? searchField, // Campo a buscar dinámicamente
     [FromQuery] int page = 1,
     [FromQuery] int pageSize = 10)
        {
            // Validación de paginación
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

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

            // Construcción de la consulta para el total de registros
            var countQueryBuilder = new StringBuilder($"SELECT COUNT(DISTINCT {searchField}) {baseQuery} {whereQuery}");

            // Construcción de la consulta con paginación
            var queryBuilder = new StringBuilder($@"
        USE [TC032841E]
        SELECT DISTINCT
            {searchField} AS SearchResult
        {baseQuery} {whereQuery}
        ORDER BY {searchField}
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
                var results = new List<string>();

                while (await reader.ReadAsync())
                {
                    results.Add(reader.GetString(0)); // Solo devuelve la columna buscada
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
