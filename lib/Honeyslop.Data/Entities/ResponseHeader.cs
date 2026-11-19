using System.ComponentModel.DataAnnotations;

namespace Honeyslop.Data.Entities;

public class ResponseHeader
{
    public const int NameMaxLength = 256;
    public const int ValueMaxLength = 1024;

    [MaxLength(NameMaxLength)]
    public string Name { get; set; } = null!;

    [MaxLength(ValueMaxLength)]
    public string Value { get; set; } = null!;
}