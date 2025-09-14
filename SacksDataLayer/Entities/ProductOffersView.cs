namespace SacksDataLayer.Entities
{
    // Keyless entity matching the ProductOffersView database view
    public class ProductOffersView
    {
        public string? EAN { get; set; }
        public string? Name { get; set; }

        public string? Description { get; set; }
        public decimal? Price { get; set; }
        public string? Currency { get; set; }
        public int? Quantity { get; set; }

        public string? Category { get; set; }
        public string? Brand { get; set; }
        public string? Line { get; set; }
        public string? Gender { get; set; }
        public string? Concentration { get; set; }
        public string? Size { get; set; }
        public string? Type { get; set; }
        public string? Decoded { get; set; }
        public string? COO { get; set; }
        public string? Units { get; set; }

        public string? Ref { get; set; }

        public string? SupplierName { get; set; }
        public DateTime? DateOffer { get; set; }

        public int? OfferRank { get; set; }
        public int? TotalOffers { get; set; }
    }
}
