# Duplicate Offer Detection Implementation

## Overview
This implementation adds duplicate offer detection to prevent processing the same supplier offer file multiple times. When a supplier is identified and an offer is about to be created, the system now checks if an offer with the same name already exists for that supplier.

## Key Components

### 1. DuplicateOfferException
**Location**: `SacksDataLayer\Exceptions\DuplicateOfferException.cs`

A custom exception that is thrown when a duplicate offer is detected. Contains:
- `SupplierName`: Name of the supplier
- `OfferName`: Name of the conflicting offer
- `FileName`: Name of the file being processed

### 2. Repository Enhancement
**Interface**: `ISupplierOffersRepository.GetBySupplierAndOfferNameAsync()`
**Implementation**: `SupplierOffersRepository.GetBySupplierAndOfferNameAsync()`

New method to efficiently check if an offer with a specific name exists for a supplier.

### 3. Service Layer Enhancement
**Interface**: `ISupplierOffersService.OfferExistsAsync()`
**Implementation**: `SupplierOffersService.OfferExistsAsync()`

Service method that uses the repository to check for duplicate offers.

### 4. Database Service Enhancement
**Interface**: `IFileProcessingDatabaseService.ValidateOfferDoesNotExistAsync()`
**Implementation**: `FileProcessingDatabaseService.ValidateOfferDoesNotExistAsync()`

Method that validates an offer doesn't already exist and throws `DuplicateOfferException` if it does.

### 5. File Processing Service Update
**Location**: `FileProcessingService.ProcessSupplierOfferWithOptimizationsAsync()`

Updated to include duplicate offer validation early in the process, before any database operations are performed.

## How It Works

1. **Supplier Identification**: After a supplier is identified or created
2. **Duplicate Check**: The system checks if an offer with the expected name already exists
3. **Validation**: If a duplicate is found, a `DuplicateOfferException` is thrown
4. **User Message**: A clear, actionable message is displayed to the user
5. **Processing Halt**: Processing stops to prevent duplicate data

## Expected Offer Name Format
The system generates offer names using the format: `{SupplierName} - {FileName}`

For example:
- Supplier: "ACME Corp"
- File: "products_2025.xlsx"
- Generated Offer Name: "ACME Corp - products_2025.xlsx"

## User Experience

When a duplicate offer is detected, the user sees:

```
⚠️ DUPLICATE OFFER DETECTED
📋 Supplier: ACME Corp
📄 File: products_2025.xlsx
🚫 Existing Offer: ACME Corp - products_2025.xlsx

💡 SOLUTION:
➡️  Rename the file 'products_2025.xlsx' to a different name
➡️  Or delete/deactivate the existing offer first
➡️  Processing has been stopped to prevent duplicates
```

## Benefits

1. **Data Integrity**: Prevents accidental duplicate imports
2. **Clear User Guidance**: Provides specific instructions on how to resolve the issue
3. **Early Detection**: Validates before any database operations occur
4. **Transaction Safety**: No partial data is committed when duplicates are detected
5. **Audit Trail**: Logging captures all duplicate detection events

## Usage Scenarios

### Scenario 1: Accidental Re-import
User accidentally tries to import the same file twice - system prevents this automatically.

### Scenario 2: File Versioning
User wants to import an updated version - system prompts to rename the file (e.g., "products_2025_v2.xlsx").

### Scenario 3: Offer Management
User can delete/deactivate old offers before importing new ones with the same name.

## Testing

Unit tests are provided in `SacksDataLayer.Tests\Exceptions\DuplicateOfferExceptionTests.cs` to verify:
- Exception property initialization
- Message formatting
- Default constructor behavior

## Configuration

No additional configuration is required. The feature is automatically enabled for all file processing operations.
