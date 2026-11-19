namespace Honeyslop.Common.Serializers;

public interface IJsonSerializer<T>
    where T : class
{
    string Serialize(T value);
    T? Deserialize(string json);
}