using System.Net.Http.Json;
using FluentAssertions;
using OrderManager.Application.Models;
using OrderManager.IntegrationTests.AutoFixture;
using Xunit;

namespace OrderManager.IntegrationTests.Endpoints
{
    public class GetOrderTests
    {

        [Theory]
        [IntegrationHostInlineData(1, 1, 9)]
        public async Task GetOrderById_OrderFound_ReturnsOrderDetails(
           int id,
           decimal expectedDiscountAmount,
           decimal expectedTotalAmount,
           IntegrationTestHostBuilder integrationTestHostBuilder)
        {
            using var host = integrationTestHostBuilder();
            using var client = host.CreateClient();

            var orderDetails = await client.GetFromJsonAsync<OrderDto>($"orders/{id}");

            orderDetails.Should()
                .BeEquivalentTo(new OrderDto()
                {
                    Id = id,
                    DiscountAmount = expectedDiscountAmount,
                    TotalAmount = expectedTotalAmount
                });
        }
    }
}
