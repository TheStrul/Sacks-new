-- Query to show all products that have offers from multiple suppliers
-- This query provides comprehensive information about products available from different suppliers
-- with pricing comparison and supplier details

-- Option 0A: Products with their offers grouped (PRODUCT PROPERTIES ONCE)
-- This shows product details once, then all offers for that product
SELECT 
    p.Id AS ProductId,
    p.Name AS ProductName,
    p.EAN,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Brand') AS Brand,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Gender') AS Gender,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS Size,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Concentration') AS Concentration,
    '--- OFFERS ---' AS Separator,
    s.Name AS SupplierName,
    so.OfferName,
    op.Price,
    so.Currency,
    op.Quantity,
    -- Calculate price per ml if size is available
    CASE 
        WHEN TRY_CAST(JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS INT) > 0 AND op.Price > 0
        THEN ROUND(op.Price / TRY_CAST(JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS INT), 4)
        ELSE NULL 
    END AS PricePerMl,
    -- Show offer rank within the product
    ROW_NUMBER() OVER (PARTITION BY p.Id ORDER BY op.Price ASC) AS OfferRank
FROM Products p
INNER JOIN OfferProducts op ON p.Id = op.ProductId
INNER JOIN SupplierOffers so ON op.OfferId = so.Id
INNER JOIN Suppliers s ON so.SupplierId = s.Id
WHERE p.Id IN (
    -- Subquery to find products with multiple offers from different suppliers
    SELECT p2.Id
    FROM Products p2
    INNER JOIN OfferProducts op2 ON p2.Id = op2.ProductId
    INNER JOIN SupplierOffers so2 ON op2.OfferId = so2.Id
    INNER JOIN Suppliers s2 ON so2.SupplierId = s2.Id
    GROUP BY p2.Id
    HAVING COUNT(DISTINCT s2.Id) > 1
)
ORDER BY p.Name, op.Price;

-- Option 0B: Alternative format - Product summary first, then offers
-- Shows a cleaner separation between product info and offers
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

-- Option 0: All individual offers for products with multiple offers (SIMPLE LIST)
SELECT 
    p.Id AS ProductId,
    p.Name AS ProductName,
    p.EAN,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Brand') AS Brand,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Gender') AS Gender,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS Size,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Concentration') AS Concentration,
    s.Id AS SupplierId,
    s.Name AS SupplierName,
    so.Id AS OfferId,
    so.OfferName,
    op.Price,
    so.Currency,
    op.Quantity,
    -- Calculate price per ml if size is available
    CASE 
        WHEN TRY_CAST(JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS INT) > 0 AND op.Price > 0
        THEN ROUND(op.Price / TRY_CAST(JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS INT), 4)
        ELSE NULL 
    END AS PricePerMl
FROM Products p
INNER JOIN OfferProducts op ON p.Id = op.ProductId
INNER JOIN SupplierOffers so ON op.OfferId = so.Id
INNER JOIN Suppliers s ON so.SupplierId = s.Id
WHERE p.Id IN (
    -- Subquery to find products with multiple offers
    SELECT p2.Id
    FROM Products p2
    INNER JOIN OfferProducts op2 ON p2.Id = op2.ProductId
    INNER JOIN SupplierOffers so2 ON op2.OfferId = so2.Id
    INNER JOIN Suppliers s2 ON so2.SupplierId = s2.Id
    GROUP BY p2.Id
    HAVING COUNT(DISTINCT s2.Id) > 1
)
ORDER BY p.Name, s.Name, op.Price;

-- Option 1: Simple view - Products with multiple suppliers count
SELECT 
    p.Id AS ProductId,
    p.Name AS ProductName,
    p.EAN,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Brand') AS Brand,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Gender') AS Gender,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS Size,
    JSON_VALUE(p.DynamicPropertiesJson, '$.Concentration') AS Concentration,
    COUNT(DISTINCT s.Id) AS SupplierCount,
    COUNT(op.Id) AS TotalOffers,
    MIN(op.Price) AS MinPrice,
    MAX(op.Price) AS MaxPrice,
    MAX(op.Price) - MIN(op.Price) AS PriceDifference,
    CASE 
        WHEN MIN(op.Price) > 0 
        THEN ROUND(((MAX(op.Price) - MIN(op.Price)) / MIN(op.Price)) * 100, 2)
        ELSE 0 
    END AS PriceDifferencePercentage,
    STRING_AGG(s.Name, ', ') AS SupplierNames
FROM Products p
INNER JOIN OfferProducts op ON p.Id = op.ProductId
INNER JOIN SupplierOffers so ON op.OfferId = so.Id
INNER JOIN Suppliers s ON so.SupplierId = s.Id
WHERE op.Price IS NOT NULL
GROUP BY p.Id, p.Name, p.EAN, p.DynamicPropertiesJson
HAVING COUNT(DISTINCT s.Id) > 1  -- Only products with multiple suppliers
ORDER BY SupplierCount DESC, PriceDifferencePercentage DESC;

-- Option 2: Detailed view - Products with all supplier offer details
WITH ProductSupplierSummary AS (
    SELECT 
        p.Id AS ProductId,
        p.Name AS ProductName,
        p.EAN,
        JSON_VALUE(p.DynamicPropertiesJson, '$.Brand') AS Brand,
        JSON_VALUE(p.DynamicPropertiesJson, '$.Gender') AS Gender,
        JSON_VALUE(p.DynamicPropertiesJson, '$.Size') AS Size,
        JSON_VALUE(p.DynamicPropertiesJson, '$.Concentration') AS Concentration,
        COUNT(DISTINCT s.Id) AS SupplierCount
    FROM Products p
    INNER JOIN OfferProducts op ON p.Id = op.ProductId
    INNER JOIN SupplierOffers so ON op.OfferId = so.Id
    INNER JOIN Suppliers s ON so.SupplierId = s.Id
    WHERE op.Price IS NOT NULL
    GROUP BY p.Id, p.Name, p.EAN, p.DynamicPropertiesJson
    HAVING COUNT(DISTINCT s.Id) > 1
)
SELECT 
    pss.ProductId,
    pss.ProductName,
    pss.EAN,
    pss.Brand,
    pss.Gender,
    pss.Size,
    pss.Concentration,
    pss.SupplierCount,
    s.Id AS SupplierId,
    s.Name AS SupplierName,
    so.Id AS OfferId,
    so.OfferName,
    op.Price,
    so.Currency,
    op.Quantity,
    -- Calculate price per ml if size is available
    CASE 
        WHEN TRY_CAST(pss.Size AS INT) > 0 AND op.Price > 0
        THEN ROUND(op.Price / TRY_CAST(pss.Size AS INT), 4)
        ELSE NULL 
    END AS PricePerMl,
    -- Mark lowest and highest prices for each product
    CASE 
        WHEN op.Price = MIN(op.Price) OVER (PARTITION BY pss.ProductId) 
        THEN 'LOWEST' 
        ELSE '' 
    END AS PriceRank_Low,
    CASE 
        WHEN op.Price = MAX(op.Price) OVER (PARTITION BY pss.ProductId) 
        THEN 'HIGHEST' 
        ELSE '' 
    END AS PriceRank_High
FROM ProductSupplierSummary pss
INNER JOIN OfferProducts op ON pss.ProductId = op.ProductId
INNER JOIN SupplierOffers so ON op.OfferId = so.Id
INNER JOIN Suppliers s ON so.SupplierId = s.Id
WHERE op.Price IS NOT NULL
ORDER BY pss.SupplierCount DESC, pss.ProductName, op.Price ASC;

-- Option 3: Quick count summary
SELECT 
    'Products with multiple suppliers' AS Metric,
    COUNT(*) AS Count
FROM (
    SELECT p.Id
    FROM Products p
    INNER JOIN OfferProducts op ON p.Id = op.ProductId
    INNER JOIN SupplierOffers so ON op.OfferId = so.Id
    INNER JOIN Suppliers s ON so.SupplierId = s.Id
    WHERE op.Price IS NOT NULL
    GROUP BY p.Id
    HAVING COUNT(DISTINCT s.Id) > 1
) AS MultiSupplierProducts;

-- Option 4: Supplier competition analysis
SELECT 
    s1.Name AS Supplier1,
    s2.Name AS Supplier2,
    COUNT(*) AS SharedProducts,
    AVG(ABS(op1.Price - op2.Price)) AS AvgPriceDifference,
    AVG(CASE 
        WHEN op1.Price > 0 
        THEN ABS(op1.Price - op2.Price) / op1.Price * 100 
        ELSE 0 
    END) AS AvgPriceDifferencePercentage
FROM Products p
INNER JOIN OfferProducts op1 ON p.Id = op1.ProductId
INNER JOIN SupplierOffers so1 ON op1.OfferId = so1.Id
INNER JOIN Suppliers s1 ON so1.SupplierId = s1.Id
INNER JOIN OfferProducts op2 ON p.Id = op2.ProductId
INNER JOIN SupplierOffers so2 ON op2.OfferId = so2.Id
INNER JOIN Suppliers s2 ON so2.SupplierId = s2.Id
WHERE s1.Id < s2.Id  -- Avoid duplicate pairs
AND op1.Price IS NOT NULL 
AND op2.Price IS NOT NULL
GROUP BY s1.Id, s1.Name, s2.Id, s2.Name
HAVING COUNT(*) >= 5  -- Only show supplier pairs with at least 5 shared products
ORDER BY SharedProducts DESC, AvgPriceDifferencePercentage DESC;
