# Inertia Minimal Sample

A minimal ASP.NET Core application demonstrating basic Inertia.js integration with React.

## Features

This sample demonstrates:
- Basic Inertia.js setup with ASP.NET Core
- Single page with server-side props
- React frontend with Vite
- Shared data via middleware
- Simple styling

## Prerequisites

- .NET 8.0 or later
- Node.js 18.0 or later

## Getting Started

### 1. Install Dependencies

Install .NET dependencies (should already be done if you built the solution):

```bash
cd ../..
dotnet restore
```

Install Node.js dependencies:

```bash
cd samples/InertiaMinimal
npm install
```

### 2. Build the Frontend

Build the React frontend assets:

```bash
npm run build
```

For development with hot reload:

```bash
npm run dev
```

(Keep this running in a separate terminal)

### 3. Run the Application

In another terminal, run the ASP.NET Core application:

```bash
dotnet run
```

### 4. Open in Browser

Navigate to `https://localhost:5001` (or the port shown in the console output).

## Project Structure

```
InertiaMinimal/
├── Controllers/          # ASP.NET Core controllers
│   └── HomeController.cs
├── Middleware/          # Inertia middleware
│   └── HandleInertiaRequests.cs
├── Resources/           # Frontend source files
│   ├── js/
│   │   ├── app.jsx     # Inertia app setup
│   │   └── Pages/      # React page components
│   │       └── Home.jsx
│   └── css/
│       └── app.css     # Styles
├── Views/              # Razor views
│   └── Shared/
│       └── app.cshtml  # Root view template
├── wwwroot/            # Built frontend assets (generated)
│   ├── js/
│   └── css/
├── Program.cs          # App configuration
├── package.json        # Node.js dependencies
└── vite.config.js      # Vite build configuration
```

## How It Works

1. **Server-Side**: The `HomeController` returns an Inertia response with props
2. **Root View**: The `app.cshtml` Razor view contains the `<inertia />` tag helper
3. **Client-Side**: React renders the `Home.jsx` component with the server props
4. **Navigation**: Subsequent navigations use XHR requests for SPA-like behavior

## Customization

- **Change the page content**: Edit `Resources/js/Pages/Home.jsx`
- **Add server-side props**: Modify `Controllers/HomeController.cs`
- **Share data globally**: Update `Middleware/HandleInertiaRequests.cs`
- **Modify styles**: Edit `Resources/css/app.css`

## Next Steps

For a more comprehensive example with multiple pages, forms, validation, and advanced features, see the [InertiaReact](../InertiaReact) sample.

## Learn More

- [Inertia.js Documentation](https://inertiajs.com/)
- [inertia-dotnet Documentation](../../docs/getting-started.md)
- [React Documentation](https://react.dev/)
