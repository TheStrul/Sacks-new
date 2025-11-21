-- Test data seed script for integration tests
-- Creates a realistic dataset for testing SacksMcp

-- Suppliers
INSERT INTO Suppliers (Name, Description) VALUES
('Supplier A', 'Premium fragrance distributor'),
('Supplier B', 'Discount perfume wholesaler'),
('Supplier C', 'Luxury cosmetics supplier');

-- Products
INSERT INTO Products (Name, EAN, CreatedAt) VALUES
('Chanel No 5 Eau de Parfum', '3145891355406', GETUTCDATE()),
('Dior Sauvage EDT', '3348901419383', GETUTCDATE()),
('Calvin Klein CK One', '3607342487710', GETUTCDATE()),
('Armani Code', '3614270086106', GETUTCDATE()),
('Paco Rabanne 1 Million', '3349668508556', GETUTCDATE());

-- Offers
INSERT INTO SupplierOffers (OfferName, Description, SupplierId, Currency, CreatedAt) VALUES
('Winter 2025 Fragrance Collection', 'Seasonal perfume offers', 1, 'USD', GETUTCDATE()),
('Spring Discounts', 'Reduced prices for spring', 2, 'EUR', GETUTCDATE()),
('Luxury Line Q1', 'Premium fragrances', 3, 'GBP', GETUTCDATE());

-- OfferProducts (linking offers with products and prices)
-- Note: Actual foreign key IDs need to be adjusted based on inserted records
INSERT INTO OfferProducts (OfferId, ProductId, Price, Currency, Description) VALUES
(1, 1, 89.99, 'USD', '50ml bottle'),
(1, 2, 74.99, 'USD', '100ml bottle'),
(2, 3, 45.50, 'EUR', 'Discounted price'),
(2, 4, 62.00, 'EUR', 'Limited stock'),
(3, 5, 68.00, 'GBP', 'Premium packaging');
