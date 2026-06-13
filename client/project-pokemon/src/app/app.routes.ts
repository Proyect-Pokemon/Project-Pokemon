import { Routes } from '@angular/router';
import { About } from './pages/about/about';
import { Profile } from './pages/profile/profile';
import { AdminPanel } from './pages/admin-panel/admin-panel';
import { Friends } from './pages/friends/friends';
import { Login } from './pages/login/login';
import { Register } from './pages/register/register';
import { TeamBuilder } from './pages/team-builder/team-builder';
import { TeamEdit } from './pages/team-edit/team-edit';
import { PokemonEdit } from './pages/pokemon-edit/pokemon-edit';
import { Battle } from './pages/battle/battle';
import { BattleSelect } from './pages/battle-select/battle-select';
import { BattleModeSelect } from './pages/battle-mode-select/battle-mode-select';
import { redirectionGuard } from './guards/redirection-guard';
import { adminGuard } from './guards/admin-guard';
import { battleLeaveGuard } from './guards/battle-leave-guard';
import { Landing } from './pages/landing/landing';

export const routes: Routes = [
    { path: 'about', component: About },
    { path: 'admin-panel', component: AdminPanel, canActivate: [redirectionGuard, adminGuard] },
    { path: 'battle', component: BattleModeSelect, canActivate: [redirectionGuard] },
    { path: 'battle-select', component: BattleSelect, canActivate: [redirectionGuard] },
    { path: 'battle/fight', component: Battle, canActivate: [redirectionGuard], canDeactivate: [battleLeaveGuard] },
    { path: 'friends', component: Friends, canActivate: [redirectionGuard] },
    { path: 'login', component: Login },
    { path: 'profile', component: Profile, canActivate: [redirectionGuard] },
    { path: 'register', component: Register },
    { path: 'team-builder', component: TeamBuilder, canActivate: [redirectionGuard] },
    { path: 'team-builder/:id', component: TeamEdit, canActivate: [redirectionGuard] },
    { path: 'team-builder/:teamId/pokemon/:pokemonTeamId/edit', component: PokemonEdit, canActivate: [redirectionGuard] },
    { path: 'landing', component: Landing },
    { path: '**', redirectTo: 'landing', pathMatch: 'full' }
];