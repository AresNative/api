using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpGet("api/v1/reporteria/ventas")]
        public async Task<IActionResult> ObtenerVentas(
    [FromQuery] string? articulo,
    [FromQuery] string? descripcion,
    [FromQuery] string? categoria,
    [FromQuery] string? grupo,
    [FromQuery] string? linea,
    [FromQuery] string? familia,
    [FromQuery] string? unidad,
    [FromQuery] string? tipoImpuesto,
    [FromQuery] string? estatus,
    [FromQuery] string? mov,
    [FromQuery] int? sucursal,
    [FromQuery] DateTime? fechaInicio,
    [FromQuery] DateTime? fechaFin,
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

            // Construir la cláusula WHERE dinámica
            if (!string.IsNullOrEmpty(articulo))
            {
                whereClauses.Add("VTA.Articulo = @articulo");
                parameters.Add(new SqlParameter("@articulo", articulo));
            }
            if (!string.IsNullOrEmpty(descripcion))
            {
                whereClauses.Add("art.Descripcion1 LIKE '%' + @descripcion + '%'");
                parameters.Add(new SqlParameter("@descripcion", descripcion));
            }
            if (!string.IsNullOrEmpty(categoria))
            {
                whereClauses.Add("art.Categoria = @categoria");
                parameters.Add(new SqlParameter("@categoria", categoria));
            }
            if (!string.IsNullOrEmpty(grupo))
            {
                whereClauses.Add("art.Grupo = @grupo");
                parameters.Add(new SqlParameter("@grupo", grupo));
            }
            if (!string.IsNullOrEmpty(linea))
            {
                whereClauses.Add("art.Linea = @linea");
                parameters.Add(new SqlParameter("@linea", linea));
            }
            if (!string.IsNullOrEmpty(familia))
            {
                whereClauses.Add("art.Familia = @familia");
                parameters.Add(new SqlParameter("@familia", familia));
            }
            if (!string.IsNullOrEmpty(unidad))
            {
                whereClauses.Add("VTA.Unidad = @unidad");
                parameters.Add(new SqlParameter("@unidad", unidad));
            }
            if (!string.IsNullOrEmpty(tipoImpuesto))
            {
                whereClauses.Add(@"(
            VTA.TipoImpuesto1 = @tipoImpuesto
            OR VTA.TipoImpuesto2 = @tipoImpuesto
            OR VTA.TipoImpuesto3 = @tipoImpuesto
        )");
                parameters.Add(new SqlParameter("@tipoImpuesto", tipoImpuesto));
            }
            if (!string.IsNullOrEmpty(estatus))
            {
                whereClauses.Add("VTE.Estatus = @estatus");
                parameters.Add(new SqlParameter("@estatus", estatus));
            }
            if (!string.IsNullOrEmpty(mov))
            {
                whereClauses.Add("VTE.Mov = @mov");
                parameters.Add(new SqlParameter("@mov", mov));
            }
            if (sucursal.HasValue)
            {
                whereClauses.Add("VTE.Sucursal = @sucursal");
                parameters.Add(new SqlParameter("@sucursal", sucursal.Value));
            }
            if (fechaInicio.HasValue)
            {
                whereClauses.Add("VTE.FechaEmision >= @fechaInicio");
                parameters.Add(new SqlParameter("@fechaInicio", fechaInicio.Value));
            }
            if (fechaFin.HasValue)
            {
                whereClauses.Add("VTE.FechaEmision <= @fechaFin");
                parameters.Add(new SqlParameter("@fechaFin", fechaFin.Value));
            }

            string whereClause = whereClauses.Any() ? "AND " + string.Join(" AND ", whereClauses) : "";

            // Si se especifica un campo para obtener valores distintos
            if (!string.IsNullOrEmpty(campoDistinct))
            {
                // Validar que el campoDistinct sea válido para evitar inyección SQL
                var camposValidos = new List<string>
        {
            "VTA.Articulo",
            "art.Descripcion1",
            "art.Categoria",
            "art.Grupo",
            "art.Linea",
            "art.Familia",
            "VTA.Unidad",
            "VTA.TipoImpuesto1",
            "VTA.TipoImpuesto2",
            "VTA.TipoImpuesto3",
            "VTE.Estatus",
            "VTE.Mov",
            "VTE.Sucursal",
            "VTE.FechaEmision"
        };

                // Mapear nombres amigables a nombres de columnas
                var campoMapeado = camposValidos.FirstOrDefault(c => c.EndsWith("." + campoDistinct, StringComparison.OrdinalIgnoreCase));
                if (campoMapeado == null)
                {
                    return BadRequest("El campo especificado para 'campoDistinct' no es válido.");
                }

                string queryDistinct = $@"
            USE TC032841E
            SELECT DISTINCT {campoMapeado} AS Valor
            FROM
                VentaD VTA
            INNER JOIN
                Venta VTE ON VTE.ID = VTA.ID
            LEFT JOIN
                ART art ON art.ARTICULO = VTA.Articulo
            WHERE
                VTE.Mov = 'NOTA'
                AND VTE.Estatus IN ('CONCLUIDO','PROCESAR')
                AND VTE.FechaEmision > '2024-09-01 00:00:00.000'
                AND VTE.FechaEmision < '2024-09-30 00:00:00.000'
                {whereClause}
            ORDER BY Valor
            OFFSET @offset ROWS FETCH NEXT @pageSize ROWS ONLY";

                string countQueryDistinct = $@"
            USE TC032841E
            SELECT COUNT(DISTINCT {campoMapeado}) 
            FROM
                VentaD VTA
            INNER JOIN
                Venta VTE ON VTE.ID = VTA.ID
            LEFT JOIN
                ART art ON art.ARTICULO = VTA.Articulo
            WHERE
                VTE.Mov = 'NOTA'
                AND VTE.Estatus IN ('CONCLUIDO','PROCESAR')
                AND VTE.FechaEmision > '2024-09-01 00:00:00.000'
                AND VTE.FechaEmision < '2024-09-30 00:00:00.000'
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
                AND VTE.FechaEmision > '2024-09-01 00:00:00.000'
                AND VTE.FechaEmision < '2024-09-30 00:00:00.000'
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
                    AND VTE.FechaEmision > '2024-09-01 00:00:00.000'
                    AND VTE.FechaEmision < '2024-09-30 00:00:00.000'
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
