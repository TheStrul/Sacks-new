-- ProductOffersView - Shows all offers for each product with pricing and supplier information
CREATE VIEW ProductOffersView AS
SELECT 
    -- Core product fields from ProductPropertiesSimpleView
    p.EAN,
    JSON_VALUE(p.DynamicProperties, '$.Category') AS Category,
    JSON_VALUE(p.DynamicProperties, '$.Brand') AS Brand,
    p.Name,
    p.Description,
    JSON_VALUE(p.DynamicProperties, '$.Gender') AS Gender,
    JSON_VALUE(p.DynamicProperties, '$.Concentration') AS Concentration,
    JSON_VALUE(p.DynamicProperties, '$.Size') AS Size,
    JSON_VALUE(p.DynamicProperties, '$.Type') AS Type,
    JSON_VALUE(p.DynamicProperties, '$.Decoded') AS Decoded,
    JSON_VALUE(p.DynamicProperties, '$.COO') AS COO,
    JSON_VALUE(p.DynamicProperties, '$.ProductLine') AS Line,
    
    -- Offer information
    so.OfferName,
    op.Price,
    op.Quantity,
    so.Currency,
    so.Description AS OfferDescription,
    
    -- Supplier information
    s.Name AS SupplierName,
    
    -- Add row number for each product to identify multiple offers
    ROW_NUMBER() OVER (PARTITION BY p.EAN ORDER BY op.Price ASC) AS OfferRank,
    
    -- Count total offers for this product
    COUNT(*) OVER (PARTITION BY p.EAN) AS TotalOffers

FROM Products p
INNER JOIN OfferProducts op ON p.Id = op.ProductId
INNER JOIN SupplierOffers so ON op.OfferId = so.Id
INNER JOIN Suppliers s ON so.SupplierId = s.Id
WHERE p.DynamicProperties IS NOT NULL;