import { httpResource } from '@angular/common/http';
import { ChangeDetectionStrategy, Component, computed, DestroyRef, inject } from '@angular/core';

const REFRESH_INTERVAL_MS = 30_000;

/** Small indicator showing whether the API (and its database) answers on `/api/health`. */
@Component({
  selector: 'app-api-status',
  styleUrl: './api-status.scss',
  templateUrl: './api-status.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ApiStatus {
  readonly health = httpResource<{ status: string }>(() => '/api/health');

  protected readonly unreachable = computed(
    () => !this.health.isLoading() && !this.health.hasValue(),
  );

  constructor() {
    const timer = setInterval(() => this.health.reload(), REFRESH_INTERVAL_MS);
    inject(DestroyRef).onDestroy(() => clearInterval(timer));
  }
}
