import { httpResource } from '@angular/common/http';
import { Injectable, computed } from '@angular/core';
import {
  ConceptReadiness,
  READINESS_URL,
  Readiness,
  ReadinessSummary,
} from '../api/readiness.models';

/**
 * The learner's readiness for the Atlas (FR-008, FR-009). Provided by `AtlasLayout`, so every Atlas
 * visit fetches it fresh (it changes after each review session) and it is dropped on leaving. A
 * failed load simply shows no readiness; browsing does not depend on it.
 */
@Injectable()
export class ReadinessStore {
  readonly resource = httpResource<Readiness>(() => READINESS_URL);

  private readonly index = computed(() => {
    const domains = new Map<string, ReadinessSummary>();
    const topics = new Map<string, ReadinessSummary>();
    const concepts = new Map<string, ConceptReadiness>();
    const value = this.resource.hasValue() ? this.resource.value() : undefined;
    for (const d of value?.domains ?? []) {
      domains.set(d.domain, d);
      for (const t of d.topics) {
        topics.set(`${d.domain}/${t.topic}`, t);
        for (const c of t.concepts) {
          concepts.set(`${d.domain}/${t.topic}/${c.concept}`, c);
        }
      }
    }
    return { domains, topics, concepts };
  });

  /** `undefined` until loaded (or for an unknown slug). Reactive when read in a template/computed. */
  domain(domain: string): ReadinessSummary | undefined {
    return this.index().domains.get(domain);
  }

  topic(domain: string, topic: string): ReadinessSummary | undefined {
    return this.index().topics.get(`${domain}/${topic}`);
  }

  /** `undefined` also for a concept without schedulable questions (it has no state). */
  concept(domain: string, topic: string, concept: string): ConceptReadiness | undefined {
    return this.index().concepts.get(`${domain}/${topic}/${concept}`);
  }
}
