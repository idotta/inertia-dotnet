# inertia-dotnet
The dotnet adapter for Inertia.js.

## Project Goal

This project aims to stay feature-complete and on par with [inertia-laravel](https://github.com/inertiajs/inertia-laravel). We track the Laravel adapter as a submodule and periodically sync new features and improvements.

## Migration Status

🎯 **Planning Phase Complete** - Comprehensive migration analysis finished!

We have analyzed the entire inertia-laravel v2.0.14 codebase (46 PHP files, 170+ features) and created a complete migration plan to C#/.NET. See our planning documents:

### 📚 Planning Documents

- **[MIGRATION_SUMMARY.md](MIGRATION_SUMMARY.md)** - Start here! Executive summary and overview
- **[MIGRATION_PLAN.md](MIGRATION_PLAN.md)** - Complete 12-week implementation plan
- **[FEATURE_COMPARISON.md](FEATURE_COMPARISON.md)** - Side-by-side feature comparison (Laravel vs .NET)
- **[IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md)** - 400+ actionable development tasks
- **[API_MAPPING.md](API_MAPPING.md)** - Code examples: Laravel → C# conversions
- **[MIGRATION.md](MIGRATION.md)** - Migration process guidelines

### 📊 Key Statistics

- **Features Identified:** 170+ across 11 categories
- **Direct Migrations:** 152 features (89%)
- **Adaptations Required:** 18 features (11%)
- **Estimated Timeline:** 12 weeks to v1.0.0
- **Target Coverage:** >80% test coverage

## inertia-laravel Submodule

This repository includes the official Laravel adapter as a git submodule to:
- Track the reference implementation
- Monitor for new features and updates
- Ensure feature parity with the Laravel ecosystem

**Current Tracked Version:** v2.0.14 (commit: 7240b646)

### Updating the Submodule

To update the inertia-laravel submodule to the latest version:

```bash
git submodule update --remote inertia-laravel
```

After updating, review changes and migrate new features to C#. See [MIGRATION.md](MIGRATION.md) for guidelines.

## Quick Start

### For Contributors

1. **Read the planning docs** (start with MIGRATION_SUMMARY.md)
2. **Pick a task** from IMPLEMENTATION_CHECKLIST.md
3. **Review code examples** in API_MAPPING.md
4. **Submit a PR** following our guidelines

### For Users (Coming Soon)

```bash
# Installation (after v1.0.0 release)
dotnet add package Inertia.AspNetCore
```

```csharp
// Startup configuration
services.AddInertia(options =>
{
    options.RootView = "app";
    options.SsrEnabled = true;
});

app.UseInertia<HandleInertiaRequests>();
```

## Development

See [MIGRATION.md](MIGRATION.md) for details on how features from inertia-laravel are migrated to this .NET implementation.

## Contributing

We welcome contributions! Check out:
- [IMPLEMENTATION_CHECKLIST.md](IMPLEMENTATION_CHECKLIST.md) for available tasks
- [GitHub Issues](https://github.com/idotta/inertia-dotnet/issues) for open items
- [GitHub Discussions](https://github.com/idotta/inertia-dotnet/discussions) for questions

## License

MIT License - Same as inertia-laravel
