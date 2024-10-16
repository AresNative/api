using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class Reporteria : BaseController
    {
        public Reporteria(IConfiguration configuration) : base(configuration) { }

        [HttpGet("api/v1/reporteria/compras")]
        public async Task<IActionResult> ObtenerCompras([FromQuery] string? codigo)
        {
            if (string.IsNullOrEmpty(codigo))
            {
                return BadRequest(new ErrorResponse { Message = "Debe proporcionar el parámetro 'codigo'." });
            }
            // Ajustamos la consulta SQL para incluir el campo password
            string query = @"SELECT
                                cb.Codigo, 
                                cb.Cuenta, 
                                cd.Unidad,
                                U.Factor AS Equivalente, -- CAMPO NUEVO
                                cd.ID AS CompraD_ID, 
                                cd.CODIGO AS CompraD_Codigo, 
                                cd.Articulo, 
                                cd.Cantidad, 
                                cd.Costo,
                                c.ID AS Compra_ID, 
                                c.MovID, 
                                c.FechaEmision, 
                                c.Proveedor,
                                p.Nombre AS Proveedor_Nombre -- Asumiendo que 'Nombre' es un campo en la tabla 'Prov'
                            FROM 
                                [TC032841E].[dbo].[CB] cb
                            JOIN 
                                [TC032841E].[dbo].[CompraD] cd ON cb.Cuenta = cd.Articulo 
                            LEFT JOIN 
                                [TC032841E].[dbo].[Compra] c ON cd.ID = c.ID
                            LEFT JOIN 
                                [TC032841E].[dbo].[Prov] p ON c.Proveedor = p.Proveedor
                            LEFT JOIN
                                [TC032841E].[dbo].[ArtUnidad] U ON cb.Cuenta = U.Articulo -- JOIN NUEVO
                            WHERE 
                                cb.Codigo = @Codigo --@CODIGO P006051 | 006023
                            AND 
                                cd.Unidad = cb.UNIDAD
                            AND 
                                u.Unidad = cb.UNIDAD -- CAMPO NUEVO
                            AND  
                                c.Estatus='CONCLUIDO'; --@ESTATUS";

            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@Codigo", codigo);
                await using var reader = await command.ExecuteReaderAsync();

                var compras = new List<CompraDto>(); // Lista donde se almacenarán los resultados

                while (await reader.ReadAsync())
                {
                    compras.Add(MapToCompraDto(reader)); // Usamos el método correcto para mapear los datos
                }

                return Ok(compras); // Retornamos los resultados en formato JSON
            }
            catch (Exception ex)
            {
                return HandleException(ex); // Método para gestionar las excepciones
            }
        }

        // Método para mapear los resultados de la base de datos a un DTO
        private CompraDto MapToCompraDto(SqlDataReader reader)
        {
            return new CompraDto
            {
                Codigo = reader["Codigo"].ToString(),
                Cuenta = reader["Cuenta"].ToString(),
                Unidad = reader["Unidad"].ToString(),
                CompraD_ID = Convert.ToInt32(reader["CompraD_ID"]),
                CompraD_Codigo = reader["CompraD_Codigo"].ToString(),
                Articulo = reader["Articulo"].ToString(),
                Cantidad = Convert.ToDecimal(reader["Cantidad"]),
                Costo = Convert.ToDecimal(reader["Costo"]),
                Compra_ID = Convert.ToInt32(reader["Compra_ID"]),
                MovID = reader["MovID"].ToString(),
                FechaEmision = Convert.ToDateTime(reader["FechaEmision"]),
                Proveedor = reader["Proveedor"].ToString(),
                Proveedor_Nombre = reader["Proveedor_Nombre"].ToString()
            };
        }
    }
}
