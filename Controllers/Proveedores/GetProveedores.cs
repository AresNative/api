using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class Proveedor : BaseController
    {
        [HttpGet("api/v1/proveedores")]
        public async Task<IActionResult> ObtenerProveedores(
            [FromQuery] string? name,
            [FromQuery] string? email,
            [FromQuery] string? company,
            [FromQuery] string? typeProd,
            [FromQuery] string? department,
            [FromQuery] string? rfc,
            [FromQuery] string? addres,
            [FromQuery] string? code,
            [FromQuery] string? ability)
        {
            // Construcción del query base
            var baseQuery = @"SELECT [id], [name], [email], [company], [type_prod], [department], [rfc], [addres], [code], [ability], [id_permission]
                              FROM [LOCAL_TC032391E].[dbo].[Website_proveedores]";

            // Construcción de la cláusula WHERE de manera dinámica
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(name))
            {
                whereClauses.Add("[name] LIKE @Name");
                parameters.Add(new SqlParameter("@Name", $"%{name}%"));
            }
            if (!string.IsNullOrEmpty(email))
            {
                whereClauses.Add("[email] LIKE @Email");
                parameters.Add(new SqlParameter("@Email", $"%{email}%"));
            }
            if (!string.IsNullOrEmpty(company))
            {
                whereClauses.Add("[company] LIKE @Company");
                parameters.Add(new SqlParameter("@Company", $"%{company}%"));
            }
            if (!string.IsNullOrEmpty(typeProd))
            {
                whereClauses.Add("[type_prod] LIKE @TypeProd");
                parameters.Add(new SqlParameter("@TypeProd", $"%{typeProd}%"));
            }
            if (!string.IsNullOrEmpty(department))
            {
                whereClauses.Add("[department] LIKE @Department");
                parameters.Add(new SqlParameter("@Department", $"%{department}%"));
            }
            if (!string.IsNullOrEmpty(rfc))
            {
                whereClauses.Add("[rfc] LIKE @Rfc");
                parameters.Add(new SqlParameter("@Rfc", $"%{rfc}%"));
            }
            if (!string.IsNullOrEmpty(addres))
            {
                whereClauses.Add("[addres] LIKE @Addres");
                parameters.Add(new SqlParameter("@Addres", $"%{addres}%"));
            }
            if (!string.IsNullOrEmpty(code))
            {
                whereClauses.Add("[code] LIKE @Code");
                parameters.Add(new SqlParameter("@Code", $"%{code}%"));
            }
            if (!string.IsNullOrEmpty(ability))
            {
                whereClauses.Add("[ability] LIKE @Ability");
                parameters.Add(new SqlParameter("@Ability", $"%{ability}%"));
            }

            // Si hay cláusulas WHERE, agregarlas al query base
            var whereQuery = whereClauses.Any() ? $" WHERE {string.Join(" AND ", whereClauses)}" : "";
            var finalQuery = $"{baseQuery}{whereQuery}";

            try
            {
                // Abre la conexión de forma segura
                await using var connection = await OpenConnectionAsync();

                // Ejecutar la consulta
                await using var command = new SqlCommand(finalQuery, connection)
                {
                    CommandTimeout = 30
                };

                // Asigna los parámetros al comando
                command.Parameters.AddRange(parameters.ToArray());

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
                return Ok(new { Data = results });
            }
            catch (Exception ex)
            {
                return HandleException(ex, finalQuery);
            }
        }
    }
}
