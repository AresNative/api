public class UploadCombos
{
    public IFormFile File { get; set; }

    public string CombosData { get; set; }
}
public class CombosData
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public decimal Price_ofer { get; set; }
    public string Description { get; set; } // Asegúrate de que sea string
    public DateTime Date { get; set; }
    public string State { get; set; }
    public string Porcentaje { get; set; }
}