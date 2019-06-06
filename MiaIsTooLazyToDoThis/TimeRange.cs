namespace MiaIsTooLazyToDoThis
{
    struct TimeRange
    {
        public int Start { get; set; }
        public int End { get; set; }

        public override string ToString() => $"{Start:0.##}:{End:0.##}";
    }
}
