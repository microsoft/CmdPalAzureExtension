// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace AzureExtension.Controls;

public class SignOutSetting : Setting<string>
{
    public bool Multiline { get; set; }

    public string Placeholder { get; set; } = string.Empty;

    private JsonSerializerOptions JsonSerializationContext => new()
    {
        WriteIndented = true,
    };

    public SignOutSetting()
        : base()
    {
        Value = string.Empty;
    }

    public SignOutSetting(string key, string defaultValue)
        : base(key, defaultValue)
    {
    }

    public SignOutSetting(string key, string label, string description, string defaultValue)
        : base(key, label, description, defaultValue)
    {
    }

    public override Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            { "type", "Input.Text" },
            { "title", Label },
            { "id", Key },
            { "label", Description },
            { "value", Value ?? string.Empty },
            { "isRequired", IsRequired },
            { "errorMessage", ErrorMessage },
            { "isMultiline", Multiline },
            { "placeholder", Placeholder },
        };
    }

    public static SignOutSetting LoadFromJson(JsonObject jsonObject) => new() { Value = jsonObject["value"]?.GetValue<string>() ?? string.Empty };

    public override void Update(JsonObject payload)
    {
        // If the key doesn't exist in the payload, don't do anything
        if (payload[Key] != null)
        {
            Value = payload[Key]?.GetValue<string>();
        }
    }

    public override string ToState() => $"\"{Key}\": {JsonSerializer.Serialize(Value, JsonSerializationContext)}";
}
