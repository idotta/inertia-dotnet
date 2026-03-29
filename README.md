# inertia-dotnet

[![NuGet](https://img.shields.io/nuget/v/Inertia.AspNetCore.svg)](https://www.nuget.org/packages/Inertia.AspNetCore/)
[![Build Status](https://github.com/idotta/inertia-dotnet/workflows/Build/badge.svg)](https://github.com/idotta/inertia-dotnet/actions)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A .NET adapter for [Inertia.js](https://inertiajs.com/). Build modern single-page applications using classic server-side routing and controllers.

> **Note:** This project is currently in active development. See [Migration Status](#migration-status) for details.

## Features

## Quick Start

## Documentation

📚 **Comprehensive guides available:**

- **[Getting Started](docs/getting-started.md)** - Detailed setup guide
- **[Responses](docs/responses.md)** - Working with Inertia responses
- **[Property Types](docs/properties.md)** - Optional, Deferred, Merge, and more
- **[Middleware](docs/middleware.md)** - Request handling and shared data
- **[Server-Side Rendering](docs/ssr-setup.md)** - SSR configuration and setup
- **[Testing](docs/testing.md)** - Testing your Inertia applications
- **[Migration from Laravel](docs/migration-from-laravel.md)** - Laravel to .NET guide

## Project Goal

This project aims to stay feature-complete and on par with [inertia-laravel](https://github.com/inertiajs/inertia-laravel). We track the Laravel adapter as a submodule and periodically sync new features and improvements.

## Migration Status

## inertia-laravel Submodule

This repository includes the official Laravel adapter as a git submodule to:

- Track the reference implementation
- Monitor for new features and updates
- Ensure feature parity with the Laravel ecosystem

**Current Tracked Version:** v3.0.1

### Updating the Submodule

To update the inertia-laravel submodule to the latest version:

```bash
git submodule update --remote inertia-laravel
```

After updating, review changes and migrate new features to C#. See [MIGRATION.md](MIGRATION.md) for guidelines.

## Examples

Check out our [sample projects](samples/) to see Inertia.js in action:

- **InertiaMinimal** - Minimal setup example
- **InertiaReact** - Full React application
- **InertiaVue** - Full Vue 3 application  
- **InertiaSsr** - Server-side rendering example

## Advanced Features

## Development & Contributing

## License

MIT License - Same as inertia-laravel
