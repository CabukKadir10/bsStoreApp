using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Services;
using Services.Contracts;
using WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Nlog için yapýlandýrma ayarlarýný nlog.config dosyasýna yükler.
LogManager.LoadConfiguration(String.Concat(Directory.GetCurrentDirectory(),"/nlog.config"));

builder.Services.AddControllers(config =>
{
    config.RespectBrowserAcceptHeader = true; // tarayýcýdan gelen accept header deðerini kullanarak yanýtýn formatýný belirliyor.
    config.ReturnHttpNotAcceptable = true; // yanýt formatý xml, csv vb olabilir. eger olmazsa 406 hatasýný döner
    config.CacheProfiles.Add("5mins", new CacheProfile() { Duration = 300 });
})
.AddXmlDataContractSerializerFormatters() // xml serileþtirme desteði ekliyoruz
.AddCustomCsvFormatter()
.AddApplicationPart(typeof(Presentation.AssemblyReference).Assembly)
.AddNewtonsoftJson(opt => 
    opt.SerializerSettings.ReferenceLoopHandling = 
    Newtonsoft.Json.ReferenceLoopHandling.Ignore);

  

builder.Services.Configure<ApiBehaviorOptions>(options =>
{ // apý davranýþ ayarlarýný yapýlandýrýr ve model durumunu yanlýþ filtrelemeden ayarlar.
    options.SuppressModelStateInvalidFilter = true;
});


builder.Services.AddEndpointsApiExplorer(); // bu iki satýr swagger arayüzü için gerekli olan bileþenleri ekler
builder.Services.ConfigureSwagger();

builder.Services.ConfigureSqlContext(builder.Configuration); // veri tabaný iþlemleri için kullanýlýr
builder.Services.ConfigureRepositoryManager();
builder.Services.ConfigureServiceManager();
builder.Services.ConfigureLoggerService();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.ConfigureActionFilters(); // web uygulamasýndaki farkli servislerin yapýlandýrýldýðý yer
builder.Services.ConfigureCors();
builder.Services.ConfigureDataShaper();
builder.Services.AddCustomMediaTypes();
builder.Services.AddScoped<IBookLinks, BookLinks>();
builder.Services.ConfigureVersioning();
builder.Services.ConfigureResponseCaching();
builder.Services.ConfigureHttpCacheHeaders();
builder.Services.AddMemoryCache();
builder.Services.ConfigureRateLimitingOptions();
builder.Services.AddHttpContextAccessor();
builder.Services.ConfigureIdentity();
builder.Services.ConfigureJWT(builder.Configuration);
builder.Services.RegisterRepositories();
builder.Services.RegisterServices();

var app = builder.Build();

var logger = app.Services.GetRequiredService<ILoggerService>();
app.ConfigureExceptionHandler(logger);

if (app.Environment.IsDevelopment())//eger development aþamasýnda ise swaggera geçiþ yapar
{
    app.UseSwagger();
    app.UseSwaggerUI(s =>
    {
        s.SwaggerEndpoint("/swagger/v1/swagger.json", "BTK Akademi v1");
        s.SwaggerEndpoint("/swagger/v2/swagger.json", "BTK Akademi v2");
    });
}

if (app.Environment.IsProduction())//eðer prodyct aþamasýnda ise Hsts'yi çalýþtýrýr.
{
    app.UseHsts(); // uygulamanýn çalýþmaya hazýr gelmesini saðlayan kod parçasýdýr.
}

app.UseHttpsRedirection();

app.UseIpRateLimiting();
app.UseCors("CorsPolicy");
app.UseResponseCaching();
app.UseHttpCacheHeaders();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();


//katmanlý mimarý,
//repository pattern:  veri tabaný metotlarýnýn yazýldýgý katmandýr. 
//generic repository: Crud iþlemlerinin yönetildiði katmandýr.
// busines nedir temelde ne iþ yapýlýyor. 
// dp injection
// sevice register iþlemleri(auto fake)
//fluent api-entity frame work core
//extentions method
//neden intefcaea kullanýlýr
//ýoc conteiner     