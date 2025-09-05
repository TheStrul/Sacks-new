-- Products with Multiple Suppliers - Conditional Display Format
-- Shows product properties only for the first offer of each product
-- Subsequent offers for the same product show only offer details
-- This query displays products that have offers from different suppliers
-- Uses empty strings instead of NULL for cleaner display

SELECT 
    CASE WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY s.Name, so.OfferName) = 1 
         THEN p.Name 
         ELSE '' 
    END AS ProductName,
    CASE WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY s.Name, so.OfferName) = 1 
         THEN p.EAN 
         ELSE '' 
    END AS EAN,
    CASE WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY s.Name, so.OfferName) = 1 
         THEN p.DynamicProperties 
         ELSE '' 
    END AS DynamicProperties,
    s.Name AS SupplierName,
    so.OfferName,
    op.Price,
    op.Quantity,
    so.Currency
FROM Products p
INNER JOIN OfferProducts op ON p.Id = op.ProductId
INNER JOIN SupplierOffers so ON op.OfferId = so.Id
INNER JOIN Suppliers s ON so.SupplierId = s.Id
WHERE p.Id IN (
    -- Subquery to find products with multiple suppliers
    SELECT p2.Id
    FROM Products p2
    INNER JOIN OfferProducts op2 ON p2.Id = op2.ProductId
    INNER JOIN SupplierOffers so2 ON op2.OfferId = so2.Id
    INNER JOIN Suppliers s2 ON so2.SupplierId = s2.Id
    GROUP BY p2.Id
    HAVING COUNT(DISTINCT s2.Id) > 2
)
ORDER BY p.EAN, s.Name, so.OfferName
