# PowerShell script to convert supplier-formats.json from nested to flattened structure
$jsonPath = "c:\Users\avist\source\repos\GitHubLocal\Customers\Sacks-New\SacksConsoleApp\Configuration\supplier-formats.json"

# Read the current JSON
$json = Get-Content $jsonPath -Raw | ConvertFrom-Json

# Function to flatten a column property
function Convert-ColumnProperty($columnProp) {
    $flattened = [ordered]@{
        targetProperty = $columnProp.targetProperty
        displayName = if ($columnProp.description) { $columnProp.description } else { $columnProp.targetProperty }
        classification = $columnProp.classification
        dataType = $columnProp.dataType.type
        format = $columnProp.dataType.format
        maxLength = $columnProp.dataType.maxLength
        transformations = $columnProp.dataType.transformations
        isRequired = $columnProp.validation.isRequired
        isUnique = $columnProp.validation.isUnique
        validationPatterns = $columnProp.validation.validationPatterns
        allowedValues = $columnProp.validation.allowedValues
        skipEntireRow = if ($columnProp.validation.PSObject.Properties['skipEntireRow']) { $columnProp.validation.skipEntireRow } else { $false }
    }
    return $flattened
}

# Process each supplier
foreach ($supplier in $json.suppliers) {
    # Convert each column property
    $newColumnProperties = [ordered]@{}
    foreach ($columnKey in $supplier.columnProperties.PSObject.Properties.Name) {
        $columnProp = $supplier.columnProperties.$columnKey
        $newColumnProperties.$columnKey = Convert-ColumnProperty $columnProp
    }
    
    # Replace the column properties
    $supplier.columnProperties = $newColumnProperties
}

# Convert back to JSON and save
$json | ConvertTo-Json -Depth 10 | Set-Content $jsonPath -Encoding UTF8

Write-Host "✅ Successfully converted supplier-formats.json to flattened structure"
