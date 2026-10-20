using GestionProyectosMonster.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/NoAutorizado";
        options.LogoutPath = "/Account/Logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
        options.Cookie.Name = "GestionMonster.Session";
    });

builder.Services.AddAuthorization();

string mongoConnectionString = builder.Configuration["MongoDB:ConnectionString"]
    ?? throw new InvalidOperationException("No se encontró la configuración 'MongoDB:ConnectionString'.");
string mongoDatabaseName = builder.Configuration["MongoDB:DatabaseName"]
    ?? throw new InvalidOperationException("No se encontró la configuración 'MongoDB:DatabaseName'.");

builder.Services.AddSingleton<IMongoClient>(_ => new MongoClient(mongoConnectionString));
builder.Services.AddScoped(sp => sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName));

builder.Services.AddScoped<DAOEmpleado>();
builder.Services.AddScoped<DAODepartamento>();
builder.Services.AddScoped<DAOUsuario>();
builder.Services.AddScoped<DAOProyecto>();
builder.Services.AddScoped<DAOHoras>();
builder.Services.AddScoped<DAOPerfil>();
builder.Services.AddScoped<DAOOpciones>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IOpcionesService>(sp =>
{
    var db = sp.GetRequiredService<IMongoClient>().GetDatabase(mongoDatabaseName);
    return new OpcionesService(db, sp.GetRequiredService<IMemoryCache>());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
