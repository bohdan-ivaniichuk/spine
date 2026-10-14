import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

/** Session header: scope chip, accent track and the "7 / 20" position. */
@Component({
  selector: 'app-progress-bar',
  templateUrl: './progress-bar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProgressBar {
  readonly position = input.required<number>();
  readonly total = input.required<number>();
  readonly label = input('');

  protected readonly percent = computed(() => {
    const total = this.total();
    if (total <= 0) {
      return 0;
    }
    return Math.min(100, Math.max(0, (this.position() / total) * 100));
  });
}
