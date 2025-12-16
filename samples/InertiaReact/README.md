# Inertia React Sample

A comprehensive ASP.NET Core application demonstrating advanced Inertia.js features with React.

## Features

This sample demonstrates:

### Core Features
- Multiple pages with client-side navigation
- Shared layouts and navigation
- Flash messages across requests
- Server-side routing with ASP.NET Core MVC

### Inertia.js Features
- **Forms and Validation**: Complete CRUD operations with model validation
- **Partial Reloads**: Load specific props on demand (see Optional Props demo)
- **Property Types**:
  - **OptionalProp**: Load statistics only when requested
  - **AlwaysProp**: Timestamp that updates on every reload
- **Shared Data**: App name and flash messages shared across all pages
- **Form Helper**: Built-in form handling with Inertia's `useForm` hook

### React Features
- Functional components with hooks
- Inertia's React adapter (`@inertiajs/react`)
- Shared layout pattern
- Form state management

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
cd samples/InertiaReact
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
InertiaReact/
├── Controllers/              # ASP.NET Core controllers
│   ├── HomeController.cs    # Home and About pages
│   └── UsersController.cs   # Full CRUD for users
├── Middleware/              # Inertia middleware
│   └── HandleInertiaRequests.cs
├── Models/                  # Data models
│   └── User.cs
├── Data/                    # Data access
│   └── SampleDataService.cs # In-memory data service
├── Resources/               # Frontend source files
│   ├── js/
│   │   ├── app.jsx         # Inertia app setup
│   │   ├── Shared/         # Shared components
│   │   │   └── Layout.jsx  # Main layout with nav
│   │   └── Pages/          # React page components
│   │       ├── Home/
│   │       │   ├── Index.jsx
│   │       │   └── About.jsx
│   │       └── Users/
│   │           ├── Index.jsx   # List users
│   │           ├── Create.jsx  # Create form
│   │           ├── Show.jsx    # View user
│   │           └── Edit.jsx    # Edit form
│   └── css/
│       └── app.css         # Styles
├── Views/                  # Razor views
│   └── Shared/
│       └── app.cshtml      # Root view template
├── wwwroot/                # Built frontend assets (generated)
│   ├── js/
│   └── css/
├── Program.cs              # App configuration
├── package.json            # Node.js dependencies
└── vite.config.js          # Vite build configuration
```

## Key Concepts Demonstrated

### 1. Partial Reloads

In `Users/Index.jsx`, clicking "Load Statistics" demonstrates partial reloads:

```jsx
router.reload({ only: ['stats'] });
```

This only fetches the `stats` prop, leaving other data unchanged.

### 2. Property Types

#### Optional Props
```csharp
// In UsersController.cs
["stats"] = new OptionalProp(async () =>
{
    await Task.Delay(100); // Simulate work
    return new { TotalUsers = _dataService.GetUsers().Count, ActiveToday = 2 };
})
```

Only loaded when explicitly requested via partial reload.

#### Always Props
```csharp
// In UsersController.cs
["timestamp"] = new AlwaysProp(DateTime.UtcNow)
```

Always included, even during partial reloads.

### 3. Forms and Validation

Forms use Inertia's `useForm` hook for seamless integration:

```jsx
const { data, setData, post, processing, errors } = useForm({
    name: '',
    email: '',
    role: 'User',
});

const submit = (e) => {
    e.preventDefault();
    post('/users');
};
```

Validation errors are automatically handled and displayed.

### 4. Flash Messages

Set in the controller:

```csharp
var flash = new Dictionary<string, string> { ["success"] = "User created!" };
HttpContext.Session.Set("flash", JsonSerializer.SerializeToUtf8Bytes(flash));
```

Automatically shared via middleware and displayed in the layout.

### 5. Shared Data

In `HandleInertiaRequests.cs`:

```csharp
shared["appName"] = "Inertia React Sample";
```

Available in all components via `usePage().props.appName`.

## Exploring the Code

### Start with Home Page
- Controller: `Controllers/HomeController.cs`
- Component: `Resources/js/Pages/Home/Index.jsx`

### Check out CRUD Operations
- Controller: `Controllers/UsersController.cs`
- Components: `Resources/js/Pages/Users/*.jsx`

### Understand the Layout
- Component: `Resources/js/Shared/Layout.jsx`
- Shows navigation, flash messages, and consistent structure

### Review Middleware
- File: `Middleware/HandleInertiaRequests.cs`
- Handles shared data, versioning, and flash messages

## Next Steps

For a simpler, minimal example, see the [InertiaMinimal](../InertiaMinimal) sample.

For SSR (Server-Side Rendering) example, check the documentation at [../../docs/ssr-setup.md](../../docs/ssr-setup.md).

## Learn More

- [Inertia.js Documentation](https://inertiajs.com/)
- [inertia-dotnet Documentation](../../docs/getting-started.md)
- [React Documentation](https://react.dev/)
- [ASP.NET Core MVC](https://learn.microsoft.com/aspnet/core/mvc/)
