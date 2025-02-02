using Microsoft.OpenApi.Any;

public class CombosParams
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public decimal PriceOfert { get; set; }
    public AnyType Descripcion { get; set; }
    public DateOnly Date { get; set; }
    public string estado { get; set; }
    public string porcentaje { get; set; }
}