import { ChangeDetectionStrategy, Component, computed, inject, input } from '@angular/core';
import { I18n } from '../../shared/i18n/i18n';

/** Session header: scope chip, accent track and the "7 / 20" position. */
@Component({
  selector: 'app-progress-bar',
  templateUrl: './progress-bar.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProgressBar {
  protected readonly i18n = inject(I18n);
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
