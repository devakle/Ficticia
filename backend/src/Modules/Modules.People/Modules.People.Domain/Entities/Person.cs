using Modules.People.Domain.Enums;

namespace Modules.People.Domain.Entities;

public sealed class Person
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string FullName { get; private set; } = default!;
    public string IdentificationNumber { get; private set; } = default!;
    public int Age { get; private set; }
    public Gender Gender { get; private set; }
    public bool IsActive { get; private set; } = true;

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; private set; } = DateTimeOffset.UtcNow;

    private Person() { }

    public Person(string fullName, string identificationNumber, int age, Gender gender)
    {
        SetFullName(fullName);
        SetIdentification(identificationNumber);
        SetAge(age);
        Gender = gender;
        IsActive = true;
    }

    public void Update(string fullName, string identificationNumber, int age, Gender gender)
    {
        SetFullName(fullName);
        SetIdentification(identificationNumber);
        SetAge(age);
        Gender = gender;
        Touch();
    }

    public void SetStatus(bool isActive)
    {
        IsActive = isActive;
        Touch();
    }

    private void SetFullName(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("FullName requerido");
        FullName = value.Trim();
    }

    private void SetIdentification(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException("Identification requerido");
        IdentificationNumber = value.Trim();
    }

    private void SetAge(int age)
    {
        if (age is < 0 or > 120) throw new ArgumentOutOfRangeException(nameof(age), "Edad inválida");
        Age = age;
    }

    private void Touch() => UpdatedAt = DateTimeOffset.UtcNow;
}
