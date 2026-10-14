import { DatePipe } from '@angular/common';
import { httpResource } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  effect,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { map } from 'rxjs';
import { KnowledgeTree } from '../../knowledge/api/knowledge-tree.models';
import { overviewUrl } from '../api/review-api';
import {
  ReviewOverview as ReviewOverviewDto,
  ReviewScopeType,
  ScopeQuery,
} from '../api/review.models';
import { SESSION_ROUTE, SessionStore } from '../session-store';

const MIN_LIMIT = 1;
const MAX_LIMIT = 100;

/** Scope + size picker with live due/new counts; the entry point of the review feature. */
@Component({
  selector: 'app-review-overview',
  imports: [ReactiveFormsModule, DatePipe],
  templateUrl: './review-overview.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewOverview implements OnInit {
  protected readonly store = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly fb = inject(NonNullableFormBuilder);

  protected readonly form = this.fb.group({
    scopeType: this.fb.control<ReviewScopeType>('all'),
    domain: '',
    topic: '',
    totalLimit: [20, [Validators.required, Validators.min(MIN_LIMIT), Validators.max(MAX_LIMIT)]],
    newLimit: [10, [Validators.required, Validators.min(MIN_LIMIT), Validators.max(MAX_LIMIT)]],
  });

  protected readonly minLimit = MIN_LIMIT;
  protected readonly maxLimit = MAX_LIMIT;

  private readonly formValue = toSignal(
    this.form.valueChanges.pipe(map(() => this.form.getRawValue())),
    { initialValue: this.form.getRawValue() },
  );

  /** `undefined` while the scope is incomplete (domain/topic not chosen yet) → no request. */
  protected readonly scopeQuery = computed<ScopeQuery | undefined>(() => {
    const value = this.formValue();
    switch (value.scopeType) {
      case 'all':
        return { scope: 'all' };
      case 'domain':
        return value.domain ? { scope: 'domain', domain: value.domain } : undefined;
      case 'topic':
        return value.domain && value.topic
          ? { scope: 'topic', domain: value.domain, topic: value.topic }
          : undefined;
    }
  });

  private readonly overviewRequest = computed(() => {
    const query = this.scopeQuery();
    return query ? overviewUrl(query) : undefined;
  });

  protected readonly tree = httpResource<KnowledgeTree>(() => '/api/knowledge/tree');
  protected readonly overview = httpResource<ReviewOverviewDto>(() => this.overviewRequest());

  protected readonly domains = computed(() =>
    this.tree.hasValue() ? this.tree.value().domains.filter((d) => !d.deprecatedAt) : [],
  );

  protected readonly topics = computed(() => {
    const domain = this.domains().find((d) => d.slug === this.formValue().domain);
    return domain?.topics.filter((t) => !t.deprecatedAt) ?? [];
  });

  protected readonly apiUnreachable = computed(
    () => this.overview.status() === 'error' || this.tree.status() === 'error',
  );

  protected readonly nothingToReview = computed(
    () =>
      this.overview.hasValue() &&
      this.overview.value().dueCount + this.overview.value().newCount === 0,
  );

  /** True while this page's own Start request runs (the store's status also covers the resume probe). */
  protected readonly starting = signal(false);

  protected readonly canStart = computed(
    () =>
      this.overview.hasValue() &&
      !this.nothingToReview() &&
      !this.starting() &&
      this.scopeQuery() !== undefined,
  );

  private defaultsApplied = false;

  constructor() {
    const destroyRef = inject(DestroyRef);
    this.form.controls.domain.valueChanges
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe(() => this.form.controls.topic.setValue(''));

    // Topic is only choosable once the chosen domain's topics are known; keep the control's own
    // disabled state in sync so model and view agree.
    effect(() => {
      const topic = this.form.controls.topic;
      if (this.topics().length > 0) {
        topic.enable({ emitEvent: false });
      } else {
        topic.disable({ emitEvent: false });
      }
    });

    // Prefill the limits from the server defaults once (the client never hardcodes them for real).
    effect(() => {
      if (this.defaultsApplied || !this.overview.hasValue()) {
        return;
      }
      const { totalLimit, newLimit } = this.overview.value().defaults;
      this.defaultsApplied = true;
      if (this.form.controls.totalLimit.pristine && this.form.controls.newLimit.pristine) {
        this.form.patchValue({ totalLimit, newLimit });
      }
    });
  }

  ngOnInit(): void {
    // Detect an unfinished session so the learner can resume it instead of starting over.
    void this.store.resume();
  }

  protected retry(): void {
    this.tree.reload();
    this.overview.reload();
  }

  protected resume(): void {
    void this.router.navigateByUrl(SESSION_ROUTE);
  }

  protected async start(): Promise<void> {
    const query = this.scopeQuery();
    if (!query || this.form.invalid || !this.canStart()) {
      this.form.markAllAsTouched();
      return;
    }
    const { totalLimit, newLimit } = this.form.getRawValue();
    this.starting.set(true);
    try {
      await this.store.start({ ...query, totalLimit, newLimit });
    } finally {
      this.starting.set(false);
    }
  }
}
