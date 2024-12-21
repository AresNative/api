using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;

namespace MyApiProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PreciosController : ControllerBase
    {
        private readonly string _connectionString;

        public PreciosController(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        // GET: api/precios
        [HttpGet]
        public async Task<IActionResult> GetPrecios([FromQuery] string? filtro)
        {
            if (string.IsNullOrEmpty(filtro))
            {
                return BadRequest(new ErrorResponse { Message = "Debe proporcionar el parámetro de búsqueda." });
            }

            var precios = new List<PrecioDto>();
            var ofertas = new List<OfertaDto>();
            string articulo = filtro; // Por defecto, asumimos que el filtro es el artículo

            // Consultas SQL
            string queryPrecios = @"
                
                SELECT 
                    CB.Codigo, 
                    CB.Cuenta, 
                    Art.Descripcion1, 
                    ListaPreciosDUnidad.Unidad, 
                    ListaPreciosDUnidad.Precio,
                    ArtUnidad.Factor 
                FROM [CB]
                INNER JOIN Art ON CB.Cuenta = Art.Articulo 
                INNER JOIN ListaPreciosDUnidad ON CB.Cuenta = ListaPreciosDUnidad.Articulo 
                INNER JOIN ArtUnidad ON CB.Cuenta = ArtUnidad.Articulo 
                WHERE 
                    ListaPreciosDUnidad.Lista = '(PRECIO 3)' 
                    AND CB.Unidad = ListaPreciosDUnidad.UNIDAD 
                    AND CB.Unidad = ArtUnidad.Unidad 
                    AND (CB.Codigo = @Filtro OR Art.Articulo = @Filtro OR Art.Descripcion1 LIKE '%' + @Filtro + '%')
                ORDER BY ArtUnidad.Factor ASC;
            ";

            string queryArticuloPorCodigo = @"
                SELECT Cuenta 
                FROM [CB]
                WHERE Codigo = @Codigo;
            ";

            string queryOfertas = @"
                SELECT 
                    OfertaD.Articulo,
                    OfertaD.Precio,
                    Oferta.FechaD,
                    Oferta.FechaA
                FROM 
                    OfertaD 
                INNER JOIN Oferta ON OfertaD.ID = Oferta.ID
                WHERE
                    OfertaD.Articulo = @Articulo
                    AND Oferta.FechaD < GETDATE() 
                    AND Oferta.FechaA > GETDATE();
            ";

            try
            {
                await using var connection = await OpenConnection();

                // Si el filtro es un código, obtener el artículo correspondiente desde la columna 'Cuenta'
                await using (var commandArticulo = new SqlCommand(queryArticuloPorCodigo, connection))
                {
                    commandArticulo.Parameters.AddWithValue("@Codigo", filtro);
                    var result = await commandArticulo.ExecuteScalarAsync();

                    if (result != null)
                    {
                        articulo = result.ToString(); // Actualizar el filtro con el artículo encontrado
                    }
                }

                // Ejecutar consulta de precios
                await using (var commandPrecios = new SqlCommand(queryPrecios, connection))
                {
                    commandPrecios.Parameters.AddWithValue("@Filtro", filtro);

                    await using var readerPrecios = await commandPrecios.ExecuteReaderAsync();
                    while (await readerPrecios.ReadAsync())
                    {
                        precios.Add(MapToPrecioDto(readerPrecios));
                    }
                }

                // Ejecutar consulta de ofertas usando el artículo
                await using (var commandOfertas = new SqlCommand(queryOfertas, connection))
                {
                    commandOfertas.Parameters.AddWithValue("@Articulo", articulo);

                    await using var readerOfertas = await commandOfertas.ExecuteReaderAsync();
                    while (await readerOfertas.ReadAsync())
                    {
                        ofertas.Add(MapToOfertaDto(readerOfertas));
                    }
                }
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }

            // Devolver los resultados encontrados
            return Ok(new { Precios = precios, Ofertas = ofertas });
        }

        private async Task<SqlConnection> OpenConnection()
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            return connection;
        }

        private PrecioDto MapToPrecioDto(SqlDataReader reader)
        {
            return new PrecioDto
            {
                Codigo = reader["Codigo"] != DBNull.Value ? reader["Codigo"].ToString() : null,
                Cuenta = reader["Cuenta"] != DBNull.Value ? reader["Cuenta"].ToString() : null,
                Descripcion1 = reader["Descripcion1"] != DBNull.Value ? reader["Descripcion1"].ToString() : null,
                Unidad = reader["Unidad"] != DBNull.Value ? reader["Unidad"].ToString() : null,
                Precio = reader["Precio"] != DBNull.Value ? Convert.ToDecimal(reader["Precio"]) : 0,
                Factor = reader["Factor"] != DBNull.Value ? Convert.ToDecimal(reader["Factor"]) : 0,
            };
        }

        private OfertaDto MapToOfertaDto(SqlDataReader reader)
        {
            return new OfertaDto
            {
                Articulo = reader["Articulo"].ToString(),
                Precio = Convert.ToDecimal(reader["Precio"]),
                FechaDesde = Convert.ToDateTime(reader["FechaD"]),
                FechaHasta = Convert.ToDateTime(reader["FechaA"])
            };
        }

        private IActionResult HandleException(Exception ex)
        {
            return StatusCode(500, new ErrorResponse { Message = "Error: " + ex.Message });
        }
    }
}
