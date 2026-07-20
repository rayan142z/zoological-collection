using System;

namespace Zoolog.Models
{
    public class CollectionFavorite
    {
        public int UserId { get; set; }
        // Navigation Properties (falls du die Entitäten verknüpft hast)
        // public User User { get; set; } = null!;

        public int CollectionId { get; set; }
        // public Collection Collection { get; set; } = null!;
        

        public DateTime FavoritedAt { get; set; }
    }
}