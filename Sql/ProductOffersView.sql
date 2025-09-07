-- ProductOffersView - Shows all offers for each product with pricing and supplier information
CREATE VIEW ProductOffersView AS
SELECT 
    -- Core product fields from ProductPropertiesSimpleView
    p.EAN,
    JSON_VALUE(p.DynamicProperties, '$.Category') AS Category,
    JSON_VALUE(p.DynamicProperties, '$.Brand') AS Brand,
    JSON_VALUE(p.DynamicProperties, '$.ProductLine') AS Line,
    op.Description,
    p.Name,
    JSON_VALUE(p.DynamicProperties, '$.Gender') AS Gender,
    JSON_VALUE(p.DynamicProperties, '$.Concentration') AS Concentration,
    JSON_VALUE(p.DynamicProperties, '$.Size') AS Volume,
    JSON_VALUE(p.DynamicProperties, '$.Type') AS Set,
    JSON_VALUE(p.DynamicProperties, '$.Decoded') AS Decoded,
    JSON_VALUE(p.DynamicProperties, '$.COO') AS COO,
    
    -- Supplier information
    s.Name AS SupplierName,
    so.CreatedAt As "Date Offer",
    op.Price,
    so.Currency,
    op.Quantity,
    
    -- Add row number for each product to identify multiple offers
    ROW_NUMBER() OVER (PARTITION BY p.EAN ORDER BY op.Price ASC) AS OfferRank,
    
    -- Count total offers for this product
    COUNT(*) OVER (PARTITION BY p.EAN) AS TotalOffers

FROM Products p
INNER JOIN OfferProducts op ON p.Id = op.ProductId
INNER JOIN SupplierOffers so ON op.OfferId = so.Id
INNER JOIN Suppliers s ON so.SupplierId = s.Id
WHERE p.DynamicProperties IS NOT NULL;