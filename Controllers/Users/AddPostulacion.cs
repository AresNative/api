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

                // Asignación de parámetros
                command.Parameters.AddWithValue("@nombre", nuevaPostulacion.nombre);
                command.Parameters.AddWithValue("@apellido_paterno", nuevaPostulacion.apellido_paterno);
                command.Parameters.AddWithValue("@apellido_materno", nuevaPostulacion.apellido_materno);
                command.Parameters.AddWithValue("@edad", nuevaPostulacion.edad);
                command.Parameters.AddWithValue("@fecha_nacimiento", nuevaPostulacion.fecha_nacimiento);
                command.Parameters.AddWithValue("@correo_electronico", nuevaPostulacion.correo_electronico);
                command.Parameters.AddWithValue("@numero_telefono", nuevaPostulacion.numero_telefono);
                command.Parameters.AddWithValue("@direccion_actual", nuevaPostulacion.direccion_actual);
                command.Parameters.AddWithValue("@fecha_radica_ciudad", nuevaPostulacion.fecha_radica_ciudad);
                command.Parameters.AddWithValue("@medio_transporte", nuevaPostulacion.medio_transporte);
                command.Parameters.AddWithValue("@tiempo_traslado", nuevaPostulacion.tiempo_traslado);
                command.Parameters.AddWithValue("@estado_civil", nuevaPostulacion.estado_civil);
                command.Parameters.AddWithValue("@tiempo_casado", nuevaPostulacion.tiempo_casado);
                command.Parameters.AddWithValue("@bienes_mancomunados", nuevaPostulacion.bienes_mancomunados);
                command.Parameters.AddWithValue("@tienes_hijos", nuevaPostulacion.tienes_hijos);
                command.Parameters.AddWithValue("@planeas_mas_hijos", nuevaPostulacion.planeas_mas_hijos);
                command.Parameters.AddWithValue("@disponibilidad_horario", nuevaPostulacion.disponibilidad_horario);
                command.Parameters.AddWithValue("@ultimo_grado_estudios", nuevaPostulacion.ultimo_grado_estudios);
                command.Parameters.AddWithValue("@tienes_certificado", nuevaPostulacion.tienes_certificado);
                command.Parameters.AddWithValue("@estudias_actualmente", nuevaPostulacion.estudias_actualmente);
                command.Parameters.AddWithValue("@dias_horario_estudio", nuevaPostulacion.dias_horario_estudio);
                command.Parameters.AddWithValue("@ultimo_lugar_trabajo", nuevaPostulacion.ultimo_lugar_trabajo);
                command.Parameters.AddWithValue("@puesto_ultimo_trabajo", nuevaPostulacion.puesto_ultimo_trabajo);
                command.Parameters.AddWithValue("@tiempo_trabajo", nuevaPostulacion.tiempo_trabajo);
                command.Parameters.AddWithValue("@salario_semanal", nuevaPostulacion.salario_semanal);
                command.Parameters.AddWithValue("@horario_trabajo", nuevaPostulacion.horario_trabajo);
                command.Parameters.AddWithValue("@dia_descanso", nuevaPostulacion.dia_descanso);
                command.Parameters.AddWithValue("@motivo_salida", nuevaPostulacion.motivo_salida);
                command.Parameters.AddWithValue("@penultimo_lugar_trabajo", nuevaPostulacion.penultimo_lugar_trabajo);
                command.Parameters.AddWithValue("@puesto_penultimo_trabajo", nuevaPostulacion.puesto_penultimo_trabajo);
                command.Parameters.AddWithValue("@tiempo_penultimo_trabajo", nuevaPostulacion.tiempo_penultimo_trabajo);
                command.Parameters.AddWithValue("@salario_semanal_penultimo", nuevaPostulacion.salario_semanal_penultimo);
                command.Parameters.AddWithValue("@horario_penultimo_trabajo", nuevaPostulacion.horario_penultimo_trabajo);
                command.Parameters.AddWithValue("@dia_descanso_penultimo", nuevaPostulacion.dia_descanso_penultimo);
                command.Parameters.AddWithValue("@motivo_salida_penultimo", nuevaPostulacion.motivo_salida_penultimo);
                command.Parameters.AddWithValue("@como_se_entero_vacante", nuevaPostulacion.como_se_entero_vacante);
                command.Parameters.AddWithValue("@conoce_trabajador", nuevaPostulacion.conoce_trabajador);
                command.Parameters.AddWithValue("@a_quien_conoce", nuevaPostulacion.a_quien_conoce);
                command.Parameters.AddWithValue("@tipo_relacion", nuevaPostulacion.tipo_relacion);
                command.Parameters.AddWithValue("@sucursal", nuevaPostulacion.sucursal);
                command.Parameters.AddWithValue("@vacante", nuevaPostulacion.vacante);

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
