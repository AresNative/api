namespace MyApiProject.Models
{
    public class VentaDto
    {
        public int Id { get; set; }
        public string IdTypeTaxes { get; set; }
        public string IdMov { get; set; }
        public string IdState { get; set; }
        public string IdCaja { get; set; }
        public string IdTypePago { get; set; }
        public string IdSucursal { get; set; }
        public string IdAlmacen { get; set; }
        public string Art { get; set; }
        public int Cant { get; set; }
        public decimal Price { get; set; }
        public string Taxes { get; set; }
        public string Unit { get; set; }
        public string StateArt { get; set; }
        public string User { get; set; }
        public string Client { get; set; }
        public decimal Pago { get; set; }
        public decimal Import { get; set; }
        public string Currency { get; set; }
    }
}
