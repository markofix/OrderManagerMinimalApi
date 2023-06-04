using FluentValidation;
using OrderManager.Application.Extensions;
using OrderManager.Application.Models;
using OrderManager.Application.Responses;
using OrderManager.Application.Services;
using OrderManager.Application.Services.Interfaces;
using OrderManager.Domain.Errors;
using OrderManagerMinimalApi.Endpoints.Internal;

namespace OrderManagerMinimalApi.Endpoints
{
    public class OrdersEndpoints : IEndpoints
    {
        private const string ContentType = "application/json";
        private const string Tag = "Orders";
        private const string BaseRoute = "orders";

        public static void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IOrderService, OrderService>();
        }

        public static void DefineEndpoints(IEndpointRouteBuilder app)
        {
            app.MapPost(BaseRoute, CreateOrder)
                .WithName("CreateOrder")
                .Accepts<OrderDto>(ContentType)
                .Produces<OrderDto>(201).Produces<ErrorResponse>(400)
                .WithTags(Tag);

            app.MapPost($"/{BaseRoute}/{{id}}/checkout", CheckoutOrder)
                .WithName("CheckoutOrder")
                .Produces<OrderDto>(200).Produces<ErrorResponse>(400)
                .WithTags(Tag);

            app.MapGet($"{BaseRoute}/{{id}}", GetOrderById)
                .WithName("GetBook")
                .Produces<OrderDto>(200).Produces(404)
                .WithTags(Tag);
        }

        private static async Task<IResult> GetOrderById(int id, IOrderService orderService)
        {
            var order = await orderService.GetOrderById(id);
            return order is not null ? Results.Ok(order) : Results.NotFound();
        }

        private static async Task<IResult> CreateOrder(OrderDto orderDto, IOrderService orderService, IValidator<OrderDto> validator)
        {
            var validationResult = await validator.ValidateAsync(orderDto);
            if (!validationResult.IsValid)
            {
                return ReturnError(validationResult.ToError());
            }

            var result = await orderService.CreateOrder(orderDto);

            if (!result.IsSuccessful)
            {
                return ReturnError(result.Error);
            }

            return Results.Created($"/{BaseRoute}/{orderDto.Id}", result.Data);
        }

        private static async Task<IResult> CheckoutOrder(int id, IOrderService orderService)
        {
            var result = await orderService.CheckoutOrder(id);

            if (!result.IsSuccessful)
            {
                return ReturnError(result.Error);
            }

            return Results.Ok(result.Data);
        }

        private static IResult ReturnError(Error error)
        {
            var response = new ErrorResponse()
            {
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Error = error
            };

            return Results.BadRequest(response);
        }
    }
}
