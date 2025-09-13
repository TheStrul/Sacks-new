USE [SacksProductsDb]
GO

SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

-- Collapsed view: shows product attributes only on the cheapest offer row per EAN
-- Adds EANKey (non-collapsed) for stable grouping/ordering in consumers
CREATE OR ALTER VIEW [dbo].[ProductOffersViewCollapse] AS
SELECT
    v.[EAN]       AS [EANKey],
    CASE WHEN v.[OfferRank] = 1 THEN v.[EAN]          END AS [EAN],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Name]         END AS [Name],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Category]     END AS [Category],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Brand]        END AS [Brand],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Line]         END AS [Line],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Gender]       END AS [Gender],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Concentration]END AS [Concentration],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Size]         END AS [Size],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Type]         END AS [Type],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Decoded]      END AS [Decoded],
    CASE WHEN v.[OfferRank] = 1 THEN v.[COO]          END AS [COO],
    CASE WHEN v.[OfferRank] = 1 THEN v.[Units]        END AS [Units],
    v.[Ref],
    v.[Details],
    v.[Price],
    v.[Currency],
    v.[Quantity],
    v.[S_Name],
    v.[O_Name],
    v.[Date],
    v.[OfferRank],
    v.[TotalOffers]
FROM [dbo].[ProductOffersView] v
WHERE v.[TotalOffers] > 1;
GO