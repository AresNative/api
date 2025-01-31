public class PromocionesParams
{
    /* public IFormFile File { get; set; } */
    public string id_article { get; set; }
    public string id_user { get; set; }
    public string id_sucursal { get; set; }
    public DateTime date { get; set; }
    public decimal promotional_price { get; set; }
    public DateTime start_date { get; set; }
    public DateTime end_date { get; set; }
}
