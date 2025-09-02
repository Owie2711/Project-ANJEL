namespace ScrcpyController.Core
{
    /// <summary>
    /// Interface for configuration validators
    /// </summary>
    public interface IConfigValidator
    {
        (bool IsValid, string ErrorMessage) Validate(object? value);
    }

    /// <summary>
    /// Validates numeric values within a range
    /// </summary>
    public class RangeValidator<T> : IConfigValidator where T : IComparable<T>
    {
        private readonly T _minValue;
        private readonly T _maxValue;

        public RangeValidator(T minValue, T maxValue)
        {
            _minValue = minValue;
            _maxValue = maxValue;
        }

        public (bool IsValid, string ErrorMessage) Validate(object? value)
        {
            try
            {
                if (value == null)
                    return (false, "Value cannot be null");

                T convertedValue;

                if (value is string stringValue)
                {
                    if (string.IsNullOrWhiteSpace(stringValue))
                        return (false, "Value cannot be empty");

                    convertedValue = (T)Convert.ChangeType(stringValue.Trim(), typeof(T));
                }
                else if (value is T directValue)
                {
                    convertedValue = directValue;
                }
                else
                {
                    convertedValue = (T)Convert.ChangeType(value, typeof(T));
                }

                if (convertedValue.CompareTo(_minValue) < 0)
                    return (false, $"Value must be at least {_minValue}");

                if (convertedValue.CompareTo(_maxValue) > 0)
                    return (false, $"Value must be at most {_maxValue}");

                return (true, "Valid");
            }
            catch (Exception)
            {
                return (false, $"Value must be a valid {typeof(T).Name}");
            }
        }
    }

    /// <summary>
    /// Validates values against a list of allowed choices
    /// </summary>
    public class ChoiceValidator<T> : IConfigValidator
    {
        private readonly HashSet<T> _choices;

        public ChoiceValidator(IEnumerable<T> choices)
        {
            _choices = new HashSet<T>(choices);
        }

        public (bool IsValid, string ErrorMessage) Validate(object? value)
        {
            if (value is T typedValue && _choices.Contains(typedValue))
                return (true, "Valid");

            var choicesList = string.Join(", ", _choices);
            return (false, $"Value must be one of: {choicesList}");
        }
    }

    /// <summary>
    /// Special validator for bitrate values
    /// </summary>
    public class BitrateValidator : IConfigValidator
    {
        public (bool IsValid, string ErrorMessage) Validate(object? value)
        {
            try
            {
                if (value == null)
                    return (false, "Bitrate cannot be null");

                string stringValue;
                if (value is string str)
                {
                    stringValue = str;
                }
                else
                {
                    stringValue = value.ToString() ?? "";
                }

                if (string.IsNullOrWhiteSpace(stringValue))
                    return (false, "Bitrate cannot be empty");

                // Remove 'M' suffix if present
                string cleanValue = stringValue.Trim().TrimEnd('M', 'm');
                
                if (!double.TryParse(cleanValue, out double bitrateValue))
                    return (false, "Bitrate must be a valid number");

                if (bitrateValue <= 0)
                    return (false, "Bitrate must be greater than 0");

                if (bitrateValue > 1000)
                    return (false, "Bitrate too high (max 1000 Mbps)");

                return (true, "Valid");
            }
            catch (Exception)
            {
                return (false, "Bitrate must be a valid number");
            }
        }
    }
}