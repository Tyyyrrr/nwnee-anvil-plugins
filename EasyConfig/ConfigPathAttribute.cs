using System;

namespace EasyConfig;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
public sealed class ConfigFileAttribute(string FileName) : Attribute
{
    internal readonly string FileName = FileName;
}