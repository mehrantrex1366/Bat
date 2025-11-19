namespace Bat.Test;

public class StaticValuesBuilder
{
    private readonly Dictionary<System.Reflection.PropertyInfo, object> _originalValues = [];


    public StaticValuesBuilder Set<TProperty>(Expression<Func<TProperty>> propertyExpression, TProperty newValue)
    {
        var memberExpression = (MemberExpression)propertyExpression.Body;
        var propInfo = (System.Reflection.PropertyInfo)memberExpression.Member;

        if (!_originalValues.ContainsKey(propInfo))
            _originalValues[propInfo] = propInfo.GetValue(null);

        propInfo.SetValue(null, newValue);
        return this;
    }

    public void Restore()
    {
        foreach (var values in _originalValues)
            values.Key.SetValue(null, values.Value);

        _originalValues.Clear();
    }
}