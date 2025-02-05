using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        public Reporteria(IConfiguration configuration) : base(configuration) { }

        [HttpPost("api/v1/reporteria/compras")]
        public async Task<IActionResult> ObtenerCompras(
            [FromBody] List<BusquedaParams> filtros,  // Recibir filtros como una lista
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
                    [TC032841E].dbo.[CB] cb
                JOIN 
                    [TC032841E].dbo.[CompraD] cd ON cb.Cuenta = cd.Articulo 
                LEFT JOIN 
                    [TC032841E].dbo.[Compra] c ON cd.ID = c.ID
                LEFT JOIN 
                    [TC032841E].dbo.[Prov] p ON c.Proveedor = p.Proveedor
                LEFT JOIN
                    [TC032841E].dbo.[ArtUnidad] U ON cb.Cuenta = U.Articulo
                LEFT JOIN
                    [TC032841E].dbo.[Art] A ON cb.Cuenta = A.Articulo
                WHERE 
					c.Estatus IN ('CONCLUIDO','PROCESAR')";

            // Construcción de cláusulas WHERE dinámicas
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            foreach (var filter in filtros)
            {
                if (!string.IsNullOrWhiteSpace(filter.Value))
                {
                    var columnName = filter.Key;
                    var parameterName = $"@{filter.Key.Replace(".", "_")}";  // Reemplazar puntos por guiones bajos

                    string operatorClause = filter.Operator?.ToLower() switch
                    {
                        "like" => "LIKE",
                        "=" => "=",
                        ">=" => ">=",
                        "<=" => "<=",
                        ">" => ">",
                        "<" => "<",
                        _ => "LIKE"  // Default: LIKE
                    };

                    whereClauses.Add($"{columnName} {operatorClause} {parameterName}");
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
                    ROW_NUMBER() OVER (ORDER BY cb.Codigo) AS ID,
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
                    NULLIF(A.TipoImpuesto1, '') AS [IVA], 
                    NULLIF(A.TipoImpuesto2, '') AS [IEPS], 
                    NULLIF(A.TipoImpuesto3, '') AS  [ISR],
                    NULLIF(A.Impuesto1, '') AS [IVA%], 
                    NULLIF(A.Impuesto2, '') AS [IEPS%], 
                    NULLIF(A.Impuesto3, '') AS [ISR%]
                {baseQuery} {whereQuery}
                ORDER BY cb.Codigo ASC
                OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Crear los parámetros de manera independiente para countCommand
                var countCommandParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value))  // Nueva instancia para count
                    .ToList();

                await using var countCommand = new SqlCommand(countQuery, connection);
                countCommand.Parameters.AddRange(countCommandParameters.ToArray());
                var totalRecords = (int)await countCommand.ExecuteScalarAsync();

                // Crear los parámetros de manera independiente para paginatedQuery
                var paginatedCommandParameters = parameters
                    .Select(p => new SqlParameter(p.ParameterName, p.Value))  // Nueva instancia para paginación
                    .ToList();

                paginatedCommandParameters.AddRange(new[]
                {
                    new SqlParameter("@Offset", offset),
                    new SqlParameter("@PageSize", pageSize)
                });

                await using var command = new SqlCommand(paginatedQuery, connection);
                command.Parameters.AddRange(paginatedCommandParameters.ToArray());
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
                    TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize),
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
