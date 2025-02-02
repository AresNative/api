public class CertificadosParams
{
    public IFormFile File { get; set; }


    public string CertificadosData { get; set; }

}
public class CertificadosData
{
    public string Name { get; set; }
    public string Certificador { get; set; }
    public DateOnly Date { get; set; }
    public string Descripcion { get; set; }
    public string estado { get; set; }
}