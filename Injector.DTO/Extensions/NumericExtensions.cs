namespace Injector.DTO
{
    public static class NumericExtensions
    {
        public static bool IsWithinRange(this int x, int min, int max)
        {
            return x >= min && x <= max;
        }
    }
}