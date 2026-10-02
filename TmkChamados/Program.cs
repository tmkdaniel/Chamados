using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using TmkChamados.Data;
using TmkChamados.Models;
using TmkChamados.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddDbContext<TmkChamadosDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("TmkChamados")));
builder.Services.AddScoped<IChamadoService, ChamadoService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();
builder.Services.AddScoped<IEmpresaService, EmpresaService>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Login";
        options.AccessDeniedPath = "/Login";
    });

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapRazorPages();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<TmkChamadosDbContext>();
    dbContext.Database.Migrate();

    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
    var empresaService = scope.ServiceProvider.GetRequiredService<IEmpresaService>();
    if (!usuarioService.Listar().Any())
    {
        var empresaPadrao = empresaService.Criar("Padrão");
        usuarioService.Criar("tmk", "a", TipoUsuario.Master, empresaPadrao.Id);
    }
}

app.Run();
