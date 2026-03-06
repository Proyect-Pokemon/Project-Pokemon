import { Routes } from '@angular/router';
import { About } from './pages/about/about';
import { Profile } from './pages/profile/profile';
import { AdminPanel } from './pages/admin-panel/admin-panel';
import { Friends } from './pages/friends/friends';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { TeamBuilder } from './pages/team-builder/team-builder';
import { Battle } from './pages/battle/battle';
import { redirectionGuard } from './guards/redirection-guard';
import { adminGuard } from './guards/admin-guard';

export const routes: Routes = [
    { path: 'about', component: About },
    { path: 'admin-panel', component: AdminPanel, canActivate: [redirectionGuard, adminGuard] },
    { path: 'battle', component: Battle },
    { path: 'friends', component: Friends, canActivate: [redirectionGuard] },
    { path: 'login', component: Login },
    { path: 'profile', component: Profile, canActivate: [redirectionGuard] },
    { path: 'register', component: Register },
    { path: 'team-builder', component: TeamBuilder, canActivate: [redirectionGuard] },
    { path: '**', redirectTo: 'login', pathMatch: 'full' }
];