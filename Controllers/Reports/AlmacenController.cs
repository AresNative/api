using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        [HttpGet("api/v1/reporteria/almacen")]
        public async Task<IActionResult> ObtenerAlmacen(
        [FromQuery] string? estatus,
        [FromQuery] string? articulo,
        [FromQuery] string? descripcion,
        [FromQuery] string? usuario,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            // Validación de paginación
            if (page <= 0) page = 1;
            if (pageSize <= 0 || pageSize > 100) pageSize = 10;

            int offset = (page - 1) * pageSize;

            // Base de la consulta SQL
            var baseQuery = @"
            FROM POSLVenta pv
            JOIN POSL pl ON pv.ID = pl.ID
            LEFT JOIN POSLCobro pc ON pl.ID = pc.ID
            LEFT JOIN Art A ON pv.Articulo = A.Articulo";

            // Construcción dinámica de cláusulas WHERE
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(articulo))
            {
                whereClauses.Add("pv.Articulo LIKE @Articulo");
                parameters.Add(new SqlParameter("@Articulo", $"%{articulo}%"));
            }
            if (!string.IsNullOrEmpty(estatus))
            {
                whereClauses.Add("pv.Estatus LIKE @Estatus");
                parameters.Add(new SqlParameter("@Estatus", $"%{estatus}%"));
            }
            if (!string.IsNullOrEmpty(usuario))
            {
                whereClauses.Add("pl.Usuario LIKE @Usuario");
                parameters.Add(new SqlParameter("@Usuario", $"%{usuario}%"));
            }
            if (!string.IsNullOrEmpty(descripcion))
            {
                whereClauses.Add("a.Descripcion1 LIKE @Descripcion");
                parameters.Add(new SqlParameter("@Descripcion", $"%{descripcion}%"));
            }

            // Agregar cláusulas WHERE a la consulta si existen
            var whereQuery = whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "";

            // Construcción de la consulta completa con paginación
            var query = $@"
            USE [TC032841E]
            SELECT
            pv.Articulo,            
            A.Descripcion1 AS Descripcion,
            pv.Cantidad,            
            pv.Precio,              
            CONCAT_WS(', ' , pv.Impuesto1, pv.Impuesto2, pv.Impuesto3) as taxes, 
            CONCAT_WS(', ' , 
                        NULLIF(pv.TipoImpuesto1, ' '), 
                        NULLIF(pv.TipoImpuesto2, ' '), 
                        NULLIF(pv.TipoImpuesto3, ' ')
            ) as TipoImpuesto,      
            pv.Unidad,              
            pv.Estatus,             
            pl.MovID,               
            pl.Usuario,             
            pl.Estatus,             
            pl.Nombre,              
            pl.Caja,                
            pc.Importe,             
            pc.FormaPago,           
            pc.Importe,             
            pc.MonedaRef,           
            1 as id_sucursal,       
            1 as id_almacen         
            {baseQuery} {whereQuery}
            ORDER BY (SELECT NULL)
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;";

            try
            {
                await using var connection = await OpenConnectionAsync();
                var command = new SqlCommand(query, connection);

                // Asignar parámetros al comando SQL
                parameters.ForEach(param => command.Parameters.Add(param));
                command.Parameters.AddWithValue("@Offset", offset);
                command.Parameters.AddWithValue("@PageSize", pageSize);

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
                    Page = page,
                    PageSize = pageSize,
                    Data = results
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return HandleException(ex, query);
            }
        }


    }
}
