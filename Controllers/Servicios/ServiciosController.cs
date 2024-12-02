using Openpay;
using Openpay.Entities;
using Openpay.Entities.Request;
using System.Net.Http;

public class PaymentService
{
    private readonly string _merchantId = "tu-merchant-id";
    private readonly string _privateKey = "tu-api-key-privada";
    private readonly OpenpayAPI _openpayAPI;

    // Constructor sin IHttpClientFactory, ya que OpenpayAPI maneja internamente el cliente HTTP
    public PaymentService()
    {
        // Instanciar OpenpayAPI directamente con las credenciales
        _openpayAPI = new OpenpayAPI(_privateKey, _merchantId);
    }

    public async Task<Charge> CreatePayment(PaymentRequest paymentRequest)
    {
        // Validación específica para el pago de luz (CFE)
        if (!string.Equals(paymentRequest.ServiceType, "cfe", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("El tipo de servicio debe ser 'CFE' para realizar este pago.");
        }

        var chargeRequest = new ChargeRequest
        {
            Method = "card", // Método de pago: tarjeta, efectivo, etc.
            SourceId = paymentRequest.CardToken, // Token de la tarjeta
            Amount = paymentRequest.Amount,
            Description = $"Pago de servicio de luz CFE, Número de Servicio: {paymentRequest.ServiceAccountNumber}",
            OrderId = Guid.NewGuid().ToString(),
            DeviceSessionId = "tu-session-id", // ID de sesión para seguridad

            // Ajuste en Metadata: usa Dictionary<string, string> en vez de object
            Metadata = new Dictionary<string, string>
            {
                { "tipo_servicio", "luz" },
                { "proveedor", "CFE" },
                { "numero_servicio", paymentRequest.ServiceAccountNumber }
            }
        };

        try
        {
            // Usar el método sincrónico Create() si CreateAsync no está disponible
            var charge = _openpayAPI.ChargeService.Create(chargeRequest);
            Console.WriteLine("Pago realizado exitosamente.");
            return charge; // Devuelve la información del pago realizado
        }
        catch (OpenpayException ex)
        {
            // Manejo de errores mejorado con logging o tratamiento adicional
            Console.WriteLine($"Error al realizar el pago: {ex.Message}");
            throw;
        }
    }
}

// Definición de la clase PaymentRequest
public class PaymentRequest
{
    public string ServiceType { get; set; } // Tipo de servicio (Ej: "CFE" para la luz)
    public string ServiceAccountNumber { get; set; } // Número de servicio de CFE
    public decimal Amount { get; set; } // Monto a pagar
    public string CardToken { get; set; } // Token de la tarjeta para el pago
}
