namespace ParsaProto
{
    /// <summary>
    /// Run-wide death tally. Both death presentations increment it; the main
    /// menu resets it (a fresh run); the outro shows it.
    /// </summary>
    public static class PP_DeathCounter
    {
        public static int Count { get; private set; }

        public static void Add() => Count++;
        public static void Reset() => Count = 0;
    }
}
