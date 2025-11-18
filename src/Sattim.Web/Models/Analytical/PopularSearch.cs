using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace Sattim.Web.Models.Analytical
{
    [Index(nameof(SearchTerm), IsUnique = true)]
    public class PopularSearch
    {
        [Key]
        public int Id { get; private set; }

        [Required]
        [StringLength(255)]
        public string SearchTerm { get; private set; }

        [Range(0, int.MaxValue)]
        public int SearchCount { get; private set; }

        public DateTime LastSearched { get; private set; }

        private PopularSearch() { }

        public PopularSearch(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                throw new ArgumentNullException(nameof(searchTerm), "Arama terimi boş olamaz.");

            SearchTerm = searchTerm;
            SearchCount = 1;
            LastSearched = DateTime.UtcNow;
        }

        public void IncrementSearch()
        {
            SearchCount++;
            LastSearched = DateTime.UtcNow;
        }
    }
}