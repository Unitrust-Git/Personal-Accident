using System.Text.Json;
using System.Text.Json.Serialization;

public class PaymentMetadata
{
    [JsonPropertyName("paymentType")]
    public string PaymentType { get; set; } = default!;

    [JsonPropertyName("invoiceNumber")]
    public string InvoiceNumber { get; set; } = default!;

    [JsonPropertyName("totalAmount")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("clientCode")]
    public string ClientCode { get; set; } = default!;

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("phone")]
    public string? Phone { get; set; }

    [JsonPropertyName("units")]
    [JsonConverter(typeof(StringToIntConverter))]
    public int Units { get; set; }

    [JsonPropertyName("unitPrice")]
    [JsonConverter(typeof(StringToDecimalConverter))]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("nextOfKinName")]
    public string? NextOfKinName { get; set; }

    [JsonPropertyName("nextOfKinPhone")]
    public string? NextOfKinPhone { get; set; }

    [JsonPropertyName("isReferral")]
    [JsonConverter(typeof(StringToBoolConverter))]
    public bool IsReferral { get; set; }

    [JsonPropertyName("cloCode")]
    public string? CloCode { get; set; }

    [JsonPropertyName("cloName")]
    public string? CloName { get; set; }

    // Corporate only
    [JsonPropertyName("companyName")]
    public string? CompanyName { get; set; }

    [JsonPropertyName("companyAddress")]
    public string? CompanyAddress { get; set; }

    [JsonPropertyName("rcNumber")]
    public string? RCNumber { get; set; }

    [JsonPropertyName("beneficiaries")]
    public List<BeneficiaryDTO>? Beneficiaries { get; set; }
}

public class StringToDecimalConverter : JsonConverter<decimal>
{
    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && decimal.TryParse(reader.GetString(), out var val))
            return val;
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetDecimal();
        return 0; 
    }

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

}

public class StringToIntConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && int.TryParse(reader.GetString(), out var val))
            return val;
        if (reader.TokenType == JsonTokenType.Number)
            return reader.GetInt32();
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }

}

public class StringToBoolConverter : JsonConverter<bool>
{
    public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.True) return true;
        if (reader.TokenType == JsonTokenType.False) return false;

        if (reader.TokenType == JsonTokenType.String)
        {
            var s = reader.GetString()?.ToLower();

            if (s == "true") return true;
            if (s == "false") return false;
            if (s == "1") return true;
            if (s == "0") return false;
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            if (reader.TryGetInt32(out var n))
                return n != 0;
        }

        return false;
    }

    public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options)
    {
        writer.WriteBooleanValue(value);
    }
}


public class BeneficiaryDTO
{
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = default!;

    [JsonPropertyName("phone")]
    public string Phone { get; set; } = default!;

    [JsonPropertyName("units")]
    public int Units { get; set; }

}


public class PaymentAddress
{
    [JsonPropertyName("street")]
    public string? Street { get; set; }

    [JsonPropertyName("town")]
    public string? Town { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("state")]
    public string? State { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}
