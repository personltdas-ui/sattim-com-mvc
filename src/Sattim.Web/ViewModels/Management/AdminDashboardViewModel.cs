using System;
using System.Collections.Generic;

namespace Sattim.Web.ViewModels.Management
{
    /// <summary>
    /// Admin panelindeki ana sayfayı (Dashboard) dolduran ana ViewModel.
    /// </summary>
    public class AdminDashboardViewModel
    {
        // 1. Ana Metrikler (KPIs)
        public int TotalUsers { get; set; }
        public decimal TotalSalesVolume { get; set; }
        public decimal TotalCommissionsEarned { get; set; }
        public int TotalActiveAuctions { get; set; }

        // 2. Moderasyon Kuyruğu
        public int PendingProducts { get; set; }
        public int PendingDisputes { get; set; }
        public int PendingReports { get; set; }
        public int PendingPayouts { get; set; }
        public int PendingComments { get; set; }

        // 3. Akış (Feeds)
        public List<RecentSaleViewModel> RecentSales { get; set; } = new List<RecentSaleViewModel>();
        public List<RecentUserViewModel> RecentUsers { get; set; } = new List<RecentUserViewModel>();
    }

    /// <summary>
    /// Dashboard'daki "Son Satışlar" akışındaki tek bir satır.
    /// </summary>
    public class RecentSaleViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Amount { get; set; }
        public DateTime SaleDate { get; set; }
    }

    /// <summary>
    /// Dashboard'daki "Son Kullanıcılar" akışındaki tek bir satır.
    /// </summary>
    public class RecentUserViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public DateTime RegisteredDate { get; set; }
    }
}