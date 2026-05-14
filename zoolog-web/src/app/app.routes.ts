import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { Profile } from './pages/profile/profile';
import { authGuard } from './guards/auth-guard';
import { Dashboard } from './pages/dashboard/dashboard';
import { Objects } from './pages/objects/objects';
import { AboutUs } from './pages/about_us/about_us';
import { NewObject } from './pages/new_object/new_object';


export const routes: Routes = [
    { path: 'login', component: Login },
    { path: 'register', component: Register },
    { path: 'profile', component: Profile, canActivate: [authGuard] },
    
    { path: 'dashboard', component: Dashboard },
    { path: 'objects', component: Objects },
    { path: 'about-us', component: AboutUs },
    { path: 'new-object', component: NewObject },

    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];
