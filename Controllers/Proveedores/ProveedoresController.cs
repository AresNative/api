using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public class Proveedor : BaseController
    {
        public Proveedor(IConfiguration configuration) : base(configuration) { }
        [HttpPost("api/v1/proveedores")]
        public async Task<IActionResult> RegistrarProveedor([FromBody] ProveedorDto nuevoProveedor)
        {
            // Consulta SQL para verificar si ya existe un proveedor con el mismo email
            string checkUserQuery = @"SELECT COUNT(1) FROM Website_proveedores WHERE email = @Email";
            // Consulta SQL para registrar un nuevo proveedor
            string insertUserQuery = @"INSERT INTO Website_proveedores (name, email, company, type_prod, department) 
                                       VALUES (@name, @Email, @Company, @Type_prod, @Department)";

            try
            {
                await using var connection = await OpenConnectionAsync();

                // Verificar si ya existe un proveedor con el mismo email
                await using (var checkCommand = new SqlCommand(checkUserQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@Email", nuevoProveedor.email);
                    var userExists = (int)await checkCommand.ExecuteScalarAsync();

                    if (userExists > 0)
                    {
                        // Retornar respuesta de conflicto si el proveedor ya existe
                        return Conflict(new { Message = "El proveedor con este email ya está registrado" });
                    }
                }

                // Si el proveedor no existe, procedemos con la inserción
                await using (var command = new SqlCommand(insertUserQuery, connection))
                {
                    command.Parameters.AddWithValue("@name", nuevoProveedor.name);
                    command.Parameters.AddWithValue("@Email", nuevoProveedor.email);
                    command.Parameters.AddWithValue("@Company", nuevoProveedor.company);
                    command.Parameters.AddWithValue("@Type_prod", nuevoProveedor.type_prod);

                    // Verifica si 'department' es nulo antes de agregarlo
                    if (nuevoProveedor.department != null)
                        command.Parameters.AddWithValue("@Department", nuevoProveedor.department);
                    else
                        command.Parameters.AddWithValue("@Department", DBNull.Value); // Si es nulo, asigna un valor nulo de SQL

                    var result = await command.ExecuteNonQueryAsync();

                    if (result > 0)
                        return Ok(new { Message = "Proveedor registrado exitosamente" });
                    else
                        return BadRequest(new { Message = "No se pudo registrar el proveedor" });
                }

            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
