namespace EasyConfig;

public interface IConfig
{
    public void Coerce();
    public bool IsValid(out string? error);
}