using CloudCostEngine.Application.Models;
using CloudCostEngine.Domain;

namespace CloudCostEngine.Application.Interfaces;

/// <summary>
/// Computes the total monthly cost of a single plan for a given storage consumption.
/// This is a Strategy interface on purpose: "monthly forward evaluation" is one strategy
/// today, but the take-home explicitly calls out that the next feature is likely
/// budget-based optimization ("what's the most storage I can buy for $X?"). That would be
/// a second, independent implementation of this interface, not a rewrite of this one.
/// </summary>
public interface ICostCalculator
{
    CostResult Calculate(CloudPlan plan, decimal storageGb);
}
