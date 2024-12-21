using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace MyApiProject.Controllers
{
    public partial class UsuariosController : BaseController
    {
        [HttpGet("api/v1/users/postulacion")]
        public async Task<IActionResult> ObtenerPostulaciones(
            [FromQuery] string? nombre,
            [FromQuery] string? correo_electronico,
            [FromQuery] string? tiempo_trabajo,
            [FromQuery] string? ultimo_lugar_trabajo,
            [FromQuery] string? descripcion)
        {
            // Construcción del query base
            var baseQuery = @"SELECT [nombre]
                                    ,[apellido_paterno]
                                    ,[apellido_materno]
                                    ,[edad]
                                    ,[fecha_nacimiento]
                                    ,[correo_electronico]
                                    ,[numero_telefono]
                                    ,[direccion_actual]
                                    ,[fecha_radica_ciudad]
                                    ,[medio_transporte]
                                    ,[tiempo_traslado]
                                    ,[estado_civil]
                                    ,[tiempo_casado]
                                    ,[bienes_mancomunados]
                                    ,[tienes_hijos]
                                    ,[planeas_mas_hijos]
                                    ,[disponibilidad_horario]
                                    ,[ultimo_grado_estudios]
                                    ,[tienes_certificado]
                                    ,[estudias_actualmente]
                                    ,[dias_horario_estudio]
                                    ,[ultimo_lugar_trabajo]
                                    ,[puesto_ultimo_trabajo]
                                    ,[tiempo_trabajo]
                                    ,[salario_semanal]
                                    ,[horario_trabajo]
                                    ,[dia_descanso]
                                    ,[motivo_salida]
                                    ,[penultimo_lugar_trabajo]
                                    ,[puesto_penultimo_trabajo]
                                    ,[tiempo_penultimo_trabajo]
                                    ,[salario_semanal_penultimo]
                                    ,[horario_penultimo_trabajo]
                                    ,[dia_descanso_penultimo]
                                    ,[motivo_salida_penultimo]
                                    ,[como_se_entero_vacante]
                                    ,[conoce_trabajador]
                                    ,[a_quien_conoce]
                                    ,[tipo_relacion]
                                    ,[sucursal]
                                    ,[vacante]
                                FROM [Postulaciones]";

            // Construcción de la cláusula WHERE de manera dinámica
            var whereClauses = new List<string>();
            var parameters = new List<SqlParameter>();

            if (!string.IsNullOrEmpty(nombre))
            {
                whereClauses.Add("[nombre] LIKE @Nombre");
                parameters.Add(new SqlParameter("@Nombre", $"%{nombre}%"));
            }
            if (!string.IsNullOrEmpty(correo_electronico))
            {
                whereClauses.Add("[correo_electronico] LIKE @CorreoElectronico");
                parameters.Add(new SqlParameter("@CorreoElectronico", $"%{correo_electronico}%"));
            }
            if (!string.IsNullOrEmpty(tiempo_trabajo))
            {
                whereClauses.Add("[tiempo_trabajo] LIKE @TiempoTrabajo");
                parameters.Add(new SqlParameter("@TiempoTrabajo", $"%{tiempo_trabajo}%"));
            }
            if (!string.IsNullOrEmpty(ultimo_lugar_trabajo))
            {
                whereClauses.Add("[ultimo_lugar_trabajo] LIKE @UltimoLugarTrabajo");
                parameters.Add(new SqlParameter("@UltimoLugarTrabajo", $"%{ultimo_lugar_trabajo}%"));
            }
            if (!string.IsNullOrEmpty(descripcion))
            {
                whereClauses.Add("[descripcion] LIKE @Descripcion");
                parameters.Add(new SqlParameter("@Descripcion", $"%{descripcion}%"));
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
