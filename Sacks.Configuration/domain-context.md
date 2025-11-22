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


