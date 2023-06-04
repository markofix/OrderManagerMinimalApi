using FluentValidation;
using OrderManager.Application.Validators;
using OrderManager.Extensions;
using OrderManager.Infrastructure.EntityFramework.Extensions;
using OrderManager.Web.Extensions;
using OrderManagerMinimalApi.Endpoints.Internal;

namespace OrderManagerMinimalApi;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddEntityFramework();
        builder.Services.AddDomainServices();
        builder.Services.AddEndpoints<Program>(builder.Configuration);
        builder.Services.AddValidatorsFromAssemblyContaining<OrderDtoValidator>();

        var app = builder.Build();

        app.UseEndpoints<Program>();
        app.AddData();

        app.Run();
    }
}