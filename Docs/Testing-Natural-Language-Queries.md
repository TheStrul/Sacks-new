# Testing Natural Language Queries - Quick Test Guide

## Test Case 1: Simple Greeting ?

**Input**: `Hi`

**Expected Behavior**:
```
?? Natural Language Query
?? Your question: Hi

?? Routing Analysis:
   Tool Selected: SearchProducts
   Confidence: 40%
   Reason: Greeting or help query detected; showing product search as default

?? Tool Result:
????????????????????????????????????????????????????????????????????????????????
[Results from SearchProducts tool]
```

**What Happens**:
- Recognized as greeting
- Defaults to SearchProducts
- Shows products (starter data for user)

---

## Test Case 2: Product Search ?

**Input**: `Show me products`

**Expected Behavior**:
```
?? Natural Language Query
?? Your question: Show me products

?? Routing Analysis:
   Tool Selected: SearchProducts
   Confidence: 30%
   Reason: 2 keyword match(es); Pattern match

?? Tool Result:
????????????????????????????????????????????????????????????????????????????????
[All products returned]
```

---

## Test Case 3: Price Filter ?

**Input**: `Show me products over $100`

**Expected Behavior**:
```
?? Natural Language Query
?? Your question: Show me products over $100

?? Routing Analysis:
   Tool Selected: SearchProducts
   Confidence: 45%
   Reason: 2 keyword match(es); Pattern match

?? Tool Result:
????????????????????????????????????????????????????????????????????????????????
[Products filtered by minValue: "100"]
```

---

## Test Case 4: Price Range ?

**Input**: `Products between $50 and $200`

**Expected Behavior**:
- Extracts: `minValue: "50"`, `maxValue: "200"`
- Returns products in price range

---

## Test Case 5: Supplier Query ?

**Input**: `How many suppliers do we have?`

**Expected Behavior**:
```
Tool Selected: GetSupplierStats
Confidence: 75%
Reason: 2 keyword match(es); Pattern match
```

---

## Test Case 6: Offer Query ?

**Input**: `Show me deals`

**Expected Behavior**:
```
Tool Selected: GetOfferDetails
Confidence: 45%
Reason: 1 keyword match(es)
```

---

## Test Case 7: Unmatched Query

**Input**: `xyz abc 123 garbage`

**Expected Behavior**:
```
Tool Selected: SearchProducts
Confidence: 30%
Reason: No specific match found; defaulting to product search
```

---

## Confidence Score Guide

| Score Range | Interpretation |
|-------------|-----------------|
| 80-100% | High confidence - specific match |
| 50-79% | Medium confidence - good match |
| 30-49% | Low confidence - default fallback |
| 0-29% | No match - error state (shouldn't happen now) |

---

## What Changed

### Before
- ? "Hi" ? Error: "No tools matched"
- ? Generic queries ? Failed to route

### After
- ? "Hi" ? Routes to SearchProducts (40% confidence)
- ? Generic queries ? Default to most likely tool
- ? Helpful fallback message
- ? Lower confidence thresholds (more forgiving)

---

## Testing Checklist

- [ ] Type "Hi" ? No error, shows products
- [ ] Type "Show me products" ? SearchProducts selected
- [ ] Type "Products over $100" ? Price extracted
- [ ] Type "How many suppliers?" ? GetSupplierStats selected
- [ ] Type "Show deals" ? GetOfferDetails selected
- [ ] Type random text ? Defaults to SearchProducts gracefully

---

## How to Debug

### Enable Debug Logging
```json
// In appsettings.json
"Serilog": {
  "MinimumLevel": {
    "Default": "Debug"
  }
}
```

### Check Logs
```
logs/sacks-YYYY-MM-DD.log
```

Look for:
- `Routing natural language query`
- `Routing query to tool`
- Confidence scores

---

## Expected Tool Improvements

After this fix:
- ? No more "No tools matched" errors
- ? Graceful fallback to default tool
- ? Helpful error messages
- ? Works with simple greetings
- ? Better parameter extraction

---

**Ready to test!** ??
