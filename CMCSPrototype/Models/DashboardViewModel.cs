namespace CMCSPrototype.Models
{
    public class DashboardViewModel
    {
        public int PendingClaims { get; set; }
        public decimal TotalClaimed { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public int TotalClaims { get; set; }
        public decimal AverageClaimAmount { get; set; }
        public List<Claim> RecentClaims { get; set; } = new();
    }
}

    


    

