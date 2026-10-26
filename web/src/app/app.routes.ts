// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
// Licensed under the MIT License. See LICENSE in the repository root.

import { Routes } from '@angular/router';
import { MemberSearchComponent } from './member-search/member-search.component';

export const routes: Routes = [
  { path: '', component: MemberSearchComponent, title: 'Members' },
  {
    // Lazy-loaded: the detail page's code downloads only when the user opens a member.
    path: 'members/:id',
    loadComponent: () =>
      import('./member-detail/member-detail.component').then((m) => m.MemberDetailComponent),
    title: 'Member detail',
  },
  { path: '**', redirectTo: '' },
];
