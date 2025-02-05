namespace MyApiProject.Models
{
    public class BusquedaParams
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public string? Operator { get; set; }  // Opcional: puedes usar operadores como 'like', '=', '>=', etc.
    }
    public class SumaParams
    {
        public string? Key { get; set; }
    }

}