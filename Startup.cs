
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }
    public void ConfigureServices(IServiceCollection services)
    {

        var allowedCorsOrigins = _configuration.GetSection("AllowedCorsOrigins").Get<string[]>();

        // Definir la política de CORS desde configuración
        services.AddCors(options =>
        {
            options.AddPolicy("AllowedCorsOrigins",
                policy =>
                {
                    policy.WithOrigins(allowedCorsOrigins)  // Cargar orígenes desde appsettings.json
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
        });

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],/* matrizmercadoliz.dyndns.org:5000 !cambiar en appsettings.json */
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Key"])) // Cambia por tu clave secreta
                };
            });

        services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.MaxDepth = int.MaxValue; // Sin límite
            })
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.MaxDepth = null; // Sin límite de profundidad
                options.SerializerSettings.Error = (sender, args) =>
                {
                    // Manejo de errores (opcional)
                    args.ErrorContext.Handled = true;
                };
            });
        // Registrar los controladores, servicios y otros componentes
        services.AddHttpClient();
        services.AddEndpointsApiExplorer();

        // Registrar AuthUtils y TokensUtils como servicios separados
        services.AddScoped<AuthUtils>();
        services.AddScoped<TokensUtils>();
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });
            c.OperationFilter<FileUploadOperationFilter>();

            // Configuración de seguridad para JWT
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese el token JWT en este formato: Bearer {token}"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    new string[] {}
                }
            });
        });

    }

    // Método para configurar el pipeline de la aplicación
    public void Configure(IApplicationBuilder app/* , IWebHostEnvironment env */)
    {
        /* 
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
        }
        */

        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "My API V1");
            c.RoutePrefix = string.Empty; // Acceso a Swagger en la raíz
        });

        app.UseCors("AllowedCorsOrigins");

        app.UseRouting();

        //app.UseHttpsRedirection();
        app.UseAuthentication();  // Si usas autenticación
        app.UseAuthorization();

        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
        });
    }
}