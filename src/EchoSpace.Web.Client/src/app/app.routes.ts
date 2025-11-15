import { Routes } from '@angular/router';
import { LoginComponent } from './components/login/login.component';
import { RegisterComponent } from './components/register/register.component';
import { UserListComponent } from './components/user-list/user-list.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { AuthCallbackComponent } from './components/auth-callback/auth-callback.component';
import { HomeComponent } from './components/home/home.component';
import { SearchResultsComponent } from './components/search-results/search-results.component';
import { ForgotPasswordComponent } from './components/forgot-password/forgot-password.component';
import { ResetPasswordComponent } from './components/reset-password/reset-password.component';
import { OperationHomeComponent } from './components/operation-home/operation-home.component';
import { authGuard } from './guards/auth.guard';
import { roleGuard } from './guards/role.guard';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },
  { path: 'forgot-password', component: ForgotPasswordComponent },
  { path: 'reset-password', component: ResetPasswordComponent },
  { path: 'auth-callback', component: AuthCallbackComponent },
  { path: 'home', component: HomeComponent, canActivate: [authGuard] },
  { path: 'search', component: SearchResultsComponent, canActivate: [authGuard] },
  { 
    path: 'admin/users', 
    component: UserListComponent, 
    canActivate: [authGuard, roleGuard(['Admin'])]
  },
  { 
    path: 'admin/dashboard', 
    component: DashboardComponent, 
    canActivate: [authGuard, roleGuard(['Admin'])]
  },
  { 
    path: 'operation', 
    component: OperationHomeComponent, 
    canActivate: [authGuard, roleGuard(['Operation'])]
  },
  { path: '', redirectTo: '/home', pathMatch: 'full' },
  { path: '**', redirectTo: '/home' }
];
