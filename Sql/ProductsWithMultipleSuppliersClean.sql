-- Products with Multiple Suppliers - Clean Format
-- Shows product properties once per product, then lists all offers for that product
-- This query displays products that have offers from different suppliers

WITH ProductsWithMultipleSuppliers AS (
    SELECT DISTINCT p.Id, p.Name, p.EAN, p.DynamicPropertiesJson
    FROM Products p
    INNER JOIN OfferProducts op ON p.Id = op.ProductId
    INNER JOIN SupplierOffers so ON op.OfferId = so.Id
    INNER JOIN Suppliers s ON so.SupplierId = s.Id
    GROUP BY p.Id, p.Name, p.EAN, p.DynamicPropertiesJson
    HAVING COUNT(DISTINCT s.Id) > 1
)
SELECT 
    -- Product section (shows once per product)
    CASE 
        WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY op.Price) = 1 
        THEN CAST(p.Id AS VARCHAR(10))
        ELSE ''
    END AS ProductId,
    CASE 
        WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY op.Price) = 1 
        THEN p.Name
        ELSE ''
    END AS ProductName,
    CASE 
        WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY op.Price) = 1 
        THEN p.EAN
        ELSE ''
    END AS EAN,
    CASE 
        WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY op.Price) = 1 
        THEN JSON_VALUE(p.DynamicPropertiesJson, '$.Brand')
        ELSE ''
    END AS Brand,
    CASE 
        WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY op.Price) = 1 
        THEN JSON_VALUE(p.DynamicPropertiesJson, '$.Gender')
        ELSE ''
    END AS Gender,
    CASE 
        WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY op.Price) = 1 
        THEN JSON_VALUE(p.DynamicPropertiesJson, '$.Size')
        ELSE ''
    END AS Size,
    CASE 
        WHEN ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY op.Price) = 1 
        THEN JSON_VALUE(p.DynamicPropertiesJson, '$.Concentration')
        ELSE ''
    END AS Concentration,
    -- Offer section (shows for each offer)
    s.Name AS SupplierName,
    so.OfferName,
    op.Price,
    so.Currency,
    op.Quantity,
    CASE 
        WHEN TRY_CAST(JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS INT) > 0 AND op.Price > 0
        THEN ROUND(op.Price / TRY_CAST(JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS INT), 4)
        ELSE NULL 
    END AS PricePerMl
FROM ProductsWithMultipleSuppliers pms
INNER JOIN Products p ON pms.Id = p.Id
INNER JOIN OfferProducts op ON p.Id = op.ProductId
INNER JOIN SupplierOffers so ON op.OfferId = so.Id
INNER JOIN Suppliers s ON so.SupplierId = s.Id
ORDER BY p.Name, op.Price;
