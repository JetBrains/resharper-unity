namespace ApiParser
{
    public class ProgressEventArgs
    {
        public ProgressEventArgs(int current, int total)
        {
            Current = current;
            Total = total;
        }

        public int Current { get; }

        public int Total { get; }

        public int Percent => Current * 100 / Total;
    }
}
