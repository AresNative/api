
public class CompraDto
{
    public string Codigo { get; set; }           // Código de la tabla CB
    public string Cuenta { get; set; }           // Cuenta de la tabla CB
    public string Unidad { get; set; }           // Unidad de la tabla CompraD
    public int CompraD_ID { get; set; }          // ID de la tabla CompraD
    public string CompraD_Codigo { get; set; }   // Código de la tabla CompraD
    public string Articulo { get; set; }         // Artículo de la tabla CompraD
    public decimal Cantidad { get; set; }        // Cantidad de la tabla CompraD
    public decimal Costo { get; set; }           // Costo de la tabla CompraD
    public int Compra_ID { get; set; }           // ID de la tabla Compra
    public string MovID { get; set; }            // MovID de la tabla Compra
    public DateTime FechaEmision { get; set; }   // Fecha de emisión de la tabla Compra
    public string Proveedor { get; set; }        // Proveedor de la tabla Compra
    public string Proveedor_Nombre { get; set; } // Nombre del proveedor (tabla Prov)
}
