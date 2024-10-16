namespace MyApiProject.Models
{
    public class Project
    {
        public int Id { get; set; } // Primary Key
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public bool Activo { get; set; }
        public string State { get; set; }
    }
}