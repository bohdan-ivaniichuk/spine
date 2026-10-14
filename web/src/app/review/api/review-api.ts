import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { firstValueFrom } from 'rxjs';
import { I18n, Lang, withLang } from '../../shared/i18n/i18n';
import {
  AnswerFeedback,
  ReviewOverview,
  ScopeQuery,
  Session,
  SessionSummary,
  StartSessionRequest,
} from './review.models';

const BASE = '/api/review';

/** `GET /api/review/overview` URL for a scope — a plain string so `httpResource` re-requests only on real changes. */
export function overviewUrl(query: ScopeQuery, lang: Lang = 'en'): string {
  let params = new HttpParams().set('scope', query.scope);
  if (query.domain) {
    params = params.set('domain', query.domain);
  }
  if (query.topic) {
    params = params.set('topic', query.topic);
  }
  return withLang(`${BASE}/overview?${params.toString()}`, lang);
}

/** Best human-readable message from a ProblemDetails error (or anything else that was thrown). */
export function errorMessage(error: unknown): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as { title?: unknown; detail?: unknown } | null;
    if (problem && typeof problem === 'object') {
      const text = [problem.title, problem.detail].filter((x) => typeof x === 'string');
      if (text.length > 0) {
        return text.join(' — ');
      }
    }
    return error.status === 0 ? 'API unreachable' : `Request failed (${error.status})`;
  }
  return error instanceof Error ? error.message : 'Something went wrong';
}

/** `GET /api/review/coverage` URL (per-topic eligible / seen / due counts). */
export function coverageUrl(lang: Lang = 'en'): string {
  return withLang(`${BASE}/coverage`, lang);
}

/**
 * Thin promise-based client for the six review endpoints. The `SessionStore` owns the state;
 * reads that are a pure function of signals (the overview) use `httpResource` in the component instead.
 */
@Injectable({ providedIn: 'root' })
export class ReviewApi {
  private readonly http = inject(HttpClient);
  private readonly i18n = inject(I18n);

  /** Content follows the UI language; English URLs stay unchanged (the server default). */
  private url(path: string): string {
    return withLang(`${BASE}${path}`, this.i18n.lang());
  }

  overview(query: ScopeQuery): Promise<ReviewOverview> {
    return firstValueFrom(this.http.get<ReviewOverview>(overviewUrl(query, this.i18n.lang())));
  }

  start(request: StartSessionRequest): Promise<Session> {
    return firstValueFrom(this.http.post<Session>(this.url('/sessions'), request));
  }

  /** The open session, or `null` when the API answers 204. */
  async current(): Promise<Session | null> {
    const response = await firstValueFrom(
      this.http.get<Session>(this.url('/sessions/current'), { observe: 'response' }),
    );
    return response.status === 204 ? null : response.body;
  }

  get(id: string): Promise<Session> {
    return firstValueFrom(this.http.get<Session>(this.url(`/sessions/${id}`)));
  }

  answer(id: string, questionId: string, selectedIndex: number): Promise<AnswerFeedback> {
    return firstValueFrom(
      this.http.post<AnswerFeedback>(this.url(`/sessions/${id}/answers`), {
        questionId,
        selectedIndex,
      }),
    );
  }

  finish(id: string): Promise<SessionSummary> {
    return firstValueFrom(this.http.post<SessionSummary>(this.url(`/sessions/${id}/finish`), {}));
  }
}
