﻿using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WhatSearch.Models.JsonConverters
{
    public class BoolConverter : JsonConverter<bool>
    {
        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value ? 1 : 0);

        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.String => bool.TryParse(reader.GetString(), out var b) ? b : throw new JsonException(),
                JsonTokenType.Number => reader.TryGetInt64(out long l) ? Convert.ToBoolean(l) : reader.TryGetDouble(out double d) ? Convert.ToBoolean(d) : false,
                _ => throw new JsonException(),
            };
    }
}
 
