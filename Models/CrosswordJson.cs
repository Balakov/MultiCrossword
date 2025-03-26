namespace Crossword.Models
{
    public class CrosswordJson
    {
        public CrosswordData data { get; set; }
        public bool canRenderAds { get; set; }
    }

    public class CrosswordData
    {
        public string id { get; set; } // "crosswords/cryptic/29648"
        public int number { get; set; } // 29648
        public string name { get; set; } // "Cryptic crossword No 29,648"

        public CrosswordCreator creator { get; set; }

        public long date { get; set; } // 1742515200000
        public long webPublicationDate { get; set; } // 1742515244000

        public List<CrosswordEntry> entries { get; set; }

        public bool solutionAvailable { get; set; } // true
        public long dateSolutionAvailable { get; set; } // 1742515200000

        public CrosswordDimensions dimensions { get; set; }
        public string crosswordType { get; set; } // "cryptic"
        public string pdf { get; set; } // "https://crosswords-static.guim.co.uk/gdn.cryptic.20250321.pdf"
    }

    public class CrosswordDimensions
    {
        public int cols { get; set; }   // 15
        public int rows { get; set; }   // 15
    }

    public class CrosswordCreator
    {
        public string name { get; set; } // "Kite"
        public string webUrl { get; set; } // "https://www.theguardian.com/profile/kite"
    }

    public class CrosswordEntryPosition
    {
        public int x { get; set; }
        public int y { get; set; }
    }

    public class CrosswordEntry
    {
        public string id { get; set; } // "9-across"
        public int number { get; set; } // 9
        public string humanNumber { get; set; } // "9"
        public string clue { get; set; } // "At age close to retirement, Ivy fit to have sex toys (5-4)"
        public string direction { get; set; } // "across"
        public int length { get; set; } // 9

        public string[] group { get; set; } // [ "9-across" ]
        public CrosswordEntryPosition position { get; set; }

        public Dictionary<string, int[]> separatorLocations { get; set; } // { "-": [5] }
        public string solution { get; set; } // SIXTYFIVE
    }
}
