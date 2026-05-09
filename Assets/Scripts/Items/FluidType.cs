public enum FluidType
{
    Gasoline,
    MotorOil,
    Coolant
}

public static class FluidTypeExtensions
{
    public static string GetDisplayName(this FluidType type)
    {
        return type switch
        {
            FluidType.Gasoline => "Gasoline",
            FluidType.MotorOil => "Motor Oil",
            FluidType.Coolant => "Coolant",
            _ => type.ToString()
        };
    }
}
