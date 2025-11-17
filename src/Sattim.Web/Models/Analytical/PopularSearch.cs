using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema; // [Index] için eklendi
using Microsoft.EntityFrameworkCore; // [Index] attribute'u için (EF Core 5+ ise)

namespace Sattim.Web.Models.Analytical
{
    /// <summary>
    /// Kullanıcılar tarafından yapılan bir arama terimini ve
    /// ne sıklıkta arandığını temsil eder.
    /// 'SearchTerm' alanı benzersiz (unique) olmalıdır.
    /// </summary>
    [Index(nameof(SearchTerm), IsUnique = true)] // EF Core 5+ için benzersizliği sağlar
    public class PopularSearch
    {
        #region Özellikler (Properties)

        [Key]
        public int Id { get; private set; }

        /// <summary>
        /// Aranan terim (Benzersiz olmalıdır).
        /// </summary>
        [Required]
        [StringLength(255)]
        public string SearchTerm { get; private set; }

        [Range(0, int.MaxValue)]
        public int SearchCount { get; private set; }

        public DateTime LastSearched { get; private set; }

        #endregion

        #region Yapıcı Metotlar ve Davranışlar (Constructors & Methods)

        /// <summary>
        /// Entity Framework Core için gerekli özel yapıcı metot.
        /// </summary>
        private PopularSearch() { }

        /// <summary>
        /// Yeni bir arama terimi (daha önce hiç aranmamış) oluşturur
        /// ve sayacı 1 olarak başlatır.
        /// </summary>
        public PopularSearch(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentNullException(nameof(searchTerm), "Arama terimi boş olamaz.");

            SearchTerm = searchTerm;
            SearchCount = 1; // İlk arama
            LastSearched = DateTime.UtcNow;
        }

        /// <summary>
        /// Mevcut bir arama teriminin sayacını artırır ve tarihini günceller.
        /// </summary>
        public void IncrementSearch()
        {
            SearchCount++;
            LastSearched = DateTime.UtcNow;
        }

        #endregion
    }
}