import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { ApiStatus } from './shared/api-status/api-status';
import { ThemeToggle } from './shared/theme/theme-toggle';

@Component({
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ApiStatus, ThemeToggle],
  selector: 'app-root',
  styleUrl: './app.scss',
  templateUrl: './app.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class App {}
