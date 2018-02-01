namespace IFramework.DependencyInjection
{
    public class Parameter
    {
        public Parameter(string parameterName, object parameterValue)
        {
            Name = parameterName;
            Value = parameterValue;
        }

        public string Name { get; }
        public object Value { get; }
    }
}