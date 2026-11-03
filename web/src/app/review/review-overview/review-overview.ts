import { httpResource } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  effect,
  inject,
  linkedSignal,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { distinctUntilChanged, map } from 'rxjs';
import { KnowledgeTreeSource } from '../../knowledge/api/knowledge-tree.source';
import { liveConcepts } from '../../knowledge/api/knowledge-tree.models';
import { EnBadge } from '../../shared/badges/en-badge';
import { I18n } from '../../shared/i18n/i18n';
import { ActivityCard } from '../activity-card/activity-card';
import { coverageUrl, overviewUrl } from '../api/review-api';
import {
  Coverage,
  ReviewMode,
  ReviewOverview as ReviewOverviewDto,
  ReviewScopeType,
  ScopeQuery,
  TopicCoverage,
} from '../api/review.models';
import { SESSION_ROUTE, SessionStore } from '../session-store';

const MIN_LIMIT = 1;
const MAX_LIMIT = 100;

const SCOPES: readonly ReviewScopeType[] = ['all', 'domain', 'topic', 'concept'];

/** One atlas tile: a domain with its topics and their coverage. */
export interface AtlasTopic {
  slug: string;
  title: string;
  /** English title served while Ukrainian is selected (EN badge). */
  titleFallback: boolean;
  coverage: TopicCoverage | null;
  /** Seen share 0–100 for the bar. */
  percent: number;
}

export interface AtlasDomain {
  slug: string;
  title: string;
  /** English title served while Ukrainian is selected (EN badge). */
  titleFallback: boolean;
  position: number;
  topics: AtlasTopic[];
  hasContent: boolean;
}

/**
 * The review home: a hero, the ink "Today" card (scope + size picker with live due/new counts) and
 * the atlas of domains with per-topic coverage. Scope can be preset by `?scope=&domain=&topic=`
 * (the spine rail's domain tabs) or by clicking a domain/topic in the atlas; `?mode=weak` selects the
 * weak-spots drill (questions last answered wrong) instead of due and new questions.
 */
@Component({
  selector: 'app-review-overview',
  imports: [ReactiveFormsModule, EnBadge, ActivityCard],
  templateUrl: './review-overview.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReviewOverview implements OnInit {
  protected readonly store = inject(SessionStore);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly fb = inject(NonNullableFormBuilder);
  protected readonly i18n = inject(I18n);
  private readonly treeSource = inject(KnowledgeTreeSource);

  protected readonly form = this.fb.group({
    mode: this.fb.control<ReviewMode>('regular'),
    scopeType: this.fb.control<ReviewScopeType>('all'),
    domain: '',
    topic: '',
    concept: '',
    totalLimit: [20, [Validators.required, Validators.min(MIN_LIMIT), Validators.max(MAX_LIMIT)]],
    newLimit: [10, [Validators.required, Validators.min(MIN_LIMIT), Validators.max(MAX_LIMIT)]],
  });

  protected readonly modes: readonly ReviewMode[] = ['regular', 'weak'];
  protected readonly minLimit = MIN_LIMIT;
  protected readonly maxLimit = MAX_LIMIT;

  private readonly formValue = toSignal(
    this.form.valueChanges.pipe(map(() => this.form.getRawValue())),
    { initialValue: this.form.getRawValue() },
  );

  protected readonly weak = computed(() => this.formValue().mode === 'weak');

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
      case 'concept':
        return value.domain && value.topic && value.concept
          ? { scope: 'concept', domain: value.domain, topic: value.topic, concept: value.concept }
          : undefined;
    }
  });

  private readonly overviewRequest = computed(() => {
    const query = this.scopeQuery();
    return query ? overviewUrl(query, this.i18n.lang()) : undefined;
  });

  protected readonly tree = this.treeSource.tree;
  protected readonly overview = httpResource<ReviewOverviewDto>(() => this.overviewRequest());
  protected readonly coverage = httpResource<Coverage>(() => coverageUrl(this.i18n.lang()));
  /** Last loaded coverage, kept while a language switch re-fetches (no atlas flicker). */
  private readonly coverageValue = linkedSignal<Coverage | undefined, Coverage | undefined>({
    source: () => (this.coverage.hasValue() ? this.coverage.value() : undefined),
    computation: (value, previous) => value ?? previous?.value,
  });

  protected readonly domains = this.treeSource.domains;

  protected readonly today = computed(() => this.i18n.formatDay(new Date()));

  /** Domains with topics and coverage, in manifest order. */
  protected readonly atlas = computed<AtlasDomain[]>(() => {
    const byKey = new Map<string, TopicCoverage>();
    const coverage = this.coverageValue();
    if (coverage) {
      for (const c of coverage.topics) {
        byKey.set(`${c.domain}/${c.topic}`, c);
      }
    }
    return this.domains().map((d) => {
      const topics = d.topics
        .filter((t) => !t.deprecatedAt)
        .map((t) => {
          const coverage = byKey.get(`${d.slug}/${t.slug}`) ?? null;
          const percent =
            coverage && coverage.eligible > 0
              ? Math.round((coverage.seen / coverage.eligible) * 100)
              : 0;
          return {
            slug: t.slug,
            title: t.title,
            titleFallback: t.titleFallback,
            coverage,
            percent,
          };
        });
      return {
        slug: d.slug,
        title: d.title,
        titleFallback: d.titleFallback,
        position: d.position,
        topics,
        hasContent: topics.some((t) => (t.coverage?.eligible ?? 0) > 0),
      };
    });
  });

  protected readonly atlasCounts = computed(() => {
    const atlas = this.atlas();
    return {
      domains: atlas.length,
      topics: atlas.reduce((n, d) => n + d.topics.length, 0),
      withContent: atlas.reduce(
        (n, d) => n + d.topics.filter((t) => (t.coverage?.eligible ?? 0) > 0).length,
        0,
      ),
    };
  });

  protected readonly topics = computed(() => {
    const domain = this.domains().find((d) => d.slug === this.formValue().domain);
    return domain?.topics.filter((t) => !t.deprecatedAt) ?? [];
  });

  /** Live concepts of the chosen topic, in sort order (the concept scope's picker). */
  protected readonly concepts = computed(() => {
    const value = this.formValue();
    const topic = this.topics().find((t) => t.slug === value.topic);
    return topic ? liveConcepts(topic) : [];
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

  /** Weak mode: nothing in scope has a wrong last answer. */
  protected readonly nothingToDrill = computed(
    () => this.overview.hasValue() && this.overview.value().weakCount === 0,
  );

  /** The last refused Start of this page (e.g. 409 "Nothing to drill"); cleared by the next one. */
  /**
   * The last refused Start of this page with whether it was weak mode's 409. Reset whenever the
   * scope or the mode changes: the refusal was about the previous choice.
   */
  private readonly refusal = linkedSignal<
    string,
    { message: string; nothingToDrill: boolean } | null
  >({
    source: () => `${this.formValue().mode}|${JSON.stringify(this.scopeQuery() ?? null)}`,
    computation: () => null,
  });

  /**
   * The alert under the form. A "Nothing to drill" refusal is said once: inline with the refreshed
   * count (no alert) unless the refreshed count still shows weak questions.
   */
  protected readonly alert = computed<string | null>(() => {
    const refusal = this.refusal();
    if (refusal) {
      const inline = this.overview.isLoading() || this.nothingToDrill();
      return refusal.nothingToDrill && inline ? null : refusal.message;
    }
    // Errors of the store's own (resume) when no session is open; start refusals are handled above.
    const error = this.store.error();
    return error && !this.store.session() && !this.store.startError() ? error : null;
  });

  protected readonly canStart = computed(
    () =>
      this.overview.hasValue() &&
      !(this.weak() ? this.nothingToDrill() : this.nothingToReview()) &&
      !this.starting() &&
      this.scopeQuery() !== undefined,
  );

  private defaultsApplied = false;

  constructor() {
    const destroyRef = inject(DestroyRef);

    // The URL is the scope's source of truth: `?scope=domain&domain=dotnet` (spine tabs, reload,
    // back/forward) presets the picker, and every picker/atlas change is written back, so a spine
    // tab always differs from the current URL and always works.
    this.route.queryParamMap.pipe(takeUntilDestroyed(destroyRef)).subscribe((params) => {
      const raw = params.get('scope');
      const scope: ReviewScopeType = SCOPES.includes(raw as ReviewScopeType)
        ? (raw as ReviewScopeType)
        : 'all';
      const mode: ReviewMode = params.get('mode') === 'weak' ? 'weak' : 'regular';
      const domain = params.get('domain') ?? '';
      const topic = params.get('topic') ?? '';
      const concept = params.get('concept') ?? '';
      const value = this.form.getRawValue();
      if (
        value.scopeType !== scope ||
        value.domain !== domain ||
        value.topic !== topic ||
        value.concept !== concept
      ) {
        this.applyScope(scope, domain, topic, concept);
      }
      // After the scope, so the URL written back never carries the new mode with a stale scope.
      if (this.form.controls.mode.value !== mode) {
        this.form.controls.mode.setValue(mode);
      }
    });
    this.form.valueChanges
      .pipe(
        map(() => scopeParams(this.form.getRawValue())),
        distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
        takeUntilDestroyed(destroyRef),
      )
      .subscribe((queryParams) => {
        void this.router.navigate([], { relativeTo: this.route, queryParams, replaceUrl: true });
      });

    this.form.controls.domain.valueChanges
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe(() => this.form.controls.topic.setValue(''));
    this.form.controls.topic.valueChanges
      .pipe(takeUntilDestroyed(destroyRef))
      .subscribe(() => this.form.controls.concept.setValue(''));

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
    // Weak sessions ignore the new-question limit: hide it and keep it out of the validity.
    effect(() => {
      const newLimit = this.form.controls.newLimit;
      if (this.weak()) {
        newLimit.disable({ emitEvent: false });
      } else {
        newLimit.enable({ emitEvent: false });
      }
    });
    effect(() => {
      const concept = this.form.controls.concept;
      if (this.concepts().length > 0) {
        concept.enable({ emitEvent: false });
      } else {
        concept.disable({ emitEvent: false });
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
    this.coverage.reload();
  }

  /** Atlas click: review that domain or topic (the Today card picks it up). */
  protected pickDomain(domain: string): void {
    this.applyScope('domain', domain, '');
  }

  protected pickTopic(domain: string, topic: string): void {
    this.applyScope('topic', domain, topic);
  }

  protected pad(position: number): string {
    return String(position).padStart(2, '0');
  }

  private applyScope(scope: ReviewScopeType, domain: string, topic: string, concept = ''): void {
    // domain first (its valueChanges clears topic), then topic (clears concept), then concept.
    this.form.controls.scopeType.setValue(scope);
    this.form.controls.domain.setValue(scope === 'all' ? '' : domain);
    if (scope === 'topic' || scope === 'concept') {
      this.form.controls.topic.enable({ emitEvent: false });
      this.form.controls.topic.setValue(topic);
    }
    if (scope === 'concept') {
      this.form.controls.concept.enable({ emitEvent: false });
      this.form.controls.concept.setValue(concept);
    }
  }

  protected setMode(mode: ReviewMode): void {
    this.form.controls.mode.setValue(mode);
  }

  protected resume(): void {
    // A refused start must not follow the learner into the resumed session.
    this.store.clearError();
    void this.router.navigateByUrl(SESSION_ROUTE);
  }

  protected async start(): Promise<void> {
    const query = this.scopeQuery();
    if (!query || this.form.invalid || !this.canStart()) {
      this.form.markAllAsTouched();
      return;
    }
    const { totalLimit, newLimit } = this.form.getRawValue();
    const weak = this.weak();
    this.starting.set(true);
    this.refusal.set(null);
    try {
      const started = await this.store.start(
        weak ? { ...query, mode: 'weak', totalLimit } : { ...query, totalLimit, newLimit },
      );
      if (started === false) {
        const message = this.store.startError();
        this.refusal.set(
          message
            ? { message, nothingToDrill: weak && message === this.i18n.t().overview.nothingToDrill }
            : null,
        );
        if (weak) {
          // The weak set changed since the counts were read (e.g. answered elsewhere): refresh them.
          this.overview.reload();
        }
      }
    } finally {
      this.starting.set(false);
    }
  }
}

/** Query params for a picker value; incomplete parts are left out, `mode` only when weak. */
function scopeParams(value: {
  mode: ReviewMode;
  scopeType: ReviewScopeType;
  domain: string;
  topic: string;
  concept: string;
}): Record<string, string> {
  const mode: Record<string, string> = value.mode === 'weak' ? { mode: 'weak' } : {};
  if (value.scopeType === 'all') {
    return mode;
  }
  const params: Record<string, string> = { scope: value.scopeType };
  if (value.domain) {
    params['domain'] = value.domain;
  }
  if ((value.scopeType === 'topic' || value.scopeType === 'concept') && value.topic) {
    params['topic'] = value.topic;
  }
  if (value.scopeType === 'concept' && value.concept) {
    params['concept'] = value.concept;
  }
  return { ...params, ...mode };
}
