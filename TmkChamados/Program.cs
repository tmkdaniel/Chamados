using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
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
    // ContentRootPath, por padrão, é o diretório de trabalho de quem chamou o processo,
    // não a pasta onde o executável está. Isso quebra a leitura de appsettings.json
    // (e, antes, falhava silenciosamente - GetConnectionString vinha vazio) sempre que o
    // .exe é chamado de outro diretório. UseWindowsService corrige isso quando o processo
    // está rodando de verdade como Windows Service, mas não quando o .exe é executado
    // diretamente (ex.: testando o publish antes de instalar o serviço).
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = args,
        ContentRootPath = AppContext.BaseDirectory
    });

    builder.Host.UseWindowsService();
    builder.WebHost.ConfigureKestrel(options =>
    {
        // Anexos de Andamento aceitam até 50MB (ver AnexoAndamentoRegras); o padrão
        // do Kestrel (~28.6MB) é menor que isso, então precisa ser elevado. A folga
        // extra cobre o overhead do multipart/form-data (boundaries, outros campos).
        options.Limits.MaxRequestBodySize = 60L * 1024 * 1024;
    });
    // Liga/desliga o log de comandos SQL do EF Core (ver Diagnostico:LogarComandosSql em
    // appsettings.json). Lido antes do UseSerilog porque o override precisa entrar na
    // configuração do logger já na criação. Exige reiniciar o programa para ter efeito -
    // não é um liga/desliga em tempo de execução. Não habilita EnableSensitiveDataLogging,
    // então valores de parâmetro continuam mascarados ("?") mesmo com o SQL visível.
    var logarComandosSql = builder.Configuration.GetValue<bool>("Diagnostico:LogarComandosSql");
    var nivelLogSql = logarComandosSql ? Serilog.Events.LogEventLevel.Information : Serilog.Events.LogEventLevel.Warning;

    builder.Host.UseSerilog((context, loggerConfiguration) =>
        loggerConfiguration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", nivelLogSql)
            .Enrich.FromLogContext()
            .WriteTo.Console(outputTemplate: "{Timestamp:HH:mm:ss} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                Path.Combine(AppContext.BaseDirectory, "Logs", "log-.txt"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                shared: true,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"));

    // Criptografia em repouso da connection string e da senha de e-mail em
    // appsettings.json (ver openspec/changes/archive/.../criptografia-segredos-config).
    // Só age quando o valor efetivo bate com o valor bruto do arquivo - se
    // appsettings.{Ambiente}.json ou uma variável de ambiente estiver sobrescrevendo,
    // usa esse valor como está, sem tocar em criptografia nem no arquivo.
    var appSettingsPath = Path.Combine(builder.Environment.ContentRootPath, "appsettings.json");
    var appSettingsRaiz = JsonNode.Parse(File.ReadAllText(appSettingsPath))!;

    // Chave do Data Protection API fica num arquivo em disco (não há DI container ainda
    // nesse ponto da inicialização, por isso usa DataProtectionProvider.Create em vez de
    // builder.Services.AddDataProtection). Cross-platform de propósito - ver design.md.
    var dataProtectionProvider = DataProtectionProvider.Create(new DirectoryInfo(Path.Combine(AppContext.BaseDirectory, "Keys")));
    var protetorSegredos = dataProtectionProvider.CreateProtector("TmkChamados.SegredosConfiguracao");
    var segredoResolver = new SegredoConfiguracaoResolver(protetorSegredos);

    var connectionStringResolvida = segredoResolver.Resolver(
        builder.Configuration.GetConnectionString("TmkChamados"),
        appSettingsRaiz,
        ["ConnectionStrings", "TmkChamados"],
        ["ConnectionStringsCripto", "TmkChamados"],
        out var connectionStringAlterada);

    var emailPasswordResolvida = segredoResolver.Resolver(
        builder.Configuration["Email:Password"],
        appSettingsRaiz,
        ["Email", "Password"],
        ["Email", "PasswordCripto"],
        out var emailPasswordAlterada);

    if (connectionStringAlterada || emailPasswordAlterada)
    {
        File.WriteAllText(appSettingsPath, appSettingsRaiz.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    builder.Configuration["Email:Password"] = emailPasswordResolvida;

    // Add services to the container.
    builder.Services.AddRazorPages();
    builder.Services.AddDbContext<TmkChamadosDbContext>(options =>
        options.UseSqlServer(connectionStringResolvida));
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

    var versaoPrograma = Assembly.GetExecutingAssembly().GetName().Version;
    Log.Information("==========================");
    Log.Information("TmkChamados iniciando - versão {Versao}", versaoPrograma);

    // Expõe o usuário autenticado para o LogContext do Serilog durante toda a requisição,
    // para que exceções não tratadas (logadas pelo UseExceptionHandler, abaixo) incluam
    // quem estava logado. Precisa vir ANTES do UseExceptionHandler: o `using` abaixo é
    // desfeito (Dispose) assim que a exceção atravessa esse ponto da pilha, então se esse
    // middleware viesse depois do UseExceptionHandler (ex.: só depois de UseAuthentication,
    // como de costume), a propriedade já teria sido removida quando o handler loga o erro.
    // Por isso autentica aqui diretamente (AuthenticateAsync), sem depender do
    // UseAuthentication() mais abaixo, que continua necessário para o resto do pipeline.
    app.Use(async (context, next) =>
    {
        var resultadoAuth = await context.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var nomeUsuario = resultadoAuth.Succeeded ? resultadoAuth.Principal?.Identity?.Name : null;
        if (nomeUsuario is not null)
        {
            using (LogContext.PushProperty("Usuario", nomeUsuario))
            {
                await next();
            }
        }
        else
        {
            await next();
        }
    });

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
