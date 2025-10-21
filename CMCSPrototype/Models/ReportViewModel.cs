namespace CMCSPrototype.Models
{
    public class ReportViewModel
    {
        public int TotalClaims { get; set; }
        public int PendingClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalApprovedAmount { get; set; }
        public decimal TotalPendingAmount { get; set; }
        public decimal TotalRejectedAmount { get; set; }
        public List<MonthlyReport> MonthlyReports { get; set; } = new();
        public List<LecturerReport> LecturerReports { get; set; } = new();
    }

    public class MonthlyReport
    {
        public string Month { get; set; } = string.Empty;
        public int ClaimsCount { get; set; }
        public decimal TotalAmount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
    }

    public class LecturerReport
    {
        public string LecturerName { get; set; } = string.Empty;
        public int TotalClaims { get; set; }
        public int ApprovedClaims { get; set; }
        public int RejectedClaims { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
    }
}
