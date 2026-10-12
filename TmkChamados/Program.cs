using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Serilog;
using TmkChamados.Data;
using TmkChamados.Models;
using TmkChamados.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File(
        Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true)
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseWindowsService();
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Anexos de Andamento aceitam até 50MB (ver AnexoAndamentoRegras); o padrão
        // do Kestrel (~28.6MB) é menor que isso, então precisa ser elevado. A folga
        // extra cobre o overhead do multipart/form-data (boundaries, outros campos).
        options.Limits.MaxRequestBodySize = 60L * 1024 * 1024;
    });
    builder.Host.UseSerilog((context, loggerConfiguration) =>
        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true));

    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddDbContext<TmkChamadosDbContext>(options =>
        options.UseSqlServer(builder.Configuration.GetConnectionString("TmkChamados")));
    builder.Services.AddScoped<ITicketService, TicketService>();
    builder.Services.AddScoped<IUsuarioService, UsuarioService>();
    builder.Services.AddScoped<IEmpresaService, EmpresaService>();
    builder.Services.AddScoped<IAndamentoService, AndamentoService>();
    builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
    builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
    builder.Services.AddScoped<INotificacaoTicketEmailService, NotificacaoTicketEmailService>();

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
}
catch (Microsoft.Extensions.Hosting.HostAbortedException)
{
    // Lançada de propósito por ferramentas de design-time (ex.: `dotnet ef
    // migrations add`) para resolver o DbContext sem rodar o host de verdade;
    // não é uma falha real da aplicação, então não é logada como erro.
}
catch (Exception ex)
{
    Log.Fatal(ex, "TmkChamados encerrou inesperadamente durante a inicialização.");
}
finally
{
    Log.CloseAndFlush();
}
