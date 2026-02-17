using System.Text;
using BreakRetailManager.Sales.Contracts;
using BreakRetailManager.Sales.Domain.Entities;
using ContractPaymentMethod = BreakRetailManager.Sales.Contracts.PaymentMethod;
using DomainPaymentMethod = BreakRetailManager.Sales.Domain.PaymentMethod;

namespace BreakRetailManager.Sales.Application;

public static class SalesMappings
{
    public static SalesOrderDto ToDto(SalesOrder order)
    {
        return new SalesOrderDto(
            order.Id,
            RepairText(order.Number),
            order.CreatedAt,
            order.Total,
            order.Lines
                .Select(line => new SalesOrderLineDto(line.Id, line.ProductId, RepairText(line.ProductName), line.Quantity, line.UnitPrice))
                .ToList(),
            ToContractPaymentMethod(order.PaymentMethod),
            order.LocationId,
            order.Cae is null ? null : RepairText(order.Cae),
            order.CaeExpirationDate,
            order.InvoiceNumber,
            order.PointOfSale,
            order.Subtotal,
            order.DiscountTotal,
            order.CreatedByObjectId,
            order.CreatedByDisplayName);
    }

    public static SalesOrder FromRequest(CreateSalesOrderRequest request)
    {
        if (request.Lines is null || request.Lines.Count == 0)
        {
            throw new ArgumentException("At least one line item is required.", nameof(request));
        }

        var order = new SalesOrder(
            $"SO-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Random.Shared.Next(1000, 9999)}",
            DateTimeOffset.UtcNow);

        order.SetPaymentMethod(ToDomainPaymentMethod(request.PaymentMethod));
        order.SetLocation(request.LocationId);

        foreach (var line in request.Lines)
        {
            order.AddLine(line.ProductId, RepairText(line.ProductName), line.Quantity, line.UnitPrice);
        }

        return order;
    }

    private static string RepairText(string value)
    {
        if (string.IsNullOrEmpty(value) || (!value.Contains('Ã') && !value.Contains('Â')))
        {
            return value;
        }

        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] > 0xFF)
            {
                return value;
            }
        }

        var repaired = Encoding.UTF8.GetString(Encoding.Latin1.GetBytes(value));
        return repaired.Contains('\uFFFD') ? value : repaired;
    }

    private static ContractPaymentMethod ToContractPaymentMethod(DomainPaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            DomainPaymentMethod.Cash => ContractPaymentMethod.Cash,
            DomainPaymentMethod.Card => ContractPaymentMethod.Card,
            DomainPaymentMethod.Transfer => ContractPaymentMethod.Transfer,
            _ => throw new ArgumentOutOfRangeException(nameof(paymentMethod), paymentMethod, "Unsupported payment method.")
        };
    }

    private static DomainPaymentMethod ToDomainPaymentMethod(ContractPaymentMethod paymentMethod)
    {
        return paymentMethod switch
        {
            ContractPaymentMethod.Cash => DomainPaymentMethod.Cash,
            ContractPaymentMethod.Card => DomainPaymentMethod.Card,
            ContractPaymentMethod.Transfer => DomainPaymentMethod.Transfer,
            _ => throw new ArgumentOutOfRangeException(nameof(paymentMethod), paymentMethod, "Unsupported payment method.")
        };
    }
}
