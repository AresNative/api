public class UploadPorveedoresPoral
{
    public IFormFile File { get; set; }

    public string PorveedoresPoralForm { get; set; }
}
public class PorveedoresPoralData
{
    public int id_proveedor { get; set; }
    public DateTime fecha { get; set; }
    public string mov { get; set; }
    public string comment { get; set; }
}