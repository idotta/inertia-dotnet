# inertia-dotnet
The dotnet adapter for Inertia.js.

## Project Goal

This project aims to stay feature-complete and on par with [inertia-laravel](https://github.com/inertiajs/inertia-laravel). We track the Laravel adapter as a submodule and periodically sync new features and improvements.

## inertia-laravel Submodule

This repository includes the official Laravel adapter as a git submodule to:
- Track the reference implementation
- Monitor for new features and updates
- Ensure feature parity with the Laravel ecosystem

### Updating the Submodule

To update the inertia-laravel submodule to the latest version:

```bash
git submodule update --remote inertia-laravel
```

After updating, review changes and migrate new features to C#. See [MIGRATION.md](MIGRATION.md) for guidelines.

## Development

See [MIGRATION.md](MIGRATION.md) for details on how features from inertia-laravel are migrated to this .NET implementation.
