using System.ComponentModel.DataAnnotations;

namespace QuickShop.Api.Common;

/// <summary>Validates that a bool value is exactly true (e.g. terms acceptance).</summary>
[AttributeUsage(AttributeTargets.Property)]
public sealed class MustBeTrueAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is true;
}
