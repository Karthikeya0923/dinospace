namespace dinospace
{
    public class SpaceObject
    {
        public string Name { get; set; }
        public string Pronunciation { get; set; }
        public string Subtitle { get; set; }     
        public string TypeLabel { get; set; }      
        public string Category { get; set; }    
        public string ShortDescription { get; set; }

        public string Stat1Label { get; set; }
        public string Stat1Value { get; set; }
        public string Stat2Label { get; set; }
        public string Stat2Value { get; set; }
        public string Stat3Label { get; set; }
        public string Stat3Value { get; set; }
        public string Stat4Label { get; set; }
        public string Stat4Value { get; set; }

        public string ImageFile { get; set; }

        public string AboutText { get; set; }
        public string KeyFeaturesText { get; set; }
        public string OrbitMovementText { get; set; }
        public string SurfaceCompositionText { get; set; }
        public string HistoryText { get; set; }
        public string WhatsInsideText { get; set; }
        public string FunFactsText { get; set; }
    }
}