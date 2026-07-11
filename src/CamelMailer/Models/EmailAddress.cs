using System.Text.Json;
using System.Text.Json.Serialization;

namespace CamelMailer;

/// <summary>
/// An email address with an optional display name. Converts implicitly from
/// a plain string, so <c>To = ["ada@example.com"]</c> just works.
/// </summary>
[JsonConverter(typeof(EmailAddressJsonConverter))]
public sealed record EmailAddress
{
    /// <summary>The address itself, e.g. <c>ada@example.com</c>.</summary>
    public required string Email { get; init; }

    /// <summary>An optional display name, e.g. <c>Ada Lovelace</c>.</summary>
    public string? Name { get; init; }

    /// <summary>Wraps a bare address string in an <see cref="EmailAddress" />.</summary>
    public static implicit operator EmailAddress(string email) => new() { Email = email };
}

/// <summary>
/// Serialises an address as a bare string when it has no display name and as
/// <c>{"email":…,"name":…}</c> otherwise — matching the API's address schema.
/// </summary>
internal sealed class EmailAddressJsonConverter : JsonConverter<EmailAddress>
{
    public override EmailAddress? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return new EmailAddress { Email = reader.GetString()! };
        }

        string? email = null;
        string? name = null;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                continue;
            }

            var property = reader.GetString();
            reader.Read();
            switch (property)
            {
                case "email":
                    email = reader.GetString();
                    break;
                case "name":
                    name = reader.GetString();
                    break;
                default:
                    reader.Skip();
                    break;
            }
        }

        return new EmailAddress { Email = email ?? string.Empty, Name = name };
    }

    public override void Write(Utf8JsonWriter writer, EmailAddress value, JsonSerializerOptions options)
    {
        if (value.Name is null)
        {
            writer.WriteStringValue(value.Email);
            return;
        }

        writer.WriteStartObject();
        writer.WriteString("email", value.Email);
        writer.WriteString("name", value.Name);
        writer.WriteEndObject();
    }
}
