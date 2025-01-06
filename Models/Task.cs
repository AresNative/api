namespace MyApiProject.Models
{
    public class TaskModel
    {

        public int id { get; set; }
        public int sprint_id { get; set; }
        public string nombre { get; set; }
        public DateTime fecha_creacion { get; set; }
        public DateTime fecha_vencimiento { get; set; }
        public string estado { get; set; }

        public string descripcion { get; set; }
        public string prioridad { get; set; }
    }
}
