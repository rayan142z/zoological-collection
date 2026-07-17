import { Routes } from '@angular/router';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { Profile } from './pages/profile/profile';
import { authGuard } from './guards/auth-guard';
import { Dashboard } from './pages/dashboard/dashboard';
import { Objects } from './pages/objects/objects';
import { AboutUs } from './pages/about_us/about_us';
import { NewObject } from './pages/new_object/new_object';
import { ObjectInfo } from './pages/object_info/object_info'; 
import { BorrowedObjects } from './pages/borrowed_objects/borrowed_objects';
import { NewCollection } from './pages/new_collection/new_collection';
import { EditObject } from './pages/edit_object/edit_object';
import { PublicSearch } from './pages/public_search/public_search';
import { CollectionEditComponent } from './pages/collection-edit/collection-edit';

export const routes: Routes = [
    { path: 'login',      component: Login },
    { path: 'register',   component: Register },
    { path: 'profile',    component: Profile, canActivate: [authGuard] },

    { path: 'dashboard',  component: Dashboard },
    // /objects acts as the collection browser; /objects/:id would be a detail page
    { path: 'objects',    component: Objects },
    
    { path: 'objects/:id', component: Objects },   // collection detail (filtered view)
    { path: 'object/:id',  component: ObjectInfo },
    { path: 'edit-object/:id', component: EditObject },
    { path: 'about-us',   component: AboutUs },
    { path: 'borrowed_objects', component: BorrowedObjects },
    { path: 'collections/new', component: NewCollection, canActivate: [authGuard] },
    { path: 'public_search', component: PublicSearch },
    { path: 'collection-edit/:id', component: CollectionEditComponent },
    // new-object is nested inside a collection
    { path: 'objects/:id/new', component: NewObject, canActivate: [authGuard] },
    { path: 'new-object', component: NewObject, canActivate: [authGuard] },  // fallback direct link

    { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
];