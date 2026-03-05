namespace Yumney.Shared.Guards;

public class GuardClause<T>
{
    private readonly T _value;

    public GuardClause(T value, string paramName)
    {
        _value = value;
        ParamName = paramName;
    }

    public string ParamName { get; }

    public T Value => _value;
}
