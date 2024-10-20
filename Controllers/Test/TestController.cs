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
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            // Validación de paginación
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10; // Limitar el tamaño de la página a un máximo de 100

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
                WHERE 
                    1 = 1"; // Esto permite agregar más condiciones sin errores de sintaxis

            // Construcción de la consulta para el total de registros
            var countQueryBuilder = new StringBuilder($"SELECT COUNT(1) {baseQuery}");

            // Construcción de la consulta con paginación
            var queryBuilder = new StringBuilder($@"
                SELECT
                    cb.Codigo, 
                    cd.Articulo,
                    c.Estatus,
                    cd.Unidad, 
                    U.Factor AS Equivalente, 
                    cd.ID AS CompraID, 
                    cd.CODIGO AS CompraCodigo, 
                    cd.Cantidad, 
                    cd.Costo,
                    c.MovID, 
                    c.FechaEmision, 
                    c.Proveedor,
                    p.Nombre AS Proveedor_Nombre
                {baseQuery}");

            // Lista de parámetros para la consulta
            var parameters = new List<SqlParameter>();

            // Agregar condiciones dinámicamente
            if (!string.IsNullOrEmpty(codigo))
            {
                countQueryBuilder.Append(" AND cb.Codigo = @Codigo");
                queryBuilder.Append(" AND cb.Codigo = @Codigo");
                parameters.Add(new SqlParameter("@Codigo", codigo));
            }
            if (!string.IsNullOrEmpty(estatus))
            {
                countQueryBuilder.Append(" AND c.Estatus = @Estatus");
                queryBuilder.Append(" AND c.Estatus = @Estatus");
                parameters.Add(new SqlParameter("@Estatus", estatus));
            }
            if (!string.IsNullOrEmpty(articulo))
            {
                countQueryBuilder.Append(" AND cd.Articulo = @Articulo");
                queryBuilder.Append(" AND cd.Articulo = @Articulo");
                parameters.Add(new SqlParameter("@Articulo", articulo));
            }
            if (!string.IsNullOrEmpty(proveedor))
            {
                countQueryBuilder.Append(" AND c.Proveedor = @Proveedor");
                queryBuilder.Append(" AND c.Proveedor = @Proveedor");
                parameters.Add(new SqlParameter("@Proveedor", proveedor));
            }

            // Agregar la cláusula ORDER BY seguida de OFFSET-FETCH para la paginación
            queryBuilder.Append(@"
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
                    CommandTimeout = 30 // Tiempo de espera de 30 segundos
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
                // Llama a HandleException con la excepción y la consulta
                return HandleException(ex, queryBuilder.ToString());
            }
        }
    }
}
