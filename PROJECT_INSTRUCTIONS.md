# Project Instructions - Product Normalization System

## 🎯 Project Overview

**Purpose**: Inventory management and supplier integration system that normalizes Excel files from different suppliers into a unified structure for data managers and business analysts.

**Team Structure**: 
- **Project Leader**: You (avist)
- **Developer**: GitHub Copilot (AI Assistant)

## 📋 Project Goals & Phases

### Phase 1: Data Normalization Foundation ⚠️ *CURRENT PHASE*
- Normalize ALL existing supplier files to unified ProductEntity structure
- Ensure configuration-based approach works for all current suppliers
- Validate data integrity and completeness

### Phase 2: Customer BI Consultation
- Present well-defined data layer to customer
- Gather BI requirements based on normalized data structure
- Plan reporting and analytics features

### Phase 3: Production Deployment
- Deploy to local PC environment with free database (MySQL/SQLite)
- Support adding new suppliers via JSON configuration only

## 🏗️ Architecture Principles

### Code Quality Standards
- **Simplicity**: Clear, readable code over complex solutions
- **Modularity**: Well-separated concerns and responsibilities
- **Object-Oriented**: Proper encapsulation, inheritance, and polymorphism
- **DRY Principle**: No duplicate code, reusable components
- **Standard Practices**: Follow established C# and .NET conventions

### Development Approach
- **Performance**: Not critical at this stage
- **Maintainability**: Priority #1
- **Extensibility**: Easy to add new suppliers via JSON
- **Testability**: Code should be easily testable

## 📁 Current Project Structure

```
Sacks-New/
├── SacksDataLayer/              # Core normalization library
│   ├── Entity.cs                # Base entity with audit fields
│   ├── ProductEntity.cs         # Product with dynamic properties
│   ├── Configuration/           # JSON-based supplier configs
│   │   ├── supplier-formats.json
│   │   ├── SupplierConfigurationModels.cs
│   │   └── Normalizers/
│   ├── FileProcessing/          # Excel file processing
│   └── Services/                # Business logic services
└── SacksConsoleApp/             # Testing and demonstration
```

## 🤝 Collaboration Rules

### For GitHub Copilot (Developer):

#### ✅ YOU CAN DO FREELY:
1. **Suggest** any improvements, new code, or architectural changes
2. **Create** new files, classes, methods, or configurations
3. **Analyze** existing code and propose optimizations
4. **Add** new features that align with project goals
5. **Refactor** code for better maintainability (with explanation)

#### ⚠️ REQUIRES EXPLICIT APPROVAL:
1. **Changing existing code** that hasn't been committed to Git
2. **Modifying core entity structures** (Entity.cs, ProductEntity.cs)
3. **Altering existing supplier configurations** in JSON files
4. **Removing or renaming** existing methods, properties, or classes

#### 📝 APPROVAL PROCESS:
When changes need approval:
1. Explain WHAT you want to change
2. Explain WHY the change is beneficial
3. Show BEFORE/AFTER code snippets
4. Wait for explicit "Yes, proceed" from project leader

### For Project Leader (avist):

#### 🎯 YOUR RESPONSIBILITIES:
1. Provide business requirements and clarifications
2. Approve/reject proposed changes to existing code
3. Test and validate normalized data output
4. Make final decisions on architecture changes

## 📊 Supplier Management Strategy

### Current Approach
- **Configuration-Driven**: Add suppliers via `supplier-formats.json`
- **No Code Changes**: New suppliers should only require JSON updates
- **Flexible Mapping**: Support for column mapping and data transformations

### Adding New Suppliers (Post-Production Goal)
```json
{
  "name": "NewSupplier",
  "detection": { /* file detection rules */ },
  "columnMappings": { /* Excel column to ProductEntity mapping */ },
  "dataTypes": { /* data transformation rules */ }
}
```

## 💾 Data Storage Strategy

### Target Environment
- **Platform**: Local PC deployment
- **Database**: Free solution (MySQL, SQLite, or PostgreSQL)
- **Storage**: Local file system for Excel inputs and database

### Data Flow
```
Excel Files → FileDataReader → ConfigurationBasedNormalizer → ProductEntity → Local Database
```

## 🔧 Development Workflow

### 1. Analyzing New Requirements
- Review existing code structure
- Identify impact areas
- Propose solution approach
- Get approval for significant changes

### 2. Implementation Process
- Create/modify code following quality standards
- Ensure backward compatibility
- Test with existing supplier files
- Document changes in code comments

### 3. Testing Strategy
- Test with all current supplier Excel files
- Validate ProductEntity output
- Ensure JSON configuration works correctly
- Verify no data loss during normalization

## 📋 Current Tasks & Priorities

### Immediate Focus (Phase 1)
1. **Validate Current Normalization**: Ensure all existing suppliers work correctly
2. **Data Quality**: Check for data loss or transformation issues
3. **Configuration Completeness**: Verify all supplier formats are properly configured
4. **Error Handling**: Robust handling of malformed Excel files

### Next Steps
1. **Database Integration**: Connect ProductEntity to local database
2. **Batch Processing**: Handle multiple files efficiently
3. **Validation Reports**: Generate data quality reports
4. **Configuration Tools**: Possibly create tools to help configure new suppliers

## 🚀 Communication Style

### When Requesting Help
- Be specific about the task or problem
- Provide context about which suppliers/files are involved
- Mention if this relates to existing or new functionality

### When Copilot Suggests Changes
- Will explain the reasoning behind suggestions
- Will show code examples when relevant
- Will highlight any potential risks or breaking changes
- Will ask for approval when required

## 📝 Documentation Standards

### Code Comments
- All public methods must have XML documentation
- Complex business logic should have inline comments
- Configuration examples in JSON files should be well-documented

### Change Tracking
- Document significant architectural decisions
- Keep track of supplier-specific configurations
- Note any data transformation rules and their business justification

---

## 🤖 Quick Reference for Copilot

**Current Priority**: Phase 1 - Normalize all existing supplier files
**Approval Needed**: Changes to existing Entity classes, existing JSON configs, existing file processing logic
**Free to Create**: New services, new models, new configurations, new utility classes
**Code Style**: Clean, simple, modular, object-oriented C#
**Database**: Plan for local free database (MySQL/SQLite)

---

*This instruction file will evolve as the project progresses. Last updated: August 24, 2025*

