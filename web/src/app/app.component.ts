import { Component } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, RouterLink],
  template: `
    <header class="topbar">
      <a routerLink="/" class="brand">Member Claims</a>
      <span class="tag">Angular · ASP.NET Core · SQL Server</span>
    </header>
    <main class="page">
      <router-outlet />
    </main>
  `,
})
export class AppComponent {}
