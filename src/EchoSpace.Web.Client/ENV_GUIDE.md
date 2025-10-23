# Environment Configuration Guide

## 📁 Location

Environment files are located in: `src/environments/`

```
src/
└── environments/
    ├── environment.ts          ← Development environment
    └── environment.prod.ts    ← Production environment
```

## 🔧 Configuration Files

### `environment.ts` (Development)
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7131/api'
};
```

### `environment.prod.ts` (Production)
```typescript
export const environment = {
  production: true,
  apiUrl: 'https://your-production-api.com/api'
};
```

## 📝 How to Change API URL

### For Development:
Edit `src/environments/environment.ts`:
```typescript
apiUrl: 'https://localhost:7131/api'  // Change this
```

### For Production:
Edit `src/environments/environment.prod.ts`:
```typescript
apiUrl: 'https://your-production-api.com/api'  // Change this
```

## 🚀 Usage in Services

Services automatically use the environment configuration:

```typescript
import { environment } from '../../environments/environment';

export class UserService {
  private apiUrl = `${environment.apiUrl}/users`;
  // Uses development URL when running ng serve
  // Uses production URL when building for production
}
```

## 🔄 How It Works

- **Development**: `ng serve` → Uses `environment.ts`
- **Production**: `ng build --configuration production` → Uses `environment.prod.ts`

The `angular.json` file is configured to automatically replace the environment file during production builds.

## ✨ Benefits

✅ No hardcoded URLs in your code  
✅ Easy to switch between dev/prod  
✅ All API URLs in one place  
✅ Type-safe configuration  

