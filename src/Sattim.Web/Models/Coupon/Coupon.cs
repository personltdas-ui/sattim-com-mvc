using Sattim.Web.Models.Coupon;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Column] için eklendi

namespace Sattim.Web.Models.Coupon
{
    /// <summary>
    /// Bir indirim kuponunu ve kurallarını (tutar, tarih, limitler) temsil eder.
    /// Kurallar, constructor ve Update metodu içinde zorunlu kılınır.
    /// </summary>
    public class Coupon
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(50)]
        public string Code { get; private set; } // Benzersizliği DbContext'te sağlanmalı

        [Required]
        [StringLength(500)]
        public string Description { get; private set; }

        [Required]
        public CouponType Type { get; private set; }

        [Range(0, 100)]
        [Column(TypeName = "decimal(5,2)")] // Oran için net tip
        public decimal? DiscountPercentage { get; private set; }

        [Range(typeof(decimal), "0.00", "79228162514264337593543950335")]
        [Column(TypeName = "decimal(18,2)")] // Para birimi için net tip
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

        #region Navigasyon Özellikleri (Navigation Properties)

        /// <summary>
        /// Navigasyon: Bu kuponun kullanıldığı yerler (1'e Çok).
        /// DÜZELTME: 'IEnumerable<T>' -> 'ICollection<T>' olarak değiştirildi (EF Core için zorunlu).
        /// DÜZELTME: 'virtual' eklendi (Tembel Yükleme için).
        /// </summary>
        public virtual ICollection<CouponUsage> Usages { get; private set; } = new List<CouponUsage>();

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private Coupon() { }

        /// <summary>
        /// Yeni bir 'Coupon' nesnesi oluşturur ve tüm iş kurallarını zorunlu kılar.
        /// </summary>
        public Coupon(string code, CouponType type, string description, DateTime startDate, DateTime endDate,
                      decimal? discountPercentage, decimal? discountAmount, decimal? minimumPurchase, int? maxUses)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new ArgumentException("Kupon kodu boş olamaz.", nameof(code));

            // Kuralları doğrulamak ve atamak için merkezi metodu kullan (DRY Prensibi)
            SetValues(description, type, startDate, endDate, discountPercentage, discountAmount, minimumPurchase, maxUses);

            Code = code;
            UsedCount = 0;
            IsActive = true; // Varsayılan olarak aktif
        }

        /// <summary>
        /// Mevcut bir kuponun ayarlarını günceller ve tüm iş kurallarını yeniden doğrular.
        /// </summary>
        public void Update(string description, CouponType type, DateTime startDate, DateTime endDate,
                           decimal? discountPercentage, decimal? discountAmount, decimal? minimumPurchase, int? maxUses)
        {
            SetValues(description, type, startDate, endDate, discountPercentage, discountAmount, minimumPurchase, maxUses);
        }

        /// <summary>
        /// Kupon değerlerini atayan ve tüm iş kurallarını zorunlu kılan merkezi metot.
        /// </summary>
        private void SetValues(string description, CouponType type, DateTime startDate, DateTime endDate,
                               decimal? discountPercentage, decimal? discountAmount, decimal? minimumPurchase, int? maxUses)
        {
            if (string.IsNullOrWhiteSpace(description))
                throw new ArgumentException("Açıklama boş olamaz.", nameof(description));
            if (endDate <= startDate)
                throw new ArgumentException("Bitiş tarihi, başlangıç tarihinden sonra olmalıdır.");

            // DÜZELTME: IValidatableObject'taki mantık buraya taşındı ve 'zorunlu' kılındı.
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


        /// <summary>
        /// Bir kuponun belirli bir satın alma tutarı için geçerli olup olmadığını kontrol eder. (Query)
        /// </summary>
        /// <param name="purchaseAmount">Sepet tutarı.</param>
        /// <param name="reason">Geçersizse nedenini döndürür.</param>
        /// <returns>Geçerliyse true.</returns>
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
            // DÜZELTME: Eksik olan MinimumPurchase kontrolü eklendi.
            if (MinimumPurchase.HasValue && purchaseAmount < MinimumPurchase.Value)
            {
                reason = $"Bu kupon için minimum {MinimumPurchase.Value:C} tutarında alışveriş gerekmektedir.";
                return false;
            }

            reason = "Kupon geçerli.";
            return true;
        }

        /// <summary>
        /// Kuponun kullanım sayacını artırır. (Command)
        /// Çağırmadan önce 'CanBeUsed' ile kontrol edilmelidir.
        /// </summary>
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