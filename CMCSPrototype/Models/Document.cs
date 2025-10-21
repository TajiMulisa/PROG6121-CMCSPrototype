namespace CMCSPrototype.Models
{
    public class Document
    {
        public int Id { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public int ClaimId { get; set; }
        public Claim Claim { get; set; }
    }
}
