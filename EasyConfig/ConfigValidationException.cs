using System;

namespace EasyConfig;

internal class ConfigValidationException(string path, string? error = null) : Exception($"Configuration file \"{path}\" is invalid: {error ?? "Unknown reason"}") { }
