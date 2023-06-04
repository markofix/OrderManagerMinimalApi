using FluentValidation;
using OrderManager.Application.Extensions;
using OrderManager.Application.Models;
using OrderManager.Domain.Errors;

namespace OrderManager.Application.Validators
{
    public class OrderDtoValidator : AbstractValidator<OrderDto>
    {
        public OrderDtoValidator()
        {
            RuleForEach(r => r.OrderItems)
               .ChildRules(items =>
               {
                   items.RuleFor(x => x.Quantity)
                       .GreaterThan(0)
                       .WithError(Errors.Order.QuantityShouldBeGreaterThenZero());
               });
        }
    }
}
