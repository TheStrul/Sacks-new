# Beauty Products Management System - Domain Context

## Overview
Database system for beauty/cosmetics products, supplier price lists, and rich product metadata.

## Key Entities

### SUPPLIERS
Companies that provide beauty products (e.g., L'Oréal, Estée Lauder, Procter & Gamble).
- Each supplier maintains multiple price lists over time
- Suppliers are identified by ID and Name

### OFFERS
Price lists or catalogs from suppliers containing multiple products with specific prices.
- Each offer is a snapshot in time from one supplier
- Contains multiple products with prices and availability
- Tracked by creation date (CreatedAt)
- Prices can be in different currencies (EUR, USD, etc.)

### PRODUCTS
Individual beauty items (perfumes, cosmetics, skincare, haircare, makeup, etc.).
- Each product has a unique identifier (ID, EAN barcode)
- Products appear in multiple offers with varying prices
- Rich metadata stored as **Dynamic Properties**

## Relationships

```
SUPPLIER (1) ──→ (N) OFFERS       [One supplier has many historical price lists]
OFFER (1) ──→ (N) PRODUCTS        [One offer contains many products with prices]
PRODUCT (1) ──→ (N) OFFERS        [One product appears in multiple offers]
```

## Dynamic Properties (CRITICAL!)

Products have extensive metadata stored as flexible key-value pairs:

### Common Properties
- **Brand**: L'Oréal, Chanel, Dior, Estée Lauder, Clinique, MAC, etc.
- **Type/Category**: Perfume, Lipstick, Foundation, Shampoo, Serum, Moisturizer, Mascara
- **Gender**: Women, Men, Unisex
- **Size/Volume**: 50ml, 100ml, 200ml, 30g, etc.
- **Concentration**: EDT (Eau de Toilette), EDP (Eau de Parfum), Parfum
- **Fragrance Notes**: Floral, Woody, Citrus, Oriental, Fresh
- **Color/Shade**: Red, Nude, Natural, Beige, etc.
- **Skin Type**: Dry, Oily, Combination, Sensitive, All
- **Hair Type**: Damaged, Colored, Fine, Thick
- **SPF**: Sun protection factor (SPF 15, SPF 30, SPF 50)
- **Vegan**: true/false
- **Cruelty-Free**: true/false
- **Ingredients**: Key ingredients or certifications

### Search Patterns
Users search by these properties:
- "women perfumes" → Gender filter
- "50ml bottles" → Volume filter
- "L'Oréal hair products" → Brand + Type filters
- "SPF 30 moisturizer" → SPF + Type filters

## Common Query Patterns

### Price Comparisons
Find same product across different suppliers/offers to compare prices and availability.

### Attribute-Based Search
- Products by brand: "Show me all Chanel products"
- Products by type: "Find all lipsticks"
- Products by attributes: "Vegan perfumes under €50"
- Combined filters: "Women's hair products from L'Oréal"

### Supplier Analytics
- Total offers from a supplier
- Product count per supplier
- Price ranges by supplier
- Recent activity (offers in last 30 days)

### Inventory Insights
- Recently added offers or products
- Products by price range
- Currency-specific searches
- Expensive/premium products (high-end items)

## Business Context

This is a **price intelligence** and **product catalog** system for the beauty industry. The data helps:
- Track price changes over time
- Compare supplier offerings
- Analyze market trends
- Discover new products
- Monitor competitor pricing

## Database Views for Complex Queries

### ProductOffersView
Pre-joined view combining Products, Offers, and Suppliers with all dynamic properties extracted.
**When to use**: Price comparisons, analyzing all offers for products, seeing complete offer history.
**Key columns**: EAN, Name, Brand, Type, Gender, Price, S_Name (supplier), Date, OfferRank, TotalOffers
**Example queries**: 
- "Show all offers for Chanel perfumes"
- "Compare prices for EAN 123456789"
- "Find cheapest and most expensive offers for women's lipsticks"

### ProductOffersViewCollapse
Collapsed version showing product details ONLY on cheapest offer row (OfferRank=1), other rows show just price/supplier.
**When to use**: Cleaner output for product lists, focusing on best prices with minimal repetition.
**Key columns**: EANKey (always present), Name/Brand/Type (only on OfferRank=1), Price, S_Name, OfferRank
**Example queries**:
- "Show me products by brand with best prices"
- "List all vegan products with cheapest offers"
- "What's the best price for each L'Oréal shampoo?"
**Note**: Only includes products with multiple offers (TotalOffers > 1)

### Choosing Between Views
- Use **ProductOffersView** when you need ALL offer details or price comparisons
- Use **ProductOffersViewCollapse** when you want product lists with best prices and minimal duplication
- Both support WHERE clauses with dynamic properties (Brand, Gender, Type, Price, etc.)
- Both support ORDER BY for sorting (Price, Date, Brand, etc.)


