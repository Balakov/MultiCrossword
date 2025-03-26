namespace Crossword.Models
{
    public class Navigation
    {
        public string ClueId { get; set; }
        public string NextCellId { get; set; }
        public string PreviousCellId { get; set; }
        public bool HasSeparator { get; set; }
    }

    public class BoardCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Number { get; set; }
        public string Value { get; set; }
        public string Solution { get; set; }
        public Dictionary<string, Navigation> ClueIdByDirection { get; set; }
    }
}
