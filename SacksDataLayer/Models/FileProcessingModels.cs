using System;
using System.Collections.Generic;
using System.Linq;

namespace SacksDataLayer.Models
{
    /// <summary>
    /// Result model for file processing operations
    /// </summary>
    public class FileProcessingResult
    {
        /// <summary>
        /// Number of products created
        /// </summary>
        public int ProductsCreated { get; set; }

        /// <summary>
        /// Number of products updated
        /// </summary>
        public int ProductsUpdated { get; set; }

        /// <summary>
        /// Number of offer-product relationships created
        /// </summary>
        public int OfferProductsCreated { get; set; }

        /// <summary>
        /// Number of offer-product relationships updated
        /// </summary>
        public int OfferProductsUpdated { get; set; }

        /// <summary>
        /// Number of errors encountered
        /// </summary>
        public int Errors { get; set; }

        /// <summary>
        /// Processing time in milliseconds
        /// </summary>
        public long ProcessingTimeMs { get; set; }

        /// <summary>
        /// Collection of error messages
        /// </summary>
        public List<string> ErrorMessages { get; set; } = new();

        /// <summary>
        /// Total number of items processed (success + errors)
        /// </summary>
        public int TotalProcessed => ProductsCreated + ProductsUpdated + OfferProductsCreated + OfferProductsUpdated + Errors;

    }
}
