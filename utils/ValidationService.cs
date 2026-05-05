namespace StoreG5G11.utils;

public static class ValidationService
{
    public static void ValidString(string propertyName, string value, bool allowNullOrEmpty = false)
    {
        if (!allowNullOrEmpty && string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{propertyName} не может быть пустым", propertyName);
    }

    public static void ValidStringLength(string propertyName, string value, int minLength = 1, int maxLength = 100, bool allowNullOrEmpty = false)
    {
        if (allowNullOrEmpty && string.IsNullOrWhiteSpace(value))
            return;

        ValidString(propertyName, value, allowNullOrEmpty);

        if (value.Length < minLength || value.Length > maxLength)
            throw new ArgumentOutOfRangeException(propertyName,
                $"{propertyName} должен содержать от {minLength} до {maxLength} символов. Текущая длина: {value.Length}");
    }

    public static void ValidPositiveDecimal(string propertyName, decimal value, bool allowZero = false)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(propertyName,
                $"{propertyName} должен быть положительным числом");

        if (!allowZero && value == 0)
            throw new ArgumentOutOfRangeException(propertyName,
                $"{propertyName} не может быть равен нулю");
    }

    public static void ValidPositiveInt(string propertyName, int value, bool allowZero = false)
    {
        if (value < 0)
            throw new ArgumentOutOfRangeException(propertyName,
                $"{propertyName} должен быть положительным числом");

        if (!allowZero && value == 0)
            throw new ArgumentOutOfRangeException(propertyName,
                $"{propertyName} не может быть равен нулю");
    }

    public static void ValidPrice(string propertyName, decimal value)
    {
        ValidPositiveDecimal(propertyName, value);

        if (value > 1000000) 
            throw new ArgumentOutOfRangeException(propertyName,
                $"{propertyName} не может превышать 1 000 000");
    }

    public static void ValidCount(string propertyName, int value)
    {
        ValidPositiveInt(propertyName, value);

        if (value > 1000) 
            throw new ArgumentOutOfRangeException(propertyName,
                $"{propertyName} не может превышать 1000");
    }
}