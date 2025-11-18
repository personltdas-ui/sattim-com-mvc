using Sattim.Web.Models.Coupon;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Sattim.Web.Models.Coupon
{
    public class Coupon
    {
        #region Özellikler

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(50)]
        public string Code { get; private set; }

        [Required]
        [StringLength(500)]
        public string Description { get; private set; }

        [Required]
        public CouponType Type { get; private set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")]
        public decimal? DiscountPercentage { get; private set; }

        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountAmount { get; private set; }

        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumPurchase { get; private set; }

        public int? MaxUses { get; private set; }
        public int UsedCount { get; private set; }
        public DateTime StartDate { get; private set; }
        public DateTime EndDate { get; private set; }
        public bool IsActive { get; private set; }

        #endregion

        #region Navigasyon Özellikleri

        public virtual ICollection<CouponUsage> Usages { get; private set; } = new List<CouponUsage>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar

        private Coupon() { }

        public Coupon(string code, CouponType type, string description, DateTime startDate, DateTime endDate,
                      decimal? discountPercentage, decimal? discountAmount, decimal? minimumPurchase, int? maxUses)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Kupon kodu boş olamaz.", nameof(code));

            SetValues(description, type, startDate, endDate, discountPercentage, discountAmount, minimumPurchase, maxUses);

            Code = code;
            UsedCount = 0;
            IsActive = true;
        }

        public void Update(string description, CouponType type, DateTime startDate, DateTime endDate,
                           decimal? discountPercentage, decimal? discountAmount, decimal? minimumPurchase, int? maxUses)
        {
            SetValues(description, type, startDate, endDate, discountPercentage, discountAmount, minimumPurchase, maxUses);
        }

        private void SetValues(string description, CouponType type, DateTime startDate, DateTime endDate,
                               decimal? discountPercentage, decimal? discountAmount, decimal? minimumPurchase, int? maxUses)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Açıklama boş olamaz.", nameof(description));
            if (endDate <= startDate)
                throw new ArgumentException("Bitiş tarihi, başlangıç tarihinden sonra olmalıdır.");

            switch (type)
            {
                case CouponType.Percentage:
                    if (!discountPercentage.HasValue || discountPercentage.Value <= 0)
                        throw new ArgumentException("Yüzdelik kuponlar için 'DiscountPercentage' pozitif bir değer olmalıdır.");
                    if (discountAmount.HasValue)
                        throw new ArgumentException("Yüzdelik kuponlarda 'DiscountAmount' olmamalıdır (null olmalıdır).");
                    break;
                case CouponType.FixedAmount:
                    if (!discountAmount.HasValue || discountAmount.Value <= 0)
                        throw new ArgumentException("Sabit fiyatlı kuponlar için 'DiscountAmount' pozitif bir değer olmalıdır.");
                    if (discountPercentage.HasValue)
                        throw new ArgumentException("Sabit fiyatlı kuponlarda 'DiscountPercentage' olmamalıdır (null olmalıdır).");
                    break;
                case CouponType.FreeShipping:
                    if (discountAmount.HasValue || discountPercentage.HasValue)
                        throw new ArgumentException("'Ücretsiz Kargo' kuponlarında indirim alanı (Amount veya Percentage) olmamalıdır.");
                    break;
            }

            Description = description;
            Type = type;
            StartDate = startDate;
            EndDate = endDate;
            DiscountPercentage = discountPercentage;
            DiscountAmount = discountAmount;
            MinimumPurchase = (minimumPurchase.HasValue && minimumPurchase.Value <= 0) ? null : minimumPurchase;
            MaxUses = (maxUses.HasValue && maxUses.Value <= 0) ? null : maxUses;
        }


        public bool CanBeUsed(decimal purchaseAmount, out string reason)
        {
            if (!IsActive)
            {
                reason = "Kupon aktif değil.";
                return false;
            }
            if (DateTime.UtcNow < StartDate)
            {
                reason = "Kupon henüz başlamadı.";
                return false;
            }
            if (DateTime.UtcNow > EndDate)
            {
                reason = "Kuponun süresi doldu.";
                return false;
            }
            if (MaxUses.HasValue && UsedCount >= MaxUses.Value)
            {
                reason = "Kupon kullanım limitine ulaştı.";
                return false;
            }
            if (MinimumPurchase.HasValue && purchaseAmount < MinimumPurchase.Value)
            {
                reason = $"Bu kupon için minimum {MinimumPurchase.Value:C} tutarında alışveriş gerekmektedir.";
                return false;
            }

            reason = "Kupon geçerli.";
            return true;
        }

        public void RecordUsage()
        {
            if (MaxUses.HasValue && UsedCount >= MaxUses.Value)
                throw new InvalidOperationException("Kupon kullanım limitine ulaştı.");

            UsedCount++;
        }

        public void Deactivate() => IsActive = false;
        public void Activate() => IsActive = true;

        #endregion
    }

    public enum CouponType
    {
        Percentage,
        FixedAmount,
        FreeShipping
    }
}