namespace MyApiProject.Models
{
    public class TaskModel
    {

        public int id { get; set; }
        public int sprint_id { get; set; }
        public string nombre { get; set; }
        public DateTime feche_creacion { get; set; }
        public DateTime feche_vencimiento { get; set; }
        public string estado { get; set; }
    }
}
