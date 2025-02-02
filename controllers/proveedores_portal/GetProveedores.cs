using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class ProveedoresPortal : BaseController
    {
        public ProveedoresPortal(IConfiguration configuration) : base(configuration) { }
        public class ProveedoresPortalRequest
        {
            public List<BusquedaParams> Filtros { get; set; } = new();
        }

        [HttpPost("api/v2/select/proveedoresPortal")]
        public async Task<IActionResult> ObtenerProveedoresPortalRequest(
            [FromBody] ProveedoresPortalRequest request,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

            var baseQuery = @"
            FROM [LOCAL_TC032391E].[dbo].[Website_proveedores_portal]";

            var whereClauses = new List<string>();
            var sumaClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            // Procesar filtros
            foreach (var filter in request.Filtros)
            {
                if (!string.IsNullOrWhiteSpace(filter.Value))
                {
                    var columnName = filter.Key;
                    var parameterName = $"@{filter.Key.Replace(".", "_")}";

                    string operatorClause = filter.Operator?.ToLower() switch
                    {
                        "like" => "LIKE",
                        "=" => "=",
                        ">=" => ">=",
                        "<=" => "<=",
                        ">" => ">",
                        "<" => "<",
                        "<>" => "<>",
                        _ => "LIKE"
                    };

                    whereClauses.Add($"{columnName} {operatorClause} {parameterName}");
                    parameters.Add(new SqlParameter(parameterName, operatorClause == "LIKE" ? $"%{filter.Value}%" : filter.Value));
                }
            }

            var whereQuery = whereClauses.Any() ? $"WHERE {string.Join(" AND ", whereClauses)}" : "";
            var sumaQuery = sumaClauses.Any() ? $"{string.Join(", ", sumaClauses)}" : "";

            var countQuery = $@"
                SELECT COUNT(*) AS TotalRegistros {baseQuery} {whereQuery}";

            var paginatedQuery = $@"
                SELECT
                    [id]
                    ,[id_proveedor]
                    ,[fecha]
                    ,[mov]
                    ,[comment]
                    ,[file]
                {baseQuery} {whereQuery}
                ORDER BY ID
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Total records
                var countCommandParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value))
                    .ToList();

                await using var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(countCommandParameters.ToArray());
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Paginated data
                var paginatedParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value))
                    .ToList();

                paginatedParameters.AddRange(new[]
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize)
                });

                await using var command = new SqlCommand(paginatedQuery, connection);
                command.Parameters.AddRange(paginatedParameters.ToArray());

                var results = new List<Dictionary<string, object>>();

                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    var row = new Dictionary<string, object>();
                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        var columnName = reader.GetName(i);
                        var columnValue = reader.GetValue(i);

                        if (columnName == "file" && columnValue != DBNull.Value)
                        {
                            var rutaCompleta = columnValue.ToString();

                            // Verifica si el archivo existe
                            if (System.IO.File.Exists(rutaCompleta))
                            {
                                var nombreArchivo = Path.GetFileName(rutaCompleta);
                                var contenidoArchivo = System.IO.File.ReadAllBytes(rutaCompleta);
                                var tipoMime = GetMimeType(nombreArchivo);

                                row[columnName] = new
                                {
                                    FileName = nombreArchivo,
                                    ContentType = tipoMime,
                                    Content = contenidoArchivo
                                };
                            }
                            else
                            {
                                // Si el archivo no existe, devuelve un mensaje de error o un valor predeterminado
                                row[columnName] = new
                                {
                                    Error = "Archivo no encontrado",
                                    FilePath = rutaCompleta
                                };
                            }
                        }
                        else
                        {
                            row[columnName] = columnValue;
                        }
                    }
                    results.Add(row);
                }

                return Ok(new
                {
                    TotalRecords = totalRecords,
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
                    PageSize = pageSize,
                    Page = page,
                    Data = results
                });
            }
            catch (Exception ex)
            {
                return HandleException(ex, paginatedQuery);
            }
        }
    }
}
