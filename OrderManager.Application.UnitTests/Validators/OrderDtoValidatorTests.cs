using FluentAssertions;
using FluentValidation.TestHelper;
using OrderManager.Application.Models;
using OrderManager.Application.Validators;
using OrderManager.Domain.Errors;
using Xunit;

namespace OrderManager.Application.UnitTests.Validators
{
    public class OrderDtoValidatorTests
    {

        [Theory]
        [AutoMoqInlineData(0)]
        [AutoMoqInlineData(-1)]
        public async Task Quantity_LessThenOne_HasValidationError(
            int quantity,
            OrderDto orderDto,
            OrderDtoValidator sut)
        {
            orderDto.OrderItems = new List<OrderItemDto>()
            {
                new OrderItemDto()
                {
                    Quantity = quantity,
                }
            };

            var result = await sut.TestValidateAsync(orderDto);

            var error = result.Errors.First(x => x.PropertyName.StartsWith(nameof(orderDto.OrderItems)));
            error.ErrorCode.Should().Be(Errors.Order.QuantityShouldBeGreaterThenZero().ErrorCode);
        }
    }
}
