using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Mvc;
using NLog;
using Services;
using Services.Contracts;
using WebApi.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Nlog i�in yap�land�rma ayarlar�n� nlog.config dosyas�na y�kler.
LogManager.LoadConfiguration(String.Concat(Directory.GetCurrentDirectory(),"/nlog.config"));

builder.Services.AddControllers(config =>
{
    config.RespectBrowserAcceptHeader = true; // taray�c�dan gelen accept header de�erini kullanarak yan�t�n format�n� belirliyor.
    config.ReturnHttpNotAcceptable = true; // yan�t format� xml, csv vb olabilir. eger olmazsa 406 hatas�n� d�ner
    config.CacheProfiles.Add("5mins", new CacheProfile() { Duration = 300 });
})
.AddXmlDataContractSerializerFormatters() // xml serile�tirme deste�i ekliyoruz
.AddCustomCsvFormatter()
.AddApplicationPart(typeof(Presentation.AssemblyReference).Assembly)
.AddNewtonsoftJson(opt => 
    opt.SerializerSettings.ReferenceLoopHandling = 
    Newtonsoft.Json.ReferenceLoopHandling.Ignore);

  

builder.Services.Configure<ApiBehaviorOptions>(options =>
{ // ap� davran�� ayarlar�n� yap�land�r�r ve model durumunu yanl�� filtrelemeden ayarlar.
    options.SuppressModelStateInvalidFilter = true;
});


builder.Services.AddEndpointsApiExplorer(); // bu iki sat�r swagger aray�z� i�in gerekli olan bile�enleri ekler
builder.Services.ConfigureSwagger();

builder.Services.ConfigureSqlContext(builder.Configuration); // veri taban� i�lemleri i�in kullan�l�r
builder.Services.ConfigureRepositoryManager();
builder.Services.ConfigureServiceManager();
builder.Services.ConfigureLoggerService();
builder.Services.AddAutoMapper(typeof(Program));
builder.Services.ConfigureActionFilters(); // web uygulamas�ndaki farkli servislerin yap�land�r�ld��� yer
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

if (app.Environment.IsDevelopment())//eger development a�amas�nda ise swaggera ge�i� yapar
{
    app.UseSwagger();
    app.UseSwaggerUI(s =>
    {
        s.SwaggerEndpoint("/swagger/v1/swagger.json", "BTK Akademi v1");
        s.SwaggerEndpoint("/swagger/v2/swagger.json", "BTK Akademi v2");
    });
}

if (app.Environment.IsProduction())//e�er prodyct a�amas�nda ise Hsts'yi �al��t�r�r.
{
    app.UseHsts(); // uygulaman�n �al��maya haz�r gelmesini sa�layan kod par�as�d�r.
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


//katmanl� mimar�,
//repository pattern:  veri taban� metotlar�n�n yaz�ld�g� katmand�r. 
//generic repository: Crud i�lemlerinin y�netildi�i katmand�r.
// busines nedir temelde ne i� yap�l�yor. 
// dp injection
// sevice register i�lemleri(auto fake)
//fluent api-entity frame work core
//extentions method
//neden intefcaea kullan�l�r
//�oc conteiner     