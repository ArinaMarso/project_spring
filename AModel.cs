using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

namespace StoreG5G11.models.ef;

public abstract class AModel : INotifyPropertyChanged, IValidatableObject
{
    [Key] public int Id { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public abstract IEnumerable<ValidationResult> Validate(ValidationContext validationContext);


    protected bool SetField<T>(ref T field, T value, Action<string,T>[]? validators = null,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;

        if (validators != null)
        {
            foreach (var validator in validators)
            {
                validator(propertyName!, value);
            }
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public abstract AModel GetCopy();
    public abstract void CopyFrom(AModel other);
    public abstract bool Equals(AModel other);
}
