using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using MyApiProject.Models;

namespace MyApiProject.Controllers
{
    public partial class UsuariosController : BaseController
    {
        [HttpPost("api/v1/users/postulacion")]
        public async Task<IActionResult> RegistrarPostulacion([FromBody] Postulacion nuevaPostulacion)
        {
            string query = @"
                INSERT INTO Postulaciones 
                (nombre, apellido_paterno, apellido_materno, edad, fecha_nacimiento, correo_electronico, 
                 numero_telefono, direccion_actual, fecha_radica_ciudad, medio_transporte, tiempo_traslado, 
                 estado_civil, tiempo_casado, bienes_mancomunados, tienes_hijos, planeas_mas_hijos, 
                 disponibilidad_horario, ultimo_grado_estudios, tienes_certificado, estudias_actualmente, 
                 dias_horario_estudio, ultimo_lugar_trabajo, puesto_ultimo_trabajo, tiempo_trabajo, 
                 salario_semanal, horario_trabajo, dia_descanso, motivo_salida, penultimo_lugar_trabajo, 
                 puesto_penultimo_trabajo, tiempo_penultimo_trabajo, salario_semanal_penultimo, 
                 horario_penultimo_trabajo, dia_descanso_penultimo, motivo_salida_penultimo, 
                 como_se_entero_vacante, conoce_trabajador, a_quien_conoce, tipo_relacion, sucursal, vacante) 
                VALUES 
                (@nombre, @apellido_paterno, @apellido_materno, @edad, @fecha_nacimiento, @correo_electronico, 
                 @numero_telefono, @direccion_actual, @fecha_radica_ciudad, @medio_transporte, @tiempo_traslado, 
                 @estado_civil, @tiempo_casado, @bienes_mancomunados, @tienes_hijos, @planeas_mas_hijos, 
                 @disponibilidad_horario, @ultimo_grado_estudios, @tienes_certificado, @estudias_actualmente, 
                 @dias_horario_estudio, @ultimo_lugar_trabajo, @puesto_ultimo_trabajo, @tiempo_trabajo, 
                 @salario_semanal, @horario_trabajo, @dia_descanso, @motivo_salida, @penultimo_lugar_trabajo, 
                 @puesto_penultimo_trabajo, @tiempo_penultimo_trabajo, @salario_semanal_penultimo, 
                 @horario_penultimo_trabajo, @dia_descanso_penultimo, @motivo_salida_penultimo, 
                 @como_se_entero_vacante, @conoce_trabajador, @a_quien_conoce, @tipo_relacion, @sucursal, @vacante)";

            try
            {
                await using var connection = await OpenConnectionAsync();
                await using var command = new SqlCommand(query, connection);

                // Asignación de parámetros con manejo de valores nulos
                command.Parameters.AddWithValue("@nombre", nuevaPostulacion.nombre ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@apellido_paterno", nuevaPostulacion.apellido_paterno ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@apellido_materno", nuevaPostulacion.apellido_materno ?? (object)DBNull.Value);

                // Manejo de tipo int y DateTime
                if (nuevaPostulacion.edad.HasValue)
                    command.Parameters.AddWithValue("@edad", nuevaPostulacion.edad.Value);
                else
                    command.Parameters.AddWithValue("@edad", DBNull.Value);

                if (nuevaPostulacion.fecha_nacimiento.HasValue)
                    command.Parameters.AddWithValue("@fecha_nacimiento", nuevaPostulacion.fecha_nacimiento.Value);
                else
                    command.Parameters.AddWithValue("@fecha_nacimiento", DBNull.Value);

                command.Parameters.AddWithValue("@correo_electronico", nuevaPostulacion.correo_electronico ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@numero_telefono", nuevaPostulacion.numero_telefono ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@direccion_actual", nuevaPostulacion.direccion_actual ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@fecha_radica_ciudad", nuevaPostulacion.fecha_radica_ciudad ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@medio_transporte", nuevaPostulacion.medio_transporte ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tiempo_traslado", nuevaPostulacion.tiempo_traslado ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@estado_civil", nuevaPostulacion.estado_civil ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tiempo_casado", nuevaPostulacion.tiempo_casado ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@bienes_mancomunados", nuevaPostulacion.bienes_mancomunados ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tienes_hijos", nuevaPostulacion.tienes_hijos ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@planeas_mas_hijos", nuevaPostulacion.planeas_mas_hijos ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@disponibilidad_horario", nuevaPostulacion.disponibilidad_horario ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ultimo_grado_estudios", nuevaPostulacion.ultimo_grado_estudios ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tienes_certificado", nuevaPostulacion.tienes_certificado ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@estudias_actualmente", nuevaPostulacion.estudias_actualmente ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@dias_horario_estudio", nuevaPostulacion.dias_horario_estudio ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@ultimo_lugar_trabajo", nuevaPostulacion.ultimo_lugar_trabajo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@puesto_ultimo_trabajo", nuevaPostulacion.puesto_ultimo_trabajo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tiempo_trabajo", nuevaPostulacion.tiempo_trabajo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@salario_semanal", nuevaPostulacion.salario_semanal ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@horario_trabajo", nuevaPostulacion.horario_trabajo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@dia_descanso", nuevaPostulacion.dia_descanso ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@motivo_salida", nuevaPostulacion.motivo_salida ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@penultimo_lugar_trabajo", nuevaPostulacion.penultimo_lugar_trabajo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@puesto_penultimo_trabajo", nuevaPostulacion.puesto_penultimo_trabajo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tiempo_penultimo_trabajo", nuevaPostulacion.tiempo_penultimo_trabajo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@salario_semanal_penultimo", nuevaPostulacion.salario_semanal_penultimo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@horario_penultimo_trabajo", nuevaPostulacion.horario_penultimo_trabajo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@dia_descanso_penultimo", nuevaPostulacion.dia_descanso_penultimo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@motivo_salida_penultimo", nuevaPostulacion.motivo_salida_penultimo ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@como_se_entero_vacante", nuevaPostulacion.como_se_entero_vacante ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@conoce_trabajador", nuevaPostulacion.conoce_trabajador ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@a_quien_conoce", nuevaPostulacion.a_quien_conoce ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@tipo_relacion", nuevaPostulacion.tipo_relacion ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@sucursal", nuevaPostulacion.sucursal ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@vacante", nuevaPostulacion.vacante ?? (object)DBNull.Value);

                var result = await command.ExecuteNonQueryAsync();

                if (result > 0)
                    return Ok(new { Message = "Postulacion registrado exitosamente" });
                else
                    return BadRequest(new { Message = "No se pudo registrar el postulacion" });
            }
            catch (Exception ex)
            {
                return HandleException(ex);
            }
        }
    }
}
