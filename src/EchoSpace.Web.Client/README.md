# EchoSpace.Web.Client

Angular frontend application for EchoSpace project.

## Getting Started

### Prerequisites
- Node.js (v18 or higher)
- npm or yarn
- Angular CLI installed globally: `npm install -g @angular/cli`

### Installation

```bash
npm install
```

### Development

Run the Angular development server:

```bash
ng serve
```

The application will be available at `http://localhost:4200`

### Backend API

Make sure the ASP.NET Core backend is running on `https://localhost:7131`

To start the backend:

```bash
cd ../EchoSpace.UI
dotnet run
```

## Project Structure

```
src/
├── app/
│   ├── components/
│   │   └── user-list/          # User list component
│   ├── services/
│   │   └── user.service.ts     # API service for users
│   ├── app.component.ts        # Root component
│   ├── app.config.ts          # App configuration
│   └── app.routes.ts          # Routing configuration
├── styles.css                  # Global styles
└── main.ts                    # Application entry point
```

## Features

- ✅ User list display
- ✅ User deletion
- ✅ HTTP client integration
- ✅ CORS configured
- ✅ Error handling
- ✅ Loading states
- ✅ Login page (`/login`)
- ✅ Register page (`/register`)
- ✅ Tailwind CSS styling
- ✅ Responsive design
- ✅ Social authentication (Google, Facebook) placeholders

## Building for Production

```bash
ng build --configuration production
```

The build artifacts will be stored in the `dist/` directory.

## Technology Stack

- Angular 18+
- TypeScript
- RxJS
- HTTP Client
