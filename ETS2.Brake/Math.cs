namespace ETS2.Brake
{
    static class Math
    {
        public static int ByPercentage(decimal percentage, decimal max) => (int) (percentage * max / 100);
    }
}