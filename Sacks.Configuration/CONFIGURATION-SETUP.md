# Sacks Configuration Setup Guide

## Quick Start

### 1. Create Your Local Configuration

Copy the template to create your local config file:

```powershell
Copy-Item Sacks.Configuration\sacks-config.template.json Sacks.Configuration\sacks-config.json
```

### 2. Add Your Secrets

Edit `Sacks.Configuration\sacks-config.json` and add your API keys and connection strings.

**Important:** This file is in `.gitignore` and will NOT be committed to version control.

### 3. Configure LLM API Key

You have two options:

#### Option A: Direct in Config File (Local Development)

Edit `sacks-config.json`:
```json
"Llm": {
  "ApiKey": "your-actual-github-pat-token-here"
}
```

#### Option B: Environment Variable (Recommended for Production)

Set the environment variable:

**PowerShell:**
```powershell
$env:SACKS__Llm__ApiKey = "your-github-pat-token"
```

**PowerShell (Persistent - User Level):**
```powershell
[Environment]::SetEnvironmentVariable("SACKS__Llm__ApiKey", "your-github-pat-token", "User")
```

**PowerShell (Persistent - Machine Level):**
```powershell
[Environment]::SetEnvironmentVariable("SACKS__Llm__ApiKey", "your-github-pat-token", "Machine")
```

## Environment Variable Override Pattern

Any configuration value can be overridden via environment variables using the pattern:

```
SACKS__{SectionName}__{PropertyName}
```

Currently supported overrides:
- `SACKS__Llm__ApiKey` - LLM API key
- `SACKS__Database__ConnectionString` - Database connection string

### Examples

```powershell
# Override API Key
$env:SACKS__Llm__ApiKey = "ghp_your_token_here"

# Override Database Connection
$env:SACKS__Database__ConnectionString = "Server=myserver;Database=SacksProductsDb;..."
```

## Security Best Practices

1. ✅ **Never commit** `sacks-config.json` with real secrets (it's in `.gitignore`)
2. ✅ **Use environment variables** for production deployments
3. ✅ **Keep template updated** - commit changes to `sacks-config.template.json`
4. ✅ **Use empty/placeholder values** in template file

## File Structure

```
Sacks.Configuration/
├── sacks-config.template.json    ← Committed to Git (no secrets)
├── sacks-config.json              ← Local only (ignored by Git)
├── ConfigurationHelper.cs         ← Loads config + applies env overrides
└── SacksConfigurationOptions.cs   ← Config schema
```

## Deployment Checklist

When deploying to a new environment:

1. Copy `sacks-config.template.json` to `sacks-config.json`
2. Set environment variables for secrets:
   - `SACKS__Llm__ApiKey`
   - `SACKS__Database__ConnectionString` (if different from default)
3. Verify configuration loads correctly
4. Test LLM connectivity

## Troubleshooting

### "Could not find sacks-config.json"

The app searches in this order:
1. Application directory (production)
2. Solution root `Sacks.Configuration/` folder (development)

**Solution:** Ensure you've created `sacks-config.json` from the template.

### "ApiKey is empty"

**Check:**
1. Is `ApiKey` set in `sacks-config.json`?
2. Is `SACKS__Llm__ApiKey` environment variable set?

**Verify environment variable:**
```powershell
$env:SACKS__Llm__ApiKey
```

### GitHub Push Protection Error

If you accidentally added secrets to a file:
1. Remove the secret from the file
2. Replace with placeholder: `"ApiKey": ""`
3. Use environment variable instead
4. If the file shouldn't be in Git, add it to `.gitignore`

## Getting GitHub Personal Access Token

1. Go to https://github.com/settings/tokens
2. Click "Generate new token" → "Generate new token (classic)"
3. Select scopes: `repo` (for private repos) or `public_repo`
4. For GitHub Models: No specific scope needed for public models
5. Copy the token (you won't see it again!)
6. Set in environment variable: `SACKS__Llm__ApiKey`
