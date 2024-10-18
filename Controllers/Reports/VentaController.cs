using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;
using System.Data;


namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpGet("api/v1/reporteria/ventas")]
        public async Task<IActionResult> ObtenerVentas(
    [FromQuery] string? searchTerm,
    [FromQuery] string? campoDistinct,
    [FromQuery] int page = 1,
    [FromQuery] int pageSize = 10)
        {
            // Validación de paginación
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10; // Limitar el tamaño de la página a un máximo de 100

            int offset = (page - 1) * pageSize;

            var parameters = new List<SqlParameter>();
            var whereClauses = new List<string>();

            // Obtener todos los parámetros de consulta
            var queryParams = HttpContext.Request.Query;

            // Mapeo de nombres de parámetros a campos de base de datos
            var fieldMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        {"articulo", "VTA.Articulo"},
        {"descripcion", "art.Descripcion1"},
        {"categoria", "art.Categoria"},
        {"grupo", "art.Grupo"},
        {"linea", "art.Linea"},
        {"familia", "art.Familia"},
        {"unidad", "VTA.Unidad"},
        {"tipoimpuesto", "VTA.TipoImpuesto1"},
        {"estatus", "VTE.Estatus"},
        {"mov", "VTE.Mov"},
        {"sucursal", "VTE.Sucursal"},
        {"fechainicio", "VTE.FechaEmision"},
        {"fechafin", "VTE.FechaEmision"}
    };

            // Construir cláusulas WHERE dinámicamente
            foreach (var param in queryParams)
            {
                var paramName = param.Key.ToLower();
                if (fieldMappings.ContainsKey(paramName) && !string.IsNullOrEmpty(param.Value))
                {
                    var fieldName = fieldMappings[paramName];
                    var paramValue = param.Value.ToString();
                    string sqlParamName = "@" + paramName;

                    if (paramName == "fechainicio")
                    {
                        whereClauses.Add($"{fieldName} >= {sqlParamName}");
                        parameters.Add(new SqlParameter(sqlParamName, SqlDbType.DateTime) { Value = DateTime.Parse(paramValue) });
                    }
                    else if (paramName == "fechafin")
                    {
                        whereClauses.Add($"{fieldName} <= {sqlParamName}");
                        parameters.Add(new SqlParameter(sqlParamName, SqlDbType.DateTime) { Value = DateTime.Parse(paramValue) });
                    }
                    else
                    {
                        whereClauses.Add($"{fieldName} = {sqlParamName}");
                        parameters.Add(new SqlParameter(sqlParamName, paramValue));
                    }
                }
            }

            string whereClause = whereClauses.Any() ? "AND " + string.Join(" AND ", whereClauses) : "";

            // Si se especifica un campo para obtener valores distintos
            if (!string.IsNullOrEmpty(campoDistinct))
            {
                // Validar que el campoDistinct sea válido
                var camposValidos = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            {"articulo", "VTA.Articulo"},
            {"descripcion1", "art.Descripcion1"},
            {"categoria", "art.Categoria"},
            {"grupo", "art.Grupo"},
            {"linea", "art.Linea"},
            {"familia", "art.Familia"},
            {"unidad", "VTA.Unidad"},
            {"tipoimpuesto1", "VTA.TipoImpuesto1"},
            {"tipoimpuesto2", "VTA.TipoImpuesto2"},
            {"tipoimpuesto3", "VTA.TipoImpuesto3"},
            {"estatus", "VTE.Estatus"},
            {"mov", "VTE.Mov"},
            {"sucursal", "VTE.Sucursal"},
            {"fechaemision", "VTE.FechaEmision"}
        };

                if (!camposValidos.TryGetValue(campoDistinct.ToLower(), out string campoBD))
                {
                    return BadRequest("El campo especificado para 'campoDistinct' no es válido.");
                }

                string queryDistinct = $@"
            USE TC032841E
            SELECT DISTINCT {campoBD} AS Valor
            FROM
                VentaD VTA
            INNER JOIN
                Venta VTE ON VTE.ID = VTA.ID
            LEFT JOIN
                ART art ON art.ARTICULO = VTA.Articulo
            WHERE
                VTE.Mov = 'NOTA'
                AND VTE.Estatus IN ('CONCLUIDO','PROCESAR')
                {whereClause}
            ORDER BY Valor
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                string countQueryDistinct = $@"
            USE TC032841E
            SELECT COUNT(DISTINCT {campoBD}) 
            FROM
                VentaD VTA
            INNER JOIN
                Venta VTE ON VTE.ID = VTA.ID
            LEFT JOIN
                ART art ON art.ARTICULO = VTA.Articulo
            WHERE
                VTE.Mov = 'NOTA'
                AND VTE.Estatus IN ('CONCLUIDO','PROCESAR')
                {whereClause}";

                try
                {
                    await using var connection = await OpenConnectionAsync();

                    // Ejecutamos la consulta de conteo total de registros
                    await using var countCommand = new SqlCommand(countQueryDistinct, connection);
                    foreach (var param in parameters)
                    {
                        countCommand.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                    }
                    int totalRecords = (int)await countCommand.ExecuteScalarAsync();

                    // Ejecutamos la consulta paginada
                    await using var command = new SqlCommand(queryDistinct, connection);
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                    }
                    command.Parameters.Add(new SqlParameter("@offset", offset));
                    command.Parameters.Add(new SqlParameter("@pageSize", pageSize));

                    await using var reader = await command.ExecuteReaderAsync();
                    var results = new List<string>();

                    while (await reader.ReadAsync())
                    {
                        results.Add(reader["Valor"].ToString());
                    }

                    // Devolvemos el resultado con la información de la paginación
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
                    return HandleException(ex); // Método para gestionar las excepciones
                }
            }
            else
            {
                // Consulta normal sin campoDistinct
                string query = $@"
            USE TC032841E
            SELECT 
                VTA.Articulo,
                art.Descripcion1,
                art.Categoria,
                art.Grupo,
                art.Linea,
                art.Familia,
                VTA.Unidad,
                CONCAT_WS(', ', 
                    NULLIF(VTA.TipoImpuesto1, ''), 
                    NULLIF(VTA.TipoImpuesto2, ''), 
                    NULLIF(VTA.TipoImpuesto3, '')
                ) as id_type_taxes,
                SUM(VTA.Cantidad) AS TotalCantidad,
                SUM(VTA.Precio * VTA.Cantidad) AS TotalImporte
            FROM
                ART art
            RIGHT JOIN
                VentaD VTA ON art.ARTICULO = VTA.Articulo
            INNER JOIN
                Venta VTE ON VTE.ID = VTA.ID
            WHERE
                VTE.Mov = 'NOTA'
                AND VTE.Estatus IN ('CONCLUIDO','PROCESAR')
                {whereClause}
            GROUP BY 
                VTA.Articulo,
                art.Descripcion1,
                art.Categoria,
                art.Grupo,
                art.Linea,
                art.Familia,
                VTA.Unidad,
                VTA.TipoImpuesto1,
                VTA.TipoImpuesto2,
                VTA.TipoImpuesto3
            ORDER BY
                TotalCantidad desc
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                string countQuery = $@"
            USE TC032841E
            SELECT COUNT(*) FROM (
                SELECT 
                    VTA.Articulo
                FROM
                    ART art
                RIGHT JOIN
                    VentaD VTA ON art.ARTICULO = VTA.Articulo
                INNER JOIN
                    Venta VTE ON VTE.ID = VTA.ID
                WHERE
                    VTE.Mov = 'NOTA'
                    AND VTE.Estatus IN ('CONCLUIDO','PROCESAR')
                    {whereClause}
                GROUP BY 
                    VTA.Articulo,
                    art.Descripcion1,
                    art.Categoria,
                    art.Grupo,
                    art.Linea,
                    art.Familia,
                    VTA.Unidad,
                    VTA.TipoImpuesto1,
                    VTA.TipoImpuesto2,
                    VTA.TipoImpuesto3
            ) AS CountTable";

                try
                {
                    await using var connection = await OpenConnectionAsync();

                    // Ejecutamos la consulta de conteo total de registros
                    await using var countCommand = new SqlCommand(countQuery, connection);
                    foreach (var param in parameters)
                    {
                        countCommand.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                    }
                    int totalRecords = (int)await countCommand.ExecuteScalarAsync();

                    // Ejecutamos la consulta paginada
                    await using var command = new SqlCommand(query, connection);
                    foreach (var param in parameters)
                    {
                        command.Parameters.Add(new SqlParameter(param.ParameterName, param.Value));
                    }
                    command.Parameters.Add(new SqlParameter("@offset", offset));
                    command.Parameters.Add(new SqlParameter("@pageSize", pageSize));

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

                    // Devolvemos el resultado con la información de la paginación
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
                    return HandleException(ex); // Método para gestionar las excepciones
                }
            }
        }


    }
}
